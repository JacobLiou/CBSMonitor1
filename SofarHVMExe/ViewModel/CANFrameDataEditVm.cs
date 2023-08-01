using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static SofarHVMExe.ViewModel.CANFrameDataConfigVm;

namespace SofarHVMExe.ViewModel
{
    /// <summary>
    /// Can帧数据编辑ViewModel
    /// </summary>
    internal class CANFrameDataEditVm : ViewModelBase
    {

        public CANFrameDataEditVm(FileConfigModel fileModel, CmdConfigModel selectModel)
        {
            fileCfgModel = fileModel;
            currentCmdCfgModel = selectModel;

            Init();
        }

        private FileConfigModel fileCfgModel = null;
        private CmdConfigModel currentCmdCfgModel = null;
        private Action updateAction;


        #region 属性

        public bool isContinue = false;
        public bool IsContinue
        {
            get => isContinue;
            set
            {
                isContinue = value;
                OnPropertyChanged();
            }
        }

        public List<string> FuncCodeSource { get; set; }
        public List<string> SrcDeviceIdSource { get; set; }
        public List<string> TargetDeviceIdSource { get; set; }

        private ObservableCollection<CanFrameDataInfo> dataSource = new ObservableCollection<CanFrameDataInfo>();
        public ObservableCollection<CanFrameDataInfo> DataSource
        {
            get => dataSource;
            set { dataSource = value; OnPropertyChanged(); }
        }

        private ObservableCollection<CanFrameDataInfo> multyDataSource = new ObservableCollection<CanFrameDataInfo>();
        public ObservableCollection<CanFrameDataInfo> MultyDataSource
        {
            get => multyDataSource;
            set { multyDataSource = value; OnPropertyChanged(); }
        }

        public CanFrameDataInfo SelectDataInfo { get; set; }
        #endregion


        public ICommand SaveCommand { get; set; }


        #region 初始化和更新
        private void Init()
        {
            SaveCommand = new SimpleCommand(SaveData);

            UpdateModel(fileCfgModel);
        }
        /// <summary>
        /// 更新model数据
        /// </summary>
        /// <param name="fileCfgModel"></param>
        public void UpdateModel(FileConfigModel model)
        {
            fileCfgModel = model;
            UpdateFrameDate();

        }//func
        private void UpdateFrameDate()
        {
            CanFrameModel? _currFrameModel = currentCmdCfgModel.FrameModel;
            if (_currFrameModel == null)
                return;

            CanFrameID frameId = _currFrameModel.FrameId;
            List<CanFrameData> frameDatas = _currFrameModel.FrameDatas;
            CanFrameData frameData = frameDatas[0];
            if (frameData != null)
            {
                if (frameId.ContinuousFlag == 0)
                {
                    //非连续帧
                    IsContinue = false;
                    DataSource = frameData.DataInfos;
                }
                else
                {
                    //连续帧
                     IsContinue = true;
                     MultyDataSource = frameData.DataInfos;
                }
            }
        }//func 
        #endregion


        #region 保存
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="o"></param>
        private void SaveData(object o)
        {
            CanFrameModel? _currFrameModel = currentCmdCfgModel.FrameModel;
            if (_currFrameModel == null)
                return;

            //保存到文件
            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                updateAction?.Invoke();
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }//func
        #endregion

        /// <summary>
        /// 注册更新，用于修改并保存帧数据后，CAN帧表同步更新
        /// </summary>
        /// <param name="method"></param>
        public void RegisterUpdate(Action method)
        {
            updateAction += method;
        }

    }//class
}
