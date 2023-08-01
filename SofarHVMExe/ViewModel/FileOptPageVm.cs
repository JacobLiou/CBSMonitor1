using Communication.Can;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanProtocol.ProtocolModel;
using System.Windows;
using NPOI.XWPF.UserModel;
using System.Threading;
using CanProtocol;
using SofarHVMExe.Model;
using System.Windows.Input;
using System.Diagnostics;
using NPOI.HPSF;
using System.Windows.Controls;
using NPOI.SS.Formula.Functions;
using SofarHVMExe.SubPubEvent;
using System.Reflection;
using System.Windows.Documents;
using NPOI.POIFS.NIO;
using System.Collections.ObjectModel;
using Org.BouncyCastle.Ocsp;

namespace SofarHVMExe.ViewModel
{
    public class FileOptPageVm : ViewModelBase
    {
        public FileOptPageVm(EcanHelper? helper)
        {
            ecanHelper = helper;
            Init();
        }

        #region 字段
        public EcanHelper? ecanHelper = null;
        private FileConfigModel? fileCfgModel = null;
        private FrameConfigModel? frameCfgModel = null;
        private byte targetAddr = 0x0; //目标设备地址，广播0x0，否则为单台升级
        private bool isPackage = false;//当前包
        private int fileNo = -1;       //当前文件编码
        private int fileTotal = -1;    //当前文件总大小
        private int dataLength = -1;   //当前文件交互长度
        private int dataLengthTemp = 0;//临时使用的非常规交互长度
        private StringBuilder logMsg = new StringBuilder();//文件字符串

        private CRCHelper crcHelper = new CRCHelper();
        //private CanFrameModel recvFrame = new CanFrameModel();
        private CancellationTokenSource cts = new CancellationTokenSource();//取消信号量

        private CanFrameModel tmpModel = new CanFrameModel(); //当前操作的临时数据
        private static List<MultyPackage2> s_packageBufferList = new List<MultyPackage2>();//接收的多包数据缓存集合
        #endregion

        #region 属性
        //当前按钮状态：开始读取/终止读取
        private bool status = false;
        public bool Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }
        //文本框内容：打印交互信息
        private string message = "";
        public string Message
        {
            get => message;
            set
            {
                message = value;
                OnPropertyChanged();
            }
        }
        //文件编号：类型/数据长度
        private Dictionary<string, int> filetypeToLength = new Dictionary<string, int>()
        {
            {"故障录波文件1",8 },
            {"故障录波文件2",8 },
            {"故障录波文件3",8 },
            {"5min特性数据文件",200 },
            {"运行日志文件",256 }
        };
        public Dictionary<string, int> FiletypeToLength { get { return filetypeToLength; } }
        //当前选择执行的文件类型
        private string currentFileType;
        public string CurrentFileType
        {
            get => currentFileType;
            set
            {
                currentFileType = value;
                OnPropertyChanged();
            }
        }
        //字段数据集合
        private ObservableCollection<CanFrameDataInfo> dataInfos = new ObservableCollection<CanFrameDataInfo>();
        public ObservableCollection<CanFrameDataInfo> DataInfos
        {
            get => dataInfos;
            set
            {
                dataInfos = value;
                OnPropertyChanged();
            }

        }
        #endregion

        #region 命令
        public ICommand ReadCommand { get; set; }

        #endregion

        #region 成员方法
        private void Init()
        {
            ReadCommand = new SimpleCommand(ExecuteBtn);
        }

        public void ExecuteBtn(object o)
        {
            //正在读取状态，需终止操作并将变量初始化；return
            if (Status)
            {
                if (cts != null)
                    cts.Cancel();

                fileNo = -1;
                dataLength = -1;
                Status = false;
                return;
            }

            /* 非读取状态，需进行读取操作
             * 1.检查选择信息
             * 2.开启多线程
             */
            //该对象可优化，主要为了获取当前的文件编码
            foreach (var item in FiletypeToLength)
            {
                fileNo++;
                if (item.Key == CurrentFileType)
                {
                    dataLength = item.Value;
                    AddMsg($"当前文件编号:{fileNo}，数据长度:{dataLength}");
                    break;
                }
            }

            if (dataLength != -1)
            {
                Task.Run(() =>
                {
                    cts = new CancellationTokenSource();
                    Status = true;

                    if (!StartReadFile())
                    {
                        AddMsg($"下发读文件指令响应失败！");
                        return;
                    }

                    Task.Factory.StartNew(() =>
                    {
                        if (!ReadFileData())
                        {
                            AddMsg($"下发读文件数据内容！");
                        }
                        else
                        {
                            AddMsg(logMsg.ToString());
                            /*//解析文件数据，并清除缓冲区
                            List<Byte> bufferLists = new List<byte>();
                            foreach (MultyPackage2 item in s_packageBufferList)
                            {
                                bufferLists.AddRange(item.datas);
                            }
                            StringBuilder sbStr = new StringBuilder();

                            Debug.WriteLine(Encoding.ASCII.GetString(bufferLists.ToArray()));
                            for (int b = 0; b < bufferLists.Count - 1; b += 2)
                            {
                                string value = Encoding.ASCII.GetString(new byte[2] { bufferLists[b], bufferLists[b + 1] });
                                sbStr.Append(value);
                            }
                            //s_packageBufferList.Clear();

                            //生成文件夹
                            Debug.WriteLine(sbStr.ToString());*/
                        }
                    }, cts.Token);
                });
            }
        }//func

        public bool StartReadFile()
        {
            bool ok = false;
            AddMsg("发起方下发读文件指令");
            for (int i = 0; i < 3; i++)
            {
                write_read_file(fileNo);
                AddMsg($"请求第{i + 1}次");
                Thread.Sleep(150 * (i + 1));

                ///在1000ms内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    try
                    {
                        if (write_read_file_check())
                        {
                            ok = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                    }

                    if (timer.ElapsedMilliseconds > 1000)
                    {
                        timer.Stop();
                        break;
                    }
                }

                if (ok)
                    break;
            }//for
            return ok;
        }//func

        public bool ReadFileData()
        {
            bool ok = false;
            AddMsg("", false);
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  发起方下发读文件数据内容");

            //根据响应文件大小循环下发获取指令
            int count;
            if (fileTotal % dataLength != 0)
            {
                dataLengthTemp = fileTotal % dataLength;
                count = fileTotal / dataLength + 1;
            }
            else
            {
                count = fileTotal / dataLength;
            }

            int sendIndex = 0;
            while (!cts.IsCancellationRequested)
            {
                if (sendIndex == count - 1)
                    return ok;

                int j = sendIndex++;
                for (int i = 0; i < 3; i++)
                {
                    AddMsg($"请求次数：{j} 请求偏移地址：{j * dataLength}，长度为{dataLength}");
                    if (sendIndex == count - 1 && dataLengthTemp != 0)
                    {
                        //Debug.WriteLine("最后一针请求：" + dataLengthTemp);
                        write_read_file_data(j * dataLength, dataLengthTemp, fileNo);
                    }
                    else
                    {
                        write_read_file_data(j * dataLength, dataLength, fileNo);
                    }
                    //ThreeReadFileData(j * dataLength, dataLength, fileNo);

                    Thread.Sleep(150);

                    ///在1000ms内如果收到正确应答则ok
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    while (true)
                    {
                        try
                        {
                            if (write_read_file_data_check())
                            {
                                ok = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                        }

                        if (timer.ElapsedMilliseconds > 1000)
                        {
                            timer.Stop();
                            break;
                        }
                    }
                    if (ok)
                    {
                        break;
                    }
                    else
                        return false;
                }
            }
            return ok;
        }//func

        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="addArrow"></param>
        private void AddMsg(string msg, bool addArrow = true)
        {
            string tmp = Message;
            if (addArrow)
            {
                tmp += ">" + msg + "\r\n";
            }
            else
            {
                tmp += msg + "\r\n";
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Message = tmp;
            });
        }
        /// <summary>
        /// 下发读取文件指令（请求帧）
        /// </summary>
        /// <param name="no">文件编码：0~4</param>
        /// <param name="readType">读取方式：0/1</param>
        private void write_read_file(int no, int readType = 1)
        {
            CanFrameID cid = new CanFrameID();
            cid.Priority = 3;
            cid.FrameType = 2;
            cid.ContinuousFlag = 0;
            cid.FC = 40;    //功能码
            cid.DstType = 4;//PCS
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 0;//CSU-MCU1  010
            cid.SrcAddr = 0;//地址1
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = 0x00;//子设备地址
            send[1] = Convert.ToByte(no & 0xff);
            send[2] = Convert.ToByte(readType & 0xff);

            SendFrame(id, send);
            MultiLanguages.Common.LogHelper.WriteLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        /// <summary>
        /// 下发读取文件指令（应答帧）
        /// </summary>
        /// <returns></returns>
        private bool write_read_file_check()
        {
            MultyPackage2 package = s_packageBufferList[0];
            CanFrameID frameId = new CanFrameID();
            frameId.ID = package.id;
            //CanFrameModel frame = new CanFrameModel(recvFrame);
            //CanFrameID frameId = frame.FrameId;
            uint id = frameId.ID;

            if (id == 0x01000000)  //过滤默认id
                return false;

            try
            {
                s_packageBufferList.Clear();//赋值后清除数据

                //CanFrameData frameDate = frame.FrameDatas[0];
                byte[] data = package.datas.ToArray();//frameDate.Data;

                MultiLanguages.Common.LogHelper.WriteLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                if (frameId.FC != 40)
                    return false;

                if (data.Length == 0)
                {
                    MultiLanguages.Common.LogHelper.WriteLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                    return false;
                }

                byte resultDesc = data[2];
                if (resultDesc == 0)
                {
                    fileTotal = (data[3] | data[4] << 8 | data[5] << 16);
                    AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  响应文件总大小：{fileTotal}");
                    return true;
                }

            }
            catch (Exception ex)
            {
                int stop = 10;
            }

            return false;
        }
        /// <summary>
        /// 下发读文件数据内容（请求帧）
        /// </summary>
        /// <param name="fileOffset">当前文件偏移地址</param>
        /// <param name="dataSize">数据长度</param>
        private void write_read_file_data(int fileOffset, int dataSize, int fileNo)
        {
            CanFrameID cid = new CanFrameID();
            cid.Priority = 3;
            cid.FrameType = 2;
            cid.ContinuousFlag = 1;
            cid.FC = 41;    //功能码
            cid.DstType = 4;//PCS
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 0;//CSU-MCU1  010
            cid.SrcAddr = 0;//地址1
            uint id = cid.ID;
            byte[] send = new byte[8];
            send[0] = 0x00;
            send[1] = Convert.ToByte(fileNo & 0xff);
            send[2] = (byte)(fileOffset & 0xff);
            send[3] = (byte)(fileOffset >> 8);
            send[4] = (byte)(fileOffset >> 16);
            send[5] = Convert.ToByte(dataSize & 0xff);
            send[6] = Convert.ToByte(dataSize >> 8);
            SendFrame(id, send);
            MultiLanguages.Common.LogHelper.WriteLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        /// <summary>
        /// 下发读文件数据内容（应答帧）
        /// </summary>
        /// <returns></returns>
        private bool write_read_file_data_check()
        {
            int packIndex = s_packageBufferList.Count - 1;

            MultyPackage2 frame = s_packageBufferList[packIndex];
            uint id = frame.id;

            if (id == 0x01000000)  //过滤默认id
                return false;

            try
            {
                //CanFrameData frameDate = frame.FrameDatas[0];
                byte[] data = frame.datas.ToArray();//frameDate.Data;

                MultiLanguages.Common.LogHelper.WriteLog($"[接收]请求应答帧-Data，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                //解析文件数据，并清除缓冲区
                for (int i = 0; i < data.Length; i++)
                {
                    String asciiStr = ((char)data[i]).ToString();//十六进制转ASCII码
                    Debug.Write(asciiStr);
                    logMsg.Append(asciiStr);
                }

                CanFrameID frameId = new CanFrameID();
                frameId.ID = id;
                if (frameId.FC != 41)//(id != 0x19a90081)
                    return false;

                if (data.Length == 0)
                {
                    MultiLanguages.Common.LogHelper.WriteLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                int stop = 10;
            }

            return false;
        }
        /// <summary>
        /// 更新帧数据
        /// </summary>
        private void UpdateFrameDate()
        {
            //初始化默认数据
            {
                CanFrameData frameData = new CanFrameData();
                frameData.AddInitMultiDataTofile();
                DataInfos = frameData.DataInfos;
            }

            CanFrameID frameId = tmpModel.FrameId;
            List<CanFrameData> _frameDatas = tmpModel.FrameDatas;

            if (frameId.ContinuousFlag != 0)
            {
                //连续帧
                CanFrameData frameData = _frameDatas[0];
                if (frameData != null)
                {
                    DataInfos = frameData.DataInfos;
                }
            }
        }
        /// <summary>
        /// 处理精度
        /// </summary>
        /// <param name="frameData"></param>
        private void ProcPresion(CanFrameData frameData)
        {
            foreach (var dataInfo in frameData.DataInfos)
            {
                if (dataInfo.Type.Contains("float") && dataInfo.Precision != null)
                {
                    //按精度有几位小数，则值也保留几位小数
                    string value = dataInfo.Value;
                    string presion = dataInfo.Precision.ToString();
                    string[] presions = presion.Split('.');
                    if (presions.Length != 2)
                        continue;

                    int num = presions[1].Length;
                    string[] values = value.Split(".");
                    if (values.Length == 2)
                    {
                        dataInfo.Value = $"{values[0]}.{values[1].Substring(0, num)}";
                    }
                }
            }
        }
        #endregion

        #region CAN操作
        /// <summary>
        /// 初始化Can设置
        /// </summary>
        public void InitCanHelper()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel != null)
            {
                frameCfgModel = fileCfgModel.FrameModel;
            }
            //接收处理
            ecanHelper.RegisterRecvProcessCan1(RecvProcCan1);
            ecanHelper.RegisterRecvProcessCan2(RecvProcCan2);
        }
        /// <summary>
        /// 通道1接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan1(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage == GlobalManager.Page.FileOpt)
                RecvFrame(recvData);
        }
        /// <summary>
        /// 通道2接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage == GlobalManager.Page.FileOpt)
                RecvFrame(recvData);
        }
        /// <summary>
        /// 接收单帧数据
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvFrame(CAN_OBJ recvData)
        {
            //转化CAN_ID
            CanFrameID frameId = new CanFrameID();
            frameId.ID = recvData.ID;

            //过滤请求帧
            if (frameId.FrameType == 2)
                return;

            //获取设备ID
            byte devAddr = DeviceManager.Instance().GetSelectDev();
            if (frameId.FC == 0x39) //组播（功能码57）不走地址设置
            {
                devAddr = 0;
            }

            //非连续帧0，连续帧1
            if (frameId.ContinuousFlag == 0)
            {
                /* MultyPackage2 newMulPackage = new MultyPackage2();
                newMulPackage.packageNum++;
                newMulPackage.id = recvData.ID;
                newMulPackage.datas.AddRange(recvData.Data);
                s_packageBufferList.Add(newMulPackage);
                 */
                CanFrameModel? frame = ProtocolHelper.FrameToModel(null, recvData.ID, recvData.Data, devAddr);

                isPackage = UpdateRecv(frame);
            }
            else
            {
                /*//子设备地址，包序号
                byte[] data = recvData.Data;
                int devIndex = data[0];
                int packageIndex = data[1];

                //?找集合里面匹配的数值。
                MultyPackage2? findMulPackage = s_packageBufferList.Find((o) =>
                {
                    return (packageIndex == o.packageNum);
                });

                //连续
                if (packageIndex == 0)
                {
                    //第一包 当前文件偏移地址
                    int offset = data[2] | (data[3] << 8) | (data[4] << 16);
                    //第一包 当前文件大小
                    int addr = data[5] | (data[6]) << 8;

                    //加载数据
                    MultyPackage2 newMulPackage = new MultyPackage2();
                    newMulPackage.packageNum++;
                    newMulPackage.id = recvData.ID;
                    newMulPackage.datas.Add(data[7]);
                    s_packageBufferList.Add(newMulPackage);
                }
                else if (packageIndex == 0xff)
                {
                    if (findMulPackage == null)
                    {
                        //追加最后3字节
                        int packageNum = s_packageBufferList.Count - 1;
                        s_packageBufferList[packageNum].datas.AddRange(data.Skip(2).Take(3));

                    }

                    //校验CRC
                    int fileDataCrc = CRCHelper.ComputeCrc16(s_packageBufferList[0].datas.ToArray(), 256);
                    if (!((byte)(fileDataCrc >> 8) == data[6] && (byte)(fileDataCrc & 0xff) == data[5]))
                    {
                        s_packageBufferList.Clear();
                        AddMsg("校验CRC错误！");
                        return;
                    }

                    isPackage = true;
                }
                else
                {
                    findMulPackage.packageNum++;
                    findMulPackage.datas.AddRange(data.Skip(2));//去除子设备地址，包序号
                }*/
                CanFrameModel? frame = ProtocolHelper.MultiFrameToModel(null, recvData.ID, recvData.Data, devAddr);

                UpdateContinueRecv(frame);
            }
        }
        /// <summary>
        /// 非连续帧解析
        /// </summary>
        private bool UpdateRecv(CanFrameModel frame)
        {
            if (frame.FrameDatas.Count <= 0)
                return false;

            CanFrameData frameData = frame.FrameDatas[0];
            ProcPresion(frameData);
            List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>(frameData.DataInfos);  //使用第一包数据
            string addr = frame.GetAddrInt().ToString();

            //去除起始地址、个数不显示
            {
                dataInfos.RemoveAt(0); //起始地址
                dataInfos.RemoveAt(0); //个数
            }

            //去除隐藏字段
            for (int i = 0; i < dataInfos.Count; i++)
            {
                CanFrameDataInfo info = dataInfos[i];
                if (info.Hide)
                {
                    dataInfos.RemoveAt(i);
                    i--;
                }
            }

            /*SendRecvFrameInfo newFrameInfo = new SendRecvFrameInfo();
            newFrameInfo.IsSend = false;
            newFrameInfo.Time = recvTime;
            newFrameInfo.ID = "0x" + recvData.ID.ToString("X8");
            newFrameInfo.Datas = BitConverter.ToString(recvData.Data);

            newFrameInfo.IsContinue = frame.FrameId.ContinuousFlag == 1;
            newFrameInfo.Addr = addr;
            newFrameInfo.PackageNum = frame.GetPackageNum();*/

            return true;
        }

        private void UpdateContinueRecv(CanFrameModel frame)
        {
            //子设备地址，包序号
            byte[] data = recvData.Data;
            int devIndex = data[0];
            int packageIndex = data[1];

            //?找集合里面匹配的数值。
            MultyPackage2? findMulPackage = s_packageBufferList.Find((o) =>
            {
                return (packageIndex == o.packageNum);
            });

            //连续
            if (packageIndex == 0)
            {
                //第一包 当前文件偏移地址
                int offset = data[2] | (data[3] << 8) | (data[4] << 16);
                //第一包 当前文件大小
                int addr = data[5] | (data[6]) << 8;

                //加载数据
                MultyPackage2 newMulPackage = new MultyPackage2();
                newMulPackage.packageNum++;
                newMulPackage.id = recvData.ID;
                newMulPackage.datas.Add(data[7]);
                s_packageBufferList.Add(newMulPackage);
            }
            else if (packageIndex == 0xff)
            {
                if (findMulPackage == null)
                {
                    //追加最后3字节
                    int packageNum = s_packageBufferList.Count - 1;
                    s_packageBufferList[packageNum].datas.AddRange(data.Skip(2).Take(3));

                }

                //校验CRC
                int fileDataCrc = CRCHelper.ComputeCrc16(s_packageBufferList[0].datas.ToArray(), 256);
                if (!((byte)(fileDataCrc >> 8) == data[6] && (byte)(fileDataCrc & 0xff) == data[5]))
                {
                    s_packageBufferList.Clear();
                    AddMsg("校验CRC错误！");
                    return;
                }

                isPackage = true;
            }
            else
            {
                findMulPackage.packageNum++;
                findMulPackage.datas.AddRange(data.Skip(2));//去除子设备地址，包序号
            }
        }
        /// <summary>
        /// 发送单帧数据
        /// </summary>
        /// <param name="frame"></param>
        private void SendFrame(uint id, byte[] datas)
        {
            if (ecanHelper.IsCan1Start)
            {
                ecanHelper.SendCan1(id, datas);
            }

            if (ecanHelper.IsCan2Start)
            {
                ecanHelper.SendCan2(id, datas);
            }
        }
        #endregion
    }
}
