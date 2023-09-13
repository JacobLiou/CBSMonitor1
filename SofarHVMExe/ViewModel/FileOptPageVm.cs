using CanProtocol;
using CanProtocol.ProtocolModel;
using Communication.Can;
using NPOI.HPSF;
using NPOI.OpenXml4Net.OPC;
using NPOI.POIFS.NIO;
using NPOI.SS.Formula.Functions;
using NPOI.XSSF.Streaming.Values;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;
using Org.BouncyCastle.Ocsp;
using SofarHVMExe.Model;
using SofarHVMExe.SubPubEvent;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;

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
        private int fileNo = -1;       //文件编码
        private int fileTotal = -1;    //当前文件总大小
        private int dataLength = -1;   //交互长度
        private string fileType;       //文件类型

        private int dataLengthTemp = 0;//临时使用的非常规交互长度
        private static bool isPackage = true;//当前包
        private static string fileName = "";  //文化名称：文件类型+设备类型+设备号+导出日期

        private StringBuilder logMsg = new StringBuilder();//文件字符串
        private String BCUStr = "时间,电池簇电压,电池簇SOC,电池簇SOH,电池簇电流,继电器状态,风扇状态,BCU状态,充放电使能,保护信息1,保护信息2,保护信息3,保护信息4,告警信息1,告警信息2,最高单体电压,最高单体电压BMU序号,最高单体电压所在BMU的第几节,最小单体电压,最小单体电压BMU序号,最小单体电压所在BMU第几节,最高单体温度,最高单体温度BMU序号,最高单体温度所在BMU第几节,最小单体温度,最小单体温度BMU序号,最小单体温度所在BMU第几节,最大Pack总压,最小Pack总压,最大Pack总压编号,最小Pack总压编号,簇号,簇内电池包数量,高压绝缘阻抗,保险丝后电压,功率侧电压,负载端电压,辅助电源电压,电池簇的充电电压,电池簇充电电流上限,电池簇放电电流上限,电池簇的放电截止电压,电池包均衡状态,最高功率端子温度,环境温度,累计充电安时,累计放电安时,累计充电瓦时,累计放电瓦时\r\n";
        private String BMUStr = "时间,PACK电池总电压累,电芯电压,SOC显示值,SOH显示值,SOC计算值,SOH计算值,电池电流,最高单体电压,最低单体电压,最高单体电压序号,最低单体电压序号,最高单体温度,最低单体温度,最高单体温度序号,最低单体温度序号,BMU编号,系统状态,充放电使能,切断请求,关机请求,充电电流上限,放电电流上限,保护1,保护2,告警1,告警2,故障1,故障2,主动均衡状态,均衡母线电压,均衡母线电流,辅助供电电压,满充容量,循环次数,累计放电安时,累计充电安时,累计放电瓦时,累计充电瓦时,环境温度,DCDC温度1,DCDC温度2,均衡温度1,均衡温度2,1-16串均衡状态,单体电压1,单体电压2,单体电压3,单体电压4,单体电压5,单体电压6,单体电压7,单体电压8,单体电压9,单体电压10,单体电压11,单体电压12,单体电压13,单体电压14,单体电压15,单体电压16,单体温度1,单体温度2,单体温度3,单体温度4,单体温度5,单体温度6,单体温度7,单体温度8,单体温度9,单体温度10,单体温度11,单体温度12,单体温度13,单体温度14,单体温度15,单体温度16\r\n";
        private String FaultRecordStr = "电流,最大电压,最小电压,最大温度,最小温度\r\n";

        private CRCHelper crcHelper = new CRCHelper();
        private CancellationTokenSource cts = new CancellationTokenSource();//取消信号量
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
        //当前选择执行的传输模块
        private int currentObjval;
        public int CurrentObjval
        {
            get => currentObjval;
            set
            {
                currentObjval = value;
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
        //BMU访问地址
        private int bmuId = 0x1;
        public int BmuId
        {
            get { return bmuId; }
            set
            {
                bmuId = value;
                OnPropertyChanged();
            }
        }
        //文件编码
        private List<FileCode> fileCodes = new List<FileCode>() {
            {new FileCode() { ID = 0, Name = "故障录波文件1", DataLength = 8 }},
            {new FileCode() { ID = 1, Name = "故障录波文件2", DataLength = 8  }},
            {new FileCode() { ID = 2, Name = "故障录波文件3", DataLength = 8  }},
            {new FileCode() { ID = 3, Name = "5min特性数据文件", DataLength = 200 }},
            {new FileCode() { ID = 4, Name = "运行日志文件", DataLength = 256 } }
        };
        public List<FileCode> FileCodeList
        {
            get { return fileCodes; }
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
            try
            {
                if (BmuId < 1 || BmuId > 31)
                    throw new ArgumentOutOfRangeException(nameof(BmuId),
                           "The valid range is between 1 and 31.");

                if (o != null)//赋值SelectData
                {
                    FileCode model = (FileCode)o;
                    fileNo = model.ID;
                    fileType = model.Name;
                    dataLength = model.DataLength;
                }

                //BCU选择设备ID
                targetAddr = DeviceManager.Instance().GetSelectDev();

                //正在读取状态，需终止操作并将变量初始化
                if (Status)
                {
                    if (cts != null)
                        cts.Cancel();

                    Status = false;
                }
                else
                {
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
                                    AddMsg($"完成读文件数据内容！");
                                    //if (fileNo == 4)
                                    //    AddMsg(logMsg.ToString());
                                }
                            }, cts.Token);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行错误：" + ex.Message);
            }
        }//func

        public bool StartReadFile()
        {
            bool ok = false;
            AddMsg("发起方下发读文件指令");
            isPackage = false;
            for (int i = 0; i < 5; i++)
            {
                write_read_file(fileNo);
                AddMsg($"请求第{i + 1}次");
                Thread.Sleep(100 * (i + 1));

                ///在1000ms内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    try
                    {
                        if (isPackage)
                        {
                            ok = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                    }

                    if (timer.ElapsedMilliseconds > 3000)
                    {
                        timer.Stop();
                        break;
                    }
                }

                if (ok) break;
            }//for
            return ok;
        }//func

        public bool ReadFileData()
        {
            bool ok = false;
            AddMsg("", false);
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  发起方下发读文件数据内容");

            int count;
            //fileTotal = 85;
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
                if (sendIndex < count)
                {
                    ok = false;
                    isPackage = false;

                    for (int i = 0; i < 5; i++)
                    {
                        int datasize = -1;
                        if (sendIndex == count - 1 && dataLengthTemp != 0)
                        {
                            datasize = dataLengthTemp;
                        }
                        else
                        {
                            datasize = dataLength;
                        }
                        write_read_file_data(sendIndex * dataLength, datasize, fileNo);
                        AddMsg($"请求次数：{i + 1} 请求偏移地址：{sendIndex * dataLength}，长度为{datasize}");
                        Thread.Sleep(50 * (i + 1));

                        //在1000ms内如果收到正确应答则ok
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        while (true)
                        {
                            try
                            {
                                if (isPackage)
                                {
                                    //isPackage = true;
                                    sendIndex++;
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
                    }
                    if (!ok)
                        return false;
                }

                if (!isPackage || sendIndex >= count)
                    cts.Cancel();
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
            cid.DstType = 4;//BCU
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 0;//CSU-MCU1  010
            cid.SrcAddr = 0;//地址1
            uint id = cid.ID;

            byte data0 = 0x00;//表示交互BCU，无需填写设备ID；交互BMU、需要设备ID+0xA0;
            if (CurrentObjval != 0)
                data0 = Convert.ToByte(BmuId + 0xA0);

            byte[] send = new byte[8];
            send[0] = data0;//子设备地址为
            send[1] = Convert.ToByte(no & 0xff);
            send[2] = Convert.ToByte(readType & 0xff);

            SendFrame(id, send);
            MultiLanguages.Common.LogHelper.WriteLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
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

            byte data0 = 0x00;//表示交互BCU，无需填写设备ID；交互BMU、需要设备ID+0xA0;
            if (CurrentObjval != 0)
                data0 = Convert.ToByte(BmuId + 0xA0);

            byte[] send = new byte[8];
            send[0] = data0;
            send[1] = Convert.ToByte(fileNo & 0xff);
            send[2] = (byte)(fileOffset & 0xff);
            send[3] = (byte)(fileOffset >> 8);
            send[4] = (byte)(fileOffset >> 16);
            send[5] = Convert.ToByte(dataSize & 0xff);
            send[6] = Convert.ToByte(dataSize >> 8);
            SendFrame(id, send);
            MultiLanguages.Common.LogHelper.WriteLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        public void FileWrite(string path, string content)
        {
            /*FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            //开始写入
            sw.Write(content);
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            fs.Close();*/

            if (!File.Exists(path))
            {
                using (StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8))
                {
                    sw.Write(content);
                }
            }
            else
            {
                FileStream fs = new FileStream(path, FileMode.Append);
                StreamWriter sw1 = new StreamWriter(fs, Encoding.UTF8);
                sw1.Write(content);
                sw1.Close();
                //fs.Flush();
            }
        }
        #endregion

        #region CAN操作
        /// <summary>
        /// 初始化Can设置
        /// </summary>
        public void InitCanHelper()
        {
            frameCfgModel = DataManager.GetFrameConfigModel();

            //接收处理
            ecanHelper.RegisterRecvProcessCan1(RecvProcCan1);
            ecanHelper.RegisterRecvProcessCan2(RecvProcCan2);
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
        /// 接收帧数据-解析
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvFrame(CAN_OBJ recvData)
        {
            //转化CAN_ID
            CanFrameID frameId = new CanFrameID();
            frameId.ID = recvData.ID;

            //过滤其他帧：默认id| 请求帧
            if (recvData.ID == 0x01000000 || frameId.FrameType == 2)
                return;

            //获取设备ID
            byte devAddr = DeviceManager.Instance().GetSelectDev();
            if (frameId.FC == 0x39) //组播（功能码57）不走地址设置
            {
                devAddr = 0;
            }

            //非连续帧0，连续帧1
            if (frameId.ContinuousFlag == 0 && frameId.FC == 40)
            {
                //测试打印
                Debug.WriteLine($"[接收]请求应答帧，0x{recvData.ID.ToString("X")} {BitConverter.ToString(recvData.Data)}");

                {
                    CanFrameModel? frame = ProtocolHelper.FrameToModel2(recvData.ID, recvData.Data, devAddr);

                    if (frame == null || frame.FrameDatas.Count <= 0 || frame.FrameDatas[0].Data.Length == 0)
                        return;

                    CanFrameData frameData = frame.FrameDatas[0];
                    MultiLanguages.Common.LogHelper.WriteLog($"[接收]请求应答帧，0x{frameId.ID.ToString("X")} {BitConverter.ToString(frameData.Data)}");

                    List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>(frameData.DataInfos);  //使用第一包数据

                    AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} result：{dataInfos[2].Value},子设备地址：{dataInfos[0].Value},文件编号：{dataInfos[1].Value},文件大小：{dataInfos[3].Value}");

                    if (int.Parse(dataInfos[2].Value) == 0x0)
                    {
                        isPackage = true;
                        fileTotal = int.Parse(dataInfos[3].Value);

                        //1.生成文件名称
                        fileName = string.Format("{0}_{1}_{2}_{3}", fileType, CurrentObjval == 0 ? "BCU" : "BMU", CurrentObjval == 0 ? targetAddr.ToString("X2") : BmuId, System.DateTime.Now.ToString("yy-MM-dd-HH-mm-ss"));
                        //2.检查文件夹，生成文件及表头
                        if (!Directory.Exists("Log//InteractionFile"))
                        {
                            Directory.CreateDirectory("Log//InteractionFile");
                        }

                        string extendName = fileType == "运行日志文件" ? "txt" : "csv";

                        fileName = $"{AppDomain.CurrentDomain.SetupInformation.ApplicationBase}//Log//InteractionFile//{fileName}.{extendName}";

                        switch (fileType)
                        {
                            case "故障录波文件1":
                            case "故障录波文件2":
                            case "故障录波文件3":
                                File.AppendAllText(fileName, FaultRecordStr);
                                break;
                            case "5min特性数据文件":
                                File.AppendAllText(fileName, currentObjval == 0 ? BCUStr : BMUStr);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            else if (frameId.ContinuousFlag == 1 && frameId.FC == 41)
            {
                //测试打印
                uint idw = recvData.ID;
                byte[] dataw = recvData.Data;
                Debug.WriteLine($"[接收]请求应答帧，0x{idw.ToString("X")} {BitConverter.ToString(dataw)}");

                bool bcu = CurrentObjval == 0 ? true : false;
                CanFrameModel? frame = ProtocolHelper.MultiFrameToModel2(fileNo, recvData.ID, recvData.Data, devAddr, bcu);

                if (frame == null || frame.FrameDatas.Count <= 0 || frame.FrameDatas[0].Data.Length == 0)
                    return;

                CanFrameData frameData = frame.FrameDatas[0];
                uint id = frameId.ID;
                byte[] data = frameData.Data;
                MultiLanguages.Common.LogHelper.WriteLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>(frameData.DataInfos);  //使用第一包数据
                string addr = frame.GetAddrInt().ToString();

                isPackage = true;
                string content = "";
                if (fileNo < 3)//故障录波
                {
                    for (int i = 4; i < dataInfos.Count - 1; i++)
                    {
                        logMsg.Append(dataInfos[i].Value + ",");
                        content += $"{dataInfos[i].Value},";
                    }
                    File.AppendAllText(fileName, content + "\r\n");
                }
                else if (fileNo == 3)//特性数据
                {
                    //时间处理
                    {
                        StringBuilder sbDate = new StringBuilder();
                        for (int i = 4; i < 10; i++)
                        {
                            sbDate.Append(i == 4 ? Convert.ToInt32(dataInfos[i].Value) + 2000 : dataInfos[i].Value.PadLeft(2, '0'));
                            if (i < 4 + 2)
                            {
                                sbDate.Append("-");
                            }
                            else if (i < 4 + 2 + 1)
                            {
                                sbDate.Append(" ");
                            }
                            else if (i < 4 + 2 + 1 + 2)
                            {
                                sbDate.Append(":");
                            }
                        }

                        logMsg.Append(sbDate.ToString() + ",");
                        content += $"{sbDate.ToString()},";
                    }

                    for (int i = 7 + 3; i < dataInfos.Count - 1; i++)
                    {
                        //均衡按BIT显示
                        logMsg.Append(dataInfos[i].Value + ",");

                        if (dataInfos[i].Name == "电池包均衡状态")
                        {
                            content += $"{Convert.ToString(Convert.ToUInt16(dataInfos[i].Value), 16)},";
                        }
                        else
                        {
                            content += $"{dataInfos[i].Value},";
                        }
                    }
                    File.AppendAllText(fileName, content + "\r\n");
                }
                else
                {
                    for (int i = 7; i < data.Length - 2; i++)
                    {
                        String asciiStr = ((char)data[i]).ToString();//十六进制转ASCII码
                        logMsg.Append(asciiStr);
                        content += asciiStr;
                    }
                    //AddMsg(content);
                    FileWrite(fileName, content);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// 文件编码(未启用)
    /// </summary>
    public class FileCode
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int DataLength { get; set; }
    }
}
