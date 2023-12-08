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
using FontAwesome.Sharp;
using NPOI.POIFS.NIO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Communication.Can;
using CanProtocol;
using CanProtocol.ProtocolModel;
using SofarHVMExe.Util;
using SofarHVMExe.Model;
using SofarHVMExe.ViewModel;
using SofarHVMExe.Utilities;
using LogInfo = SofarHVMExe.Utilities.LogHelper;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace SofarHVMExe.ViewModel
{
    public class ProjectCfgPageVm : ViewModelBase
    {
        public ProjectCfgPageVm()
        {
            Init();
        }
        public ProjectCfgPageVm(EcanHelper? helper)
        {
            ecanHelper = helper;
            Init();
        }

        #region 字段
        protected new string Title = "项目配置";            //当前模型的标题
        private static EcanHelper? ecanHelper = null;
        private FrameConfigModel FrameModel = null;
        private PrjConfigModel? prjCfgModel = null;
        private List<byte> fileData = null;                 //安规文档参数数据
        private CanFrameModel tmpModel = null;              //当前操作的临时数据
        public CanFrameModel CurrentModel = null;           //当前数据（只有保存时，使用上面临时数据覆盖此数据）
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
        private Dictionary<int, CanFrameModel> safetyList = new Dictionary<int, CanFrameModel>();
        private Dictionary<int, CanFrameModel> SafetyList
        {
            get => safetyList;
            set { safetyList = value; OnPropertyChanged(); }
        }
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

        #region 命令
        public ICommand SaveCommand { get; set; }
        public ICommand ImportCommand { get; set; }
        public ICommand SetWorkDirectoryCommand { get; set; }

        private void Init()
        {
            SaveCommand = new SimpleCommand(SaveData);
            ImportCommand = new SimpleCommand(ImportFile);
            SetWorkDirectoryCommand = new SimpleCommand(SetWorkDirectory);
        }

        #endregion

        #region 成员方法
        private void SaveData(object o)
        {
            if (DataManager.UpdatePrjConfigModel(prjCfgModel))
            {
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }

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
            byte[] buffer = new byte[datas.Length];
            Buffer.BlockCopy(datas, 4, buffer, 0, buffer.Length - 4);
            Buffer.BlockCopy(datas, 0, buffer, buffer.Length - 4, 4);

            //清理安规集合数据
            SafetyList.Clear();

            FrameConfigModel model = DataManager.GetFrameConfigModel();
            foreach (var selectModel in model.CanFrameModels)
            {
                if (selectModel.Id == 0x19952141 || selectModel.Id == 0x19A12141)
                {
                    var number = selectModel.FrameDatas[0].DataInfos[1].Value;
                    int groupNumber;
                    int.TryParse(number, out groupNumber);
                    SafetyList.Add(groupNumber, selectModel);
                }
            }

            try
            {
                foreach (var item in SafetyList)
                {
                    CurrentModel = item.Value;
                    tmpModel = new CanFrameModel(item.Value);
                    //更新model数据
                    UpdateModel();

                    //编辑数据信息
                    var dataLists = item.Value.FrameDatas[0].DataInfos;
                    int index = item.Key / 256 * 128;

                    for (int j = 3; j < MultiDataSource.Count - 1; j++)
                    {
                        CanFrameDataInfo info = dataLists[j];

                        int realVal = -1;
                        switch (info.Type)
                        {
                            case "U8":
                                realVal = buffer[index] & 0xff;
                                index += 1;
                                break;
                            case "U16":
                                int byteU1 = buffer[index] << 8;
                                int byteU2 = buffer[index + 1] & 0xff;
                                realVal = byteU1 + byteU2;
                                index += 2;
                                break;
                            case "I16":
                                string byteI1 = buffer[index++].ToString("X2");
                                string byteI2 = buffer[index++].ToString("X2");
                                realVal = Convert.ToInt16(byteI1 + byteI2, 16);
                                index += 2;
                                break;
                            default:
                                int byte1 = buffer[index] << 8;
                                int byte2 = buffer[index + 1] & 0xff;
                                realVal = byte1 + byte2;
                                index += 2;
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

        public void GetPrjCfgModel()
        {
            FrameModel = DataManager.GetFrameConfigModel();
            prjCfgModel = DataManager.GetPrjConfigModel();
        }
        public void UpdateModel()
        {
            //1、更新ID数据
            int bitNum = FrameModel.SrcIdBitNum;
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
            bitNum = FrameModel.TargetIdBitNum;
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
        }
        private void UpdateFrameDate()
        {
            //初始化默认数据
            InitMultyData();

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
        }
        private void SaveUncontinue()
        {
            List<CanFrameData> _frameDatas = tmpModel.FrameDatas;
            _frameDatas.Clear();
            CanFrameData frameData = new CanFrameData();
            frameData.DataInfos = MultiDataSource;
            _frameDatas.Add(frameData);

            // 更新当前帧
            DataManager.UpdateCanFrameDataModels(tmpModel);
        }
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

            ushort crcVal = CRCHelper.ComputeCrc16(dataCRC.ToArray(), 1156);
            int encodecrc = CRCHelper.Encode(crcVal);

            string rightstr = string.Format("CS:{0},{1}", (encodecrc & 0xff), (encodecrc >> 8));
            if (!lines[10].Equals(rightstr)) return 4;

            return 0;
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
                    Thread.Sleep(50);
                    MultiLanguages.Common.LogHelper.WriteLog($"[发送]下载起始帧[1]，0x{id.ToString("X")} {BitConverter.ToString(fd.Data)}");
                }
            }
        }
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
        }

        #endregion

    }
}
