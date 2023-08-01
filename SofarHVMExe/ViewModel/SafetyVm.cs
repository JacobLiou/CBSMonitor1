using CanProtocol.Utilities;
using CanProtocol.ProtocolModel;
using Communication.Can;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using CanProtocol;
using SofarHVMExe.Utilities.Global;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.Globalization;
using static SofarHVMExe.ViewModel.CANFrameDataConfigVm;
using NPOI.SS.Formula.Functions;
using System.Windows.Controls;
using NPOI.SS.Formula.Eval;

namespace SofarHVMExe.ViewModel
{
    public class SafetyVm : ViewModelBase
    {
        /// <summary>
        /// 监控界面ViewModel
        /// </summary>
        public SafetyVm(EcanHelper? helper)
        {
            ecanHelper = helper;

            Init();
        }

        #region 字段
        public EcanHelper? ecanHelper = null;
        private string recvTime = "";
        private List<byte> fileData;

        private FileConfigModel fileCfgModel = null;
        private Operation dataOpetion = Operation.None;
        private Action updateAction;
        private CanFrameModel tmpModel = null; //当前操作的临时数据
        public CanFrameModel CurrentModel = null; //当前数据（只有保存时，使用上面临时数据覆盖此数据）
        #endregion


        private string filePath = ""; //文件路径
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged();
            }
        }
        #region 属性
        private List<CanFrameModel> safetyList = new List<CanFrameModel>();
        private List<CanFrameModel> SafetyList
        {
            get => safetyList;
            set { safetyList = value; OnPropertyChanged(); }
        }
        private string msg = "";//导入提示
        public string Message
        {
            get => msg;
            set { msg = value; OnPropertyChanged(); }
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

        #region 命令
        public ICommand ImportCommand { get; set; }             //导入
        #endregion

        #region 成员方法
        private void Init()
        {
            ImportCommand = new SimpleCommand(ImportFile);
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
            byte[] buffer = new byte[datas.Length - 4];
            Buffer.BlockCopy(datas, 4, buffer, 0, buffer.Length);

            //读取当前配置文件信息
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel == null)
                return;

            SafetyList.Clear();//清理安规集合数据

            /*List<CmdGrpConfigModel> CmdModels = fileCfgModel.CmdModels;
            for (int i = 11; i < CmdModels.Count; i++)
            {
                //过滤非安规参数组
                List<CmdConfigModel> cmdConfigModels = CmdModels[i].cmdConfigModels;

                for (int groupNumber = 0; groupNumber < cmdConfigModels.Count; groupNumber++)
                {
                    SelectData = cmdConfigModels[groupNumber];
                    currentCmdGrpModel = fileCfgModel.CmdModels[groupNumber];

                    CanFrameModel? canModel = SelectData.FrameModel;
                    if (canModel.Id == 0x19952141)
                    {
                        CanFrameID frameId = canModel.FrameId;
                        if (frameId.Continueflg == 0)
                            continue;

                        var data = canModel.FrameDatas[0].DataInfos;

                        int startadd = 0;
                        int.TryParse(data[1].Value, out startadd);
                        int length = Convert.ToInt32(data[2].Value);

                        for (int d = 3; d < data.Count - 1; d++)
                        {
                            int index = (startadd * 128) + (d - 3);
                            int byte1 = buffer[index++] >> 8;
                            int byte2 = buffer[index++] & 0xff;
                            int realVal = byte1 + byte2;
                            data[d].Value = realVal.ToString();
                        }
                    }
                }
            }
            *///for

            FrameConfigModel FrameModel = fileCfgModel.FrameModel;
            foreach (var selectModel in FrameModel.CanFrameModels)
            {
                if (selectModel.Id == 0x19952141)
                {
                    SafetyList.Add(selectModel);
                }
            }

            try
            {
                for (int i = 0; i < SafetyList.Count; i++)
                {
                    CurrentModel = SafetyList[i];
                    tmpModel = new CanFrameModel(SafetyList[i]);

                    //更新model数据
                    UpdateModel(fileCfgModel);

                    //编辑数据信息
                    var dataLists = SafetyList[i].FrameDatas[0].DataInfos;
                    int index = i * 128;
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

                    CanFrameID frameId = SafetyList[i].FrameId;
                    if (frameId.ContinuousFlag != 0)
                    {
                        SaveUncontinue(); //连续
                    }
                }//for

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
        #endregion


        #region CAN操作
        /// <summary>
        /// 初始化Can设置
        /// </summary>
        public void InitCanHelper()
        {
            //接收处理
            //ecanHelper.OnReceiveCan1 += RecvProcCan1;
            //ecanHelper.OnReceiveCan2 += RecvProcCan2;
            ecanHelper.RegisterRecvProcessCan1(RecvProcCan1);
            ecanHelper.RegisterRecvProcessCan2(RecvProcCan2);
        }
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
            if (frame.FrameDatas.Count > 0)
            {
                if (frame.FrameId.ContinuousFlag == 0)
                {
                    //非连续
                    Task.Run(() =>
                    {
                        ProtocolHelper.AnalyseFrameModel(frame);
                        SendSingleFrame(id, frameData.Data, frameData);
                    });
                }
                else
                {
                    //连续
                    Task.Run(() =>
                    {
                        List<CanFrameData> frameDataList = ProtocolHelper.AnalyseMultiPackage(frameData);
                        foreach (CanFrameData fd in frameDataList)
                        {
                            SendSingleFrame(id, fd.Data, fd);
                            Thread.Sleep(5);
                            //Thread.Sleep(sendInterval);
                        }
                    });
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

        }//func

        private void RecvProcCan1(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Monitor)
                return;

            RecvFrame(recvData);
        }
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Monitor)
                return;

            RecvFrame(recvData);
        }
        private void RecvFrame(CAN_OBJ recvData)
        {
            string currentTime = DateTime.Now.ToString("HH:mm:ss.fff");
            uint id = recvData.ID;

            //过滤心跳 暂不过滤
            //string strId = id.ToString();
            //if (strId.Contains("197F")) //过滤dsp的bootloader心跳和app心跳
            //    return;

            //增加到报文信息
            {
                //string msg = $"[接收]{currentTime}  :  0x{id.ToString("X8")}\t{BitConverter.ToString(recvData.Data)}";
                //AddDebugInfo(msg);
            }

            CanFrameID frameId = new CanFrameID();
            frameId.ID = id;

            //过滤请求帧
            if (frameId.FrameType == 2)
                return;

            byte devAddr = DeviceManager.Instance().GetSelectDev();
            if (frameId.FC == 0x39) //组播（功能码57）不走地址设置
            {
                devAddr = 0;
            }

            /*if (frameId.Continueflg == 0)
            {
                //非连续
                recvTime = currentTime;

                CanFrameModel? frame = ProtocolHelper.FrameToModel(frameCfgModel.CanFrameModels, recvData.ID, recvData.Data, devAddr);
                if (frame == null)
                    return;

                UpdateRecv(recvData, frame);
            }
            else
            {
                //连续
                ///使用第一包的时间
                {
                    if (recvData.Data[0] == 0)
                    {
                        recvTime = currentTime;
                    }
                }

                CanFrameModel? frame = ProtocolHelper.MultiFrameToModel(frameCfgModel.CanFrameModels, recvData.ID, recvData.Data, devAddr);
                if (frame == null)
                    return;

                UpdateContinueRecv(frame);
            }*/
        }


        private void UpdateRecv(CAN_OBJ recvData, CanFrameModel frame)
        {
            if (frame.FrameDatas.Count <= 0)
                return;

            CanFrameData frameData = frame.FrameDatas[0];
            //ProcPresion(frameData);
            List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>(frameData.DataInfos);  //使用第一包数据
            string addr = frame.GetAddrInt().ToString();

            //去除起始地址、个数不显示
            {
                dataInfos.RemoveAt(0); //起始地址
                dataInfos.RemoveAt(0); //个数
            }

            //去除隐藏字段
            for (int i = 0; i < dataInfos.Count; i++)
            {
                CanFrameDataInfo info = dataInfos[i];
                if (info.Hide)
                {
                    dataInfos.RemoveAt(i);
                    i--;
                }
            }

            SendRecvFrameInfo newFrameInfo = new SendRecvFrameInfo();
            newFrameInfo.IsSend = false;
            newFrameInfo.Time = recvTime;
            newFrameInfo.ID = "0x" + recvData.ID.ToString("X8");
            newFrameInfo.Datas = BitConverter.ToString(recvData.Data);

            newFrameInfo.IsContinue = frame.FrameId.ContinuousFlag == 1;
            newFrameInfo.Addr = addr;
            newFrameInfo.PackageNum = frame.GetPackageNum();


            int count = dataInfos.Count;
            if (count > 0 && !dataInfos[0].Hide)
            {
                newFrameInfo.Info1 = dataInfos[0].Name;
                newFrameInfo.Value1 = $"{dataInfos[0].Value} {dataInfos[0].Unit}";
            }
            if (count > 1 && !dataInfos[1].Hide)
            {
                newFrameInfo.Info2 = dataInfos[1].Name;
                newFrameInfo.Value2 = $"{dataInfos[1].Value} {dataInfos[1].Unit}";
            }
            if (count > 2 && !dataInfos[2].Hide)
            {
                newFrameInfo.Info3 = dataInfos[2].Name;
                newFrameInfo.Value3 = $"{dataInfos[2].Value} {dataInfos[2].Unit}";
            }
            if (count > 3 && !dataInfos[3].Hide)
            {
                newFrameInfo.Info4 = dataInfos[3].Name;
                newFrameInfo.Value4 = $"{dataInfos[3].Value} {dataInfos[3].Unit}";
            }
            //if (count > 4 && !dataInfos[4].Hide)
            //{
            //    newFrameInfo.Info5 = dataInfos[4].Name;
            //    newFrameInfo.Value5 = dataInfos[4].Value;
            //}

            //UpdateRecv(newFrameInfo);
        }
        #endregion
    }
}
