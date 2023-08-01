using CanProtocol.ProtocolModel;
using Communication.Can;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using MessageBox = System.Windows.MessageBox;

namespace SofarHVMExe.ViewModel
{
    public class DownloadPageVm : ViewModelBase
    {
        public DownloadPageVm(EcanHelper? helper)
        {
            ecanHelper = helper;
            Init();
        }

        #region 字段
        private bool stopUpdate = false;
        private bool isUpdating = false;
        private bool startCollect = false;
        private object collectLock = new object();

        private byte[] fileData;
        //private int blockSize = 1024; //数据块大小（byte），不能超过1024B
        private int blockNum = 0;     //数据块个数
        private int realBlockNum = 0; //实际数据块个数
        private int fileDataCrc = 0;  //固件文件数据crc校验值
        private List<byte> blockData = new List<byte>(); //用于收集每个数据块，计算校验
        private List<byte> totalBlockData = new List<byte>(); //用于收集所有数据块，计算校验
        private List<byte[]> fileDataList = new List<byte[]>(); //文件数据集合

        private FileConfigModel? fileCfgModel = null;
        private FrameConfigModel? frameCfgModel = null;
        public EcanHelper? ecanHelper = null;
        private CRCHelper crcHelper = new CRCHelper();
        private CanFrameModel recvFrame = new CanFrameModel();
        private List<CancellationTokenSource> sendCancellList = new List<CancellationTokenSource>(); //发送取消标志
        private CharProgressBar charProgress = new CharProgressBar();
        private Stopwatch timeDeltaSw = new Stopwatch();
        private StringBuilder debugInfoSb = new StringBuilder();
        private DownloadDebugInfoWnd? debugInfoWnd = null;
        private List<CanFrameModel> resultFrameList = new List<CanFrameModel>(); //结果校验帧数据集合
        //private List<LostPackage> lostPackageList = new List<LostPackage>();     //丢包集合

        #endregion

        #region 属性
        private byte targetAddr = 0x0; //目标设备地址，广播0x0，否则为单台升级
        public string TargetAddr
        {
            get => targetAddr.ToString();
            set
            {
                byte v = 0;
                if (byte.TryParse(value, out v))
                {
                    targetAddr = v;
                    OnPropertyChanged();
                }
            }
        }
        private bool isBroadcast = true;
        public bool IsBroadcast //是否广播更新
        {
            get => isBroadcast;
            set
            {
                isBroadcast = value;
                if (value)
                {
                    TargetAddr = "0";
                }
            }
        }

        private string filePath = ""; //文件路径
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged();
            }
        }

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
        private string tmpMsg = "";

        private bool openDebug = true;
        public bool OpenDebug
        {
            get => openDebug;
            set
            {
                openDebug = value;
                OnPropertyChanged();
            }
        }

        private bool jumpDownload = false;
        public bool JumpDownload
        {
            get => jumpDownload;
            set
            {
                jumpDownload = value;
                OnPropertyChanged();
            }
        }

        private bool deleteFFData = true;
        public bool DeleteFFData
        {
            get => deleteFFData;
            set
            {
                deleteFFData = value;
                OnPropertyChanged();
            }
        }

        private int sendInterval = 5; //发送帧间隔，默认5ms
        public int SendInterval
        {
            get => sendInterval;
            set
            {
                sendInterval = value;
                OnPropertyChanged();
            }
        }
        private int maxPackageNum = 10; //最大补包次数
        public int MaxPackageNum
        {
            get => maxPackageNum;
            set
            {
                maxPackageNum = value;
                OnPropertyChanged();
            }
        }
        private int blockSize = 256; //数据块大小（byte），不能超过1024B
        public int BlockSize
        {
            get => blockSize;
            set
            {
                blockSize = value;
                OnPropertyChanged();
            }
        }
        public string BlockNum
        {
            get => blockNum.ToString();
            set
            {
                if (value == "" || value == "0")
                {
                    blockNum = realBlockNum;
                    OnPropertyChanged();
                }
                else
                {
                    int v = 0;
                    if (int.TryParse(value, out v))
                    {
                        blockNum = v;
                        OnPropertyChanged();
                    }
                }
            }
        }
        private List<CommonTreeView> devSource = new List<CommonTreeView>();
        public List<CommonTreeView> DevSource
        {
            get => devSource;
            set
            {
                devSource = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 命令
        public ICommand ImportCommand { get; set; }             //导入
        public ICommand StartDownloadCommand { get; set; }      //开始升级
        public ICommand StopDownloadCommand { get; set; }       //停止升级
        public ICommand ClearMsgCommand { get; set; }           //清除信息
        public ICommand ShowDebugInfoCommand { get; set; }      //显示调试信息对话框
        public ICommand DeleteFFDataCommand { get; set; }      //显示调试信息对话框
        #endregion

        #region 成员方法
        private void Init()
        {
            ImportCommand = new SimpleCommand(ImportFile);
            StartDownloadCommand = new SimpleCommand(StartDownload);
            StopDownloadCommand = new SimpleCommand(StopDownload);
            ClearMsgCommand = new SimpleCommand(ClearMsg);
            ShowDebugInfoCommand = new SimpleCommand(ShowDebugInfo);

            //CAN
            //ecanHelper = CommManager.Instance().GetCanHelper();
            //CommManager.Instance().RegistRecvProc(RecvProcCan1, RecvProcCan2);

            //初始化设备树列表
            InitDeviceTreeView();

            //初始化固件下拉集合
            //List<string> list = new List<string>();
            //list.Add("测试");
            //FirmwareList = list;
        }
        
        private void ImportFile(object o)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择升级文件";
            dlg.Filter = "升级文件(*.bin)|*.bin";
            //dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            FilePath = dlg.FileName;

            int fileLength = 0;
            using (FileStream file = new FileStream(FilePath, FileMode.Open))
            {
                fileLength = (int)file.Length;
                fileData = new byte[fileLength];

                file.Read(fileData, 0, fileLength);
                file.Close();
            }

            fileDataCrc = crcHelper.ComputeCrc16(fileData, fileLength);
            //文件数据块总数：等于文件大小/数据块的大小 或者 文件大小/数据块的大小+1
            blockNum = (fileLength % blockSize == 0) ? (fileLength / blockSize) : (fileLength / blockSize + 1);
            realBlockNum = blockNum;
            BlockNum = blockNum.ToString();
        }
        private void StartDownload(object o)
        {
            if (!CheckCanConnect())
                return;

            if (isUpdating)
                return;

            if (FilePath == null || FilePath == "")
            {
                MessageBox.Show("请先导入文件！", "提示");
                return;
            }

            stopUpdate = false;
            isUpdating = true;
            debugInfoSb.Clear();
            CollectFileData();

            //设置新的log文件
            LogHelper.SubDirectory = "Download";
            LogHelper.CreateNewLogger();
            LogHelper.AddLog("**************** 程序下载日志 ****************");
            
            //开始升级
            Task task1 = Task.Factory.StartNew(() => { Download(); });
        }
        private void Download()
        {
            bool ok = false;
            AddMsg("", false);
            AddMsg("开始进行程序升级✈");

            //1、请求进行固件升级
            AddMsg("发起升级请求"); //1S内读取到该指令，则认为成功
            for (int i = 0; i < 3; i++)
            {
                AddMsg($"请求第{i + 1}次");
                write_firmware_request();

                ///在1S内如果收到正确应答则ok
                {
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    while (true)
                    {
                        if (write_firmware_request_check())
                        {
                            AddMsg($"请求成功");
                            ok = true;
                            break;
                        }

                        if (timer.ElapsedMilliseconds > 1000)
                        {
                            timer.Stop();
                            break;
                        }
                    }
                }

                if (ok)
                    break;
                else
                    AddMsg($"请求失败");
            }//for

            if (!ok)
            {
                AddMsg($"升级失败，请再次尝试！！！☹");
                isUpdating = false;
                return;
            }

            Thread.Sleep(1000);

            //2.下载固件文件
            AddMsg("");
            AddMsg($"开始下载固件文件...");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            AddMsg($"文件大小：{fileData.Length}B");
            AddMsg($"数据块个数：{blockNum}，数据块大小：{blockSize}B");
            tmpMsg = Message;
            //LogHelper.AddLog(Message);

            //blockNum = jumpDownload ? 0 : realBlockNum;
            for (int i = 0; i < blockNum; i++)
            {
                //进度条
                {
                    int percent = (int)((double)(i + 1) / blockNum * 100);
                    string strProgressBar = charProgress.Update(percent);
                    string progress = tmpMsg + $">正在下载：" + strProgressBar;
                    UpdateMsg(progress);
                }

                //下载数据块
                {
                    byte[] dataArr = fileDataList[i];
                    if (dataArr.Length == 0)
                        continue;

                    blockData.Clear(); //一个数据块
                    blockData.AddRange(dataArr);
                    write_firmware_block(targetAddr, i, blockData.ToArray(), SendInterval, true);
                    Thread.Sleep(10);
                }

                if (stopUpdate)
                {
                    AddMsg($"升级失败，已停止更新！！！☹");
                    isUpdating = false;
                    return;
                }
            }//for
            sw.Stop();
            TimeSpan time = sw.Elapsed;
            AddMsg($"固件下载完成：{time.Hours}:{time.Minutes}:{time.Seconds}.{time.Milliseconds}");


            //3.校验固件文件接收结果 并进行补包
            ok = false;
            Thread.Sleep(10);
            file_recieve_ok_check();


            //4、启动固件升级
#if false   //暂时不用
            AddMsg($"启动固件升级");
            update_request();
            Thread.Sleep(100);

            if (update_result_check(ref errMsg))
            {
                AddMsg($"升级成功！！！☺");
            }
            else
            {
                AddMsg($"升级失败：{errMsg} ☹");
            }
#endif

            isUpdating = false;
        }
        private void StopDownload(object o)
        {
            if (!CheckCanConnect())
                return;

            stopUpdate = true;
        }
        private void ClearMsg(object o)
        {
            Message = "";

            debugInfoSb.Clear();
        }
        private void ShowDebugInfo(object o)
        {
            string debugText;
            if (openDebug)
            {
                debugText = debugInfoSb.ToString();
            }
            else
            {
                debugText = "未打开调试，请先勾选调试！";
            }

            if (debugInfoWnd == null)
            {
                debugInfoWnd = new DownloadDebugInfoWnd();
            }

            debugInfoWnd.UpdateInfo(debugText);
            debugInfoWnd.Topmost = true;

            try
            {
                debugInfoWnd.Show();
            }
            catch (Exception ex)
            {
            }
        }
        public void write_firmware_request()
        {
            CanFrameID cid = new CanFrameID();
            cid.Prio = 7;
            cid.FrameType = 2;
            cid.Continueflg = 0;
            cid.FC = 50;    //功能码
            cid.DstType = 1;//PCS
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 2;//CSU-MCU1  010
            cid.SrcAddr = 1;//地址1
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = Convert.ToByte(realBlockNum & 0xff); //文件数据块总数
            send[1] = Convert.ToByte(realBlockNum >> 8);
            send[2] = Convert.ToByte(blockSize & 0xff );    //数据块大小
            send[3] = Convert.ToByte(blockSize >> 8);

            SendFrame(id, send);
            LogHelper.AddLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        public void write_firmware_block(byte dstAddr, int blockIndex, byte[] block, int time, bool lostPackage = false)
        {
            CanFrameID cid = new CanFrameID();
            cid.Continueflg = 1;
            cid.Prio = 7;
            cid.FrameType = 1;
            cid.FC = 52;
            cid.DstType = 1;//PCS
            cid.DstAddr = dstAddr; //目标地址
            cid.SrcType = 2;//CSU-MCU1  010
            cid.SrcAddr = 1;//地址1
            uint id = cid.ID;

            int index = 0;

            //起始帧
            {
                byte[] send = new byte[3];
                send[0] = block[index++];
                send[1] = block[index++];
                send[2] = block[index++];
                write_start_package(blockIndex, id, send);
            }

            //调试 丢包
            //if (lostPackage)
            //{
            //    if (blockIndex == 5 ||
            //        blockIndex == 10 ||
            //        blockIndex == 58 
            //        )
            //    {
            //        return;
            //    }
            //}

            byte[] midData = block.Skip(index).ToArray();
            int totalSize = fileData.Length;
            int count = midData.Length / 7;
            //中间帧
            {
                for (int i = 0; i < count; i++)
                {
                    Thread.Sleep(time);
                    byte[] send = midData.Skip(i*7).Take(7).ToArray();
                    write_mid_package(i + 1, id, send);

                    if (stopUpdate)
                        return;
                }
            }

            //结束帧
            {
                Thread.Sleep(10);
                byte[] send = midData.Skip(count * 7).ToArray();
                if (send.Length == 6)
                {
                    //处理最后一帧数据个数正好为6个的情况
                    //拆分CRC，低字节放到中间帧，高字节放到结束帧
                    int crc = crcHelper.ComputeCrc16(block.ToArray(), block.Length);
                    byte crcL = (byte)(crc & 0xff);
                    List<byte> tmp = new List<byte>(send);
                    tmp.Add(crcL);
                    write_mid_package(count + 1, id, tmp.ToArray());

                    Thread.Sleep(20);
                    byte crcH = (byte)(crc >> 8);
                    send = new byte[] { crcH };
                    write_end_package(id, send, null, false);
                }
                else
                {
                    write_end_package(id, send, block);
                }
            }
        }
        private void write_start_package(int blockIndex, uint id, byte[] data)
        {
            byte[] send = new byte[8];
            send[0] = 0x0; //包序号
            send[1] = (byte)(blockIndex & 0xff);        //数据块序号低位
            send[2] = (byte)((blockIndex >> 8) & 0xff); //数据块序号高位
            send[3] = (byte)(blockSize & 0xff);         //数据长度低位
            send[4] = (byte)((blockSize >> 8) & 0xff);  //数据长度高位
            send[5] = data[0];
            send[6] = data[1];
            send[7] = data[2];

            SendFrame(id, send);

            LogHelper.AddLog($"第{blockIndex}个数据块开始下载");
            LogHelper.AddLog($"[发送]下载起始帧[1]，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        private void write_mid_package(int packageIndex, uint id, byte[] data)
        {
            List<byte> send = new List<byte>();
            send.Add((byte)packageIndex); //包序号
            send.AddRange(data);

            SendFrame(id, send.ToArray());

            LogHelper.AddLog($"[发送]下载中间帧[{packageIndex + 1}]，0x{id.ToString("X")} {BitConverter.ToString(send.ToArray())}");
        }
        private void write_end_package(uint id, byte[] data, byte[] block, bool addCrc = true)
        {
            byte[] send = new byte[8];
            List<byte> tmp = new List<byte>();
            tmp.Add(0xff); //包序号
            tmp.AddRange(data); //数据

            //crc校验
            if (addCrc)
            {
                int crc = crcHelper.ComputeCrc16(block.ToArray(), block.Length);
                tmp.Add((byte)(crc & 0xff));
                tmp.Add((byte)(crc >> 8));
            }

            int len = tmp.Count;
            for (int i = 0; i < len; i++)
            {
                send[i] = tmp[i];
            }

            SendFrame(id, send);

            LogHelper.AddLog($"[发送]下载结束帧[0xff]，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        private void write_firmware_result_request(byte dstAddr)
        {
            CanFrameID cid = new CanFrameID();
            cid.Prio = 7;
            cid.FrameType = 2;
            cid.Continueflg = 0;
            cid.FC = 53;
            cid.DstType = 1;//PCS
            cid.DstAddr = dstAddr; //目标地址
            cid.SrcType = 2;//CSU-MCU1  010
            cid.SrcAddr = 1;//地址1
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = 0xAA;
            send[1] = (byte)(fileDataCrc & 0xff);
            send[2] = (byte)(fileDataCrc >> 8);

            SendFrame(id, send);

            LogHelper.AddLog($"[发送]固件接收结果请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        private void update_request()
        {
            CanFrameID cid = new CanFrameID();
            cid.Prio = 7;
            cid.FrameType = 2;
            cid.Continueflg = 0;
            cid.FC = 54;    //功能码
            cid.DstType = 1;//PCS
            cid.DstAddr = targetAddr; //目标地址
            cid.SrcType = 2;//CSU-MCU1  010
            cid.SrcAddr = 1;//地址1
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = 0x02;

            SendFrame(id, send);

            LogHelper.AddLog($"[发送]启动更新请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }

        #region 接收校验
        private bool write_firmware_request_check()
        {
            CanFrameModel frame = new CanFrameModel(recvFrame);
            uint id = frame.Id;
            if (id == 0x01000000) //去除默认id
                return false;

            if (frame.FrameDatas.Count == 0)
            {
                LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                return false;
            }

            CanFrameData frameDate = frame.FrameDatas[0];
            byte[] data = frameDate.Data;

            uint func_code = (id >> 16) & 0xff;
            if (func_code != 50)
            {
                return false;
            }

            if (data.Length == 0)
                return false;

            LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

            byte resultDesc = data[0];
            if (resultDesc == 0)
                return true;

            return false;
        }
        private bool write_firmware_result_check(ref string errMsg)
        {
            //实际应答id：1F354121
            uint id = recvFrame.Id;
            if (id == 0x01000000) //去除默认id
                return false;

            if (recvFrame.FrameDatas.Count == 0)
            {
                LogHelper.AddLog($"[接收]总接收结果查询应答，0x{id.ToString("X")} 数据为空！)");
                return false;
            }

            CanFrameData frameDate = recvFrame.FrameDatas[0];
            byte[] data = frameDate.Data;
            if (data.Length == 0)
                return false;

            uint func_code = (id >> 16) & 0xff;
            if (func_code != 53)
            {
                LogHelper.AddLog($"[接收]总接收结果查询应答-功能码不为53，错误功能码[{func_code}]，0x{id.ToString("X")} {BitConverter.ToString(data)}");
                return false;
            }

            LogHelper.AddLog($"[接收]总接收结果查询应答，0x{id.ToString("X")} {BitConverter.ToString(data)}");

            byte resultDesc = data[0];
            if (resultDesc == 0)
            {
                return true;
            }
            else if (resultDesc == 1)
            {
                errMsg = "文件校验错误";
                return false;
            }
            else if (resultDesc == 2)
            {
                errMsg = "整包校验错误";
                return false;
            }

            return false;
        }
        private void write_firmware_result_collect()
        {
            CanFrameModel frame = new CanFrameModel(recvFrame);
            uint id = frame.Id;

            //过滤不需要的id
            {
                CanFrameID frameID = new CanFrameID();
                frameID.ID = id;

                if (frameID.FC != 53 ||
                    frameID.FrameType != 3)
                {
                    return;
                }

                if (frame.FrameDatas.Count == 0 ||
                    frame.FrameDatas[0].Data.Length < 5)
                {
                    return;
                }
            }

            resultFrameList.Add(frame);

            LogHelper.AddLog($"[接收]固件接收结果应答帧，0x{id.ToString("X")} {BitConverter.ToString(frame.FrameDatas[0].Data)}");
        }
        private bool file_recieve_ok_check()
        {
            string errMsg = "";
            AddMsg($"校验固件接收结果");

            //1、发送接收结果请求
            write_firmware_result_request(targetAddr);

            //2、指定时间内收集一台或多台pcs设备结果校验帧
            resultFrameList.Clear();
            ///开启接收结果校验
            {
                lock (this.collectLock)
                {
                    startCollect = true;
                }

                Thread.Sleep(2000);

                lock (this.collectLock)
                {
                    startCollect = false;
                }
            }

            //3、解析结果校验帧 是否有丢包
            bool hasLostPackage = false;
            List<LostPackage> lostPackageList = new List<LostPackage>();
            {
                foreach (CanFrameModel frame in resultFrameList)
                {
                    uint id = frame.Id;
                    byte[] datas = frame.FrameDatas[0].Data;
                    if (AnalyseLostPackage(id, datas, lostPackageList))
                    {
                        hasLostPackage = true;
                    }
                }
            }

            //4、有设备丢包 进行补包
            if (hasLostPackage)
            {
                AddMsg("检测到有丢包！");
                LogHelper.AddLog("检测到有丢包！");

                CanFrameID frameId = new CanFrameID();
                int num = lostPackageList.Count;
                for (int i = 0; i < num; i++)
                {
                    //对每一个设备进行循环补包（指定补包次数）
                    bool ok = false;
                    LostPackage package = lostPackageList[i];
                    for (int j = 0; j < maxPackageNum; j++)
                    {
                        frameId.ID = package.id;
                        LostPackage resultPackage = SendLostPackage(package);
                        if (resultPackage != null)
                        {
                            if (resultPackage.resultType == 0)
                            {
                                ok = true;
                                AddMsg($"校验成功");
                                break;
                            }
                            else if (resultPackage.resultType == 1)
                            {
                                //继续补包
                                AddMsg($"检测到有丢包！");
                                package = resultPackage;
                                ok = false;
                            }
                            else if (resultPackage.resultType == 2)
                            {
                                AddMsg($"整包校验不通过！");
                                ok = false;
                                break;
                            }
                            else
                            {
                                //不可预期的其他情况，一般不会出现
                            }
                        }
                    }//for

                    if (ok)
                    {
                        AddMsg($"设备[{frameId.SrcAddr}]升级成功！！！☺");
                    }
                    else
                    {
                        AddMsg($"设备[{frameId.SrcAddr}]升级失败！！！☹");
                    }
                }//for

                return false;
            }
            else
            {
                AddMsg($"校验成功");
                resultFrameList.ForEach((frameModel) =>
                {
                    byte addr = frameModel.FrameId.SrcAddr;
                    AddMsg($"设备[{addr}]升级成功！！！☺");
                });
                
                return true;
            }
        }//func

        private LostPackage SendLostPackage(LostPackage package)
        {
            bool ok = false;
            LostPackage resultPackage = null;
            CanFrameID frameId = new CanFrameID();
            uint packageId = package.id;
            frameId.ID = packageId;
            byte dstAddr = frameId.SrcAddr;

            //1、有设备丢包 进行补包
            string strIndex = "";
            foreach (int index in package.blockIdxList)
            {
                strIndex += index.ToString() + " ";
            }

            AddMsg($"设备[{dstAddr}]丢失数据块序号: {strIndex}");
            AddMsg("开始进行补包...");
            LogHelper.AddLog($"对设备[{dstAddr}]进行补包，丢失数据块序号: {strIndex}");

            List<int> blockIdxList = package.blockIdxList;

            try
            {
                foreach (int blockIndex in blockIdxList)
                {
                    write_firmware_block(dstAddr, blockIndex, fileDataList[blockIndex], SendInterval);
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                int stop = 10;
            }
            AddMsg($"设备[{dstAddr}]补包完成！");
            LogHelper.AddLog($"设备[{dstAddr}]补包完成！");

            //2、发送接收结果请求
            AddMsg($"校验固件接收结果");
            write_firmware_result_request(dstAddr);

            //3、指定时间内收集一台或多台pcs设备结果校验帧
            resultFrameList.Clear();
            ///开启接收结果校验
            {
                lock (this.collectLock)
                {
                    startCollect = true;
                }

                Thread.Sleep(2000);

                lock (this.collectLock)
                {
                    startCollect = false;
                }
            }

            //4、解析结果校验帧 是否有丢包
            List<LostPackage> lostPackageList = new List<LostPackage>();
            foreach (CanFrameModel frame in resultFrameList)
            {
                uint id = frame.Id;
                if (id != packageId)
                    continue;

                byte[] datas = frame.FrameDatas[0].Data;
                AnalyseLostPackage(id, datas, lostPackageList);
            }

            resultPackage = lostPackageList.Find((o) =>
            {
                return (o.id == packageId);
            });

            return resultPackage;
        }//func

        private bool file_recieve_ok_check_old()
        {
            string errMsg = "";
            bool ok = false;
            AddMsg($"开始校验固件文件接收结果");

            for (int i = 0; i < 3; i++)
            {
                AddMsg($"校验第{i + 1}次");
                write_firmware_result_request(0);
                ///在1S内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    if (write_firmware_result_check(ref errMsg))
                    {
                        AddMsg($"校验成功");
                        ok = true;
                        break;
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
                {
                    AddMsg($"校验失败：{errMsg}");
                }

                if (stopUpdate)
                {
                    AddMsg($"停止更新");
                    isUpdating = false;
                    return false;
                }
            }

            return false;
        }//func
        private bool AnalyseLostPackage(uint id, byte[] datas, List<LostPackage> lostPackageList)
        {
            byte resultDesc = datas[0];
            LostPackage lostPackage = new LostPackage();
            lostPackage.id = id;
            lostPackage.resultType = resultDesc; //为1时：丢包

            if (resultDesc == 0)
            {
                lostPackageList.Add(lostPackage);
                return false;
            }
            else if (resultDesc == 2)
            {
                ///整包校验不过时，不收集数据块索引
                lostPackageList.Add(lostPackage);
                return true;
            }

            if (datas[1] == 0 && datas[2] == 0 &&
                datas[3] == 0 && datas[4] == 0)
            {
                return false;
            }

            //1、收集丢失数据块索引
            int blockGroup = datas[1];
            int blockIndex = (int)(datas[2] | (datas[3] << 8) | (datas[4] << 16));

            for (int i = 0; i < 24; i++)
            {
                int index = (blockIndex & (0x1 << i)) >> i;
                if (index == 1)
                {
                    int tmp = blockGroup * 24 + i;
                    lostPackage.blockIdxList.Add(tmp);
                }
            }

            //2、将丢的包加入到集合中
            LostPackage? findPackage = lostPackageList.Find((o) =>
            {
                return (o.id == id);
            });

            if (findPackage != null)
            {
                //相同id的设备
                findPackage.blockIdxList.AddRange(lostPackage.blockIdxList);
            }
            else
            {
                lostPackageList.Add(lostPackage);
            }

            return true;
        }
        #endregion


        #region CAN操作
        /// <summary>
        /// 初始化Can设置
        /// </summary>
        public void InitCanHelper()
        {
            //接收处理
            ecanHelper.OnReceiveCan1 += RecvProcCan1;
            ecanHelper.OnReceiveCan2 += RecvProcCan2;
        }
        /// <summary>
        /// 检查Can连接
        /// </summary>
        /// <returns></returns>
        private bool CheckCanConnect()
        {
            if (!ecanHelper.IsConnected)
            {
                MessageBox.Show("未连接CAN设备，请先连接后再进行操作！", "提示");
                return false;
            }

            if (!ecanHelper.IsCan1Start && !ecanHelper.IsCan2Start)
            {
                MessageBox.Show("CAN通道未打开！", "提示");
                return false;
            }

            return true;
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
        private void RecvFrame(CAN_OBJ recvData)
        {
            string strId = recvData.ID.ToString("X8");
            if (strId.Contains("197F")) //过滤dsp的app心跳
                return;

            recvFrame.Id = recvData.ID;

            CanFrameData frameData = new CanFrameData();
            for (int i = 0; i < recvData.DataLen; i++)
            {
                frameData.Data[i] = recvData.Data[i];
            }

            recvFrame.FrameDatas.Clear();
            recvFrame.FrameDatas.Add(frameData);

            lock (this.collectLock)
            {
                if (startCollect)
                {
                    write_firmware_result_collect();
                }
            }
        }
        /// <summary>
        /// 通道1接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan1(CAN_OBJ recvData)
        {
            //if (GlobalManager.Instance().CurrentPage == GlobalManager.Page.Download)
            if (isUpdating)
                RecvFrame(recvData);
        }
        /// <summary>
        /// 通道2接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            //if (GlobalManager.Instance().CurrentPage == GlobalManager.Page.Download)
            if (isUpdating)
                RecvFrame(recvData);
        }
        #endregion

        /// <summary>
        /// 初始化树列表集合
        /// </summary>
        /// <returns></returns>
        public void InitDeviceTreeView()
        {
            List<CommonTreeView> list = new List<CommonTreeView>();
            CommonTreeView newItem = new CommonTreeView("全选");
            list.Add(newItem);

            for (int i = 0; i < 8; i++)
            {
                CommonTreeView item = new CommonTreeView(i.ToString());
                newItem.CreateTreeWithChildre(item);
            }

            DevSource = list;
        }
        /// <summary>
        /// 获取选中的设备集合
        /// </summary>
        public List<string> GetSelectDevices()
        {
            List<string> selectDevList = new List<string>(); //选中的设备名称集合
            foreach (CommonTreeView tree in DevSource)
            {
                GetCheckedItems(tree, ref selectDevList);
            }

            return selectDevList;
        }
        /// <summary>
        /// 获取选中项
        /// </summary>
        /// <param name="tree"></param>
        private void GetCheckedItems(CommonTreeView tree, ref List<string> selectDevList)
        {
            if (tree.Parent != null && (tree.Children == null || tree.Children.Count == 0))
            {
                if (tree.IsChecked.HasValue && tree.IsChecked == true)
                    selectDevList.Add(tree.NodeName);
            }
            else if (tree.Children != null && tree.Children.Count > 0)
            {
                foreach (CommonTreeView tv in tree.Children)
                    GetCheckedItems(tv, ref selectDevList);
            }
        }
        private void CollectFileData()
        {
            fileDataList.Clear();

            for (int i = 0; i < blockNum; i++)
            {
                byte[] dataArr = fileData.Skip(i * blockSize).Take(blockSize).ToArray();

                //过滤字节全为FF的数据块，但是数据块索引不变
                if (deleteFFData)
                {
                    bool hasValidData = false;
                    foreach (byte d in dataArr)
                    {
                        if (d != 0xFF)
                        {
                            hasValidData = true;
                            break;
                        }
                    }

                    if (!hasValidData)
                    {
                        fileDataList.Add(new byte[]{ });
                        continue;
                    }
                }

                fileDataList.Add(dataArr);
            }
        }
        private void UpdateMsg(string msg)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Message = msg;
            });
        }
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
        private void AddDebugInfo(string info, bool addArrow = true, bool isUpdate = false)
        {
            if (addArrow)
            {
                info = ">" + info + "\r\n";
            }
            else
            {
                info = info + "\r\n";
            }

            if (isUpdate)
            {
                debugInfoSb.Clear();
            }

            debugInfoSb.Append(info);
        }
        #endregion
    }//class

    /// <summary>
    /// 丢包对象
    /// </summary>
    public class LostPackage
    {
        public uint id;
        public List<int> blockIdxList = new List<int>(); //丢失的数据块索引集合
        public int resultType = 0; //校验结果返回类型 0:校验ok  1:数据块校验错误   2:整包校验错误
    }

    /// <summary>
    /// 字符串进度条类
    /// </summary>
    public class CharProgressBar
    {
        public int Total = 50;

        public string CompleteChar = "▉"; //'+';
        public string UncompleteChar = "  ";
        //public 
        //private uint progress = 0; //0~100
        //public uint Progress
        //{
        //    get => progress;
        //    set
        //    {
        //        if (value >= 0 && value < 101)
        //        {
        //            progress = value;
        //        }
        //    }
        //}


        public string Update(int progress, bool addNewLine = true)
        {
            StringBuilder sb = new StringBuilder("", Total * 2);

            int complete = (int)((double)progress / 100 * Total);
            int uncomplete = Total - complete;

            for (int i = 0; i < complete; i++)
            {
                sb.Append(CompleteChar);
            }

            for (int i = 0; i < uncomplete; i++)
            {
                sb.Append(UncompleteChar);
            }

            string strProgress = sb.ToString();
            strProgress = /*"[" +*/ strProgress + $" {progress}%";
            if (addNewLine)
            {
                strProgress += "\r\n";
            }
            return strProgress;
        }
    }//class
}
