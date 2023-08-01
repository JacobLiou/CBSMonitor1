using Communication.Can;
using SofarHVMExe.Utilities;
using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using SofarHVMExe.Model;
using SofarHVMExe.ViewModel;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using SofarHVMExe.Utilities.FileOperate;
using SofarHVMExe.Utilities.Global;
using System.Windows.Controls;
using System.Threading;

namespace SofarHVMExe.ViewModel
{
    internal class CANConnectPageVm : ViewModelBase
    {
        public CANConnectPageVm()
        {
            ecanHelper = new EcanHelper();
            //ecanHelper.OnReceiveCan1 += RecvProcCan1;
            //ecanHelper.OnReceiveCan2 += RecvProcCan2;
            ecanHelper.RegisterRecvProcessCan1(RecvProcCan1);
            ecanHelper.RegisterRecvProcessCan2(RecvProcCan2);

            Init();
        }

        private FileConfigModel? fileCfgModel = null;
        public EcanHelper? ecanHelper = null;

        #region 成员属性
        public uint DeviceInx
        {
            get => ecanHelper.DevIndex;

            set
            {
                if (ecanHelper.DevIndex != value)
                {
                    ecanHelper.DevIndex = value;
                    OnPropertyChanged();

                    //保存更新到文件
                    SaveDevIndex(value);
                }
            }
        }
        public int BaudrateInx1
        {
            get => Baudrate2Inx(ecanHelper.BaudCan1);

            set
            {
                ecanHelper.BaudCan1 = Inx2Baudrate(value);
                OnPropertyChanged();

                //保存波特率设置到文件
                SaveBaudrate1(value);
            }
        }
        public int BaudrateInx2
        {
            get => Baudrate2Inx(ecanHelper.BaudCan2);

            set
            {
                ecanHelper.BaudCan2 = Inx2Baudrate(value);
                OnPropertyChanged();

                //保存波特率设置到文件
                SaveBaudrate2(value);
            }
        }
        public string SendIdCan1 { get; set; }
        public string SendIdCan2 { get; set; }
        public string SendDataCan1 { get; set; }
        public string SendDataCan2 { get; set; }
        private List<string> recvDataListCan1 = new List<string>();
        private List<string> recvDataListCan2 = new List<string>();
        private string recvDataCan1 = "";
        public string RecvDataCan1
        {
            get => recvDataCan1;
            set
            {
                recvDataCan1 = value;
                OnPropertyChanged();
            }
        }
        private string recvDataCan2 = "";
        public string RecvDataCan2
        {
            get => recvDataCan2;
            set { recvDataCan2 = value; OnPropertyChanged(); }
        }
        private bool showHeartBeatCan1 = true;
        public bool ShowHeartBeatCan1
        {
            get => showHeartBeatCan1;
            set { showHeartBeatCan1 = value; OnPropertyChanged(); }
        }
        private bool showHeartBeatCan2 = true;
        public bool ShowHeartBeatCan2
        {
            get => showHeartBeatCan2;
            set { showHeartBeatCan2 = value; OnPropertyChanged(); }
        }
        public string SendTimeCan1 { get; set; }
        public string SendTimeCan2 { get; set; }
        private bool enable1 = true;
        public bool RecvDataEnableCan1
        {
            get => enable1;
            set
            {
                if (!ecanHelper.IsConnected)
                {
                    MessageBox.Show("请先打开设备！");
                    return;
                }

                if (!ecanHelper.IsCan1Start)
                {
                    MessageBox.Show("请先打开通道1！");
                    return;
                }

                enable1 = value;
                OnPropertyChanged();
            }
        }
        private bool enable2 = true;
        public bool RecvDataEnableCan2
        {
            get => enable2;
            set
            {
                if (!ecanHelper.IsConnected)
                {
                    MessageBox.Show("请先打开设备！");
                    return;
                }

                if (!ecanHelper.IsCan1Start)
                {
                    MessageBox.Show("请先打开通道2！");
                    return;
                }

                enable2 = value;
                OnPropertyChanged();
            }
        }

        //private bool isConnected = false;
        //public bool IsConnected
        //{
        //    get => isConnected;
        //    set
        //    {
        //        isConnected = value;
        //        OnPropertyChanged();
        //    }
        //}

        public bool IsConnected
        {
            get => ecanHelper.IsConnected;
            set
            {
                ecanHelper.IsConnected = value;
                OnPropertyChanged();
            }
        }

        private bool isChannel1Opened = true;
        public bool IsChannel1Opened
        {
            get => isChannel1Opened;
            set
            {
                if (value)
                {
                    //打开通道
                    OpenChannel1();
                }
                else
                {
                    //关闭通道
                    CloseChannel1();
                }
                isChannel1Opened = value;
                OnPropertyChanged();
            }
        }
        private bool isChannel2Opened = false;
        public bool IsChannel2Opened
        {
            get => isChannel2Opened;
            set
            {
                if (value)
                {
                    //打开通道
                    OpenChannel2();
                }
                else
                {
                    //关闭通道
                    CloseChannel2();
                }
                isChannel2Opened = value;
                OnPropertyChanged();
            }
        }
        //0为正常模式，=1为只听模式，=2为自发自收模式
        public int Mode
        {
            get => (int)ecanHelper.Mode;
            set
            {
                ecanHelper.Mode = (byte)value;
                OnPropertyChanged();
            }
        }
        public List<string> BaudrateList { get; set; }
        public List<string> ModeList { get; set; }
        public ICommand OpenDeviceCommand { get; set; }
        public ICommand CloseDeviceCommand { get; set; }
        public ICommand SendChannel1Command { get; set; }
        public ICommand SendChannel2Command { get; set; }
        public ICommand ClearRecvCan1Command { get; set; }
        public ICommand ClearRecvCan2Command { get; set; }
        public ICommand SaveCan1Command { get; set; }
        public ICommand SaveCan2Command { get; set; }
        #endregion

        #region 成员方法
        //波特率转索引号
        private int Baudrate2Inx(EcanHelper.Bauds baud)
        {
            switch (baud)
            {
                case EcanHelper.Bauds._5kbps:
                    return 0;
                case EcanHelper.Bauds._10kbps:
                    return 1;
                case EcanHelper.Bauds._20kbps:
                    return 2;
                case EcanHelper.Bauds._40kbps:
                    return 3;
                case EcanHelper.Bauds._50kbps:
                    return 4;
                case EcanHelper.Bauds._80kbps:
                    return 5;
                case EcanHelper.Bauds._100kbps:
                    return 6;
                case EcanHelper.Bauds._125kbps:
                    return 7;
                case EcanHelper.Bauds._200kbps:
                    return 8;
                case EcanHelper.Bauds._250kbps:
                    return 9;
                case EcanHelper.Bauds._400kbps:
                    return 10;
                case EcanHelper.Bauds._500kbps:
                    return 11;
                case EcanHelper.Bauds._666kbps:
                    return 12;
                case EcanHelper.Bauds._800kbps:
                    return 13;
                case EcanHelper.Bauds._1000kbps:
                    return 14;
                default:
                    return 0;
            }
        }
        //波特率转索引号
        private int Baudrate2Inx(int baud)
        {
            switch (baud)
            {
                case 5:
                    return 0;
                case 10:
                    return 1;
                case 20:
                    return 2;
                case 40:
                    return 3;
                case 50:
                    return 4;
                case 80:
                    return 5;
                case 100:
                    return 6;
                case 125:
                    return 7;
                case 200:
                    return 8;
                case 250:
                    return 9;
                case 400:
                    return 10;
                case 500:
                    return 11;
                case 666:
                    return 12;
                case 800:
                    return 13;
                case 1000:
                    return 14;
                default:
                    return 0;
            }
        }
        //索引号转波特率
        private EcanHelper.Bauds Inx2Baudrate(int index)
        {
            switch (index)
            {
                case 0:
                    return EcanHelper.Bauds._5kbps;
                case 1:
                    return EcanHelper.Bauds._10kbps;
                case 2:
                    return EcanHelper.Bauds._20kbps;
                case 3:
                    return EcanHelper.Bauds._40kbps;
                case 4:
                    return EcanHelper.Bauds._50kbps;
                case 5:
                    return EcanHelper.Bauds._80kbps;
                case 6:
                    return EcanHelper.Bauds._100kbps;
                case 7:
                    return EcanHelper.Bauds._125kbps;
                case 8:
                    return EcanHelper.Bauds._200kbps;
                case 9:
                    return EcanHelper.Bauds._250kbps;
                case 10:
                    return EcanHelper.Bauds._400kbps;
                case 11:
                    return EcanHelper.Bauds._500kbps;
                case 12:
                    return EcanHelper.Bauds._666kbps;
                case 13:
                    return EcanHelper.Bauds._800kbps;
                case 14:
                    return EcanHelper.Bauds._1000kbps;
                default:
                    return EcanHelper.Bauds._250kbps;
            }
        }

        private void Init()
        {
            SendIdCan1 = "0x18FF1234";
            SendIdCan2 = "0x18FF9527";
            SendDataCan1 = "01 00 00 00 00 00 00 00";
            SendDataCan2 = "02 00 00 00 00 00 00 00";
            SendTimeCan1 = "1";
            SendTimeCan2 = "1";

            OpenDeviceCommand = new SimpleCommand(OpenDevice);
            CloseDeviceCommand = new SimpleCommand(CloseDevice);
            SendChannel1Command = new SimpleCommand(SendChannel1);
            SendChannel2Command = new SimpleCommand(SendChannel2);
            ClearRecvCan1Command = new SimpleCommand(ClearRecvCan1);
            ClearRecvCan2Command = new SimpleCommand(ClearRecvCan2);
            SaveCan1Command = new SimpleCommand(SaveMsgCan1);
            SaveCan2Command = new SimpleCommand(SaveMsgCan2);

            BaudrateList = new List<string>
            {
                "5K",
                "10K",
                "20K",
                "40K",
                "50K",
                "80K",
                "100K",
                "125K",
                "200K",
                "250K",
                "400K",
                "500K",
                "800K",
                "666K",
                "1000K"
            };
            ModeList = new List<string>
            {
                "正常模式",
                "只听模式",
                "自发自收模式"
            };//暂未使用

            UpdateModel();
        }
        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel != null && fileCfgModel.PrjModel != null)
            {
                ecanHelper.DevIndex = fileCfgModel.PrjModel.DeviceInx;
                ecanHelper.BaudCan1 = Inx2Baudrate(Baudrate2Inx(fileCfgModel.PrjModel.Baudrate1));
                ecanHelper.BaudCan2 = Inx2Baudrate(Baudrate2Inx(fileCfgModel.PrjModel.Baudrate2));
            }
        }
        private void SaveDevIndex(uint index)
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel != null && fileCfgModel.PrjModel != null)
            {
                fileCfgModel.PrjModel.DeviceInx = index;
                SaveData();
            }
        }
        private void SaveBaudrate1(int index)
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel != null && fileCfgModel.PrjModel != null)
            {
                string strBaud = BaudrateList[index].Replace("K", "");
                fileCfgModel.PrjModel.Baudrate1 = int.Parse(strBaud);
                SaveData();
            }
        }
        private void SaveBaudrate2(int index)
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel != null && fileCfgModel.PrjModel != null)
            {
                string strBaud = BaudrateList[index].Replace("K", "");
                fileCfgModel.PrjModel.Baudrate2 = int.Parse(strBaud);
                SaveData();
            }
        }
        private async void OpenDevice(object o)
        {
            if (ecanHelper == null)
                return;

            LogHelper.SubDirectory = "Info";
            LogHelper.CreateNewLogger();
            LogHelper.AddLog("**************** 运行日志 ****************");
            LogHelper.AddLog($"[OpenDevice]-Start");

            (o as UIElement).IsEnabled = false;
            GlobalManager.Instance().UpdataUILock(true);

            Task<bool> task = Task.Run(() => { return ecanHelper.OpenDevice(); });
            await task;
            if (task.Result)
            {
                //IsConnected = true;

                if (isChannel1Opened)
                {
                    LogHelper.AddLog($"[OpenDevice]-OpenChannel1");
                    OpenChannel1();
                }

                if (isChannel2Opened)
                {
                    LogHelper.AddLog($"[OpenDevice]-OpenChannel2");
                    OpenChannel2();
                }

                LogHelper.AddLog($"[OpenDevice]-已连接");
                GlobalManager.Instance().UpdateStatusBar_ConnectStatus("已连接");
                //MessageBox.Show("打开设备成功！", "提示");
            }
            else
            {
                LogHelper.AddLog($"[OpenDevice]-断开");
                GlobalManager.Instance().UpdateStatusBar_ConnectStatus("断开");
                //MessageBox.Show("打开设备失败！", "提示");
            }
            (o as UIElement).IsEnabled = true;
            GlobalManager.Instance().UpdataUILock(false);
        }
        private async void CloseDevice(object o)
        {
            if (ecanHelper == null || !ecanHelper.IsConnected)
                return;

            LogHelper.SubDirectory = "Info";
            LogHelper.CreateNewLogger();
            LogHelper.AddLog("**************** 运行日志 ****************");
            LogHelper.AddLog($"[CloseDevice]-Start");
            (o as UIElement).IsEnabled = false;
            GlobalManager.Instance().UpdataUILock(true);
            Task<bool> task = Task.Run(() => { return ecanHelper.Close(); });
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));// 设置超时时间为5秒
            var completedTask = await Task.WhenAny(task, timeoutTask);
            bool result = (completedTask == task) && (await task);
            if (result)
            {
                LogHelper.AddLog($"[CloseDevice]-CloseChannel");
                //IsConnected = false;
                CloseChannel1();
                CloseChannel2();

                LogHelper.AddLog($"[CloseDevice]-UpdateStatusBar_ConnectStatus");
                GlobalManager.Instance().UpdateStatusBar_ConnectStatus("断开");
                GlobalManager.Instance().UpdateStatusBar_SelectDev("无");
                MessageBox.Show("关闭设备成功！", "提示");
                LogHelper.AddLog($"[CloseDevice]-关闭设备成功");
            }
            else
            {
                MessageBox.Show("关闭设备失败！", "提示");
            }
            (o as UIElement).IsEnabled = true;
            GlobalManager.Instance().UpdataUILock(false);
        }
        private void OpenChannel1()
        {
            if (ecanHelper.StartupCan1())
            {
                ecanHelper.StartRecvCan1();
            }
            else
            {
                //MessageBox.Show("打开通道失败！");
            }
        }
        private void OpenChannel2()
        {
            if (ecanHelper.StartupCan2())
            {
                ecanHelper.StartRecvCan2();
            }
            else
            {
                //MessageBox.Show("打开通道失败！");
            }
        }
        private void CloseChannel1()
        {
            ecanHelper.StopRecvCan1();
            ecanHelper.CloseCan1();
        }
        private void CloseChannel2()
        {
            ecanHelper.StopRecvCan2();
            ecanHelper.CloseCan2();
        }
        private void SendChannel1(object obj)
        {
            if (ecanHelper == null)
                return;

            //id
            uint id = 0;
            SendIdCan1 = SendIdCan1.Replace("0x", "").Replace("0X", "");
            if (!uint.TryParse(SendIdCan1, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
            {
                MessageBox.Show("CAN ID 错误", "提示");
                return;
            }

            //数据
            string[] byteArr = SendDataCan1.Trim().Split(" ");
            byte[] dataArr = new byte[8];
            for (int i = 0; i < byteArr.Length; i++)
            {
                byte.TryParse(byteArr[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out dataArr[i]);
            }

            //发送
            int count = int.Parse(SendTimeCan1.Trim());
            for (int i = 0; i < count; i++)
            {
                ecanHelper.SendCan1(id, dataArr);
            }
        }
        private void SendChannel2(object obj)
        {
            if (ecanHelper == null)
                return;

            //id
            uint id = 0;
            SendIdCan2 = SendIdCan2.Replace("0x", "").Replace("0X", "");
            if (!uint.TryParse(SendIdCan2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
            {
                MessageBox.Show("CAN ID 错误", "提示");
                return;
            }

            //数据
            string[] byteArr = SendDataCan2.Trim().Split(" ");
            byte[] dataArr = new byte[8];
            for (int i = 0; i < dataArr.Length; i++)
            {
                byte.TryParse(byteArr[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out dataArr[i]);
            }

            //发送
            int count = int.Parse(SendTimeCan2.Trim());
            for (int i = 0; i < count; i++)
            {
                ecanHelper.SendCan2(id, dataArr);
            }
        }
        private void RecvProcCan1(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Can_Connect)
                return;

            if (!RecvDataEnableCan1)
                return;

            //过滤心跳
            if (!ShowHeartBeatCan1)
            {
                string recvId = recvData.ID.ToString("X");
                if (recvId.Contains("197F"))
                    return;
            }
            /*//微秒显示间隔时间
            uint timeus = recvdata.timestamp - lasttime;
            lasttime = recvdata.timestamp;
            uint timesecond = timeus / 1000000;
            uint timems = timeus % 1000000 / 1000;
            timeus = timeus % 1000000 % 1000;
            string time = $"{timesecond}:{timems}:{timeus}";*/
            string time = DateTime.Now.ToString("HH:mm:ss.fff"); //时分秒毫秒
            string id = "0x" + recvData.ID.ToString("X8");
            string len = recvData.DataLen.ToString();
            string data = "";
            string frame = "";
            foreach (byte d in recvData.Data)
            {
                data += " " + d.ToString("X2");
            }

            frame = $"{time,-15}\t{id,-10}     {len}    {data}\r\n";
            //RecvDataCan1 += frame;
            if (recvDataListCan1.Count > 99)
            {
                recvDataListCan1.RemoveAt(0);
            }
            recvDataListCan1.Add(frame);
            RecvDataCan1 = string.Join("", recvDataListCan1);
        }
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Can_Connect)
                return;

            if (!RecvDataEnableCan2)
                return;

            //过滤心跳
            if (!ShowHeartBeatCan2)
            {
                string recvId = recvData.ID.ToString("X");
                if (recvId.Contains("197F"))
                    return;
            }

            string time = DateTime.Now.ToString("HH:mm:ss.fff"); //时分秒毫秒
            string id = "0x" + recvData.ID.ToString("X8");
            string len = recvData.DataLen.ToString();
            string data = "";
            string frame = "";
            foreach (byte d in recvData.Data)
            {
                data += " " + d.ToString("X2");
            }

            frame = $"{time,-15}\t{id,-10}     {len}    {data}\r\n";
            //RecvDataCan2 += frame;
            if (recvDataListCan2.Count > 99)
            {
                recvDataListCan2.RemoveAt(0);
            }
            recvDataListCan2.Add(frame);
            RecvDataCan2 = string.Join("", recvDataListCan2);
        }
        private void ClearRecvCan1(object obj)
        {
            RecvDataCan1 = "";
        }
        private void ClearRecvCan2(object obj)
        {
            RecvDataCan2 = "";
        }
        private void SaveMsgCan1(object obj)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "保存到文件";
            dlg.Filter = "(*.txt)|*.txt";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string filePath = dlg.FileName;
            TxtFileHelper.Save2File(RecvDataCan1, filePath);
        }
        private void SaveMsgCan2(object obj)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "保存到文件";
            dlg.Filter = "(*.txt)|*.txt";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            string filePath = dlg.FileName;
            TxtFileHelper.Save2File(RecvDataCan2, filePath);
        }
        private void SaveData()
        {
            JsonConfigHelper.WirteConfigFile(fileCfgModel);
            /*if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                //MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                //MessageBox.Show("保存失败！", "提示");
            }*/
        }
        #endregion

    }//class
}
