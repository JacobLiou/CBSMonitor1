using CanProtocol.ProtocolModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace SofarHVMExe.ViewModel
{
    partial class CommandGroupPageVm : ObservableObject
    {
        public CommandGroupPageVm()
        {
            Init();
        }


        private int groupNumber = 0;
        private FileConfigModel? fileCfgModel = null;
        private CmdGrpConfigModel? currentCmdGrpModel = null;
        public Action DataSrcChangeAction;


        public string CmdGrpNumberText
        {
            get { return $"命令组{groupNumber}"; }
        }
        public string CmdGrpName
        {
            get
            {
                return currentCmdGrpModel.Name;
            }
            set
            {
                currentCmdGrpModel.Name = value;
                OnPropertyChanged();
            }
        }

        private BindingList<CmdConfigModel> dataSource = null;
        public BindingList<CmdConfigModel> DataSource 
        {
            get => dataSource;
            set
            {
                dataSource = value;
                OnPropertyChanged();
            }
        }

        private List<FrameItemData> canFrameList = new List<FrameItemData>();
        public List<FrameItemData> CanFrameList
        {
            get => canFrameList;
            set
            {
                canFrameList = value;
                OnPropertyChanged();
            }
        }
        private CmdConfigModel selectData = null;
        public CmdConfigModel SelectData
        {
            get => selectData;
            set
            {
                selectData = value;
                OnPropertyChanged();
            }
        }


        public ICommand SetDataCommand { get; set; }
        public ICommand SaveCommand { get; set; }


        private void Init()
        {
            SetDataCommand = new SimpleCommand(SetData);
            SaveCommand = new SimpleCommand(SaveData);
        }
        public void ChangeGroup(int groupNum)
        {
            groupNumber = groupNum;
        }
        /// <summary>
        /// 更新model数据
        /// </summary>
        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel == null)
                return;

            currentCmdGrpModel = fileCfgModel.CmdModels[groupNumber];


            UpdateSource();
        }
        /// <summary>
        /// 更新数据源
        /// </summary>
        private void UpdateSource()
        {
            //表格数据源
            DataSource = new BindingList<CmdConfigModel>(currentCmdGrpModel.cmdConfigModels);
            DataSource.ListChanged += DataSource_ListChanged;

            //id列下拉框数据源
            List<FrameItemData> frameList = new List<FrameItemData>();
            var canFrameModels = fileCfgModel.FrameModel.CanFrameModels;
            for (int i = 0; i < canFrameModels.Count; i++)
            {
                CanFrameModel frame = canFrameModels[i];
                string text = $"0x{frame.Id.ToString("X")}({frame.Name})";
                frameList.Add(new FrameItemData(frame.Guid, text));
            }
            CanFrameList = frameList;
        }

        private void DataSource_ListChanged(object? sender, ListChangedEventArgs e)
        {
            DataSrcChangeAction?.Invoke();
            Save2File(false);
        }

        /// <summary>
        /// 更新选择命令的帧
        /// </summary>
        public void UpdateFrameModel(int index)
        {
            if (SelectData == null || index < 0 || fileCfgModel == null )
                return;

            //复制一个新的帧model对象给选择的命令
            FrameConfigModel frameCfgModel = fileCfgModel.FrameModel;
            CanFrameModel selectFrameModel = frameCfgModel.CanFrameModels[index];
            CanFrameModel newFrameModel = new CanFrameModel(selectFrameModel);
            SelectData.FrameGuid = newFrameModel.Guid;
            SelectData.FrameModel = newFrameModel;
        }

        [RelayCommand]
        private void AddNew()
        {
            CmdConfigModel newCmd = new CmdConfigModel();
            newCmd.CmdType = 1; //可设命令
            newCmd.FrameGuid = "";
            currentCmdGrpModel.cmdConfigModels.Add(newCmd);
            DataSource = new BindingList<CmdConfigModel>(currentCmdGrpModel.cmdConfigModels);
        }
        [RelayCommand]
        private void DeleteSelect()
        {
            if (SelectData == null)
            {
                MessageBox.Show("请选择一行数据！", "提示");
                return;
            }

            currentCmdGrpModel.cmdConfigModels.Remove(SelectData);
            DataSource = new BindingList<CmdConfigModel>(currentCmdGrpModel.cmdConfigModels);
        }
        [RelayCommand]
        private void MoveUp()
        {
            if (SelectData == null)
            {
                MessageBox.Show("请选择一行数据！", "提示");
                return;
            }

            List<CmdConfigModel> cmdConfigModels = new List<CmdConfigModel>(DataSource);
            int index = DataSource.IndexOf(SelectData);
            if (index == -1 || index == 0)
                return;

            CmdConfigModel curr = cmdConfigModels[index];
            CmdConfigModel before = cmdConfigModels[index - 1];
            cmdConfigModels[index] = before;
            cmdConfigModels[index - 1] = curr;

            DataSource = new BindingList<CmdConfigModel>(cmdConfigModels);
            fileCfgModel.CmdModels[groupNumber].cmdConfigModels = new List<CmdConfigModel>(DataSource);
            UpdateSetValue();
            Save2File(false);
        }
        [RelayCommand]
        private void MoveDown()
        {
            if (SelectData == null)
            {
                MessageBox.Show("请选择一行数据！", "提示");
                return;
            }

            List<CmdConfigModel> cmdConfigModels = new List<CmdConfigModel>(DataSource);
            int index = DataSource.IndexOf(SelectData);
            if (index == -1 || (index == cmdConfigModels.Count - 1))
                return;

            CmdConfigModel curr = cmdConfigModels[index];
            CmdConfigModel after = cmdConfigModels[index + 1];
            cmdConfigModels[index] = after;
            cmdConfigModels[index + 1] = curr;

            DataSource = new BindingList<CmdConfigModel>(cmdConfigModels);
            fileCfgModel.CmdModels[groupNumber].cmdConfigModels = new List<CmdConfigModel>(DataSource);
            UpdateSetValue();
            Save2File(false);
        }
        private void SetData(object o)
        {
            if (SelectData == null)
                return;

            if (SelectData.FrameModel == null)
            {
                MessageBox.Show("未选择ID！", "提示");
                return;
            }

            CANFrameDataEditVm vm = new CANFrameDataEditVm(fileCfgModel, SelectData);
            CANFrameDataEditWnd wnd = new CANFrameDataEditWnd();
            byte continueFlg = SelectData.FrameModel.FrameId.ContinuousFlag;
            vm.isContinue = continueFlg == 0 ? false : true;
            vm.RegisterUpdate(UpdateModel);
            wnd.DataContext = vm;
            wnd.ShowDialog();
        }
        private void SaveData(object o)
        {
            fileCfgModel.CmdModels[groupNumber].cmdConfigModels = new List<CmdConfigModel>(DataSource);
            UpdateSetValue();
            Save2File();
        }
        private void UpdateSetValue()
        {
            foreach (var cmdModel in currentCmdGrpModel.cmdConfigModels)
            {
                cmdModel.FrameDatas2SetValue();
            }
        }
        private void Save2File(bool hint = true)
        {
            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                if (hint) MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                if (hint) MessageBox.Show("保存失败！", "提示");
            }
        }
    }//class


    public class FrameItemData
    {
        public FrameItemData(string guid, string text)
        {
            Guid = guid;
            Text = text;
        }

        public string Guid { get;set;}  
        public string Text { get;set;}
    }
}
