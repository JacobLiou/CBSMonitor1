using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using NPOI.HPSF;
using NPOI.OpenXml4Net.OPC;
using NPOI.POIFS.NIO;
using NPOI.SS.Formula.Functions;
using NPOI.XSSF.Streaming.Values;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
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
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using CanProtocol;
using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;
using MathNet.Numerics;
using NPOI.Util;
using SixLabors.ImageSharp;

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
        private FrameConfigModel? frameCfgModel = null;
        private byte targetAddr = 0x0; //目标设备地址，广播0x0，否则为单台升级
        private int fileNo = -1;       //文件编码
        private int fileTotal = -1;    //当前文件总大小
        private int dataLength = -1;   //交互长度
        private string fileType;       //文件类型
        private bool Flag_sn = true;

        private static bool isPackage; //当前包
        private static string fileName = "";  //文件名称：文件类型+设备类型+设备号+导出日期

        private StringBuilder logMsg = new StringBuilder();//文件字符串
        private String BCUStr = "时间,电池簇电压(V),电池簇SOC(%),电池簇SOH(%),电池簇电流(mA),继电器状态,风扇状态,继电器切断请求,BCU状态,充放电使能,保护信息1,保护信息2,保护信息3,保护信息4,告警信息1,告警信息2,最高PACK电压,最低PACK电压,最高PACK电压序号,最低PACK电压序号,簇号,簇内电池包数量,高压绝缘阻抗(kΩ),保险丝后电压(V),功率侧电压(V),负载端电压(V),辅助电源电压(V),电池簇的充电电压(V),电池簇充电电流上限(A),电池簇放电电流上限(A),电池簇的放电截止电压(V),电池包均衡状态,最高功率端子温度(℃),环境温度(℃),累计充电安时(Ah),累计放电安时(Ah),累计充电瓦时(Wh),累计放电瓦时(Wh),BMU编号,电芯电压1,电芯电压2,电芯电压3,电芯电压4,电芯电压5,电芯电压6,电芯电压7,电芯电压8,电芯电压9,电芯电压10,电芯电压11,电芯电压12,电芯电压13,电芯电压14,电芯电压15,电芯电压16,PACK1最大单体电压,PACK2最大单体电压,PACK3最大单体电压,PACK4最大单体电压,PACK5最大单体电压,PACK6最大单体电压,PACK7最大单体电压,PACK8最大单体电压,PACK9最大单体电压,PACK10最大单体电压,PACK1最小单体电压,PACK2最小单体电压,PACK3最小单体电压,PACK4最小单体电压,PACK5最小单体电压,PACK6最小单体电压,PACK7最小单体电压,PACK8最小单体电压,PACK9最小单体电压,PACK10最小单体电压,PACK1平均单体电压,PACK2平均单体电压,PACK3平均单体电压,PACK4平均单体电压,PACK5平均单体电压,PACK6平均单体电压,PACK7平均单体电压,PACK8平均单体电压,PACK9平均单体电压,PACK10平均单体电压,PACK1最大单体温度,PACK2最大单体温度,PACK3最大单体温度,PACK4最大单体温度,PACK5最大单体温度,PACK6最大单体温度,PACK7最大单体温度,PACK8最大单体温度,PACK9最大单体温度,PACK10最大单体温度,PACK1最小单体温度,PACK2最小单体温度,PACK3最小单体温度,PACK4最小单体温度,PACK5最小单体温度,PACK6最小单体温度,PACK7最小单体温度,PACK8最小单体温度,PACK9最小单体温度,PACK10最小单体温度,预留1,预留2\r\n";
        private String BMUStr = "时间,电池采集电压(mV),电池累计电压(mV),SOC显示值(%),SOH显示值(%),SOC计算值,SOH计算值,电池电流(mA),最高单体电压(mV),最低单体电压(mV),最高单体电压序号,最低单体电压序号,最高单体温度(℃),最低单体温度(℃),最高单体温度序号,最低单体温度序号,BMU编号,系统状态,充放电使能,切断请求,关机请求,充电电流上限(A),放电电流上限(A),保护1,保护2,告警1,告警2,故障1,故障2,主动均衡状态,均衡母线电压(mV),均衡母线电流(mA),辅助供电电压(mV),满充容量(Ah),循环次数,累计放电安时(Ah),累计充电安时(Ah),累计放电瓦时(Wh),累计充电瓦时(Wh),环境温度(℃),DCDC温度1(℃),DCDC温度2(℃),均衡温度1(℃),均衡温度2(℃),1-16串均衡状态,单体电压1(mV),单体电压2(mV),单体电压3(mV),单体电压4(mV),单体电压5(mV),单体电压6(mV),单体电压7(mV),单体电压8(mV),单体电压9(mV),单体电压10(mV),单体电压11(mV),单体电压12(mV),单体电压13(mV),单体电压14(mV),单体电压15(mV),单体电压16(mV),单体温度1(℃),单体温度2(℃),单体温度3(℃),单体温度4(℃),单体温度5(℃),单体温度6(℃),单体温度7(℃),单体温度8(℃),单体温度9(℃),单体温度10(℃),单体温度11(℃),单体温度12(℃),单体温度13(℃),单体温度14(℃),单体温度15(℃),单体温度16(℃),RSV1,RSV2,RSV3,RSV4,RSV5,RSV6,RSV7,RSV8,RSV9,RSV10\r\n";
        private String FaultRecordStr = "电流(A),最大电压(mV),最小电压(mV),最大温度(℃),最小温度(℃)\r\n";
        private String PCSStr = "序号,时间,故障码,类型,故障编号,故障信息1,故障信息2,故障信息3,故障信息4,故障信息5,故障信息6,故障信息7,故障信息8\r\n";

        private CancellationTokenSource cts = new CancellationTokenSource();//取消信号量
        #endregion

        #region 属性
        //sn号
        private string sn = "";
        public string SN
        {
            get => sn;
            set
            {
                sn = value;
                OnPropertyChanged();
            }
        }
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
            {"运行日志文件",256 },
            {"PCS事件记录",26 }
        };
        public Dictionary<string, int> FiletypeToLength { get { return filetypeToLength; } }
        private int currentFileval;

        public int CurrentFileval
        {
            get { return currentFileval; }
            set
            {
                currentFileval = value;
                if (currentFileval == 5)
                {
                    CurrentObjval = 2;
                }
            }
        }

        //当前选择执行的传输模块
        private int currentObjval;
        public int CurrentObjval
        {
            get => currentObjval;
            set
            {
                currentObjval = value;
                if (currentObjval == 0)
                {
                    IsTiming = Visibility.Visible;
                }
                else
                {
                    IsTiming = Visibility.Hidden;
                }
                OnPropertyChanged();
            }
        }
        //当前选择BMU编号框
        private Visibility isTiming = Visibility.Visible;
        public Visibility IsTiming
        {
            get => isTiming;
            set
            {
                isTiming = value;
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
        //起始地址
        private int startLocation = 0;
        public int StartLocation
        {
            get { return startLocation; }
            set
            {
                startLocation = value;
                OnPropertyChanged();
            }
        }
        //读取条数
        private int readNumber = 0;
        public int ReadNumber
        {
            get { return readNumber; }
            set
            {
                readNumber = value;
                OnPropertyChanged();
            }
        }
        public bool readAllData { get; set; } = false;
        public bool ReadAllData
        {
            get => readAllData;
            set
            {
                readAllData = value;
                OnPropertyChanged();
            }
        }
        //文件编码
        private List<FileCode> fileCodes = new List<FileCode>() {
            {new FileCode() { ID = 0, Name = "故障录波文件1", DataLength = 8 }},
            {new FileCode() { ID = 1, Name = "故障录波文件2", DataLength = 8  }},
            {new FileCode() { ID = 2, Name = "故障录波文件3", DataLength = 8  }},
            {new FileCode() { ID = 3, Name = "5min特性数据文件", DataLength = 200 }},
            {new FileCode() { ID = 4, Name = "运行日志文件", DataLength = 256 } },
            {new FileCode() { ID = 200, Name = "PCS事件记录", DataLength=26 } }
        };
        public List<FileCode> FileCodeList
        {
            get { return fileCodes; }
        }
        #endregion

        #region 命令
        public ICommand ReadCommand { get; set; }

        #endregion

        List<CanFrameModel> snList = new List<CanFrameModel>();

        #region 成员方法
        private void Init()
        {
            ReadCommand = new SimpleCommand(ExecuteBtn);
        }

        public void ExecuteBtn(object o)
        {
            try
            {
                if (BmuId < 0 || BmuId > 31)
                    throw new ArgumentOutOfRangeException(nameof(BmuId),
                           "The valid range is between 0 and 31.");

                if (o == null)
                    return;

                //BCU选择设备ID
                targetAddr = DeviceManager.Instance().GetSelectDev();

                foreach (CanFrameModel item in frameCfgModel.CanFrameModels)
                {
                    if (item.Name.Contains("SN"))
                    {
                        if (item.Id == 0x19850081 || item.Id == 0x0D8B80A3 || item.Id == 0x0D8B80A1 || item.Id == 0x0D8B0080)
                        {
                            snList.Add(item);
                        }
                    }
                }

                //正在读取状态，需终止操作并将变量初始化
                if (Status)
                {
                    if (cts != null)
                        cts.Cancel();

                    Status = false;
                }
                else
                {
                    FileCode model = (FileCode)o;
                    fileNo = model.ID;
                    fileType = model.Name;
                    dataLength = model.DataLength;

                    Task.Run(() =>
                    {
                        cts = new CancellationTokenSource();
                        Status = true;
                        Flag_sn = true;

                        if (currentObjval < 2 && !GetSn())
                        {
                            AddMsg($"下发读设备序列号失败！");
                            Flag_sn = false;
                        }

                        if (!StartReadFile())
                        {
                            AddMsg($"下发读文件指令响应失败！");
                            return;
                        }

                        int sendCount = 0, dataLengthTemp = dataLength;
                        if (fileTotal % dataLength != 0)
                        {
                            sendCount = fileTotal / dataLength + 1;
                            dataLengthTemp = fileTotal % dataLength;
                        }
                        else
                        {
                            sendCount = fileTotal / dataLength;
                        }

                        if (currentObjval < 2)
                        {
                            Task.Factory.StartNew(() =>
                            {
                                if (!ReadFileData(sendCount, dataLengthTemp))
                                {
                                    AddMsg($"下发读文件数据内容！");
                                }
                                else
                                {
                                    AddMsg($"完成读文件数据内容！");
                                }
                            }, cts.Token);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行错误：" + ex.Message);
            }
        }

        private bool GetSn()
        {
            bool ok = false;
            AddMsg("发起方下发读序列号指令");
            isPackage = false;
            for (int i = 0; i < 3; i++)
            {
                SN = "00000000000000000000";//初始化序列号

                write_read_sn();
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
                            Debug.WriteLine("success-sn:" + SN);
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

                if (ok) break;
            }//for
            return ok;
        }

        public bool StartReadFile()
        {
            bool ok = false;
            AddMsg("", false);
            AddMsg("发起方下发读文件指令");
            isPackage = false;
            for (int i = 0; i < 5; i++)
            {
                if (currentObjval < 2)
                {
                    write_read_file(fileNo);
                }
                else
                {
                    write_read_file_pcs();
                }
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
        }

        public bool ReadFileData(int count, int dataLengthTemp)
        {
            bool ok = false;
            AddMsg("", false);
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  发起方下发读文件数据内容");

            int sendIndex = startLocation;
            int endSeekIndex = StartLocation + ReadNumber;
            while (!cts.IsCancellationRequested)
            {
                if ((sendIndex < count && ReadAllData) || (sendIndex < endSeekIndex && !ReadAllData))
                {
                    ok = false;
                    isPackage = false;

                    for (int i = 0; i < 5; i++)
                    {
                        int datasize = sendIndex == count - 1 ? dataLengthTemp : dataLength;
                        if (datasize == -1)
                        {
                            AddMsg($"返回数据长度为空，已终止");
                            return false;
                        }
                        write_read_file_data(sendIndex * dataLength, datasize, fileNo);
                        AddMsg($"请求次数：{i + 1} 请求偏移地址：{sendIndex * dataLength}，长度为{datasize}");
                        Thread.Sleep(5 * (i + 1));

                        //在1000ms内如果收到正确应答则ok
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        while (true)
                        {
                            try
                            {
                                if (isPackage)
                                {
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

                if ((ReadAllData && (!isPackage || sendIndex >= count))
                    || (!ReadAllData && (!isPackage || sendIndex >= endSeekIndex)))
                    cts.Cancel();
            }

            return ok;
        }

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
        /// 下发读取序列号指令（请求帧）
        /// </summary>
        private void write_read_sn()
        {
            CanFrameID frameID = null;
            if (CurrentObjval == 0)
            {
                //BCU
                if (BmuId == 0)
                {
                    frameID = new CanFrameID(0x0E0B8000);
                    frameID.DstAddr = targetAddr;

                    SendFrame(frameID.ID, new byte[] { 0x03, 0x00, 0x0A, 0x00 });
                }
                else
                {
                    int startAddr = 33 + 144 * (BmuId - 1);

                    frameID = new CanFrameID(0x0E058000);
                    frameID.DstAddr = targetAddr;

                    SendFrame(frameID.ID, new byte[] { (byte)(startAddr & 0xff), (byte)(startAddr << 8), 0x0A, 0x00 });
                }
            }
            else
            {
                frameID = new CanFrameID(0x0E0BA080);
                frameID.DstAddr = targetAddr;

                SendFrame(frameID.ID, new byte[] { 0x03, 0x00, 0xA0, 0x00 });
            }
        }

        /// <summary>
        /// 下发读取文件指令（请求帧）
        /// </summary>
        /// <param name="no">文件编码：0~4</param>
        /// <param name="readType">读取方式：0/1</param>
        private void write_read_file(int no, int readType = 1)
        {
            if (ReadAllData)
                readType = 0;

            int dstType = 0;
            byte data0 = 0x00;//表示交互BCU，无需填写设备ID；交互BMU、需要设备ID+0xA0;
            switch (CurrentObjval)
            {
                case 0:
                    dstType = 4;
                    data0 = Convert.ToByte(BmuId + 0xA0); break;
                    break;
                case 1:
                    dstType = 5;
                    data0 = Convert.ToByte(BmuId + 0xA0); break;
                default:
                    break;
            }

            CanFrameID cid = new CanFrameID();
            cid.Priority = 3;
            cid.FrameType = 2;
            cid.ContinuousFlag = 0;
            cid.FC = 40;    //功能码
            cid.DstType = (byte)dstType;//BCU  ->BMU
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 0;//CSU-MCU1  010
            cid.SrcAddr = 0;//地址1
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = data0;//子设备地址为
            send[1] = Convert.ToByte(no & 0xff);
            send[2] = Convert.ToByte(readType & 0xff);

            SendFrame(id, send);
            MultiLanguages.Common.LogHelper.WriteLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        /// <summary>
        /// 下发读取文件指令（请求帧）-PCS
        /// </summary>
        /// <param name="no">文件编号</param>
        /// <param name="readType">读取方式</param>
        private void write_read_file_pcs()
        {
            CanFrameID cid = new CanFrameID();//0xOE292341
            cid.Priority = 3;
            cid.FrameType = 2;
            cid.ContinuousFlag = 0;
            cid.FC = 41;    //功能码
            cid.DstType = 0x1;//PCS
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 0x2;//CSU-MCU1  010
            cid.SrcAddr = 1;//地址1
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = 0xC8;//no=200
            send[1] = 0x01;//readType=1
            send[2] = Convert.ToByte(StartLocation & 0xff);
            send[3] = Convert.ToByte(StartLocation >> 8);
            send[4] = Convert.ToByte(ReadNumber & 0xff);
            send[5] = Convert.ToByte(ReadNumber >> 8);

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
            int dstType = 0;
            byte data0 = 0x00;//表示交互BCU，无需填写设备ID；交互BMU、需要设备ID+0xA0;
            switch (CurrentObjval)
            {
                case 0:
                    dstType = 4;
                    data0 = Convert.ToByte(BmuId + 0xA0); break;
                    break;
                case 1:
                    dstType = 5;
                    data0 = Convert.ToByte(BmuId + 0xA0); break;
                default:
                    break;
            }

            CanFrameID cid = new CanFrameID();
            cid.Priority = 3;
            cid.FrameType = 2;
            cid.ContinuousFlag = 1;
            cid.FC = 41;    //功能码
            cid.DstType = (byte)dstType;//PCS
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 0;//CSU-MCU1  010
            cid.SrcAddr = 0;//地址1
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = data0;
            send[1] = Convert.ToByte(fileNo & 0xff);
            send[2] = (byte)(fileOffset & 0xff);
            send[3] = (byte)(fileOffset >> 8);
            send[4] = (byte)(fileOffset >> 16);
            //默认-1将触发“{"Index was outside the bounds of the array."}”
            send[5] = Convert.ToByte(dataSize & 0xff);
            send[6] = Convert.ToByte(dataSize >> 8);
            SendFrame(id, send);
            MultiLanguages.Common.LogHelper.WriteLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        public void FileWrite(string path, string content)
        {

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
            if (recvData.ID == 0x01000000 || frameId.FrameType == 2 || frameId.FC == 127)// || frameId.FC == 1
                return;

            //获取设备ID
            byte devAddr = DeviceManager.Instance().GetSelectDev();
            if (frameId.FC == 0x39) //组播（功能码57）不走地址设置
            {
                devAddr = 0;
            }

            //非连续帧0，连续帧1
            if (frameId.ContinuousFlag == 1 && Flag_sn && (frameId.FC == 11 || frameId.FC == 5))
            {
                CanFrameModel? frame = ProtocolHelper.MultiFrameToModel(snList, recvData.ID, recvData.Data, devAddr);
                if (frame == null)
                    return;

                UpdateContinueRecv(frame);
                isPackage = true;
                Flag_sn = false;
            }
            else if (frameId.ContinuousFlag == 0 && frameId.FC == 40)
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

                    int val = 0;
                    int.TryParse(dataInfos[2].Value, out val);
                    if (val == 0x0)
                    {
                        isPackage = true;
                        fileTotal = int.Parse(dataInfos[3].Value);

                        //1.生成文件名称
                        string fileNo = "";
                        string fileNameType = "";
                        string fileHeadContent = "";
                        if (CurrentObjval == 0 && BmuId == 0)
                        {
                            fileNo = targetAddr.ToString("X2");
                            fileNameType = "BCU";
                            fileHeadContent = BCUStr;
                        }
                        else if (CurrentObjval == 0 && BmuId != 0)
                        {
                            fileNo = BmuId.ToString();
                            fileNameType = "BMU";
                            fileHeadContent = BMUStr;
                        }
                        else
                        {
                            fileNo = targetAddr.ToString("X2");
                            fileNameType = "BMU";
                            fileHeadContent = BMUStr;
                        }


                        fileName = string.Format("{0}_{1}_{2}_{3}_{4}", fileType, fileNameType, fileNo, System.DateTime.Now.ToString("yy-MM-dd-HH-mm-ss"), SN);
                        //2.检查文件夹，生成文件及表头
                        if (!Directory.Exists("Log//InteractionFile"))
                        {
                            Directory.CreateDirectory("Log//InteractionFile");
                        }

                        string extendName = fileType == "运行日志文件" ? "txt" : "csv";

                        fileName = $"{AppDomain.CurrentDomain.SetupInformation.ApplicationBase}//Log//InteractionFile//{fileName}.{extendName}";
                        /*if (!File.Exists(fileName))
                        {
                            //MessageBox.Show($"{fileName}.{extendName}文件格式非法，创建失败！");
                            fileName = string.Format("{0}_{1}_{2}_{3}", fileType, fileNameType, CurrentObjval == 0 ? targetAddr.ToString("X2") : BmuId, System.DateTime.Now.ToString("yy-MM-dd-HH-mm-ss"));
                            File.Create(fileName);
                        }*/

                        switch (fileType)
                        {
                            case "故障录波文件1":
                            case "故障录波文件2":
                            case "故障录波文件3":
                                File.AppendAllText(fileName, FaultRecordStr);
                                break;
                            case "5min特性数据文件":
                                File.AppendAllText(fileName, fileHeadContent);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            else if (frameId.ContinuousFlag == 1 && frameId.FC == 41 && CurrentObjval <= 1)
            {
                //测试打印
                uint idw = recvData.ID;
                byte[] dataw = recvData.Data;
                Debug.WriteLine($"[接收]请求应答帧，0x{idw.ToString("X")} {BitConverter.ToString(dataw)}");

                bool bcu = false;
                if (CurrentObjval == 0 && bmuId == 0)
                {
                    bcu = true;
                }
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
            else if (frameId.FC == 41 && CurrentObjval > 1)
            {
                isPackage = true;

                if (frameId.ContinuousFlag == 0)
                {
                    //生成文件
                    string fileNameType = "PCS";
                    fileName = string.Format("{0}_{1}_{2}_{3}", fileType, fileNameType, fileNo, System.DateTime.Now.ToString("yy-MM-dd-HH-mm-ss"));
                    if (!Directory.Exists("Log//InteractionFile"))
                    {
                        Directory.CreateDirectory("Log//InteractionFile");
                    }

                    fileName = $"{AppDomain.CurrentDomain.SetupInformation.ApplicationBase}//Log//InteractionFile//{fileName}.csv";

                    string fileHeadContent = PCSStr;
                    File.AppendAllText(fileName, fileHeadContent);
                }
                else
                {
                    //测试打印
                    uint idw = recvData.ID;
                    byte[] dataw = recvData.Data;
                    Debug.WriteLine($"[接收]请求应答帧，0x{idw.ToString("X")} {BitConverter.ToString(dataw)}");

                    CanFrameModel? frame = ProtocolHelper.MultiFrameToModelToPSCFile(fileNo, recvData.ID, recvData.Data, devAddr);

                    if (frame == null || frame.FrameDatas.Count <= 0 || frame.FrameDatas[0].Data.Length == 0)
                        return;

                    CanFrameData frameData = frame.FrameDatas[0];
                    uint id = frameId.ID;
                    byte[] data = frameData.Data;
                    MultiLanguages.Common.LogHelper.WriteLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                    List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>(frameData.DataInfos);  //使用第一包数据

                    int startVal = Convert.ToInt32(dataInfos[1].Value, 16);
                    int number = Convert.ToInt32(dataInfos[2].Value) / 26;
                    AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} [接收]起始地址：{startVal}，响应条数：{number}");

                    string content = "";
                    if (fileNo == 200)
                    {
                        int offset = 0; //第1条

                        for (int i = (offset * 17) + 3; i < dataInfos.Count - 1; i += 17)
                        {
                            if (offset >= number) break;

                            content = $"{startVal},";
                            //时间处理
                            StringBuilder sbDate = new StringBuilder();
                            for (int d = i; d < i + 6; d++)
                            {
                                int tempD = d % 17;
                                sbDate.Append(tempD == 3 ? Convert.ToInt32(dataInfos[d].Value) + 2000 : dataInfos[d].Value.PadLeft(2, '0'));
                                if (tempD < 3 + 2)
                                {
                                    sbDate.Append("-");
                                }
                                else if (tempD < 3 + 2 + 1)
                                {
                                    sbDate.Append(" ");
                                }
                                else if (tempD < 3 + 2 + 1 + 2)
                                {
                                    sbDate.Append(":");
                                }
                            }
                            logMsg.Append(sbDate.ToString() + ",");
                            content += $"{sbDate.ToString()},";

                            int startIndex = (offset * 17) + 3 + 6;
                            for (int n = startIndex; n < startIndex + 11; n++)
                            {
                                try
                                {
                                    logMsg.Append(dataInfos[n].Value + ",");

                                    content += $"{dataInfos[n].Value},";
                                }
                                catch (Exception)
                                {

                                }
                            }

                            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} [SAVE]序号值：{startVal++}");
                            File.AppendAllText(fileName, content + "\r\n");
                            content = "";
                            offset++;
                        }
                    }
                }
            }
        }

        private void UpdateContinueRecv(CanFrameModel frame)
        {
            if (frame.FrameDatas.Count <= 0 || frame.FrameDatas[0].Data.Length < 20)
                return;

            try
            {
                byte[] datas = frame.FrameDatas[0].Data;
                byte[] bytes = new byte[20];

                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = datas[i + 4];
                }

                SN = System.Text.Encoding.UTF8.GetString(bytes).Replace("\0", ""); //Encoding.ASCII.GetString(bytes);
            }
            catch (Exception ex)
            {
                SN = "00000000000000000000";
            }
        }

        #endregion
    }

    /// <summary>
    /// 文件编码
    /// </summary>
    public class FileCode
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int DataLength { get; set; }
    }
}
