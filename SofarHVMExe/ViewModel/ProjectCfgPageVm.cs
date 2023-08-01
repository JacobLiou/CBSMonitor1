using SofarHVMDAL;
using SofarHVMExe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Windows.Navigation;
using Communication.Can;
using FontAwesome.Sharp;
using SofarHVMExe.Model;
using CanProtocol.ProtocolModel;
using SofarHVMExe.ViewModel;
using NPOI.POIFS.NIO;
using System.Diagnostics;
using static SofarHVMExe.ViewModel.CANFrameDataConfigVm;
using System.Collections.ObjectModel;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using System.IO;
using System.Threading;
using CanProtocol;

namespace SofarHVMExe.ViewModel
{
    class ProjectCfgPageVm : ViewModelBase
    {
        public ProjectCfgPageVm()
        {
            ecanHelper = new EcanHelper();
            Init();
        }

        #region 字段
        public EcanHelper? ecanHelper = null;
        private FrameConfigModel? frameCfgModel = null;
        private FileConfigModel? fileCfgModel = null;
        private PrjConfigModel? prjCfgModel = null;
        private List<byte> fileData = null; //安规文档参数数据
        private CanFrameModel tmpModel = null; //当前操作的临时数据
        public CanFrameModel CurrentModel = null; //当前数据（只有保存时，使用上面临时数据覆盖此数据）
        #endregion

        #region 属性
        public string ProjectName
        {
            get { return prjCfgModel.ProjectName; }
            set
            {
                prjCfgModel.ProjectName = value;
                OnPropertyChanged();
            }
        }
        public string WorkPath
        {
            get { return prjCfgModel.WorkPath; }
            set
            {
                prjCfgModel.WorkPath = value;
                OnPropertyChanged();
            }
        }
        public string SendInterval
        {
            get { return prjCfgModel.SendInterval.ToString(); }
            set
            {
                int v = 1;
                if (int.TryParse(value, out v))
                {
                    prjCfgModel.SendInterval = v;
                    OnPropertyChanged();
                }
            }
        }
        public string RouteTimeout
        {
            get { return prjCfgModel.RouteTimeout.ToString(); }
            set
            {
                int v = 1;
                if (int.TryParse(value, out v))
                {
                    prjCfgModel.RouteTimeout = v;
                    OnPropertyChanged();
                }
            }
        }
        public string ThreadTimeout
        {
            get { return prjCfgModel.ThreadTimeout.ToString(); }
            set
            {
                int v = 1;
                if (int.TryParse(value, out v))
                {
                    prjCfgModel.ThreadTimeout = v;
                    OnPropertyChanged();
                }
            }
        }
        public uint DeviceInx
        {
            get => prjCfgModel.DeviceInx;
            set
            {
                prjCfgModel.DeviceInx = value;
                OnPropertyChanged();
            }
        }
        public string BaudrateInx1
        {
            get { return prjCfgModel.Baudrate1.ToString() + "K"; }
            set
            {
                string bardrate = value.Replace("K", "");
                prjCfgModel.Baudrate1 = int.Parse(bardrate);
                OnPropertyChanged();
            }
        }
        public string BaudrateInx2
        {
            get { return prjCfgModel.Baudrate2.ToString() + "K"; }
            set
            {
                string bardrate = value.Replace("K", "");
                prjCfgModel.Baudrate2 = int.Parse(bardrate);
                OnPropertyChanged();
            }
        }
        public string AddrMark
        {
            get
            {
                return "0x" + prjCfgModel.AddrMark.ToString("X");
            }
            set
            {
                int addrMark;
                string temp = value.Replace("0x", "").Replace("0X", "");
                if (int.TryParse(temp, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addrMark))
                {
                    prjCfgModel.AddrMark = addrMark;
                    OnPropertyChanged();
                }
            }
        }
        public int HostHeartbeatIndex
        {
            get
            {
                string guid = prjCfgModel.HostFrameGuid;
                return FrameHelper.GetFrameIndex(fileCfgModel, guid);
            }
            set
            {
                int i = value;
                if (fileCfgModel != null && fileCfgModel.FrameModel != null &&
                    fileCfgModel.FrameModel.CanFrameModels.Count != 0)
                {
                    prjCfgModel.HostFrameGuid = fileCfgModel.FrameModel.CanFrameModels[i].Guid;
                    OnPropertyChanged();
                }
            }
        }
        public int ModuleHeartbeatIndex
        {
            get
            {
                string guid = prjCfgModel.ModuleFrameGuid;
                return FrameHelper.GetFrameIndex(fileCfgModel, guid);
            }
            set
            {
                int i = value;
                if (fileCfgModel != null && fileCfgModel.FrameModel != null &&
                    fileCfgModel.FrameModel.CanFrameModels.Count != 0)
                {
                    prjCfgModel.ModuleFrameGuid = fileCfgModel.FrameModel.CanFrameModels[i].Guid;
                    OnPropertyChanged();
                }
            }
        }
        private List<string> canFrameList = new List<string>();
        public List<string> CanFrameList
        {
            get => canFrameList;
            set
            {
                canFrameList = value;
                OnPropertyChanged();
            }
        }
        private string filePath = string.Empty;
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged();
            }
        }
        private Dictionary<int, CanFrameModel> safetyList2 = new Dictionary<int, CanFrameModel>();
        private Dictionary<int, CanFrameModel> SafetyList2
        {
            get => safetyList2;
            set { safetyList2 = value; OnPropertyChanged(); }
        }
        //private List<CanFrameModel> safetyList = new List<CanFrameModel>();
        //private List<CanFrameModel> SafetyList
        //{
        //    get => safetyList;
        //    set { safetyList = value; OnPropertyChanged(); }
        //}
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
        /*//用于显示的ID（蓝色）
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
        }*/

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


        public ICommand SetWorkDirectoryCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand ImportCommand { get; set; }

        private void Init()
        {
            SetWorkDirectoryCommand = new SimpleCommand(SetWorkDirectory);
            SaveCommand = new SimpleCommand(SaveData);
            ImportCommand = new SimpleCommand(ImportFile);
        }


        #region 成员方法
        private void ImportFile(object o)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择安规文件";
            dlg.Filter = "安规文件(*.txt)|*.txt";
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            FilePath = dlg.FileName;

            int result = this.CheckSatefyFile(FilePath, out fileData);
            if (result != 0)
                return;

            byte[] datas = fileData.ToArray();
            byte[] buffer = new byte[datas.Length - 4];
            Buffer.BlockCopy(datas, 4, buffer, 0, buffer.Length);

            //读取当前配置文件信息
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel == null)
                return;

            //清理安规集合数据
            //SafetyList.Clear();
            SafetyList2.Clear();

            FrameConfigModel FrameModel = fileCfgModel.FrameModel;
            foreach (var selectModel in FrameModel.CanFrameModels)
            {
                if (selectModel.Id == 0x19952141)
                {
                    var number = selectModel.FrameDatas[0].DataInfos[1].Value;
                    int groupNumber;
                    int.TryParse(number, out groupNumber);
                    SafetyList2.Add(groupNumber, selectModel);
                }
            }

            try
            {
                foreach (var item in SafetyList2)
                {
                    CurrentModel = item.Value;
                    tmpModel = new CanFrameModel(item.Value);
                    //更新model数据
                    UpdateModel(fileCfgModel);

                    //编辑数据信息
                    var dataLists = item.Value.FrameDatas[0].DataInfos;
                    int index = item.Key / 256;
                    for (int j = 3; j < MultiDataSource.Count - 1; j++)
                    {
                        CanFrameDataInfo info = dataLists[j];
                        index = ((j - 3) * 2);
                        int realVal = -1;
                        switch (info.Type)
                        {
                            case "U16":
                                int byteU1 = buffer[index] << 8;
                                int byteU2 = buffer[index + 1] & 0xff;
                                realVal = byteU1 + byteU2;
                                break;
                            case "I16":
                                string byteI1 = buffer[index++].ToString("X2");
                                string byteI2 = buffer[index++].ToString("X2");
                                realVal = Convert.ToInt16(byteI1 + byteI2, 16);
                                break;
                            default:
                                int byte1 = buffer[index] << 8;
                                int byte2 = buffer[index + 1] & 0xff;
                                realVal = byte1 + byte2;
                                break;
                        }

                        //数据精度并对Value进行赋值
                        switch (info.Precision)
                        {
                            case 10:
                            case 100:
                                info.Value = (Convert.ToInt64(realVal) * Convert.ToUInt32(info.Precision)).ToString();
                                break;
                            case (decimal)0.1:
                            case (decimal)0.01:
                            case (decimal)0.001:
                            case (decimal)0.0001:
                                info.Value = (Convert.ToInt64(realVal) / (1 / Convert.ToDouble(info.Precision))).ToString();
                                break;
                            default:
                                info.Value = Convert.ToInt32(realVal).ToString();
                                break;
                        }

                        MultiDataSource[j].Value = info.Value;
                    }

                    CanFrameID frameId = item.Value.FrameId;
                    if (frameId.ContinuousFlag != 0)
                    {
                        SaveUncontinue(); //连续

                        SendFrame(tmpModel);//发送数据
                    }
                }

                

                MessageBox.Show("安规参数配置已完成更新！", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"安规参数配置更新异常，错误信息：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 更新model数据
        /// </summary>
        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel == null)
                return;

            prjCfgModel = fileCfgModel.PrjModel;
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
                //InitSingleData();
                InitMultyData();
            }

            CanFrameID frameId = tmpModel.FrameId;
            List<CanFrameData> _frameDatas = tmpModel.FrameDatas;

            if (frameId.ContinuousFlag != 0)
            {
                //连续帧
                CanFrameData frameData = _frameDatas[0];
                if (frameData != null)
                {
                    MultiDataSource = frameData.DataInfos;
                }
            }
        }//func 
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
        }
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
            int index = frameModels.IndexOf(CurrentModel);
            frameModels[index] = tmpModel;
            CurrentModel = tmpModel;

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
            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                Debug.WriteLine("保存成功！", "提示");
            }
            else
            {
                Debug.WriteLine("保存失败！", "提示");
            }
        }

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
        /// 检查安规文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int CheckSatefyFile(string path, out List<byte> buffer)
        {
            //验证文件（是否为空，格式是否正常，CRC校验是否通过等）
            buffer = new List<byte>();

            //路径非空判断
            if (string.IsNullOrEmpty(path)) return 1;

            //获取文件内容
            string[] lines = File.ReadAllLines(path);
            if (lines.Length != 11) return 2;

            for (int i = 0; i < lines.Length - 1; i++)
            {
                string line = lines[i];

                string[] keyValue = line.Split(":".ToCharArray());

                string[] valueArray = keyValue[1].TrimEnd(",".ToCharArray()).Split(",".ToCharArray());

                if (i == 0)
                {
                    if (valueArray.Length != 3) return 3;//安规文件格式错误

                    int country = Convert.ToInt32(valueArray[0]);
                    buffer.Add((byte)country);

                    int region = Convert.ToInt32(valueArray[1]);
                    buffer.Add((byte)region);

                    int ver = int.Parse(valueArray[2]);
                    buffer.Add((byte)(ver >> 8));
                    buffer.Add((byte)(ver & 0xff));
                }
                else
                {
                    for (int j = 0; j < valueArray.Length; j++)
                    {
                        int vint = int.Parse(valueArray[j]);
                        buffer.Add((byte)(vint >> 8));
                        buffer.Add((byte)(vint & 0xff));
                    }
                }
            }

            //校验CRC
            byte[] dataCRC = buffer.ToArray();
            if (dataCRC.Length != 1156) return 3;

            //CRCHelper helper = new CRCHelper();
            ushort crcVal = CRCHelper.ComputeCrc16(dataCRC.ToArray(), 1156);
            int encodecrc = CRCHelper.Encode(crcVal);

            string rightstr = string.Format("CS:{0},{1}", (encodecrc & 0xff), (encodecrc >> 8));
            if (!lines[10].Equals(rightstr)) return 4;

            return 0;
        }//func

        /// <summary>
        /// 更新CAN帧显示数据源
        /// 心跳下拉框id集合
        /// </summary>
        private void UpdateFrameSource()
        {
            if (fileCfgModel == null)
                return;

            List<string> list = new List<string>();
            foreach (CanFrameModel frame in fileCfgModel.FrameModel.CanFrameModels)
            {
                string id = $"0x{frame.Id.ToString("X")}({frame.Name})";
                list.Add(id);
            }
            CanFrameList = list;
        }

        private void SetWorkDirectory(object o)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "选择工作目录";
            dlg.UseDescriptionForTitle = true;
            dlg.ShowNewFolderButton = true;
            dlg.SelectedPath = WorkPath;
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.Cancel)
                return;

            WorkPath = dlg.SelectedPath.Trim();
        }

        private void SaveData(object o)
        {
            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }
        #endregion

        #region CAN操作
        /// <summary>
        /// 检查Can连接
        /// </summary>
        /// <returns></returns>
        private bool CheckConnect(bool hint = true)
        {
            if (!ecanHelper.IsConnected)
            {
                if (hint) MessageBox.Show("未连接CAN设备，请先连接后再进行操作！", "提示");
                return false;
            }

            if (!ecanHelper.IsCan1Start && !ecanHelper.IsCan2Start)
            {
                if (hint) MessageBox.Show("CAN通道未打开！", "提示");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 发送单帧数据
        /// </summary>
        /// <param name="frame"></param>
        private void SendFrame(CanFrameModel frame)
        {
            uint id = frame.Id;
            CanFrameData frameData = frame.FrameDatas[0];

            //1、发送CAN帧
            if (frame.FrameDatas.Count > 0) //连续
            {
                List<CanFrameData> frameDataList = ProtocolHelper.AnalyseMultiPackage(frameData);
                foreach (CanFrameData fd in frameDataList)
                {
                    SendSingleFrame(id, fd.Data, fd);
                    Thread.Sleep(5 * 10);
                    Debug.WriteLine($"[发送]下载起始帧[1]，0x{id.ToString("X")} {BitConverter.ToString(fd.Data)}");
                }
            }
        }//func
        /// <summary>
        /// 发送单帧数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="datas"></param>
        /// <param name="frameData"></param>
        private void SendSingleFrame(uint id, byte[] datas, CanFrameData frameData)
        {
            if (ecanHelper.IsCan1Start)
            {
                if (id == 0x19142141)
                {
                    int stop = 10;
                }
                ecanHelper.SendCan1(id, datas);
            }

            if (ecanHelper.IsCan2Start)
            {
                ecanHelper.SendCan2(id, datas);
            }

#if false
            //2、更新发送/接收信息数据表
            if (IsShowSend)
            {
                SendRecvFrameInfo newFrameInfo = new SendRecvFrameInfo();
                newFrameInfo.IsSend = true;
                newFrameInfo.Time = DateTime.Now.ToString("HH:mm:ss.fff"); //时分秒毫秒
                newFrameInfo.ID = "0x" + id.ToString("X");
                newFrameInfo.Datas = BitConverter.ToString(datas);
                if (frameData != null)
                {
                    var dataInfos = frameData.DataInfos;
                    int count = dataInfos.Count;
                    if (count > 0)
                    {
                        newFrameInfo.Info1 = dataInfos[0].Name;
                        newFrameInfo.Value1 = dataInfos[0].Value;
                    }
                    if (count > 1)
                    {
                        newFrameInfo.Info2 = dataInfos[1].Name;
                        newFrameInfo.Value2 = dataInfos[1].Value;
                    }
                    if (count > 2)
                    {
                        newFrameInfo.Info3 = dataInfos[2].Name;
                        newFrameInfo.Value3 = dataInfos[2].Value;
                    }
                    if (count > 3)
                    {
                        newFrameInfo.Info4 = dataInfos[3].Name;
                        newFrameInfo.Value4 = dataInfos[3].Value;
                    }
                    if (count > 4)
                    {
                        newFrameInfo.Info5 = dataInfos[4].Name;
                        newFrameInfo.Value5 = dataInfos[4].Value;
                    }
                }

                //子线程更新发送/接收信息数据表
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    FrameInfoDataSrc.Add(newFrameInfo);
                });
            }
#endif
            //3、增加到报文信息
            {
                //string msg = $"[发送]{DateTime.Now.ToString("HH:mm:ss.fff")}  :  0x{id.ToString("X8")}\t{BitConverter.ToString(datas)}";
                //DebugTool.Output(msg);
                //AddDebugInfo(msg);
            }
        }//func

        #endregion

    }//class
}
