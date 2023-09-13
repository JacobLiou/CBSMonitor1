using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.ViewModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace SofarHVMExe.ViewModel
{
    /// <summary>
    /// 主参数配置ViewModel
    /// </summary>
    class MainParamDisPageVm : ViewModelBase
    {
        public MainParamDisPageVm()
        {
            Init();
            SaveCommand = new SimpleCommand(SaveData);
        }

        private FrameConfigModel frameModel = null;
        //private FileConfigModel? fileCfgModel = null;
        private MonitorConfigModel? monCfgModel1 = null;
        private MonitorConfigModel? monCfgModel2 = null;


        public int CanIdIndex1
        {
            get
            {
                return GetIdIndex(monCfgModel1.CanText);
            }
            set
            {
                if (value < 0) return;
                List<CanFrameModel> frameModels = frameModel.CanFrameModels;
                CanFrameModel model = frameModels[value];
                monCfgModel1.CanText = $"0x{model.Id.ToString("X")}({model.Name})";
                UpdateMemberList();
                OnPropertyChanged();
            }
        }
        public int CanIdIndex2
        {
            get
            {
                return GetIdIndex(monCfgModel2.CanText);
            }
            set
            {
                if (value < 0) return;
                List<CanFrameModel> frameModels = frameModel.CanFrameModels;
                CanFrameModel model = frameModels[value];
                monCfgModel2.CanText = $"0x{model.Id.ToString("X")}({model.Name})";
                UpdateMemberList();
                OnPropertyChanged();
            }
        }
        public int MemberIndex1
        {
            get => GetMemberIndex(monCfgModel1.MemberText, memberList1);
            set
            {
                if (value < 0) return;
                monCfgModel1.MemberText = memberList1[value];
                OnPropertyChanged();
            }
        }
        public int MemberIndex2
        {
            get => GetMemberIndex(monCfgModel2.MemberText, memberList2);
            set
            {
                if (value < 0) return;
                monCfgModel2.MemberText = memberList2[value];
                OnPropertyChanged();
            }
        }
        public double UpRedAlertVal1
        {
            get => monCfgModel1.AlertVal1;
            set { monCfgModel1.AlertVal1 = value; OnPropertyChanged(); }
        }
        public double UpRedAlertVal2
        {
            get => monCfgModel2.AlertVal1;
            set { monCfgModel2.AlertVal1 = value; OnPropertyChanged(); }
        }
        public double UpYellowAlertVal1
        {
            get => monCfgModel1.AlertVal2;
            set { monCfgModel1.AlertVal2 = value; OnPropertyChanged(); }
        }
        public double UpYellowAlertVal2
        {
            get => monCfgModel2.AlertVal2;
            set { monCfgModel2.AlertVal2 = value; OnPropertyChanged(); }
        }
        public double DownYellowAlertVal1
        {
            get => monCfgModel1.AlertVal3;
            set { monCfgModel1.AlertVal3 = value; OnPropertyChanged(); }
        }
        public double DownYellowAlertVal2
        {
            get => monCfgModel2.AlertVal3;
            set { monCfgModel2.AlertVal3 = value; OnPropertyChanged(); }
        }
        public double DownRedAlertVal1
        {
            get => monCfgModel1.AlertVal4;
            set { monCfgModel1.AlertVal4 = value; OnPropertyChanged(); }
        }
        public double DownRedAlertVal2
        {
            get => monCfgModel2.AlertVal4;
            set { monCfgModel2.AlertVal4 = value; OnPropertyChanged(); }
        }


        private List<string> canIdList = new List<string>();
        public List<string> CanIdList
        {
            get => canIdList;
            set
            {
                canIdList = value;
                OnPropertyChanged();
            }
        }

        private List<string> memberList1 = new List<string>();
        public List<string> MemberList1
        {
            get => memberList1;
            set
            {
                memberList1 = value;
                OnPropertyChanged();
            }
        }

        private List<string> memberList2 = new List<string>();
        public List<string> MemberList2
        {
            get => memberList2;
            set
            {
                memberList2 = value;
                OnPropertyChanged();
            }
        }



        public ICommand SaveCommand { get; set; }


        private void Init()
        {
            //读取配置文件获取主参数数据

            //test code
            {
                //monCfgModel1 = new MonitorConfigModel("title", "0x90010000", "0x90010000", "0(Vout)", "1(Iout)", 
                //                                     270, 180, 80, 160, -0.2, -0.4, -0.5, -0.8);

            }
        }

        /// <summary>
        /// 更新model数据
        /// </summary>
        public void UpdateModel()
        {
            frameModel = DataManager.GetFrameConfigModel();
            var monCfgModels = DataManager.GetMonitorConfigModel();
            if (monCfgModels != null && monCfgModels.Count == 2)
            {
                monCfgModel1 = monCfgModels[0];
                monCfgModel2 = monCfgModels[1];
            }
            else
            {
                monCfgModel1 = new MonitorConfigModel("主参数1", "", "", 0, 0, 0, 0);
                monCfgModel2 = new MonitorConfigModel("主参数2", "", "", 0, 0, 0, 0);
            }

            UpdateCanIdList();
            UpdateParams();
        }

        /// <summary>
        /// 更新Canid下拉框数据源
        /// </summary>
        private void UpdateCanIdList()
        {
            List<string> list = new List<string>();
            List<CanFrameModel> frameModels = frameModel.CanFrameModels;
            foreach (CanFrameModel model in frameModels)
            {
                string idInfo = $"0x{model.Id.ToString("X")}({model.Name})";
                list.Add(idInfo);
            }
            CanIdList.Clear();
            CanIdList = list;
        }

        /// <summary>
        /// 更新参数值
        /// </summary>
        private void UpdateParams()
        {
            CanIdIndex1 = GetIdIndex(monCfgModel1.CanText);
            MemberIndex1 = GetMemberIndex(monCfgModel1.MemberText, memberList1);
            UpRedAlertVal1 = monCfgModel1.AlertVal1;
            UpYellowAlertVal1 = monCfgModel1.AlertVal2;
            DownYellowAlertVal1 = monCfgModel1.AlertVal3;
            DownRedAlertVal1 = monCfgModel1.AlertVal4;

            MemberIndex2 = GetMemberIndex(monCfgModel2.MemberText, memberList2);
            UpRedAlertVal2 = monCfgModel2.AlertVal1;
            UpYellowAlertVal2 = monCfgModel2.AlertVal2;
            DownYellowAlertVal2 = monCfgModel2.AlertVal3;
            DownRedAlertVal2 = monCfgModel2.AlertVal4;
        }

        /// <summary>
        /// 更新成员显示数据源
        /// id成员集合
        /// </summary>
        private void UpdateMemberList()
        {
            MemberList1.Clear();
            MemberList2.Clear();

            List<CanFrameModel> frameModels = frameModel.CanFrameModels;
            List<string> list1 = new List<string>();
            List<string> list2 = new List<string>();

            //参数1的成员集合
            if (CanIdIndex1 >= 0)
            {
                CanFrameModel canFrameModel1 = frameModels[CanIdIndex1];
                CanFrameData frameData = canFrameModel1.FrameDatas[0];
                foreach (var dataInfo in frameData.DataInfos)
                {
                    list1.Add(dataInfo.Name);
                }
                MemberList1 = list1;
            }

            //参数2的成员集合
            if (CanIdIndex2 >= 0)
            {
                CanFrameModel canFrameModel2 = frameModels[CanIdIndex2];
                CanFrameData frameData = canFrameModel2.FrameDatas[0];
                foreach (var dataInfo in frameData.DataInfos)
                {
                    list2.Add(dataInfo.Name);
                }
                MemberList2 = list2;
            }
        }

        /// <summary>
        /// 查找对于Id名称所在的索引号
        /// </summary>
        /// <param name="canText"></param>
        /// <returns></returns>
        private int GetIdIndex(string canText)
        {
            List<CanFrameModel> frameModels = frameModel.CanFrameModels;
            int index = frameModels.FindIndex(frameModel =>
            {
                string text = $"0x{frameModel.Id.ToString("X")}({frameModel.Name})";
                return (canText == text) ? true : false;
            });

            return index;
        }

        /// <summary>
        /// 查找字段名称所在索引号
        /// </summary>
        /// <param name="memberText"></param>
        /// <returns></returns>
        private int GetMemberIndex(string memberText, List<string> memberList)
        {
            return memberList.FindIndex(text =>
            {
                if (text == memberText)
                    return true;

                return false;
            });
        }

        private void SaveData(object o)
        {
            //fileCfgModel?.MonModels.Clear();
            //fileCfgModel?.MonModels.Add(monCfgModel1);
            //fileCfgModel?.MonModels.Add(monCfgModel2);

            List<MonitorConfigModel> monitorConfigs = new List<MonitorConfigModel>();
            monitorConfigs.Add(monCfgModel1);
            monitorConfigs.Add(monCfgModel2);

            if (DataManager.UpdateMonitorConfigModel(monitorConfigs))
            {
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }

    }//class
}
