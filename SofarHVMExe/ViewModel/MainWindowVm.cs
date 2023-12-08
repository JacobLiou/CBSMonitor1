using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using log4net.Util;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using Communication.Can;
using static SofarHVMExe.Utilities.Global.GlobalManager;
using MessageBox = System.Windows.MessageBox;


namespace SofarHVMExe.ViewModel
{
    partial class MainWindowVm : ObservableObject
    {
        public static object CurVM { get; set; }

        public MainWindowVm()
        {
            //检查配置文件（注意：检查配置文件和初始化配置页面viewModel必须要放到前面）
            CheckConfigFile();

            //初始化viewModel，初始化是有顺序的
            configPageVm = new ConfigPageVm();
            canTestPageVm = new CANConnectPageVm();
            monitorPageVm = new MonitorPageVm(canTestPageVm.ecanHelper);
            mapOptPageVm = new MapOptPageVm(canTestPageVm.ecanHelper);
            projectCfgPageVm = new ProjectCfgPageVm(canTestPageVm.ecanHelper);
            heartBeatPageVm = new HeartBeatPageVm(canTestPageVm.ecanHelper);
            downloadPageVm = new DownloadPageVm(canTestPageVm.ecanHelper);
            oscilloscopePageVm = new OscilloscopePageVM(canTestPageVm.ecanHelper);
            fileOptPageVm = new FileOptPageVm(canTestPageVm.ecanHelper);

            configPageVm.RegisterUpdate2CanConfig(monitorPageVm.UpdateModel);
            DeviceManager.Instance().SubscribDevSelect(monitorPageVm.ClearDataByUnSelectDev);

            monitorPageVm.InitCanHelper();
            mapOptPageVm.InitCanHelper();
            heartBeatPageVm.InitCanHelper();
            downloadPageVm.InitCanHelper();
            fileOptPageVm.InitCanHelper();

            CommManager.Instance().SetCanHelper(canTestPageVm.ecanHelper);

            //初始化命令
            CANTestPageCommand = new SimpleCommand(SetPageCanTest);
            HeartBeatPageCommand = new SimpleCommand(SetPageHeartBeat);
            MonitorPageCommand = new SimpleCommand(SetPageMonitor);
            MapOptPageCommand = new SimpleCommand(SetPageMapOpt);
            ConfigPageCommand = new SimpleCommand(SetPageConfig);
            DownloadPageCommand = new SimpleCommand(SetPageDownload);
            OscilloscopeCommand = new SimpleCommand(SetPageOscilloscope);
            FilePageCommand = new SimpleCommand(SetPageFile);
            //设置初始页面
            CurrentView = canTestPageVm;

            GlobalManager.Instance().UpdataStatusAction += UpdateConnectStatus;
            GlobalManager.Instance().UpdataStatusAction += UpdateSelectDev;
            GlobalManager.Instance().UpdataStatusAction += UpdateConnectDevs;
            GlobalManager.Instance().UpdataStatusAction += UpdateCanErrInfo;
            GlobalManager.Instance().UpdataUILock += UpdataUILock;
        }

        #region 字段 
        private ConfigPageVm configPageVm = null;
        private MonitorPageVm monitorPageVm = null;
        private MapOptPageVm mapOptPageVm = null;
        private ProjectCfgPageVm projectCfgPageVm = null;
        private HeartBeatPageVm heartBeatPageVm = null;
        private CANConnectPageVm canTestPageVm = null;
        private DownloadPageVm downloadPageVm = null;
        private OscilloscopePageVM oscilloscopePageVm = null;
        private LogInfoWnd? logInfoWnd = null;
        private FileOptPageVm fileOptPageVm = null;
        private bool logInfoWndClose = true;

        #endregion

        #region 属性
        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private string connectStatus = "断开";

        [ObservableProperty]
        private string canErrInfo = "";

        private string selectDev = "无";
        public string SelectDev
        {
            get => selectDev;
            set
            {
                if (selectDev != value)
                {
                    selectDev = value;
                    OnPropertyChanged();

                    if (DeviceManager.Instance().UpdateDev)
                    {
                        //更新心跳的设备选择
                        if (selectDev == "无")
                        {
                            var devs = DeviceManager.Instance().GetConnectDevs();
                            devs.ForEach((dev) =>
                            {
                                dev.UpdateStatusBar = false; //不再循环更新状态栏
                                dev.Selected = false;
                            });
                        }
                        else
                        {
                            var devs = DeviceManager.Instance().GetConnectDevs();
                            Device findDev = devs.Find((dev) => dev.Name == selectDev);
                            if (findDev != null)
                            {
                                findDev.UpdateStatusBar = false; //不再循环更新状态栏
                                findDev.Selected = true;
                            }
                        }
                    }
                    else
                    {
                        DeviceManager.Instance().UpdateDev = true;
                    }
                }
            }//set
        }

        [ObservableProperty]
        private List<string> connectDevs = new List<string>();

        private bool uiLocked;
        public bool UILocked
        {
            get { return uiLocked; }
            set
            {
                if (uiLocked != value)
                {
                    uiLocked = value;
                    OnPropertyChanged(nameof(UILocked));
                }
            }
        }

        #endregion

        #region 命令
        public ICommand CANTestPageCommand { get; set; }
        public ICommand HeartBeatPageCommand { get; set; }
        public ICommand MonitorPageCommand { get; set; }
        public ICommand MapOptPageCommand { get; set; }
        public ICommand OscilloscopeCommand { get; set; }
        public ICommand StatisticCommand { get; set; }
        public ICommand OrderCommand { get; set; }
        public ICommand DownloadPageCommand { get; set; }
        public ICommand ConfigPageCommand { get; set; }
        public ICommand FilePageCommand { get; set; }

        #endregion

        #region 成员方法
        private void SetPageCanTest(Object o)
        {
            canTestPageVm.UpdateModel();
            CurrentView = canTestPageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.Can_Connect;
        }
        private void SetPageHeartBeat(Object o)
        {
            CurrentView = heartBeatPageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.HeartBeat;
        }
        private void SetPageMonitor(Object o)
        {
            monitorPageVm.UpdateModel();
            CurrentView = monitorPageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.Monitor;
            monitorPageVm.UpdateFaultDisplay("");
        }
        private void SetPageMapOpt(Object o)
        {
            mapOptPageVm.UpdateModel();
            CurrentView = mapOptPageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.MapOpt;
        }

        private void SetPageOscilloscope(Object o)
        {
            oscilloscopePageVm.UpdateModel();
            CurrentView = oscilloscopePageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.Oscilloscope;
        }

        private void SetPageConfig(Object o)
        {
            configPageVm.UpdateConfig();
            CurrentView = configPageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.Config;
        }
        private void SetPageDownload(Object o)
        {
            CurrentView = downloadPageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.Download;
        }

        private void SetPageFile(Object o)
        {
            CurrentView = fileOptPageVm;

            GlobalManager.Instance().CurrentPage = GlobalManager.Page.FileOpt;
        }

        private void UpdateConnectStatus(StatusBarOption option, string value)
        {
            if (option != StatusBarOption.ConnectStatus)
                return;

            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                ConnectStatus = value;
            });
        }
        private void UpdateCanErrInfo(StatusBarOption option, string value)
        {
            if (option != StatusBarOption.CanErrInfo)
                return;

            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                CanErrInfo = value;
            });

            CommManager.Instance().GetCanHelper().RsetEcanMode();
        }
        private void UpdataUILock(bool locked)
        {
            UILocked = locked;
        }
        private void UpdateConnectDevs(StatusBarOption option, string value)
        {
            if (option != StatusBarOption.ConnectDevs)
                return;

            string[] devs = value.Split(',');
            List<string> list = new List<string>();
            list.Add("无");
            list.AddRange(devs);

            // 对比两个集合是否一致

            if (!string.Join('-', ConnectDevs.ToList()).Equals(string.Join('-', list)))
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    ConnectDevs = list;
                });
            }
        }
        private void UpdateSelectDev(StatusBarOption option, string value)
        {
            if (option != StatusBarOption.SelectDev)
                return;

            int stop = 10;
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                SelectDev = value;
            });
        }
        [RelayCommand]
        private void OpenLogInfoPannel()
        {
            if (logInfoWndClose)
            {
                logInfoWnd = new LogInfoWnd();
                logInfoWnd.Closed += LogInfoWnd_Closed;
                logInfoWnd.Topmost = true;
                logInfoWnd.Show();
                logInfoWndClose = false;
            }
        }
        private void LogInfoWnd_Closed(object? sender, EventArgs e)
        {
            logInfoWndClose = true;
        }
        #endregion

        /// <summary>
        /// 检查配置文件
        /// </summary>
        private void CheckConfigFile()
        {
            string cfgFilePath = AppCfgHelper.ReadField("配置文件路径");
            if (cfgFilePath != null && cfgFilePath != "")
            {
                if (!File.Exists(cfgFilePath))
                {
                    JsonConfigHelper.SetDefaultCfgPath();
                }
            }
            else
            {
                JsonConfigHelper.SetDefaultCfgPath();
            }
        }

    }//class
}
