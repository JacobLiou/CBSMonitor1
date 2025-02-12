﻿using CanProtocol.ProtocolModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NPOI.SS.Formula.Functions;
using SofarHVMExe.Model;
using SofarHVMExe.Util;
using SofarHVMExe.Utilities;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using static SofarHVMExe.ViewModel.CANFrameDataConfigVm;

namespace SofarHVMExe.ViewModel
{
    partial class CANFrameCfgPageVm : ObservableObject
    {
        public CANFrameCfgPageVm()
        {
            Init();
        }

        #region 字段
        private FileConfigModel? fileCfgModel = null;
        private FrameConfigModel frameCfgModel = null;
        private CanFrameModel copyFrame = null;
        private byte pasteCount = 0;
        private Action updateAction;
        public Action DataSrcChangeAction;
        #endregion

        #region 属性
        /// <summary>
        /// 源设备id位数
        /// </summary>
        public string SrcIdBitNum
        {
            get => frameCfgModel.SrcIdBitNum.ToString();
            set
            {
                frameCfgModel.SrcIdBitNum = byte.Parse(value);
                frameCfgModel.SrcAddrBitNum = (byte)(8 - (int)frameCfgModel.SrcIdBitNum);
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// 目标设备id位数
        /// </summary>
        public string TargetIdBitNum
        {
            get => frameCfgModel.TargetIdBitNum.ToString();
            set
            {
                frameCfgModel.TargetIdBitNum = byte.Parse(value);
                frameCfgModel.TargetAddrBitNum = (byte)(8 - (int)frameCfgModel.TargetIdBitNum);
                OnPropertyChanged();
            }
        }
        public CanFrameModel SelectModel { get; set; }
        private BindingList<CanFrameModel> dataSource = null;
        public BindingList<CanFrameModel> DataSource
        {
            get => dataSource;
            set
            {
                dataSource = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 成员命令
        public ICommand AddNewCommand { get; set; }
        public ICommand EditSelectCommand { get; set; }
        public ICommand CopySelectCommand { get; set; }
        public ICommand PasteCommand { get; set; }
        public ICommand MoveUpCommand { get; set; }
        public ICommand DeleteSelectCommand { get; set; }

        #endregion

        #region 成员函数
        private void Init()
        {
            AddNewCommand = new SimpleCommand(AddNewFrame);
            EditSelectCommand = new SimpleCommand(EditSelFrame);
            CopySelectCommand = new SimpleCommand(CopySelFrame);
            PasteCommand = new SimpleCommand(PasteFrame);
            MoveUpCommand = new SimpleCommand(MoveUp);
            DeleteSelectCommand = new SimpleCommand(DeleteSelFrame);
        }

        /// <summary>
        /// 更新model数据
        /// </summary>
        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel == null)
                return;

            frameCfgModel = fileCfgModel.FrameModel;

            DataSource = new BindingList<CanFrameModel>(frameCfgModel.CanFrameModels);
            DataSource.ListChanged += DataSource_ListChanged;
#if false
            //重置所有帧的Guid，保持唯一性，这个只在有重复GUID的少数情况下打开重置
            //不要乱开，否则会导致命令组绑定帧失效
            {
                var frames = fileCfgModel.FrameModel.CanFrameModels;
                int count = frames.Count;
                for (int i = 0; i < count; i++)
                {
                    var frame = frames[i];
                    frame.Guid = Guid.NewGuid().ToString();
                }
                SaveData();
            }
#endif
        }

        private void DataSource_ListChanged(object? sender, ListChangedEventArgs e)
        {
            DataSrcChangeAction?.Invoke();
            //SaveData();

            DataManager.ReSortCanFrame(dataSource.ToList());
        }

        public void RegisterUpdate(Action method)
        {
            updateAction += method;
        }

        /// <summary>
        /// 增加新的一帧
        /// </summary>
        /// <param name="o"></param>
        private void AddNewFrame(object o)
        {
            CANFrameDataConfigVm vm = new CANFrameDataConfigVm(fileCfgModel, SelectModel, CANFrameDataConfigVm.Operation.Add);
            CANFrameDataConfigWnd wnd = new CANFrameDataConfigWnd();
            vm.RegisterUpdate(UpdateModel);
            vm.RegisterUpdate(updateAction);
            wnd.DataContext = vm;
            wnd.Show();
        }

        /// <summary>
        /// 编辑选中CAN帧
        /// </summary>
        /// <param name="o"></param>
        private void EditSelFrame(object o)
        {
            CANFrameDataConfigVm vm = new CANFrameDataConfigVm(fileCfgModel, SelectModel, CANFrameDataConfigVm.Operation.Edit);
            CANFrameDataConfigWnd wnd = new CANFrameDataConfigWnd();
            vm.RegisterUpdate(UpdateModel);
            vm.RegisterUpdate(updateAction);
            wnd.DataContext = vm;
            //wnd.ShowDialog();
            wnd.Show();
        }

        /// <summary>
        /// 复制选中CAN帧
        /// </summary>
        /// <param name="o"></param>
        private void CopySelFrame(object o)
        {
            if (SelectModel == null)
                return;

            pasteCount = 0;
            copyFrame = SelectModel;
            return;
        }

        /// <summary>
        /// 粘贴复制的帧
        /// </summary>
        /// <param name="o"></param>
        private void PasteFrame(object o)
        {
            if (copyFrame == null ||
                SelectModel == null)
                return;

            string guid = SelectModel.Guid;
            List<CanFrameModel> dataSource = new List<CanFrameModel>(DataSource);
            int findIndex = dataSource.FindIndex((o) =>
            {
                return (guid == o.Guid);
            });

            if (findIndex == -1)
                return;

            //目标地址+1
            pasteCount++;
            CanFrameModel newFrame = new CanFrameModel(copyFrame);
            newFrame.Guid = Guid.NewGuid().ToString();
            newFrame.FrameId.DstAddr = (byte)(copyFrame.FrameId.DstAddr + pasteCount);

            List<CanFrameModel> beforeList = dataSource.Take(findIndex + 1).ToList();
            List<CanFrameModel> behindList = dataSource.Skip(findIndex + 1).ToList();
            beforeList.Add(newFrame);
            beforeList.AddRange(behindList);
            DataSource = new BindingList<CanFrameModel>(beforeList);
            DataSource.ListChanged += DataSource_ListChanged;

            SaveData();
            newFrame.Sort = SelectModel.Sort + 1;
            newFrame.Name += "(复制)";
            InsertCanFrame(newFrame);
        }

        /// <summary>
        /// 删除选中CAN帧
        /// </summary>
        /// <param name="o"></param>
        private void DeleteSelFrame(object o)
        {
            if (SelectModel == null)
            {
                MessageBox.Show("请选择一帧数据！", "提示");
                return;
            }

            DataManager.DeleteCanFrame(SelectModel.Guid);
            DataSource.Remove(SelectModel);
            //SaveData();
        }

        /// <summary>
        /// 保存当前数据到配置文件
        /// </summary>
        private void SaveData()
        {
            frameCfgModel.CanFrameModels = DataSource.ToList();
            JsonConfigHelper.WirteConfigFile(fileCfgModel);
        }

        private void MoveUp(object o)
        {
            if (SelectModel == null)
            {
                MessageBox.Show("请选择一行数据！", "提示");
                return;
            }

            List<CanFrameModel> canFrameModels = new List<CanFrameModel>(DataSource);
            int index = DataSource.IndexOf(SelectModel);
            if (index == -1 || index == 0)
                return;

            CanFrameModel curr = canFrameModels[index];
            CanFrameModel before = canFrameModels[index - 1];

            var beforeSort = before.Sort;
            var currSort = curr.Sort;

            // 更新 CanFrameModels 修改行
            if (DataManager.UpdateCanFrame(beforeSort, curr.Guid))
            {
                curr.Sort = beforeSort;
            }

            if (DataManager.UpdateCanFrame(currSort, before.Guid))
            {
                before.Sort = currSort;
            }

            canFrameModels = canFrameModels.OrderBy(t => t.Sort).ToList();
            DataSource = new BindingList<CanFrameModel>(canFrameModels);
            DataSource.ListChanged += DataSource_ListChanged;
            frameCfgModel.CanFrameModels = DataSource.ToList();
            //SaveData();
        }

        [RelayCommand]
        private void MoveDown()
        {
            if (SelectModel == null)
            {
                MessageBox.Show("请选择一行数据！", "提示");
                return;
            }

            List<CanFrameModel> canFrameModels = new List<CanFrameModel>(DataSource);
            int index = DataSource.IndexOf(SelectModel);
            if (index == -1 || index == (DataSource.Count - 1))
                return;

            CanFrameModel curr = canFrameModels[index];
            CanFrameModel after = canFrameModels[index + 1];
            var afterSort = after.Sort;
            var currSort = curr.Sort;

            // 更新 CanFrameModels 修改行
            if (DataManager.UpdateCanFrame(afterSort, curr.Guid))
            {
                curr.Sort = afterSort;
            }

            if (DataManager.UpdateCanFrame(currSort, after.Guid))
            {
                after.Sort = currSort;
            }

            canFrameModels = canFrameModels.OrderBy(t => t.Sort).ToList();

            DataSource = new BindingList<CanFrameModel>(canFrameModels);
            DataSource.ListChanged += DataSource_ListChanged;
            frameCfgModel.CanFrameModels = DataSource.ToList();
            //SaveData();
        }



        /// <summary>
        /// 更新行信息
        /// </summary>
        /// <param name="currSort">当前更新的行索引</param>
        /// <param name="afterSort">更新后的行索引</param>
        private void InsertCanFrame(CanFrameModel newFrame)
        {
            Dictionary<string, string> keys = DataManager.ProperyConvert(newFrame);
            keys.Add("CanID", newFrame.Id.ToString());

            keys.Remove(nameof(newFrame.FrameId));
            keys.Remove(nameof(newFrame.Id));
            var keys1 = DataManager.ProperyConvert(newFrame.FrameId);
            keys["Sort"] = "0";
            keys = keys.Concat(keys1).ToDictionary(postParK => postParK.Key, PostParV => PostParV.Value);

            if (DataManager.InsertCanFrameModels("CanFrame", keys))
            {
                foreach (var frame in newFrame.FrameDatas)
                {
                    var dataGuid = Guid.NewGuid().ToString();
                    keys = DataManager.ProperyConvert(frame);
                    keys.Add("CanGuid", newFrame.Guid);
                    keys.Add("Guid", dataGuid);
                    keys.Remove(nameof(frame.DataInfos));
                    SqliteHelper.ExecuteInsert("CanFrameData", keys);

                    foreach (var info in frame.DataInfos)
                    {
                        keys = DataManager.ProperyConvert(info);
                        keys.Add("DataGuid", dataGuid);
                        SqliteHelper.ExecuteInsert("CanFrameDataInfo", keys);
                    }
                }

                DataManager.ReSortCanFrame(dataSource.ToList());
            }
            else
            {
                MessageBox.Show("更新失败");
            }
        }

        #endregion

    }//class
}
