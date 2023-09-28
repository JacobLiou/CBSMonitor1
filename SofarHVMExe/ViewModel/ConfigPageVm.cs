using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPOI.HSSF.Record.Chart;
using SofarHVMDAL;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SofarHVMExe.ViewModel
{
    internal class ConfigPageVm : ViewModelBase
    {
        public ConfigPageVm()
        {
            Init();
        }

        #region 字段
        private ProjectCfgPageVm pfjCfgPageVm = new ProjectCfgPageVm();
        private MCUCfgPageVm mcuCfgPageVm = new MCUCfgPageVm();
        private CANFrameCfgPageVm canFrameCfgPageVm = new CANFrameCfgPageVm();
        private MainParamDisPageVm mainParamDisPageVm = new MainParamDisPageVm();
        private EventGroupPageVm eventGroupPageVm = new EventGroupPageVm();
        private CommandGroupPageVm cmdGroupPageVm = new CommandGroupPageVm();
        #endregion


        #region 属性
        public ObservableCollection<DeviceTreeModel> DeviceTreeModels { get; set; } = new ObservableCollection<DeviceTreeModel>();
        public object SelectItem { get; set; }

        //页面切换属性
        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 导出文件路径
        /// </summary>
        private string exportFilePath = "";

        private string cfgFilePath = "";
        public string CfgFilePath
        {
            get => cfgFilePath;
            set { cfgFilePath = value; OnPropertyChanged(); }
        }

        private bool _isPrjCfgSelected = false;
        public bool IsPrjCfgSelected
        {
            get => _isPrjCfgSelected;
            set
            {
                _isPrjCfgSelected = value;
                if (_isPrjCfgSelected)
                {
                    pfjCfgPageVm = new ProjectCfgPageVm();
                    pfjCfgPageVm.GetPrjCfgModel();
                    CurrentView = pfjCfgPageVm;
                }
                OnPropertyChanged();
            }
        }

        private bool _isMCUCfgSelected = false;
        public bool IsMCUCfgSelected
        {
            get => _isMCUCfgSelected;
            set
            {
                _isMCUCfgSelected = value;
                if (_isMCUCfgSelected)
                {
                    mcuCfgPageVm = new MCUCfgPageVm();
                    mcuCfgPageVm.UpdateModel();
                    CurrentView = mcuCfgPageVm;
                }
                OnPropertyChanged();
            }
        }

        private bool _isCANFrameCfgSelected = false;
        public bool IsCANFrameCfgSelected
        {
            get => _isCANFrameCfgSelected;
            set
            {
                _isCANFrameCfgSelected = value;
                if (_isCANFrameCfgSelected)
                {
                    //canFrameCfgPageVm = new CANFrameCfgPageVm();
                    canFrameCfgPageVm.UpdateModel();
                    CurrentView = canFrameCfgPageVm;
                }
                OnPropertyChanged();
            }
        }

        private bool _isMainParamDisSelected = false;
        public bool IsMainParamDisSelected
        {
            get => _isMainParamDisSelected;
            set
            {
                _isMainParamDisSelected = value;
                if (_isMainParamDisSelected)
                {
                    mainParamDisPageVm = new MainParamDisPageVm();
                    mainParamDisPageVm.UpdateModel();
                    CurrentView = mainParamDisPageVm;
                }
                OnPropertyChanged();
            }
        }
        #endregion

        #region 命令
        public ICommand LoadCfgFileCommand { get; set; }
        public ICommand ExportCfgFileCommand { get; set; }
        public ICommand SelectItemChangeCmd => new RelayCommand<DeviceTreeModel>(SelectItemChange);
        #endregion

        #region 成员方法
        private void Init()
        {
            LoadCfgFileCommand = new SimpleCommand(LoadCfgFile);
            ExportCfgFileCommand = new SimpleCommand(ExportCfgFile);

            DeviceTreeModels.Clear();
            DeviceTreeModels.Add(new DeviceTreeModel("项目配置", 0, 0));
            DeviceTreeModels.Add(new DeviceTreeModel("CAN配置", 10, 10));
            DeviceTreeModels.Add(new DeviceTreeModel("安规配置", 40, -1, new DeviceTreeModel[] {
                                                                        new DeviceTreeModel("启动参数组",41,40),
                                                                        new DeviceTreeModel("电压参数组",42,40),
                                                                        new DeviceTreeModel("频率参数组",43,40),
                                                                        new DeviceTreeModel("DCI保护参数组",44,40),
                                                                        new DeviceTreeModel("远程及有功参数组",45,40),
                                                                        new DeviceTreeModel("频率有功参数组",46,40),
                                                                        new DeviceTreeModel("无功参数组",47,40),
                                                                        new DeviceTreeModel("电压穿越参数组",48,40),
                                                                        new DeviceTreeModel("ISO孤岛参数组",49,40),}));
            DeviceTreeModel[] cmdTreeModels = new DeviceTreeModel[11];
            for (int i = 0; i < cmdTreeModels.Length; i++)
            {
                string name = $"命令组{i}";

                if (i == cmdTreeModels.Length - 1)
                    name = "广播命令";

                cmdTreeModels[i] = new DeviceTreeModel(name, (i + 1) + 60, 60);
            }
            DeviceTreeModels.Add(new DeviceTreeModel("命令组配置", 60, -1, cmdTreeModels));
            DeviceTreeModel[] eventTreeModels = new DeviceTreeModel[50];
            for (int i = 0; i < 50; i++)
            {
                eventTreeModels[i] = new DeviceTreeModel($"事件组{i}", i + 31, 30);
            }
            DeviceTreeModels.Add(new DeviceTreeModel("事件配置", 30, -1, eventTreeModels));

            //设置model数据
            CfgFilePath = AppCfgHelper.ReadField("配置文件路径");

            FileConfigModel? newConfigModel = JsonConfigHelper.ReadConfigFile();
            if (newConfigModel == null)
            {
                //如果没有正常从文件加载，则使用一个新的配置来初始化
                newConfigModel = new FileConfigModel();
                newConfigModel.InitModels();

                // 初始化数据库数据

                #region 暂时使用

                //保存一个初始化的配置到文件中，以供其他地方使用（否则其他地方ReadConfigFile将为空null）；
                JsonConfigHelper.SetDefaultCfgPath();
                CfgFilePath = AppCfgHelper.ReadField("配置文件路径");
                if (!JsonConfigHelper.WirteConfigFile_Data(newConfigModel))
                {
                    MessageBox.Show("初始化配置到json文件失败！", "提示");
                }
                #endregion
            }

            //默认选择项目配置，这个必须放在后面
            IsPrjCfgSelected = true;
        }
        /// <summary>
        /// 树形菜单触发事件
        /// </summary>
        /// <param name="obj">选择菜单对象</param>
        public void SelectItemChange(DeviceTreeModel obj)
        {
            if (obj == null) return;

            switch (obj.ParentId)
            {
                case 0:
                    pfjCfgPageVm = new ProjectCfgPageVm();
                    pfjCfgPageVm.GetPrjCfgModel();
                    CurrentView = pfjCfgPageVm;
                    break;
                case 10:
                    canFrameCfgPageVm.UpdateModel();
                    CurrentView = canFrameCfgPageVm;
                    break;
                case 60:
                    int commandNumber;
                    bool isBoardCor = int.TryParse(obj.Name.Substring(3), out commandNumber);
                    if (!isBoardCor) commandNumber = 10;

                    Switch2CommandGrpPage(commandNumber);
                    break;
                case 30:
                    string eventNumber = obj.Name.Substring(3);

                    Switch2EventGrpPage(Convert.ToInt32(eventNumber));
                    break;
                case 40:
                    int safetyNumber = obj.ID - (41 - 11);

                    Switch2CommandGrpPage(safetyNumber);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 重新选择配置文件
        /// </summary>
        /// <param name="o"></param>
        private void LoadCfgFile(object o)
        {
            //加载配置文件
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "打开配置文件";
            dlg.Filter = "配置文件(*.json)|*.json";
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            CfgFilePath = dlg.FileName;
            var load = new LoadWindow();
            load.DoWork = LoadConfigInfo;
            load.Owner = App.Current.MainWindow;
            load.ShowDialog();

            MessageBox.Show("更新配置成功", "提示");
        }

        /// <summary>
        /// 导出配置文件
        /// </summary>
        /// <param name="o"></param>
        private void ExportCfgFile(object o)
        {
            //加载配置文件
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "导出配置文件";
            sfd.Filter = "配置文件(*.json)|*.json";
            sfd.RestoreDirectory = true;
            if (sfd.ShowDialog() != true)
                return;

            exportFilePath = sfd.FileName;
            var load = new LoadWindow();
            load.DoWork = ExportConfigInfo;
            load.Owner = App.Current.MainWindow;
            load.ShowDialog();

            MessageBox.Show("导出配置成功", "提示");
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <returns></returns>
        private bool LoadConfigInfo()
        {
            AppCfgHelper.WriteField("配置文件路径", CfgFilePath);

            //读取配置文件内容更新model, 再更新界面显示
            UpdateConfig(true);

            // 刷新当前界面，重新获取数据源
            pfjCfgPageVm = new ProjectCfgPageVm();
            pfjCfgPageVm.GetPrjCfgModel();
            CurrentView = pfjCfgPageVm;
            return true;
        }

        /// <summary>
        /// 导出配置文件
        /// </summary>
        /// <returns></returns>
        private bool ExportConfigInfo()
        {
            // 加载数据库文件,读取数据库
            JsonConfigHelper.FileConfig = DataManager.ReadData();

            // 导出配置文件
            JsonConfigHelper.WirteConfigFile(exportFilePath);

            return true;
        }

        /// <summary>
        /// 切换到事件组页面
        /// </summary>
        /// <param name="groupNumber">事件组编号</param>
        private void Switch2EventGrpPage(int groupNumber)
        {
            eventGroupPageVm.ChangeGroup(groupNumber);
            eventGroupPageVm.UpdateModel(-1);
            CurrentView = eventGroupPageVm;
        }

        /// <summary>
        /// 切换到命令组页面
        /// </summary>
        /// <param name="groupNumber">命令组编号</param>
        private void Switch2CommandGrpPage(int groupNumber)
        {
            cmdGroupPageVm = new CommandGroupPageVm();
            cmdGroupPageVm.ChangeGroup(groupNumber);
            cmdGroupPageVm.UpdateModel();
            CurrentView = cmdGroupPageVm;
        }

        /// <summary>
        /// 更新所有的配置
        /// </summary>
        public void UpdateConfig(bool isRefresh = false)
        {
            if (isRefresh)
            {
                JsonConfigHelper.ReadConfigFile_Temp();
            }

            //这里没有必要更新所有的，更新当前的页面数据即可
            ViewModelBase? vm = CurrentView as ViewModelBase;
            if (vm != null)
                return;

            //vm = CurrentView as ProjectCfgPageVm;
            //if (vm != null)
            //{
            //    vm.UpdateModel();
            //}

            //pfjCfgPageVm.UpdateModel(); 
            //mcuCfgPageVm.UpdateModel(configModel);
            //canFrameCfgPageVm.UpdateModel(configModel);
            //mainParamDisPageVm.UpdateModel(configModel);
        }

        /// <summary>
        /// 注册更新到Can帧配置中
        /// </summary>
        /// <param name="method"></param>
        public void RegisterUpdate2CanConfig(Action method)
        {
            canFrameCfgPageVm.RegisterUpdate(method);
        }
        #endregion

    }//class
}
