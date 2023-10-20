using CanProtocol.ProtocolModel;
using Communication.Can;
using FontAwesome.Sharp;
using Org.BouncyCastle.Crypto.Paddings;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using static log4net.Appender.RollingFileAppender;
using MessageBox = System.Windows.MessageBox;
using SofarHVMExe.Util.TI;
using System.Windows;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.OpenXmlFormats;
using Org.BouncyCastle.Ocsp;

namespace SofarHVMExe.ViewModel
{
    class DownloadPageVm : ViewModelBase
    {
        public DownloadPageVm(EcanHelper? helper)
        {
            ecanHelper = helper;
            Init();
        }

        #region 字段
        private bool isUpdating = false;
        private bool startCollect = false;
        private object collectLock = new object();
        private byte[] fileData;
        private int fileDataCrc = 0;  //固件文件数据crc校验值
        private int realBlockNum = 0; //实际数据块个数
        private int validBlockNum = 0;//有效的数据块个数（不全是0xFF的数据）
        private const int KeepSignatureBytes = 1024 * 1 + 64; //固件数据最后的签名数据必须保留
        private List<byte> blockData = new List<byte>(); //用于收集每个数据块，计算校验
        private List<byte> totalBlockData = new List<byte>(); //用于收集所有数据块，计算校验
        private List<byte[]> fileDataList = new List<byte[]>(); //文件数据集合

        public EcanHelper? ecanHelper = null;
        private FileConfigModel? fileCfgModel = null;
        private FrameConfigModel? frameCfgModel = null;
        private CanFrameModel recvFrame = new CanFrameModel();
        private List<CancellationTokenSource> sendCancellList = new List<CancellationTokenSource>(); //发送取消标志
        private CharProgressBar charProgress = new CharProgressBar();
        private Stopwatch timeDeltaSw = new Stopwatch();
        private StringBuilder debugInfoSb = new StringBuilder();
        private DownloadDebugInfoWnd? debugInfoWnd = null;
        private List<CanFrameModel> resultFrameList = new List<CanFrameModel>(); //结果校验帧数据集合
        private List<CAN_OBJ> _FramesList = new List<CAN_OBJ>();
        private List<int> _multiPackData = new List<int>();
        uint firmwareCrc = 0;
        FirmwareModel currentModel = null;
        List<FirmwareModel> firmwareModels = new List<FirmwareModel>();
        Dictionary<byte, string> fileTypeDic = new Dictionary<byte, string>()
        {
            { 0x00, "app" },
            { 0x01, "core" },
            { 0x02, "kernel" },
            { 0x03, "rootfs" },
            { 0x04, "safety" },
            { 0x80, "pack" },
        };
        Dictionary<byte, string> chipCodeDic = new Dictionary<byte, string>()
        {
            { 0x01, "旧格式_ARM" },
            { 0x02, "旧格式_DSPM" },
            { 0x03, "旧格式_DSPS" },
            { 0x04, "旧格式_FUSE" },
            { 0x05, "旧格式_BMS" },
            { 0x06, "旧格式_PCU" },
            { 0x07, "旧格式_BDU" },
            { 0x08, "旧格式_WIFI" },
            { 0x09, "旧格式_蓝牙" },
            { 0x10, "旧格式_AFCI" },
            { 0x11, "旧格式_DCDC" },
            { 0x12, "旧格式_MPPT" },
            { 0x21, "ARM" },
            { 0x22, "DSPM" },
            { 0x23, "DSPS" },
            { 0x24, "BMS/BCU" },
            { 0x25, "PCU" },
            { 0x26, "BDU" },
            { 0x27, "DCDC" },
            { 0x28, "DCDC-ARM" },
            { 0x29, "DCDC-DSP" },
            { 0x2D, "BMU" },
            { 0x30, "PCS-M" },
            { 0x31, "PCS-S" },
            { 0x32, "PCS-C" },
            { 0x33, "CSU-MCU1" },
            { 0x34, "CSU-MCU2" },
            { 0x38, "DCDC-M" },
            { 0x39, "DCDC-S" },
            { 0x3A, "DCDC-C" },
            { 0x3B, "CMU-MCU1" },
            { 0x3C, "CMU-MCU2" },
            { 0x41, "FUSE" },
            { 0x42, "AFCI" },
            { 0x43, "MPPT" },
            { 0x61, "WIFI" },
            { 0x62, "BT" },
            { 0x63, "PLC-CCO" },
            { 0x64, "PLC-STA" },
            { 0x65, "SUB1G-GW" },
            { 0x66, "SUB1G-ST" },
            { 0x67, "HUB-MCU1" },
            { 0x68, "HUB-MCU2" },
            { 0x80, "CSU" },
            { 0x81, "TFC" },
            { 0x82, "SSM" },
            { 0x83, "GFD" },
            { 0x88, "COM" },
            { 0x90, "D2D" },
            { 0x91, "PFC" },
        };
        static AutoResetEvent eventResetEvent = new AutoResetEvent(false);
        private int upgradeExecuteResult = -1;
        System.Timers.Timer t1 = new System.Timers.Timer(500); //启动查询状态的定时器
        #endregion

        #region 属性
        public int TempInterval { get; set; } = 100;

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
        private string firmwareFilePath = "";
        public string FirmwareFilePath
        {
            get => firmwareFilePath;
            set
            {
                firmwareFilePath = value;
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
        private int sendInterval = 3; //发送帧间隔，默认5ms
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
        private int blockNum = 0;      //数据块个数
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
        private short? chipRole = null; //芯片角色
        public short? ChipRole
        {
            get => chipRole;
            set
            {
                chipRole = value;
                OnPropertyChanged();
            }
        }
        private string chipNumber = "S3"; //芯片编码E0
        public byte[] ChipNumber
        {
            get
            {
                return ASCIIEncoding.Default.GetBytes(chipNumber);
            }
        }
        private short fileNumber = 0x80;//文件类型编码 0-APP,1-CORE(注销),0x80-SOFAR
        public short FileNumber
        {
            get => fileNumber;
            set
            {
                fileNumber = value == 0 ? value : (short)0x80;

                OnPropertyChanged();
            }
        }
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
        private string buttonText = "开始更新";
        public string ButtonText
        {
            get => buttonText;
            set
            {
                buttonText = value;
                OnPropertyChanged();
            }
        }


        private bool buttonTextVisible = true;
        public bool ButtonTextVisible
        {
            get => buttonTextVisible;
            set
            {
                buttonTextVisible = value;
                OnPropertyChanged();
            }
        }
        private string myDateTime;  //升级时间
        public string MyDateTime
        {
            get { return myDateTime; }
            set
            {
                myDateTime = value;
                OnPropertyChanged();
            }
        }
        /*public List<string> FirmwareList
        {
            get { return new List<string> { "PCS", "BCU", "BMU" }; }
        }*/
        /// <summary>
        /// 当前升级的固件下标
        /// </summary>
        private int firmwareIndex = 0;
        public int FirmwareIndex
        {
            get => firmwareIndex;
            set
            {
                firmwareIndex = value;
                /*switch (firmwareIndex)
                {
                    case 0:
                        IsTiming = Visibility.Hidden;
                        DeleteFFData = true;
                        break;
                    case 1:
                        IsTiming = Visibility.Visible;
                        DeleteFFData = false;
                        ChipRole = 0x24;

                        break;
                    case 2:
                        IsTiming = Visibility.Visible;
                        DeleteFFData = false;
                        ChipRole = 0x2D;
                        break;
                }*/

                if (firmwareIndex <= 1)
                {
                    IsTiming = Visibility.Visible;
                    DeleteFFData = true;
                }
                else
                {
                    IsTiming = Visibility.Hidden;
                    DeleteFFData = false;
                }

                OnPropertyChanged();
            }
        }
        public DstAndSrcType[] dstAndSrcTypes
        {
            //get { return typeKey[FirmwareIndex].ToArray(); }
            get
            {
                int index = FirmwareIndex <= 1 ? 0 : FirmwareIndex - 1;

                return typeKey[index].ToArray();
            }
        }
        private Dictionary<int, List<DstAndSrcType>> typeKey = new Dictionary<int, List<DstAndSrcType>>()
        {
            {0, new List<DstAndSrcType>() {DstAndSrcType.PCS,DstAndSrcType.MUC1 } },
            {1, new List<DstAndSrcType>() {DstAndSrcType.BCU, DstAndSrcType.PCS } },
            {2, new List<DstAndSrcType>() {DstAndSrcType.BMU, DstAndSrcType.PCS } }
        };
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

        /// <summary>
        /// 当前升级对象集合
        /// </summary>
        public Dictionary<int, bool> UpDeviceDic = new Dictionary<int, bool>();
        #endregion

        #region 命令
        public ICommand ImportCommand { get; set; }             //导入
        public ICommand StartDownloadCommand { get; set; }      //开始升级
        public ICommand ClearMsgCommand { get; set; }           //清除信息
        public ICommand ShowDebugInfoCommand { get; set; }      //显示调试信息对话框
        public ICommand DeleteFFDataCommand { get; set; }      //显示调试信息对话框
        public ICommand TimeChangeCmd { get; set; }             //时间触发命令

        #endregion

        #region 成员方法
        private void Init()
        {
            ImportCommand = new SimpleCommand(ImportFile);
            StartDownloadCommand = new SimpleCommand(StartDownload);
            ClearMsgCommand = new SimpleCommand(ClearMsg);
            ShowDebugInfoCommand = new SimpleCommand(ShowDebugInfo);
            //初始化设备树列表
            InitDeviceTreeView();
        }

        private void ImportFile(object o)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择升级文件";
            dlg.Filter = "固件文件|*.bin;*.out;*.sofar";
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var saveDir = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "FirmwareUpdate"));

                // 清除过期文件
                foreach (var fileInfo in saveDir.GetFiles())
                {
                    fileInfo.Delete();
                }

                // .out转.bin
                string realBinPath = dlg.FileName;
                if (System.IO.Path.GetExtension(dlg.FileName) == ".out")
                {
                    // 复制文件
                    string objFileName = System.IO.Path.GetFileName(dlg.FileName);
                    string copyFilePath = System.IO.Path.Combine(saveDir.FullName, objFileName);
                    System.IO.File.Copy(dlg.FileName, copyFilePath);

                    string hexSavePath = System.IO.Path.Combine(saveDir.FullName, System.IO.Path.GetFileNameWithoutExtension(copyFilePath) + ".hex");
                    string binSavePath = System.IO.Path.Combine(saveDir.FullName, System.IO.Path.GetFileNameWithoutExtension(copyFilePath) + ".bin");

                    FirmwareFilePath = "加载中...";

                    try
                    {
                        TIPluginHelper.ConvertCoffToHexAsync(copyFilePath, hexSavePath, saveDir.FullName).Wait();
                        TIPluginHelper.ConvertHexToBinAsync(hexSavePath, binSavePath, saveDir.FullName).Wait();
                        FirmwareFilePath = dlg.FileName;
                        realBinPath = binSavePath;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        FirmwareFilePath = "";
                        return;
                    }
                }
                else
                {
                    FirmwareFilePath = dlg.FileName;
                }

                // 读取文件
                int fileLength = 0;
                FileStream file = new FileStream(realBinPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                {
                    fileLength = (int)file.Length;
                    fileData = new byte[fileLength];
                    file.Read(fileData, 0, fileLength);
                    file.Close();
                }

                // 文件CRC、底层未使用
                fileDataCrc = CRCHelper.ComputeCrc16(fileData, fileLength);

                // 获取固件升级包芯片角色等信息
                if (System.IO.Path.GetExtension(dlg.FileName) == ".sofar")
                {
                    firmwareCrc = CRCHelper.ComputeCrc32(fileData, fileLength);
                    this.AnalysisData(fileData);

                    //检查固件文件包
                    {
                        bool isChiprole = true;
                        for (int i = 0; i < firmwareModels.Count; i++)
                        {
                            byte _chipRole = firmwareModels[i].FirmwareChipRole;
                            if (i == 0)
                            {
                                ChipRole = _chipRole;
                            }
                            else
                            {
                                if (ChipRole != _chipRole)
                                {
                                    isChiprole = false;
                                    break;
                                }
                            }
                        }

                        if (!isChiprole)
                        {
                            MessageBox.Show("选择的升级固件类型与导入文件不匹配！", "导入失败");
                            return;
                        }
                    }
                }

                //文件数据块总数：等于文件大小/数据块的大小 或者 文件大小/数据块的大小+1
                blockNum = (fileLength % blockSize == 0) ? (fileLength / blockSize) : (fileLength / blockSize + 1);
                BlockNum = blockNum.ToString();

                if (blockNum == 0)
                {
                    MessageBox.Show("导入失败");
                    FirmwareFilePath = "";
                    return;
                }

                realBlockNum = blockNum;
                MessageBox.Show($"文件路径：{FirmwareFilePath}", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void StartDownload(object o)
        {
            if (!CheckConnect())
                return;

            if (isUpdating)
            {
                isUpdating = false;
                ButtonText = "开始更新";
                // 设置界面按钮 10s 内不能点击
                {
                    ButtonTextVisible = false;
                    Task.Run(() =>
                    {
                        Thread.Sleep(1000 * 10);
                        ButtonTextVisible = true;
                    });
                }
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    if (string.IsNullOrEmpty(FirmwareFilePath))
                    {
                        MessageBox.Show("请先导入文件！", "提示");
                        return;
                    }

                    /*bool isChiprole = true;
                    foreach (var item in firmwareModels)
                    {
                        if (item.FirmwareChipRole != ChipRole)
                        {
                            isChiprole = false;
                            break;
                        }
                    }

                    if (!isChiprole)
                    {
                        MessageBox.Show("选择的升级固件类型与导入文件不匹配！", "提示");
                        return;
                    }*/

                    if (!isBroadcast)
                        targetAddr = DeviceManager.Instance().GetSelectDev();

                    isUpdating = true;
                    ButtonText = "停止更新";

                    // 设置界面按钮 10s 内不能点击
                    // 设置界面按钮 10s 内不能点击
                    {
                        ButtonTextVisible = false;
                        Task.Run(() =>
                        {
                            Thread.Sleep(1000 * 10);
                            ButtonTextVisible = true;
                        });
                    }

                    debugInfoSb.Clear();
                    CollectFileData();

                    //设置新的log文件
                    LogHelper.SubDirectory = "Download";
                    LogHelper.CreateNewLogger();
                    LogHelper.AddLog("**************** 程序下载日志 ****************");

                    //开始升级
                    Task.Factory.StartNew(() => { Update(); });
                });
            }
        }
        private void Update()
        {
            bool ok = false;
            AddMsg("", false);
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  开始进行程序升级✈");

            this.UpDeviceDic.Clear(); //清除升级对象
            foreach (var item in DeviceManager.Instance().Devices)
            {
                if (item.Connected)
                {
                    // 当前已经连接的对象
                    this.UpDeviceDic.Add(Convert.ToInt32(item.address), false);
                }
            }

            //1、请求进行固件升级--握手
            if (!ShakeHandsV2())
            {
                AddMsg($"升级失败，请再次尝试！！！☹");
                ButtonText = "开始更新";
                isUpdating = false;
                return;
            }

            Thread.Sleep(50);

            //2.下载固件文件
            AddMsg("");
            AddMsg($"开始下载固件文件...");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            AddMsg($"文件大小：{fileData.Length}B");
            AddMsg($"数据块个数：{blockNum}，数据块大小：{blockSize}B");
            tmpMsg = Message;
            AddDebugInfo("");

            int downloadNum = 0;
            for (int i = 0; i < blockNum; i++)
            {
                byte[] dataArr = fileDataList[i];
                if (dataArr.Length == 0)
                    continue;

                //进度条
                {
                    downloadNum++;
                    int percent = (int)((double)(downloadNum) / (double)validBlockNum * 100.0);
                    string strProgressBar = charProgress.Update(percent);
                    string progress = tmpMsg + $">正在下载：" + strProgressBar;
                    UpdateMsg(progress);
                }

                //下载数据块
                {
                    blockData.Clear(); //一个数据块
                    blockData.AddRange(dataArr);
                    write_firmware_block(targetAddr, i, blockData.ToArray(), SendInterval, true);
                    Thread.Sleep(20);
                }

                if (!isUpdating)
                {
                    AddMsg($"升级失败，已停止更新！！！☹");
                    ButtonText = "开始更新";
                    isUpdating = false;
                    return;
                }
            }//for
            sw.Stop();
            TimeSpan time = sw.Elapsed;
            AddMsg($"固件下载完成：{time.Hours}:{time.Minutes}:{time.Seconds}.{time.Milliseconds}");

            //3.校验固件文件接收结果 并进行补包
            ok = false;
            if (file_recieve_ok_check())
            {
                //4.启动升级请求帧
                if (FirmwareIndex > 1)
                {
                    AddMsg("");
                    if (!StartUpgrade())
                    {
                        AddMsg($"升级失败，请再次尝试！！！☹");
                        ButtonText = "开始更新";
                        isUpdating = false;
                        return;
                    }

                    //判断是否需要继续查询
                    if (upgradeExecuteResult == 3)
                    {
                        if (QueryUpgrade() && upgradeExecuteResult == 0)
                        {
                            t1.Stop();
                            isUpdating = false;
                            ButtonText = "开始更新";
                            AddMsg($"已完成本轮升级操作☺ ☺ ☺");
                        }
                    }
                }
                else
                {
                    isUpdating = false;
                    ButtonText = "开始更新";
                    AddMsg($"已完成本轮升级操作☺ ☺ ☺");
                }
            }
            else
            {
                AddMsg($"升级失败，请再次尝试！！！☹");
                ButtonText = "开始更新";
                isUpdating = false;
                return;
            }
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
                debugInfoWnd.Topmost = true;
            }

            debugInfoWnd.UpdateInfo(debugText);

            try
            {
                debugInfoWnd.Show();
            }
            catch (Exception ex)
            {
            }
        }
        public bool ShakeHands()
        {
            bool ok = false;
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  发起升级请求");
            for (int i = 0; i < MaxPackageNum; i++)
            {
                AddMsg($"请求第{i + 1}次");
                write_firmware_request();
                Thread.Sleep(150 * (i + 1));

                ///在1000ms内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    try
                    {
                        if (write_firmware_request_check())
                        {
                            AddMsg($"请求成功");
                            ok = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"请求错误" + ex.Message);
                    }

                    if (timer.ElapsedMilliseconds > 1000)
                    {
                        timer.Stop();
                        break;
                    }
                }

                if (ok)
                    break;
                else
                    AddMsg($"请求失败");
            }//for

            return ok;
        }

        /// <summary>
        /// 握手操作
        /// </summary>
        /// <returns></returns>
        public bool ShakeHandsV2()
        {
            bool ok = false;
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  发起升级请求");

            int objCount = UpDeviceDic.Count;

            // 遍历当前所有的请求对象
            for (int i = 0; i < MaxPackageNum; i++)
            {
                this.IsRecieveList = true;
                this.CanRecieveList.Clear();

                AddMsg($"请求第{i + 1}次");
                write_firmware_request();
                Thread.Sleep(2000);

                ///在 2s 内如果收到正确应答则ok
                write_firmware_request_check_ShakeHandsV2();

                ok = UpDeviceDic.Values.All(t => t);

                if (ok)
                {
                    AddMsg($"请求成功");
                    return ok;
                }
                this.IsRecieveList = false;
            }//for

            AddMsg($"请求失败");
            return ok;
        }
        public bool StartUpgrade()
        {
            Thread.Sleep(500);

            bool ok = false;
            MaxPackageNum = 5;
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  发起启动升级固件请求");
            write_upgrade_execute_request();

            ///在1000ms内如果收到正确应答则ok
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (true)
            {
                if (write_upgrade_execute_request_check())
                {
                    AddMsg($"请求成功{upgradeExecuteResult}");
                    ok = true;
                    break;
                }

                if (timer.ElapsedMilliseconds > 1000)
                {
                    timer.Stop();
                    break;
                }
            }

            return ok;
        }
        public bool QueryUpgrade()
        {
            bool ok = false;
            MaxPackageNum = 5;
            upgradeExecuteResult = -1;
            AddMsg($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}  发起查询升级固件请求");
            for (int i = 0; i < 90; i++)
            {
                write_upgrade_execute_request(0x01);
                Thread.Sleep(1000);

                ///在1000ms内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    if (write_upgrade_execute_request_check())
                    {
                        if (upgradeExecuteResult == 0)
                        {
                            AddMsg($"请求成功");
                            ok = true;
                        }

                        break;
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
        }
        public bool CheckDateTime()
        {
            bool ok = false;
            AddMsg("发起升级时间请求");
            for (int i = 0; i < 3; i++)
            {
                AddMsg($"请求第{i + 1}次");

                string[] strTime = MyDateTime.Split('-', ':', ' ');
                byte[] upgradeTime = new byte[6];
                upgradeTime[0] = Convert.ToByte(Convert.ToInt32(strTime[0]) - 2000);
                upgradeTime[1] = Convert.ToByte(strTime[1]);
                upgradeTime[2] = Convert.ToByte(strTime[2]);
                upgradeTime[3] = Convert.ToByte(strTime[3]);
                upgradeTime[4] = Convert.ToByte(strTime[4]);
                upgradeTime[5] = Convert.ToByte(strTime[5]);
                write_upgrade_time_request(true, upgradeTime);
                Thread.Sleep(150);

                ///在1000ms内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    if (write_upgrade_time_request_check())
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

                if (ok)
                    break;
                else
                    AddMsg($"请求失败");
            }//for

            return ok;
        }
        private bool CheckSdsp(byte dstAddr, int blockSize)
        {
            for (int i = 0; i < 3; i++)
            {
                //一块数据发完后延时200ms再发功能码51的问询
                Thread.Sleep(TempInterval);

                //发起数据块接收确认数据帧请求
                write_firmware_block_request(dstAddr, blockSize);

                ///在1000ms内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    try
                    {
                        if (write_firmware_block_response())
                        {
                            return true;
                        }
                        else
                        {
                            Thread.Sleep(TempInterval);
                            write_firmware_block_request(dstAddr, blockSize);//发起数据块接收确认数据帧请求
                        }
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"请求错误" + ex.Message);
                    }

                    if (timer.ElapsedMilliseconds > 1000)
                    {
                        timer.Stop();
                        break;
                    }
                }
            }

            return false;
        }
        private void write_firmware_block_request(byte dstAddr, int blockSize)
        {
            CanFrameID cid = new CanFrameID();
            cid.Priority = 7;
            cid.FrameType = 2;
            cid.ContinuousFlag = 0;
            cid.FC = 51;
            cid.DstType = Convert.ToByte(dstAndSrcTypes[0]);
            cid.DstAddr = targetAddr;
            cid.SrcType = Convert.ToByte(dstAndSrcTypes[1]);
            cid.SrcAddr = 1;
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = 0x00;
            send[1] = 0x00;
            send[2] = Convert.ToByte(blockSize & 0xff);    //数据块大小
            send[3] = Convert.ToByte(blockSize >> 8);
            if (openDebug)
            {
                AddDebugInfo("");
                AddDebugInfo($"发送请求帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(send)}");
            }
            SendFrame(id, send);
            LogHelper.AddLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        public void write_firmware_request()
        {
            CanFrameID cid = new CanFrameID();
            cid.Priority = 7;
            cid.FrameType = 2;
            cid.ContinuousFlag = 0;
            cid.FC = 50;
            cid.DstType = Convert.ToByte(dstAndSrcTypes[0]);
            cid.DstAddr = targetAddr;
            cid.SrcType = Convert.ToByte(dstAndSrcTypes[1]);
            cid.SrcAddr = 1;
            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = Convert.ToByte(realBlockNum & 0xff); //文件数据块总数realBlockNum->validBlockNum有效传输大小
            send[1] = Convert.ToByte(realBlockNum >> 8);
            send[2] = Convert.ToByte(blockSize & 0xff);    //数据块大小
            send[3] = Convert.ToByte(blockSize >> 8);

            Byte[] chipNumbers;
            switch (FirmwareIndex)
            {
                case 0:
                    chipNumbers = ASCIIEncoding.Default.GetBytes("T7");

                    send[0] = Convert.ToByte(validBlockNum & 0xff);  //有效数据块数目
                    send[1] = Convert.ToByte(validBlockNum >> 8);
                    send[4] = 0x22;
                    send[5] = chipNumbers[1];
                    send[6] = chipNumbers[0];
                    send[7] = Convert.ToByte(FileNumber);
                    break;
                case 1:
                    chipNumbers = ASCIIEncoding.Default.GetBytes("T1");

                    send[0] = Convert.ToByte(validBlockNum & 0xff);  //有效数据块数目
                    send[1] = Convert.ToByte(validBlockNum >> 8);
                    send[4] = 0x23;
                    send[5] = chipNumbers[1];
                    send[6] = chipNumbers[0];
                    send[7] = Convert.ToByte(FileNumber);
                    break;
                default:
                    send[4] = Convert.ToByte(chipRole);
                    send[5] = ChipNumber[1];
                    send[6] = ChipNumber[0];
                    break;
            }

            if (openDebug)
            {
                AddDebugInfo("");
                AddDebugInfo($"发送请求帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(send)}");
            }
            SendFrame(id, send);
            LogHelper.AddLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        public void write_firmware_block(byte dstAddr, int blockIndex, byte[] block, int time, bool lostPackage = false)
        {
            if (block.Length == 0)
                return;

            CanFrameID cid = new CanFrameID
            {
                ContinuousFlag = 1,
                Priority = 7,
                FrameType = 1,
                FC = 52,
                DstType = Convert.ToByte(dstAndSrcTypes[0]),//PCS
                DstAddr = dstAddr, //目标地址
                SrcType = Convert.ToByte(dstAndSrcTypes[1]),//CSU-MCU1  010
                SrcAddr = 1
            };

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

            byte[] midData = block.Skip(index).ToArray();
            int totalSize = fileData.Length;
            int count = midData.Length / 7;
            //中间帧
            {
                for (int i = 0; i < count; i++)
                {
                    Thread.Sleep(time);
                    byte[] send = midData.Skip(i * 7).Take(7).ToArray();
                    write_mid_package(i + 1, id, send);

                    if (!isUpdating)
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
                    int crc = CRCHelper.ComputeCrc16(block.ToArray(), block.Length);
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

            //副DSP，新增“升级数据块接收结果查询帧”
            {
                if (FirmwareIndex == 1)
                {
                    bool isSuccess = CheckSdsp(targetAddr, blockIndex);

                    if (openDebug)
                    {
                        if (isSuccess)
                        {
                            AddDebugInfo($"请求应答帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : 数据块接收确认数据帧，请求成功");
                        }
                        else
                        {

                            AddDebugInfo($"请求应答帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : 数据块接收确认数据帧，请求失败");
                        }
                    }
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

            if (openDebug)
            {
                AddDebugInfo("");
                AddDebugInfo($"第{blockIndex + 1}个数据块开始下载");
                AddDebugInfo($"下载起始帧[1]，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(send)}");
            }

            SendFrame(id, send);

            LogHelper.AddLog($"第{blockIndex}个数据块开始下载");
            LogHelper.AddLog($"[发送]下载起始帧[1]，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        private void write_mid_package(int packageIndex, uint id, byte[] data)
        {
            List<byte> send = new List<byte>();
            send.Add((byte)packageIndex); //包序号
            send.AddRange(data);

            if (openDebug)
            {
                string msg = $"下载中间帧[{packageIndex + 1}]，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(send.ToArray())}";
                timeDeltaSw.Stop();
                msg += $" {timeDeltaSw.ElapsedMilliseconds}ms";
                timeDeltaSw.Reset();
                timeDeltaSw.Start();
                AddDebugInfo(msg);
            }
            //DebugTool.OutputBytes($"下载中间帧，id：{id.ToString("X")}，data：", send.ToArray(), true);

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
                int crc = CRCHelper.ComputeCrc16(block.ToArray(), block.Length);
                tmp.Add((byte)(crc & 0xff));
                tmp.Add((byte)(crc >> 8));
            }

            int len = tmp.Count;
            for (int i = 0; i < len; i++)
            {
                send[i] = tmp[i];
            }

            if (openDebug)
            {
                AddDebugInfo($"下载结束帧[0xff]，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(send)}");
            }

            SendFrame(id, send);

            LogHelper.AddLog($"[发送]下载结束帧[0xff]，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        private void write_firmware_result_request(byte dstAddr)
        {
            CanFrameID cid = new CanFrameID
            {
                Priority = 7,
                FrameType = 2,
                ContinuousFlag = 0,
                FC = 53,
                DstType = Convert.ToByte(dstAndSrcTypes[0]),
                DstAddr = dstAddr,
                SrcType = Convert.ToByte(dstAndSrcTypes[1]),
                SrcAddr = 1
            };

            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = 0xAA;
            send[1] = (byte)(fileDataCrc & 0xff);
            send[2] = (byte)(fileDataCrc >> 8);
            if (FirmwareIndex > 1)
            {
                send[3] = Convert.ToByte(chipRole);
                send[4] = ChipNumber[1];
                send[5] = ChipNumber[0];
            }

            if (openDebug)
            {
                AddDebugInfo($"下载固件结果请求帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(send)}");
            }

            SendFrame(id, send);

            LogHelper.AddLog($"[发送]固件接收结果请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        private void write_upgrade_execute_request(int code = 0x02)
        {
            //code：01:查询执行结果 02:启动升级 03:暂存升级
            CanFrameID cid = new CanFrameID
            {
                Priority = 7,
                FrameType = 2,
                ContinuousFlag = 0,
                FC = 54,
                DstType = Convert.ToByte(dstAndSrcTypes[0]),
                DstAddr = targetAddr,
                SrcType = Convert.ToByte(dstAndSrcTypes[1]),
                SrcAddr = 1
            };

            uint id = cid.ID;

            byte[] send = new byte[8];
            send[0] = Convert.ToByte(code);
            send[1] = Convert.ToByte(fileNumber);
            send[2] = Convert.ToByte(chipRole);    //数据块大小
            send[3] = ChipNumber[1];//0x30;
            send[4] = ChipNumber[0];//0x45;

            if (openDebug)
            {
                AddDebugInfo("");
                AddDebugInfo($"发送请求帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(send)}");
            }
            SendFrame(id, send);
            LogHelper.AddLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(send)}");
        }
        private void write_upgrade_time_request(bool upgradetime, byte[] data)
        {
            CanFrameID cid = new CanFrameID
            {
                Priority = 7,
                FrameType = 2,
                ContinuousFlag = 0,
                FC = 55,
                DstType = 1,
                DstAddr = targetAddr,
                SrcType = 2,
                SrcAddr = 1
            };

            uint id = cid.ID;

            //强制进入boot升级功能，无时间帧数据；
            data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xAA, 0x03 };
            if (upgradetime)
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(data);
                bytes.Add(0x00);
                bytes.Add(0x00);

                data = bytes.ToArray();
            }

            if (openDebug)
            {
                AddDebugInfo("");
                AddDebugInfo($"发送请求帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X")} {BitConverter.ToString(data)}");
            }
            //SendFrame(id, data);
            //LogHelper.AddLog($"[发送]请求帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");
        }
        private void AnalysisData(byte[] binchar)
        {
            //清空旧数据
            firmwareModels.Clear();
            //固件模块数量
            int count = binchar[binchar.Length - 2048 + 78];
            byte[] firmwareBytes = binchar.Skip(binchar.Length - 2048 + 137).Take(104 * count).ToArray();
            AddMsg($"固件模块数量：" + count);

            //固件状态
            int index = 0;
            for (int i = 0; i < firmwareBytes.Length; i += 104)
            {
                FirmwareModel firmwareModel = new FirmwareModel();
                //文件类型
                firmwareModel.FirmwareFileType = firmwareBytes[i];
                //芯片角色
                firmwareModel.FirmwareChipRole = firmwareBytes[i + 1];
                //名称
                firmwareModel.FirmwareName = Encoding.ASCII.GetString(firmwareBytes.Skip(i + 2).Take(56).ToArray()).Replace("\0", "");
                //起始偏移地址
                long startAddressByte1 = firmwareBytes[i + 58] & 0xFF;
                long startAddressByte2 = firmwareBytes[i + 59] << 8;
                long startAddressByte3 = firmwareBytes[i + 60] << 16;
                long startAddressByte4 = firmwareBytes[i + 61] << 24;
                firmwareModel.FirmwareStartAddress = startAddressByte1 + startAddressByte2 + startAddressByte3 + startAddressByte4;
                //长度
                long lengthByte1 = firmwareBytes[i + 62] & 0xFF;
                long lengthByte2 = firmwareBytes[i + 63] << 8;
                long lengthByte3 = firmwareBytes[i + 64] << 16;
                long lengthByte4 = firmwareBytes[i + 65] << 24;
                firmwareModel.FirmwareLength = lengthByte1 + lengthByte2 + lengthByte3 + lengthByte4;
                //版本号
                firmwareModel.FirmwareVersion = Encoding.ASCII.GetString(firmwareBytes.Skip(i + 66).Take(20).ToArray());

                string test1 = firmwareModel.FirmwareName;
                //dgvUpgradeProgress.Rows.Add();
                //dgvUpgradeProgress.Rows[index].Cells["nameCell"].Value = firmwareModel.FirmwareName;
                //dgvUpgradeProgress.Rows[index].Cells["checkStateCell"].Value = false;
                //文件类型
                if (fileTypeDic.ContainsKey(firmwareModel.FirmwareFileType))
                {
                    var test2 = fileTypeDic[firmwareModel.FirmwareFileType];
                    //dgvUpgradeProgress.Rows[index].Cells["fileTypeCell"].Value = fileTypeDic[firmwareModel.FirmwareFileType];
                }
                else
                {
                    var test3 = firmwareModel.FirmwareFileType.ToString();
                    //dgvUpgradeProgress.Rows[index].Cells["fileTypeCell"].Value = firmwareModel.FirmwareFileType.ToString();
                }
                //芯片角色
                if (chipCodeDic.ContainsKey(firmwareModel.FirmwareChipRole))
                {
                    var test4 = chipCodeDic[firmwareModel.FirmwareChipRole];
                    //dgvUpgradeProgress.Rows[index].Cells["chipRoleCell"].Value = chipCodeDic[firmwareModel.FirmwareChipRole];
                }
                else
                {
                    var test5 = firmwareModel.FirmwareChipRole.ToString();
                    //dgvUpgradeProgress.Rows[index].Cells["chipRoleCell"].Value = firmwareModel.FirmwareChipRole.ToString();
                }
                //升级进度
                AddMsg($"name:{firmwareModel.FirmwareName},version:{firmwareModel.FirmwareVersion},startAddr：{firmwareModel.FirmwareStartAddress},Length:{firmwareModel.FirmwareLength};Filetype:{fileTypeDic[firmwareModel.FirmwareFileType]},ChipRole:{chipCodeDic[firmwareModel.FirmwareChipRole]}");

                //dgvUpgradeProgress.Rows[index].Cells["progressCell"].Value = 0;
                firmwareModels.Add(firmwareModel);
                index++;
            }
        }
        #region 接收校验
        private bool write_firmware_block_response()
        {
            CanFrameModel frame = new CanFrameModel(recvFrame);
            CanFrameID frameId = frame.FrameId;
            uint id = frame.Id;

            if (id == 0x01000000)  //过滤默认id
                return false;

            try
            {
                CanFrameData frameDate = frame.FrameDatas[0];
                byte[] data = frameDate.Data;

                if (frameId.FC != 51)
                    return false;

                if (data.Length == 0)
                {
                    LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                    return false;
                }

                LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                byte resultDesc = data[0];
                if (resultDesc == 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                int stop = 10;
            }

            return false;
        }
        private bool write_firmware_request_check()
        {
            CanFrameModel frame = new CanFrameModel(recvFrame);
            CanFrameID frameId = frame.FrameId;
            uint id = frame.Id;

            if (id == 0x01000000)  //过滤默认id
                return false;

            try
            {
                CanFrameData frameDate = frame.FrameDatas[0];
                byte[] data = frameDate.Data;

                //LogHelper.AddLog($"[接收]请求应答帧，0x{id:X)} {BitConverter.ToString(data)}");

                if (frameId.FC != 50)
                    return false;

                LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                if (data.Length == 0)
                {
                    LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                    return false;
                }

                byte resultDesc = data[0];
                if (resultDesc == 0)
                    return true;

            }
            catch (Exception ex)
            {
                int stop = 10;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool write_firmware_request_check_ShakeHandsV2()
        {
            // 遍历所有的 Can 帧
            foreach (var recvData in this.CanRecieveList)
            {
                uint id = recvData.ID;
                string strId = id.ToString("X");
                if (strId.Contains("197F") || id == 0x01000000)  //过滤dsp的app心跳
                    continue;

                CanFrameID frameID = new CanFrameID();
                frameID.ID = id;

                if (frameID.FrameType != 3 || !(frameID.FC >= 50 && frameID.FC <= 55))  //过滤非升级帧的数据
                    continue;

                try
                {
                    CanFrameData frameData = new CanFrameData();
                    for (int i = 0; i < recvData.DataLen; i++)
                    {
                        frameData.Data[i] = recvData.Data[i];
                    }
                    byte[] data = frameData.Data;

                    //LogHelper.AddLog($"[接收]请求应答帧，0x{id:X)} {BitConverter.ToString(data)}");

                    if (frameID.FC != 50)
                        return false;

                    LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                    if (data.Length == 0)
                    {
                        LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                        return false;
                    }

                    byte resultDesc = data[0];
                    if (resultDesc == 0) // 0 表示成功 
                    {
                        var device = Convert.ToInt16(frameID.SrcAddr);// 当前握手设备
                        if (this.UpDeviceDic.ContainsKey(device))
                        {
                            this.UpDeviceDic[device] = true;
                            continue;
                        }
                    }

                }
                catch (Exception ex)
                {
                    int stop = 10;
                }
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
            if (openDebug)
            {
                AddDebugInfo($"收集结果应答帧，{DateTime.Now.ToString("HH:mm:ss.fff")} : {id.ToString("X8")} {BitConverter.ToString(frame.FrameDatas[0].Data)}");
            }
            LogHelper.AddLog($"[接收]固件接收结果应答帧，0x{id.ToString("X")} {BitConverter.ToString(frame.FrameDatas[0].Data)}");
        }
        private bool file_recieve_ok_check()
        {
            for (int k = 0; k < MaxPackageNum; k++)
            {
                string errMsg = "";
                AddMsg($"校验固件接收结果");

                //2、指定时间内收集一台或多台pcs设备结果校验帧
                resultFrameList.Clear();

                //1、发送接收结果请求
                Thread.Sleep(200);
                write_firmware_result_request(targetAddr);

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

                if (resultFrameList.Count == 0)
                {
                    AddMsg($"校验固件接收结果失败，重新查询");
                    continue;
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
                _multiPackData.Clear();
                if (hasLostPackage)
                {
                    AddMsg("检测到有丢包！");
                    LogHelper.AddLog("检测到有丢包！");

                    CanFrameID frameId = new CanFrameID();
                    int num = lostPackageList.Count;
                    //整合丢失数据包
                    for (int i = 0; i < lostPackageList.Count; i++)
                    {
                        for (int j = 0; j < lostPackageList[i].blockIdxList.Count; j++)
                        {
                            int _num = lostPackageList[i].blockIdxList[j];
                            if (_multiPackData.Exists(x => x == _num) == false)
                                _multiPackData.Add(_num);
                        }
                    }

                    /*for (int i = 0; i < num; i++)
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

                        //待调整区域
                        if (ok)
                        {
                            AddMsg($"设备[{frameId.SrcAddr}]升级成功！！！☺");
                        }
                        else
                        {
                            AddMsg($"设备[{frameId.SrcAddr}]升级失败！！！☹");
                        }
                    }//for
                    */

                    //对每一个设备进行循环补包（指定补包次数）
                    bool ok = false;
                    LostPackage package = new LostPackage() { id = 0x1B3521A0, blockIdxList = _multiPackData };
                    frameId.ID = package.id;

                    /*int retryCnt = 0;
                    do
                    {
                        if (SendLostPackage(package))
                            break;

                        //发送请求，应答提示存在丢包现象
                        AddMsg("丢包失败，重新执行补包流程！");
                        retryCnt++;

                    } while (retryCnt < MaxPackageNum);

                    return false;*/
                    return SendLostPackage(package);
                }
                else
                {
                    AddMsg($"校验成功");

                    resultFrameList.ForEach((frameModel) =>
                    {
                        AddMsg($"设备[{frameModel.FrameId.SrcAddr}]升级成功！！！☺");
                    });

                    return true;
                }
            }

            return false;
        }//func
        private bool write_upgrade_time_request_check()
        {
            CanFrameModel frame = new CanFrameModel(recvFrame);
            CanFrameID frameId = frame.FrameId;
            uint id = frame.Id;

            if (id == 0x01000000)  //过滤默认id
                return false;

            try
            {
                CanFrameData frameDate = frame.FrameDatas[0];
                byte[] data = frameDate.Data;

                LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                if (frameId.FC != 50)
                    return false;

                if (data.Length == 0)
                {
                    LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                    return false;
                }

                if (data[0] == 0x0)
                    return true;
            }
            catch (Exception ex)
            {
                int stop = 10;
            }

            return false;
        }
        private bool write_upgrade_execute_request_check()
        {
            CanFrameModel frame = new CanFrameModel(recvFrame);
            CanFrameID frameId = frame.FrameId;
            uint id = frame.Id;

            if (id == 0x01000000)  //过滤默认id
                return false;

            try
            {
                CanFrameData frameDate = frame.FrameDatas[0];
                byte[] data = frameDate.Data;

                LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} {BitConverter.ToString(data)}");

                if (frameId.FC != 54)
                    return false;

                if (data.Length == 0)
                {
                    LogHelper.AddLog($"[接收]请求应答帧，0x{id.ToString("X")} 数据为空！)");
                    return false;
                }

                byte resultDesc = data[0];
                switch (resultDesc)
                {
                    case 0:
                    case 3:
                        upgradeExecuteResult = resultDesc;

                        /*if (resultDesc == 0x03)
                        {
                            t1 = new System.Timers.Timer(100);
                            t1.Elapsed += T1_Elapsed;
                            t1.Enabled = true;
                            t1.Start();
                        }*/
                        return true;
                        break;
                    default:
                        LogHelper.AddLog($"非正常状态值：{resultDesc}");
                        break;
                }
                //if (resultDesc == 0)
                //    return true;

            }
            catch (Exception ex)
            {
                int stop = 10;
            }

            return false;
        }
        private bool SendLostPackage(LostPackage package)//result LostPackage->bool
        {
            bool ok = false;
            //LostPackage resultPackage = null;
            CanFrameID frameId = new CanFrameID();
            uint packageId = package.id;
            frameId.ID = packageId;
            byte dstAddr = frameId.SrcAddr;

            //0.检查
            if (package.blockIdxList.Count == 0)
            {
                return true;
            }

            //1、有设备丢包 进行补包
            string strIndex = "";
            foreach (int index in package.blockIdxList)
            {
                strIndex += index.ToString() + " ";
            }

            AddMsg($"开始进行补包，丢失数据块序号: {strIndex}");
            LogHelper.AddLog($"开始进行补包，丢失数据块序号: {strIndex}");

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

            AddMsg("补包完成！");
            LogHelper.AddLog("补包完成！");

            //2、发送接收结果请求
            return file_recieve_ok_check();

            /*//2、发送接收结果请求
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

            return resultPackage;*/
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
                foreach (int index in lostPackage.blockIdxList)
                {
                    findPackage.blockIdxList.Add(index);
                }
            }
            else
            {
                lostPackageList.Add(lostPackage);
            }

            return true;
        }
        #endregion

        public bool IsRecieveList = false;

        public List<CAN_OBJ> CanRecieveList = new List<CAN_OBJ>();

        #region CAN操作
        /// <summary>
        /// 初始化Can设置
        /// </summary>
        public void InitCanHelper()
        {
            ecanHelper.RegisterRecvProcessCan1(RecvProcCan1);
            ecanHelper.RegisterRecvProcessCan2(RecvProcCan2);
        }
        /// <summary>
        /// 检查Can连接
        /// </summary>
        /// <returns></returns>
        private bool CheckConnect()
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
            if (!isUpdating)
            {
                LogHelper.AddLog("未启动升级，发送无效");
                return;
            }

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
            uint id = recvData.ID;
            string strId = id.ToString("X");
            if (strId.Contains("197F") || id == 0x01000000)  //过滤dsp的app心跳
                return;

            CanFrameID frameID = new CanFrameID();
            frameID.ID = id;

            if (frameID.FrameType != 3 || !(frameID.FC >= 50 && frameID.FC <= 55))  //过滤非升级帧的数据
                return;

            recvFrame.Id = id;
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

            if (IsRecieveList)
            {
                CanRecieveList.Add(recvData);
            }
        }
        /// <summary>
        /// 通道1接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan1(CAN_OBJ recvData)
        {
            if (isUpdating)
                RecvFrame(recvData);
        }
        /// <summary>
        /// 通道2接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan2(CAN_OBJ recvData)
        {
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
        /// <summary>
        /// 收集bin文件中数据
        /// 转为数据块数组存储
        /// </summary>
        private void CollectFileData()
        {
            fileDataList.Clear();
            validBlockNum = 0;

            var keepLastPacks = KeepSignatureBytes / blockSize + 1;
            for (int i = 0; i < blockNum; i++)
            {
                byte[] dataArr = fileData.Skip(i * blockSize).Take(blockSize).ToArray();

                //过滤字节全为FF的数据块，但是数据块索引不变
                if (deleteFFData && i < blockNum - keepLastPacks)
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
                        fileDataList.Add(new byte[] { });
                        continue;
                    }

                }
                validBlockNum++;

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

        private void T1_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            write_upgrade_execute_request(0x1);
        }
    }//class

    /// <summary>
    /// 存储单个设备丢包数据
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
            strProgress = strProgress + $" {progress}%";
            if (addNewLine)
            {
                strProgress += "\r\n";
            }
            return strProgress;
        }
    }//class

    public class FirmwareModel
    {
        public FirmwareModel()
        {

        }
        public FirmwareModel(string firmwareName, byte firmwareFileType, byte firmwareChipRole, int firmwareStartAddress, string firmwareVersion, int firmwareLength)
        {
            this.FirmwareName = firmwareName;
            this.FirmwareFileType = firmwareFileType;
            this.FirmwareChipRole = firmwareChipRole;
            this.FirmwareStartAddress = firmwareStartAddress;
            this.FirmwareVersion = firmwareVersion;
            this.FirmwareLength = firmwareLength;
        }

        //名称
        public string FirmwareName { get; set; }

        //文件类型
        public byte FirmwareFileType { get; set; }

        //芯片角色
        public byte FirmwareChipRole { get; set; }

        //起始偏移地址
        public long FirmwareStartAddress { get; set; }

        //版本号
        public string FirmwareVersion { get; set; }

        //长度
        public long FirmwareLength { get; set; }
    }//class

    public enum DstAndSrcType
    {
        PCS = 1,
        MUC1 = 2,
        MUC2 = 3,
        BCU = 4,
        BMU = 5
    }
}
