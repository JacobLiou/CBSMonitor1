using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using SofarHVMExe.Util;
using SofarHVMExe.Utilities;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SofarHVMExe.ViewModel
{
    /// <summary>
    /// Can帧数据配置ViewModel
    /// </summary>
    internal class CANFrameDataConfigVm : ViewModelBase
    {
        /// <summary>
        /// 操作
        /// </summary>
        public enum Operation
        {
            None,
            Add,    //新增CAN帧
            Edit    //编辑CAN帧
        }

        public CANFrameDataConfigVm(FileConfigModel fileModel, CanFrameModel selectModel, Operation opt)
        {
            fileCfgModel = fileModel;
            dataOpetion = opt;

            if (dataOpetion == Operation.Add)
            {
                //新增CAN帧数据
                tmpModel = new CanFrameModel();
            }
            else if (dataOpetion == Operation.Edit)
            {
                CurrentModel = selectModel;
                tmpModel = new CanFrameModel(selectModel);
            }

            Init();
        }

        private bool isNewAdd = false; //当前帧是否为刚刚新加的帧（用于处理新增一帧后多次点击保存）
        private int lastSelectTypeIndex = -1; //之前选择的数据类型索引
        private FileConfigModel fileCfgModel = null;
        private Operation dataOpetion = Operation.None;
        private Action updateAction;
        private CanFrameModel tmpModel = null; //当前操作的临时数据
        public CanFrameModel CurrentModel = null; //当前数据（只有保存时，使用上面临时数据覆盖此数据）

        #region 属性

        public string Name
        {
            get => tmpModel.Name;
            set { tmpModel.Name = value; OnPropertyChanged(); }
        }
        public bool AutoTx
        {
            get => tmpModel.AutoTx;
            set { tmpModel.AutoTx = value; OnPropertyChanged(); }
        }
        public string Priority
        {
            get => tmpModel.FrameId.Priority.ToString();
            set
            {
                if (Priority == value)
                    return;

                tmpModel.FrameId.Priority = byte.Parse(value);
                OnPropertyChanged();
                OnPropertyChanged("ID");
            }
        }
        public int FrameType
        {
            get => tmpModel.FrameId.FrameType;
            set
            {
                if (FrameType == value)
                    return;

                tmpModel.FrameId.FrameType = (byte)value;
                OnPropertyChanged();
                OnPropertyChanged("ID");
            }
        }
        public string FunctionCode
        {
            get => tmpModel.FrameId.FC.ToString();
            set
            {
                if (FunctionCode == value)
                    return;

                tmpModel.FrameId.FC = byte.Parse(value);
                OnPropertyChanged();
                OnPropertyChanged("ID");
            }
        }
        public string ContinueFlg
        {
            get => tmpModel.FrameId.ContinuousFlag.ToString();
            set
            {
                if (ContinueFlg == value)
                    return;

                tmpModel.FrameId.ContinuousFlag = byte.Parse(value);
                OnPropertyChanged();
                OnPropertyChanged("ID");
            }
        }
        public string SrcDevId
        {
            get => tmpModel.FrameId.SrcType.ToString();
            set
            {
                if (SrcDevId == value)
                    return;

                tmpModel.FrameId.SrcType = byte.Parse(value);
                OnPropertyChanged();
                OnPropertyChanged("ID");
            }
        }

        public string SrcDevAddr
        {
            get => tmpModel.FrameId.SrcAddr.ToString();
            set
            {
                if (SrcDevAddr == value)
                    return;

                byte v = 0;
                if (byte.TryParse(value, out v))
                {
                    tmpModel.FrameId.SrcAddr = v;
                    OnPropertyChanged();
                    OnPropertyChanged("ID");
                }
            }
        }

        public string TargetDevId
        {
            get => tmpModel.FrameId.DstType.ToString();
            set
            {
                if (TargetDevId == value)
                    return;

                tmpModel.FrameId.DstType = byte.Parse(value);
                OnPropertyChanged();
                OnPropertyChanged("ID");
            }
        }

        public string TargetDevAddr
        {
            get => tmpModel.FrameId.DstAddr.ToString();
            set
            {
                if (TargetDevAddr == value)
                    return;

                byte v = 0;
                if (byte.TryParse(value, out v))
                {
                    tmpModel.FrameId.DstAddr = v;
                    OnPropertyChanged();
                    OnPropertyChanged("ID");
                }
            }
        }

        //用于显示的ID（蓝色）
        public string ID
        {
            get => "0x" + tmpModel.FrameId.ID.ToString("X8");
            set
            {
                if (ID == value)
                    return;

                string id = value;
                id = id.Replace("0x", "").Replace("0X", "");
                uint v;
                if (uint.TryParse(id, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v))
                {
                    tmpModel.FrameId.ID = v;
                }

                OnPropertyChanged("Priority");
                OnPropertyChanged("FrameType");
                OnPropertyChanged("FunctionCode");
                OnPropertyChanged("ContinueFlg");
                OnPropertyChanged("SrcDevId");
                OnPropertyChanged("SrcDevAddr");
                OnPropertyChanged("TargetDevId");
                OnPropertyChanged("TargetDevAddr");
            }
        }

        //源设备地址范围提示
        public string srcAddrHint = "";
        public string SrcAddrHint { get => srcAddrHint; set { srcAddrHint = value; OnPropertyChanged(); } }

        //目标设备地址范围提示
        public string targetAddrHint = "";
        public string TargetAddrHint { get => targetAddrHint; set { targetAddrHint = value; OnPropertyChanged(); } }

        public List<string> FuncCodeSource { get; set; }
        public List<string> SrcDeviceIdSource { get; set; }
        public List<string> TargetDeviceIdSource { get; set; }

        private ObservableCollection<CanFrameDataInfo> dataSource = new ObservableCollection<CanFrameDataInfo>();
        public ObservableCollection<CanFrameDataInfo> DataSource
        {
            get => dataSource;
            set { dataSource = value; OnPropertyChanged(); }
        }

        private ObservableCollection<CanFrameDataInfo> multiDataSource = new ObservableCollection<CanFrameDataInfo>();
        public ObservableCollection<CanFrameDataInfo> MultiDataSource
        {
            get => multiDataSource;
            set { multiDataSource = value; OnPropertyChanged(); }
        }

        #endregion


        public ICommand SaveCommand { get; set; }
        public ICommand AddNewCommand { get; set; }
        public ICommand DeleteSelectCommand { get; set; }


        #region 初始化和更新
        private void Init()
        {
            SaveCommand = new SimpleCommand(SaveData);

            UpdateModel(fileCfgModel);
        }
        private void InitSingleData()
        {
            CanFrameData frameData = new CanFrameData();
            frameData.AddInitData();
            DataSource = frameData.DataInfos;
        }
        private void InitMultyData()
        {
            CanFrameData frameData = new CanFrameData();
            frameData.AddInitMultiData();
            MultiDataSource = frameData.DataInfos;

            //CanFrameData st = new CanFrameData();
            //st.AddStFrameDefData();

            //CanFrameDataInfo crc = new CanFrameDataInfo();
            //crc.Name = "CRC校验";
            //crc.Type = "U16";
            //crc.Value = "0xff";

            //List<CanFrameDataInfo> infoList = new List<CanFrameDataInfo>();
            //infoList.AddRange(st.DataInfos);
            //infoList.Add(crc);
            //MultiDataSource = new ObservableCollection<CanFrameDataInfo>(infoList.ToArray());
        }
        /// <summary>
        /// 更新model数据
        /// </summary>
        /// <param name="fileCfgModel"></param>
        public void UpdateModel(FileConfigModel model)
        {
            fileCfgModel = model;

            //1、更新ID数据
            ///源设备ID选择集合
            int bitNum = fileCfgModel.FrameModel.SrcIdBitNum;
            int count = (int)Math.Pow(2, bitNum);
            List<string> srcIdSource = new List<string>();
            for (int i = 0; i < count; i++)
            {
                srcIdSource.Add(i.ToString());
            }
            SrcDeviceIdSource = srcIdSource;

            ///源设备地址提示
            bitNum = 8 - bitNum;
            SrcAddrHint = $"0~{(int)Math.Pow(2, bitNum) - 1}";

            ///目标设备ID选择集合
            bitNum = fileCfgModel.FrameModel.TargetIdBitNum;
            count = (int)Math.Pow(2, bitNum);
            List<string> targetIdSource = new List<string>();
            for (int i = 0; i < count; i++)
            {
                targetIdSource.Add(i.ToString());
            }
            TargetDeviceIdSource = targetIdSource;

            ///目标设备地址提示
            bitNum = 8 - bitNum;
            TargetAddrHint = $"0~{(int)Math.Pow(2, bitNum) - 1}";

            ///功能码集合
            List<string> funCodeList = new List<string>();
            for (int i = 0; i < 128; i++)
            {
                funCodeList.Add(i.ToString());
            }
            FuncCodeSource = funCodeList;

            //2、更新数据
            UpdateFrameDate();

        }//func
        private void UpdateFrameDate()
        {
            //初始化默认数据
            {
                InitSingleData();
                InitMultyData();
            }

            CanFrameID frameId = tmpModel.FrameId;
            List<CanFrameData> _frameDatas = tmpModel.FrameDatas;

            if (frameId.ContinuousFlag == 0)
            {
                //非连续帧
                if (dataOpetion == Operation.Add)
                {
                }
                else if (dataOpetion == Operation.Edit)
                {
                    CanFrameData frameData = _frameDatas[0];
                    if (frameData != null)
                    {
                        DataSource = frameData.DataInfos;
                    }
                }
            }
            else
            {
                //连续帧
                if (dataOpetion == Operation.Add)
                {
                    //添加新帧，默认为非连续帧，没有此情况
                }
                else if (dataOpetion == Operation.Edit)
                {
                    CanFrameData frameData = _frameDatas[0];
                    if (frameData != null)
                    {
                        MultiDataSource = frameData.DataInfos;
                    }

                    //List<CanFrameDataInfo> infoList = new List<CanFrameDataInfo>();
                    //foreach (CanFrameData frameData in _frameDatas)
                    //{
                    //    ObservableCollection<CanFrameDataInfo> dataInfos = frameData.DataInfos;
                    //    string index = frameData.GetPackageIndex();
                    //    if (index == "0")
                    //    {
                    //        //起始帧
                    //        infoList.AddRange(dataInfos.Skip(3));
                    //    }
                    //    else if (index == "0xff")
                    //    {
                    //        //结束帧
                    //        infoList.AddRange(dataInfos.Skip(1));
                    //    }
                    //    else
                    //    {
                    //        //中间帧
                    //        infoList.AddRange(dataInfos.Skip(1));
                    //    }
                    //}

                    //MultyDataSource = new ObservableCollection<CanFrameDataInfo>(infoList.ToArray());
                }
            }//else
        }//func 
        #endregion


        #region 保存
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="o"></param>
        private void SaveData(object o)
        {
            CanFrameID frameId = tmpModel.FrameId;
            if (frameId.ContinuousFlag == 0)
            {
                //非连续
                SaveContinue();
            }
            else
            {
                //连续
                SaveUncontinue();
            }

            /// <summary>
            /// 保存
            /// </summary>
            /// <param name="o"></param>
        }//func

        /// <summary>
        /// 保存连续
        /// </summary>
        private void SaveContinue()
        {
            List<CanFrameData> _frameDatas = tmpModel.FrameDatas;
            _frameDatas.Clear();
            CanFrameData frameData = new CanFrameData();
            frameData.DataInfos = DataSource;
            _frameDatas.Add(frameData);

            FrameConfigModel frameConfigModel = fileCfgModel.FrameModel;
            List<CanFrameModel> frameModels = frameConfigModel.CanFrameModels;

            //保存数据
            if (dataOpetion == Operation.Add)
            {
                //查询当前帧是否已经存在，防止多次保存同一帧时连续添加
                uint id = tmpModel.Id;
                string addr = tmpModel.FrameDatas[0].DataInfos[0].Value;
                CanFrameModel? findFrameModel = frameModels.Find((CanFrameModel model) =>
                {
                    if (model.Id == id) //id相同
                    {
                        //点表地址相同
                        if (IsSameAddr(model.FrameDatas[0].DataInfos[0].Value, addr))
                            return true;

                        return false;
                    }
                    return false;
                });

                if (findFrameModel != null)
                {
                    //处理相同id和点表起始地址的帧，不允许增加
                    if (isNewAdd)
                    {
                        //设置到新加的帧数据上
                        findFrameModel = tmpModel;
                    }
                    else
                    {
                        MessageBox.Show("保存失败，已存在相同ID和点表起始地址的CAN帧！", "提示");
                        return;
                    }
                }
                else
                {
                    isNewAdd = true;
                    tmpModel.Guid = Guid.NewGuid().ToString();
                    frameModels.Add(tmpModel);
                    CurrentModel = tmpModel;
                }
            }
            else if (dataOpetion == Operation.Edit)
            {
                int index = frameModels.IndexOf(CurrentModel);
                frameModels[index] = tmpModel;
                CurrentModel = tmpModel;
            }
            else
            {
                return;
            }

            SaveFile();
        }

        /// <summary>
        /// 保存非连续
        /// </summary>
        private void SaveUncontinue()
        {
            List<CanFrameData> _frameDatas = tmpModel.FrameDatas;
            _frameDatas.Clear();
            CanFrameData frameData = new CanFrameData();
            frameData.DataInfos = MultiDataSource;
            _frameDatas.Add(frameData);

            string addr = MultiDataSource[1].Value;
            FrameConfigModel frameConfigModel = fileCfgModel.FrameModel;
            List<CanFrameModel> frameModels = frameConfigModel.CanFrameModels;

            //保存数据
            if (dataOpetion == Operation.Add)
            {
                //查询当前帧是否已经存在，防止多次保存同一帧时连续添加
                CanFrameModel? findFrameModel = frameModels.Find((CanFrameModel model) =>
                {
                    if (model.Id == tmpModel.Id) //id相同
                    {
                        //点表地址相同
                        if (IsSameAddr(model.FrameDatas[0].DataInfos[0].Value, addr))
                            return true;

                        return false;
                    }
                    return false;
                });

                if (findFrameModel != null)
                {
                    //处理相同id和点表起始地址的帧，不允许增加
                    if (isNewAdd)
                    {
                        //设置到新加的帧数据上
                        findFrameModel = tmpModel;
                    }
                    else
                    {
                        MessageBox.Show("保存失败，已存在相同ID和点表起始地址的CAN帧！", "提示");
                        return;
                    }
                }
                else
                {
                    isNewAdd = true;
                    tmpModel.Guid = Guid.NewGuid().ToString();
                    frameModels.Add(tmpModel);
                    CurrentModel = tmpModel;
                }
            }
            else if (dataOpetion == Operation.Edit)
            {
                int index = frameModels.IndexOf(CurrentModel);
                frameModels[index] = tmpModel;
                CurrentModel = tmpModel;
            }
            else
            {
                return;
            }

            SaveFile();
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        private void SaveFile()
        {
            //更新命令配置数据
            UpdateCmdConfig();
            //更新事件配置数据
            UpdateEventConfig();

            //保存到文件
            //if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            if (UpdateCmdConfigDal())
            {
                //更新帧配置界面数据表显示
                updateAction?.Invoke();
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }

        public bool UpdateCmdConfigDal()
        {
            if (isNewAdd)
            {
                DataManager.InsertCanFrameDataModels(CurrentModel);
            }
            else
            {
                DataManager.UpdateCanFrameDataModels(CurrentModel);
            }

            return true;
        }

        #endregion


        /// <summary>
        /// 注册更新，用于修改并保存帧数据后，CAN帧表同步更新
        /// </summary>
        /// <param name="method"></param>
        public void RegisterUpdate(Action method)
        {
            updateAction += method;
        }

        /// <summary>
        /// 判断两个点表地址是否相同
        /// </summary>
        /// <param name="addr1"></param>
        /// <param name="addr2"></param>
        private bool IsSameAddr(string strAddr1, string strAddr2)
        {
            strAddr1 = strAddr1.Replace("0x", "").Replace("0X", "");
            strAddr2 = strAddr2.Replace("0x", "").Replace("0X", "");

            long addr1, addr2;
            long.TryParse(strAddr1, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addr1);
            long.TryParse(strAddr2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addr2);

            return (addr1 == addr2);
        }

        public void UpdateLastTypeIndex(int index)
        {
            lastSelectTypeIndex = index;
        }

        /// <summary>
        /// 选择的新类型是否超出8个字节长度
        /// </summary>
        /// <param name="newType"></param>
        /// <param name="lastIndex"></param>
        /// <returns></returns>
        public bool IsTypeOverflow(string newType, out int lastIndex)
        {
            lastIndex = this.lastSelectTypeIndex;

            //拿第一包数据
            CanFrameData frameData = CurrentModel.FrameDatas[0];

            //判断数据是否超出8个字节长度
            int count = 0;
            foreach (CanFrameDataInfo info in frameData.DataInfos)
            {
                if (info.Type.Contains("8"))
                {
                    count += 1;
                }
                else if (info.Type.Contains("16"))
                {
                    count += 2;
                }
                else if (info.Type.Contains("32"))
                {
                    count += 4;
                }
            }

            if (newType.Contains("8"))
            {
                count += 1;
            }
            else if (newType.Contains("16"))
            {
                count += 2;
            }
            else if (newType.Contains("32"))
            {
                count += 4;
            }

            if (count > 8)
            {
                MessageBox.Show($"长度最大8个字节，无法更改类型为{newType}！", "提示");
                return true;
            }

            return false;
        }//func

        /// <summary>
        /// 更新命令配置
        /// </summary>
        private void UpdateCmdConfig()
        {
            string guid = CurrentModel.Guid;
            var cmdGrpModels = fileCfgModel.CmdModels;
            foreach (var cmdGrpModel in cmdGrpModels)
            {
                var configModels = cmdGrpModel.cmdConfigModels;
                CmdConfigModel cmd = configModels.Find((o) =>
                {
                    return (o.FrameGuid == guid);
                });

                if (cmd != null)
                {
                    cmd.FrameModel = new CanFrameModel(CurrentModel);
                    cmd.FrameDatas2SetValue();
                }
            }

            //for (int i = 0; i < fileCfgModel.CmdModels.Count; i++)
            //{
            //    CmdGrpConfigModel cmdGroup = fileCfgModel.CmdModels[i];
            //    for (int j = 0; j < cmdGroup.cmdConfigModels.Count; j++)
            //    {
            //        CmdConfigModel cmd = cmdGroup.cmdConfigModels[j];
            //        if (cmd.FrameGuid == guid)
            //        {
            //            cmd.FrameModel = new CanFrameModel(CurrentModel);
            //            cmd.FrameDatas2SetValue();
            //        }
            //    }
            //}
        }//func

        /// <summary>
        /// 更新事件配置
        /// </summary>
        private void UpdateEventConfig()
        {
            string guid = CurrentModel.Guid;
            for (int i = 0; i < fileCfgModel.EventModels.Count; i++)
            {
                EventGroupModel groupModel = fileCfgModel.EventModels[i];
                if (groupModel.FrameGuid == guid)
                {
                    //...
                }
            }
        }//func

        /// <summary>
        /// 将一组多包字段信息拆分成多组多包信息
        /// </summary>
        /// <param name="multyInfos">包序号、起始地址、个数、数据1、数据2、数据3...CRC校验</param>
        /// <returns></returns>
        private List<ObservableCollection<CanFrameDataInfo>> SplitPackage(ObservableCollection<CanFrameDataInfo> multyInfos)
        {
            List<ObservableCollection<CanFrameDataInfo>> frameDataList = new List<ObservableCollection<CanFrameDataInfo>>();
            ObservableCollection<CanFrameDataInfo> frameDatas = null;

            //起始帧
            {
                ObservableCollection<CanFrameDataInfo> stDataInfos = new ObservableCollection<CanFrameDataInfo>();
                int dataLen = 0; //一包中数据字节长度（最大8个字节）
                int packageIndex = 0;
                int count = multyInfos.Count;
                for (int i = 0; i < count; i++)
                {
                    CanFrameDataInfo info = multyInfos[i];
                    //int len = info.ByteRange;

                    //if (dataLen)
                }
            }

            //中间帧
            {

            }

            //结束帧
            {

            }

            frameDatas = new ObservableCollection<CanFrameDataInfo>();
            frameDatas.Add(multyInfos[0]); //包序号0
            frameDatas.Add(multyInfos[1]); //起始地址
            frameDatas.Add(multyInfos[2]); //个数


            return frameDataList;
        }//func

    }//class
}
