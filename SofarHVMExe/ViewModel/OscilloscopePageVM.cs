using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CanProtocol.ProtocolModel;
using Communication.Can;
using SofarHVMExe.Utilities;
using SofarHVMExe.Model;

namespace SofarHVMExe.ViewModel
{
    public class OscilloscopePageVM {

        public OscilloscopePageVM(EcanHelper? ecanHelper)
        {
            this._ecanHelper = ecanHelper;
            OscilloscopeVm = new OscilloscopeVm(ecanHelper);
            FaultWaveRecordVm = new FaultWaveRecordVm(ecanHelper);

            this._ecanHelper.RegisterRecvProcessCan1(OnReceiveFrame);
        }

        
        private EcanHelper? _ecanHelper = null;
        private FileConfigModel? fileCfgModel = null;
        
        public OscilloscopeVm OscilloscopeVm { set; get; } 

        public FaultWaveRecordVm FaultWaveRecordVm { set; get; }

        public void UpdateModel()
        {
            OscilloscopeVm.UpdateModel();  
        }

        public void OnPageLoadedJobs()
        {
            IsWorking = true;
        }

        public void OnPagesUnloadedJobs()
        {
            // 
            OscilloscopeVm.ClosedJobs();
            FaultWaveRecordVm.ClosedJobs();
            IsWorking = false;
        }
        

        #region CAN操作（自己造的轮子）
        public bool IsWorking { get; set; }

        /// <summary>
        /// 接收事件响应，帧筛选
        /// </summary>
        private void OnReceiveFrame(CAN_OBJ rawCanObj)
        {
            if (!IsWorking)
            {
                return;
            }

            var canID = new CanFrameID(rawCanObj.ID);
            rawCanObj.Data = rawCanObj.Data.Take(rawCanObj.DataLen).ToArray();
            // 功能码30~50为示波器保留
            if (canID.FC is >= 30 and <= 35)
            {
                OscilloscopeVm.RxQueue.Enqueue(rawCanObj);

            }
            else if (canID.FC == 41)
            {
                FaultWaveRecordVm.RxQueue.Enqueue(rawCanObj);
            }
            else
            {
                return;
            }

            MultiLanguages.Common.LogHelper.WriteLog($"接收: " + $"0x{rawCanObj.ID:X8}: " + $"{BytesToString(rawCanObj.Data)}");
        }
        

        /// <summary>
        /// 将连续数据帧去除包序号并拼接在一起
        /// </summary>
        /// <param name="rxQueue"></param>
        /// <param name="rxCANID"></param>
        /// <param name="dataBytes"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public static bool ReadDataFrames(ConcurrentQueue<CAN_OBJ> rxQueue,CanFrameID rxCANID, 
                                            out List<byte> dataBytes, int timeoutMs)
        {
            dataBytes = new List<byte>();
            int nextPackSn = 0;

            bool isAllReceived = false;
            var sw = Stopwatch.StartNew();

            MultiLanguages.Common.LogHelper.WriteLog($"连续数据帧接收开始【{sw.ElapsedMilliseconds}ms】");

            do
            {
                if (rxQueue.TryDequeue(out var rawCanFrame))
                {
                    var canIDinfo = new CanFrameID(rawCanFrame.ID);
                    var data = rawCanFrame.Data;

                    if (canIDinfo.FC != rxCANID.FC)
                    {
                        // CANID只核对功能码
                        break;
                    }

                    //MultiLanguages.Common.LogHelper.WriteLog("获取帧：" + canIDinfo.ID.ToString("X8") + ": " + BytesToString(data));

                    if (canIDinfo.ContinuousFlag == 0)
                    {
                        // 单数据帧
                        dataBytes.AddRange(data);
                        isAllReceived = true;
                        break;
                    }

                    byte packSn = data[0];
                    if (packSn == nextPackSn)
                    {
                        // 数据帧头、中间帧
                        dataBytes.AddRange(data[1..^0]);
                        nextPackSn++;
                    }
                    else if (packSn == 0xFF && nextPackSn > 0)
                    {
                        // 数据帧尾
                        dataBytes.AddRange(data[1..^0]);
                        isAllReceived = true;
                        break;
                    }
                    else
                    {
                        // 非数据帧、缺帧
                        MultiLanguages.Common.LogHelper.WriteLog("错误数据帧");
                        return false;
                    }

                }

            } while (sw.ElapsedMilliseconds < timeoutMs);

            sw.Stop();
            
            if (isAllReceived)
            {
                MultiLanguages.Common.LogHelper.WriteLog($"连续数据帧接收完成，接收时长【{sw.ElapsedMilliseconds}ms】");
                return true;
            }

            MultiLanguages.Common.LogHelper.WriteLog($"连续数据帧接收不完整，接收时长【{sw.ElapsedMilliseconds}ms】");
            return false;
        }

        /// <summary>
        /// 点表连续数据帧
        /// </summary>
        /// <param name="rxQueue"></param>
        /// <param name="rxCANID"></param>
        /// <param name="address"></param>
        /// <param name="number"></param>
        /// <param name="dataBytes"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="CheckCRC"></param>
        /// <returns></returns>
        public static bool ReadAddressDataFrames(ConcurrentQueue<CAN_OBJ> rxQueue, CanFrameID rxCANID, int address, int number, 
                                                out List<byte> dataBytes, int timeoutMs = 500, bool CheckCRC = false)
        {
            dataBytes = new List<byte>();

            if (!ReadDataFrames(rxQueue, rxCANID, out var rawDataBytes, timeoutMs))
            {
                return false;
            }

            int addrRx = rawDataBytes[1] << 8 | rawDataBytes[0];
            int byteNumRx = rawDataBytes[2];    // 字长16bits
            int crc = rawDataBytes.Count > 8 ?  rawDataBytes[2 + byteNumRx * 2 + 2] << 8 | rawDataBytes[2 + byteNumRx * 2 + 1] : 0;

            if (addrRx != address || byteNumRx != number)
            {
                return false;
            }
            ////MultiLanguages.Common.LogHelper.WriteLog("完整数据包：" + FrameDataToString(rawDataBytes));

            dataBytes = rawDataBytes.GetRange(3, byteNumRx * 2);
            if (CheckCRC)
            {
                if (CRCHelper.ComputeCrc16(dataBytes.ToArray(), dataBytes.Count) != (ushort)crc)
                {
                    return false;
                }
            }

            // //MultiLanguages.Common.LogHelper.WriteLog("数据包：" + BytesToString(dataBytes));

            return true;
        }

        /// <summary>
        /// 文件请求连续数据帧
        /// </summary>
        /// <param name="rxQueue"></param>
        /// <param name="rxCANID"></param>
        /// <param name="offset"></param>
        /// <param name="dataLen"></param>
        /// <param name="dataBytes"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="CheckCRC"></param>
        /// <returns></returns>
        public static bool ReadFileDataFrames(ConcurrentQueue<CAN_OBJ> rxQueue, CanFrameID rxCANID, int offset, int dataLen,
                                        out List<byte> dataBytes, int timeoutMs = 500, bool CheckCRC = false)
        {
            dataBytes = new List<byte>();
            if (!ReadDataFrames(rxQueue, rxCANID, out var rawDataBytes, timeoutMs))
            {
                return false;
            }

            int offsetRx = rawDataBytes[1] << 8 | rawDataBytes[0];
            int dataLenRx = rawDataBytes[3] << 8 | rawDataBytes[2]; // 字长8bits
            int crc = rawDataBytes[3 + dataLenRx + 2] << 8 | rawDataBytes[3 + dataLenRx + 1];

            if (offsetRx != offset || dataLenRx != dataLen)
            {
                return false;
            }

            // MultiLanguages.Common.LogHelper.WriteLog("完整数据包：" + FrameDataToString(rawDataBytes));

            dataBytes = rawDataBytes.GetRange(4, dataLenRx);
            if (CheckCRC)
            {
                if (CRCHelper.ComputeCrc16(dataBytes.ToArray(), dataBytes.Count) != (ushort)crc)
                {
                    return false;
                }
            }

            // MultiLanguages.Common.LogHelper.WriteLog("数据包：" + BytesToString(dataBytes));

            return true;
        }


        public static bool SendFrameCan1(EcanHelper ecanHelper, uint CanID, IList<byte> txData)
        {
            MultiLanguages.Common.LogHelper.WriteLog("发送："+ $"0x{CanID:X8}: " + BytesToString(txData));
            //LogHelper.AddLog("发送：" + requestCanID.ID.ToString("X8") + ": " + OscilloscopePageVM.BytesToString(txData));
            if (!ecanHelper.SendCan1(CanID, txData.ToArray()))
            {
                MultiLanguages.Common.LogHelper.WriteLog("发送失败：" + $"0x{CanID:X8}: " + BytesToString(txData));
                return false;
            }

            return true;
        }

        public static bool SendAddressDataFramesCan1(EcanHelper ecanHelper, uint CANID, List<byte> dataBytes)
        {
            // 数据量
            if (dataBytes.Count % 2 == 1)
            {
                dataBytes.Add(0);  // 字长为16bit，不足时补一位
            }
            byte byteNum = (byte)(dataBytes.Count / 2);

            // CRC
            ushort crc = CRCHelper.ComputeCrc16(dataBytes.ToArray(), dataBytes.Count);
            dataBytes.Add((byte)(crc & 0xFF));
            dataBytes.Add((byte)(crc >> 8));

            var frameBytes = new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 };
            int remains = dataBytes.Count;
            byte packSn = 0;

            // 帧头
            frameBytes[0] = packSn++;
            frameBytes[1] = 0;
            frameBytes[2] = 0;
            frameBytes[3] = byteNum;
            frameBytes[4] = dataBytes[0];
            frameBytes[5] = dataBytes[1];
            frameBytes[6] = dataBytes[2];
            frameBytes[7] = dataBytes[3];

            MultiLanguages.Common.LogHelper.WriteLog("连续数据帧发送开始：");

            if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
            {
                return false;
            }

            remains -= 4;

            // 中间帧
            int p = 4;
            while (remains > 7)
            {
                frameBytes.Clear();
                frameBytes.Add(packSn++);
                frameBytes.AddRange(dataBytes.GetRange(p, 7));

                if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
                {
                    return false;
                }

                p += 7;
                remains -= 7;
            }

            // 帧尾
            frameBytes.Clear();
            frameBytes.Add(0XFF);
            frameBytes.AddRange(dataBytes.GetRange(p, remains));
            while (frameBytes.Count < 8)
            {
                frameBytes.Add(0);
            }
            
            if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
            {
                return false;
            }

            MultiLanguages.Common.LogHelper.WriteLog("连续数据帧发送结束\r\n");
            return true;
        }

        public static string BytesToString(IList<byte> byteData)
        {
            string dataStr = "";
            for (int i = 0; i < byteData.Count; i++)
            {
                dataStr += byteData[i].ToString("X2") + " ";
            }
            return dataStr;
        }


        #endregion

    }

    

}
