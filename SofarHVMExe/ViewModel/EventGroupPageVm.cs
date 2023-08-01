using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SofarHVMExe.ViewModel
{
    class EventGroupPageVm : ViewModelBase
    {
        public EventGroupPageVm()
        {
            Init();
        }

        #region 字段
        private FileConfigModel? fileCfgModel = null;
        private EventGroupModel? currEventGrpModel = null;
        //private EventGroupModel? tempEventGrpModel = null;//临时事件组
        #endregion

        #region 属性
        private int groupNumber = 0;
        public int GroupNumber
        {
            get => groupNumber;
            set
            {
                groupNumber = value;
                OnPropertyChanged();
            }
        }
        private bool eventEnable = true;
        public bool EventEnable
        {
            get => currEventGrpModel.Enable;
            set
            {
                currEventGrpModel.Enable = value;
                OnPropertyChanged();
            }
        }
        private int canIdIndex = 0;
        public int CanIdIndex
        {
            get => canIdIndex;
            set
            {
                canIdIndex = value;
                if (canIdIndex != -1)
                {
                    List<CanFrameModel> frameModels = fileCfgModel.FrameModel.CanFrameModels;
                    currEventGrpModel.FrameGuid = frameModels[canIdIndex].Guid;
                }
                else
                {
                    currEventGrpModel.FrameGuid = "";
                }

                OnPropertyChanged();
                UpdateMemberList();
                MemberIndex = -1;
            }
        }
        private int memberIndex = -1;
        public int MemberIndex
        {
            get => memberIndex;
            set
            {
                memberIndex = value;
                if (memberIndex != -1)
                {
                    currEventGrpModel.MemberIndex = memberIndex;
                }
                UpdateMemberSize();
                OnPropertyChanged();
            }
        }
        string memberSize = "";
        public string MemberSize
        {
            get => memberSize;
            set
            {
                memberSize = value;
                OnPropertyChanged();
            }
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
        private List<string> memberList = new List<string>();
        public List<string> MemberList
        {
            get => memberList;
            set
            {
                memberList = value;
                OnPropertyChanged();
            }
        }
        private List<EventInfoModel> dataSource = new List<EventInfoModel>();
        public List<EventInfoModel> DataSource
        {
            get => dataSource;
            set
            {
                dataSource = value;
                OnPropertyChanged();
            }
        }
        private List<ComboBoxItemModel<EventGroupModel>> eventModels = new List<ComboBoxItemModel<EventGroupModel>>();
        public List<ComboBoxItemModel<EventGroupModel>> EventModels
        {
            get => eventModels;
            set
            {
                eventModels = value;
                OnPropertyChanged();
            }
        }
        private EventGroupModel selectedModel;
        public EventGroupModel SelectedModel
        {
            get => selectedModel;
            set
            {
                selectedModel = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 命令
        public ICommand SaveCommand { get; set; }
        public ICommand ImportCommand { get; set; }
        #endregion

        #region 成员方法
        private void Init()
        {
            SaveCommand = new SimpleCommand(SaveData);
            ImportCommand = new SimpleCommand(ImportData);
        }

        private void ImportData(object obj)
        {
            UpdateModel(selectedModel.Group);
        }

        /// <summary>
        /// 修改组编号
        /// </summary>
        /// <param name="groupNumber"></param>
        public void ChangeGroup(int groupNum)
        {
            GroupNumber = groupNum;
        }

        /// <summary>
        /// 更新model数据
        /// </summary>
        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel == null)
                return;

            List<EventGroupModel> eventModels = fileCfgModel.EventModels;
            var findModel = eventModels.Find((model) => model.Group == groupNumber);
            if (findModel != null)
            {
                currEventGrpModel = findModel;
            }
            else
            {
                currEventGrpModel = new EventGroupModel(groupNumber);
                eventModels.Add(currEventGrpModel);
            }

            //初始化32条事件
            if (currEventGrpModel.InfoModels.Count == 0)
            {
                currEventGrpModel.Init();
            }

            //初始化现有事件组的源数据
            EventModels.Clear();

            for (int i = 0; i < eventModels.Count; i++)
            {
                if (!fileCfgModel.EventModels[i].Enable) continue;

                ComboBoxItemModel<EventGroupModel> model = new ComboBoxItemModel<EventGroupModel> { Description = $"事件组{fileCfgModel.EventModels[i].Group}", SelectedModel = fileCfgModel.EventModels[i] };
                EventModels.Add(model);
            }
            SelectedModel = EventModels[0].SelectedModel;

            EventEnable = currEventGrpModel.Enable;
            UpdateCanId();
            MemberIndex = currEventGrpModel.MemberIndex;
            UpdateSource();
        }

        /// <summary>
        /// 更新model数据
        /// </summary>
        /// <param name="_groupNum">进入-1/导入Number</param>
        public void UpdateModel(int _groupNum = -1)
        {
            try
            {
                fileCfgModel = JsonConfigHelper.ReadConfigFile();
                if (fileCfgModel == null)
                    return;

                List<EventGroupModel> eventModels = fileCfgModel.EventModels;
                var findModel = eventModels.Find((model) => model.Group == groupNumber);
                if (findModel != null)
                {
                    //currEventGrpModel = findModel;
                    if (_groupNum == -1)
                    {
                        currEventGrpModel = findModel;
                    }
                    else
                    {
                        selectedModel.Group = currEventGrpModel.Group;
                        currEventGrpModel = selectedModel;
                    }
                }
                else
                {
                    currEventGrpModel = new EventGroupModel(groupNumber);
                    eventModels.Add(currEventGrpModel);
                }

                //初始化32条事件
                if (currEventGrpModel.InfoModels.Count == 0)
                {
                    currEventGrpModel.Init();
                }

                //初始化现有事件组的源数据
                EventModels.Clear();
                for (int i = 0; i < eventModels.Count; i++)
                {
                    if (!fileCfgModel.EventModels[i].Enable) continue;

                    ComboBoxItemModel<EventGroupModel> model = new ComboBoxItemModel<EventGroupModel> { Description = $"事件组{fileCfgModel.EventModels[i].Group}", SelectedModel = fileCfgModel.EventModels[i] };
                    EventModels.Add(model);
                }

                EventEnable = currEventGrpModel.Enable;
                UpdateCanId();
                MemberIndex = currEventGrpModel.MemberIndex;
                UpdateSource();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 保存model数据
        /// </summary>
        /// <param name="o"></param>
        private void SaveData(object o)
        {
            if (selectedModel != null)
            fileCfgModel.EventModels[selectedModel.Group] = currEventGrpModel;

            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }

        /// <summary>
        /// 更新Canid
        /// </summary>
        private void UpdateCanId()
        {
            if (fileCfgModel == null)
                return;

            //1、更新id下拉集合
            List<string> list = new List<string>();
            List<CanFrameModel> frameModels = fileCfgModel.FrameModel.CanFrameModels;
            foreach (CanFrameModel model in frameModels)
            {
                string idInfo = $"0x{model.Id.ToString("X")}({model.Name})";
                list.Add(idInfo);
            }
            CanIdList.Clear();
            CanIdList = list;

            //2、更新选中id
            int index = fileCfgModel.FrameModel.FindFrameModelIndex(currEventGrpModel.FrameGuid);
            CanIdIndex = index;
        }

        /// <summary>
        /// 更新成员显示
        /// </summary>
        private void UpdateMemberList()
        {
            if (fileCfgModel == null)
                return;

            MemberList.Clear();

            List<CanFrameModel> frameModels = fileCfgModel.FrameModel.CanFrameModels;
            List<string> list = new List<string>();

            //更新成员集合
            if (CanIdIndex >= 0)
            {
                CanFrameModel frameModel = frameModels[CanIdIndex];
                CanFrameData frameData = frameModel.FrameDatas[0];
                foreach (var dataInfo in frameData.DataInfos)
                {
                    list.Add(dataInfo.Name);
                }
                MemberList = list;
            }
        }

        /// <summary>
        /// 更新数据源
        /// </summary>
        private void UpdateSource()
        {
            //事件信息数据源
            DataSource = currEventGrpModel.InfoModels;
        }

        /// <summary>
        /// 更新成员字节大小
        /// </summary>
        private void UpdateMemberSize()
        {
            if (MemberIndex < 0)
            {
                MemberSize = "";
                return;
            }

            List<CanFrameModel> frameModels = fileCfgModel.FrameModel.CanFrameModels;
            CanFrameModel frameModel = frameModels[CanIdIndex];
            CanFrameData frameData = frameModel.FrameDatas[0];

            //关闭所有事件使能
            foreach (EventInfoModel model in currEventGrpModel.InfoModels)
            {
                model.Enable = false;
            }

            if (MemberIndex < frameData.DataInfos.Count)
            {
                CanFrameDataInfo dataInfo = frameData.DataInfos[MemberIndex];
                MemberSize = dataInfo.ByteRange.ToString() + "字节";

                //设置事件使能
                int num = dataInfo.ByteRange * 8;
                for (int i = 0; i < num; i++)
                {
                    currEventGrpModel.InfoModels[i].Enable = true;
                }
            }
        }
        #endregion

    }//class

    public class ComboBoxItemModel<T>
    {
        public string Description { get; set; }
        public T SelectedModel { get; set; }
    }
}
