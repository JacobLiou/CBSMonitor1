using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using CanProtocol.ProtocolModel;
using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using SofarHVMExe.Util.TI;
using SofarHVMExe.Utilities;

namespace SofarHVMExe.ViewModel
{
    public class FirmwareUpdate : ObservableObject
    {

        public FirmwareUpdate(EcanHelper? ecanHelper)
        {
            this._ecanHelper = ecanHelper;
            this._ecanHelper.RegisterRecvProcessCan1(OnReceiveFrame);
        }

        private EcanHelper? _ecanHelper;

        #region CAN操作

        public ConcurrentQueue<CAN_OBJ> RxQueue = new();
        public bool IsWorking { get; set; } = true;
        private void OnReceiveFrame(CAN_OBJ rawCanObj)
        {
            if (!IsWorking)
            {
                return;
            }

            var canID = new CanFrameID(rawCanObj.ID);
            rawCanObj.Data = rawCanObj.Data.Take(rawCanObj.DataLen).ToArray();
            // 功能码50~54为固件升级
            if (canID.FC is >= 50 and <= 54)
            {
                RxQueue.Enqueue(rawCanObj);
            }
            else
            {
                return;
            }

            // MultiLanguages.Common.LogHelper.WriteLog($"接收: " + $"0x{rawCanObj.ID:X8}: " + $"{BytesToString(rawCanObj.Data)}");
        }
        

        public static bool SendFrameCan1(EcanHelper ecanHelper, uint CanID, IList<byte> txData)
        {
            MultiLanguages.Common.LogHelper.WriteLog("发送：" + $"0x{CanID:X8}: " + BytesToString(txData));
            //LogHelper.AddLog("发送：" + requestCanID.ID.ToString("X8") + ": " + OscilloscopePageVM.BytesToString(txData));
            if (!ecanHelper.SendCan1(CanID, txData.ToArray()))
            {
                MultiLanguages.Common.LogHelper.WriteLog("发送失败：" + $"0x{CanID:X8}: " + BytesToString(txData));
                return false;
            }

            return true;
        }

        public static bool SendDataFramesCan1(EcanHelper ecanHelper, uint CANID, IList<byte> dataBytes, ushort crc16)
        {
            
            // CRC
            dataBytes.Add((byte)(crc16 & 0xFF));
            dataBytes.Add((byte)(crc16 >> 8));

            var frameBytes = new List<byte>();
            int remains = dataBytes.Count;
            byte packSn = 0;
            
            // 起始、中间帧
            int p = 0;
            while (remains > 7)
            {
                frameBytes.Clear();
                frameBytes.Add(packSn++);
                frameBytes.AddRange(dataBytes.ToList().GetRange(p, 7));

                if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
                {
                    return false;
                }

                p += 7;
                remains -= 7;
            }

            // 帧尾
            frameBytes.Clear();
            frameBytes.Add(0xFF);
            frameBytes.AddRange(dataBytes.ToList().GetRange(p, remains));
            while (frameBytes.Count < 8)
            {
                frameBytes.Add(0);
            }

            if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
            {
                return false;
            }

            //MultiLanguages.Common.LogHelper.WriteLog("连续数据帧发送结束\r\n");
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

        #region 固件升级参数


        // 广播升级
        private bool _isBroadcast = true;
        public bool IsBroadcast
        {
            get => _isBroadcast;
            set { _isBroadcast = value; OnPropertyChanged(); }
        }

        // 发送时跳过全为0xFF的数据块
        private List<bool> skipBlockFlags;

        private bool _skipFfBlock = true;
        public bool SkipFFBlock
        {
            get => _skipFfBlock;
            set { _skipFfBlock = value; OnPropertyChanged(); }
        }

        // 固件文件路径
        private string _binFilePath = "";
        public string BinFilePath
        {
            get => _binFilePath;
            set { _binFilePath = value; OnPropertyChanged(); }
        }

        // 固件数据
        private byte[] firmwareData;
        private const int KeepSignatureBytes = 1024 * 1 + 64; // 固件数据最后的签名数据必须保留

        // 数据块大小（byte），不能超过1024B
        private int _blockSize = 256; 
        public int BlockSize
        {
            get => _blockSize;
            set { _blockSize = value; OnPropertyChanged(); }
        }

        // 数据块数量
        private int? _blockNum;
        public int? BlockNum
        {
            get => _blockNum;
            set { _blockNum = value; OnPropertyChanged(); }
        }


        #endregion

        #region 升级操作
        private bool _isUpdating = true;
        public bool IsUpdating
        {
            get => _isUpdating;
            set { _isUpdating = value; OnPropertyChanged(); }
        }

        private void ImportFirmwareFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择升级文件";
            dlg.Filter = "固件文件|*.bin;*.out";
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != true)
                return;


            var saveDir = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "FirmwareUpdate"));

            // 清除过期文件
            foreach (var fileInfo in saveDir.GetFiles())
            {
                var lastWriteTime = fileInfo.LastWriteTime;
                var currentTime = DateTime.Now;
                if (currentTime - lastWriteTime > TimeSpan.FromHours(12))
                {
                    fileInfo.Delete();
                }
            }

            // .out转.bin
            if (System.IO.Path.GetExtension(dlg.FileName) == ".out")
            {
                // 复制文件
                string objFileName = System.IO.Path.GetFileName(dlg.FileName);
                string copyFilePath = System.IO.Path.Combine(saveDir.FullName, objFileName);
                System.IO.File.Copy(dlg.FileName, copyFilePath);

                string hexSavePath = System.IO.Path.Combine(saveDir.FullName, System.IO.Path.GetFileNameWithoutExtension(copyFilePath) + ".hex");
                string binSavePath = System.IO.Path.Combine(saveDir.FullName, System.IO.Path.GetFileNameWithoutExtension(copyFilePath) + ".bin");

                BinFilePath = "加载中...";

                try
                {

                    TIPluginHelper.ConvertCoffToHexAsync(copyFilePath, hexSavePath, saveDir.FullName).Wait();
                    TIPluginHelper.ConvertHexToBinAsync(hexSavePath, binSavePath, saveDir.FullName).Wait();
                    BinFilePath = dlg.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    BinFilePath = "";
                    return;
                }

            }
            else
            {
                BinFilePath = dlg.FileName;
            }

            MessageBox.Show($"文件路径：{BinFilePath}", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);

            // 读取文件
            FileStream file = new FileStream(BinFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            {
                var fileLength = (int)file.Length;
                firmwareData = new byte[fileLength];

                file.Read(firmwareData, 0, fileLength);
                file.Close();
                
                BlockNum = (fileLength % BlockSize == 0) ? (fileLength / BlockSize) : (fileLength / BlockSize + 1);
            }

            // 跳过发送标志列表
            skipBlockFlags = new List<bool>();
            int keepLastPacks = KeepSignatureBytes / BlockSize + 1;
            for (int i = 0; i < BlockNum; i++)
            {
                skipBlockFlags.Add(false);
                if (SkipFFBlock && i < BlockNum - keepLastPacks)
                {
                    for (int j = 0; j < BlockSize; j++)
                    {
                        if (firmwareData[i * BlockSize + j] != 0xFF)
                        {
                            skipBlockFlags[i] = true;
                            break;
                        }
                    }
                }
            }

            var validBlockNum = skipBlockFlags.Count(flag => flag == false);

        }
        
        private bool RequestUpdate(byte dstAddr, int validBlockNum)
        {
            var txCanID = new CanFrameID()
            {
                // 请求帧，功能码50
                Priority = 3,
                FrameType = 2,
                ContinuousFlag = 0,
                PF = 50,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            var rxCanID = new CanFrameID()
            {
                // 应答帧，功能码50
                Priority = 3,
                FrameType = 3,
                ContinuousFlag = 0,
                PF = 50,
                DstType = 2,
                DstAddr = 1,
                SrcType = 1,
                SrcAddr = dstAddr,
            };
            
            var txData = new List<byte>()
            {
                (byte)((BlockNum ?? 0) & 0xFF), (byte)((BlockNum ?? 0) >> 8),
                // (byte)(BlockSize & 0xFF), (byte)(BlockSize >> 8),
                (byte)(validBlockNum & 0xFF), (byte)(validBlockNum >> 8),
                0, 0, 
                0, 0
            };

            RxQueue.Clear();
            if (!SendFrameCan1(_ecanHelper, txCanID.ID, txData))
            {
                return false;
            }

            // TODO: log

            var sw = Stopwatch.StartNew();
            do
            {
                if (RxQueue.TryDequeue(out var rawFrame) && new CanFrameID(rawFrame.ID).FC != txCanID.ID)
                {
                    // TODO: log
                    var resultCode = rawFrame.Data[0];
                    if (resultCode != 0)
                    {
                        // TODO:log
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

            } while (sw.ElapsedMilliseconds < 2000);


            return false;
        }

        private bool SendDataBlock(byte dstAddr)
        {
            var txCanID = new CanFrameID()
            {
                // 数据帧，功能码52
                Priority = 6,
                FrameType = 1,
                ContinuousFlag = 1,
                PF = 50,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            for (int i = 0; i < BlockNum; i++)
            {
                if (!IsUpdating) return false;
                if (skipBlockFlags[i]) continue;

                var blockData = (i == BlockNum - 1) ?
                                        firmwareData[(i * BlockSize)..^0] : firmwareData[(i * BlockSize)..((i + 1) * BlockSize)];
                var dataHead = new List<byte>()
                {

                };

                //SendDataFramesCan1(this._ecanHelper, txCanID.ID, blockData);


            }

            return true;
        }

        private bool RequestReceivingResult(byte dstAddr)
        {
            return true;
        }

        private bool RequestFinalOperation(byte dstAddr, byte funcCode, out byte result, out byte hasFirmware)
        {
            result = 0;
            hasFirmware = 1;
            return true;
        }

        #endregion

    }
}
