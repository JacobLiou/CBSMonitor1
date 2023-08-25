using CanProtocol.ProtocolModel;
using Communication.Can;
using FontAwesome.Sharp;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Utilities.Net;
using SofarHVMDAL;
using SofarHVMExe.Commun;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace SofarHVMExe.ViewModel
{
    class MapOptPageVm : ViewModelBase
    {
        /// <summary>
        /// 监控界面ViewModel
        /// </summary>
        public MapOptPageVm(EcanHelper? helper)
        {
            ecanHelper = helper;
            Init();
        }

        //声明CancellationTokenSource对象
        public static CancellationTokenSource cancelTokenSource = null;
        #region 字段
        private int sendInterval = 1; //发送帧间隔，默认1ms
        private FileConfigModel? fileCfgModel = null;
        public EcanHelper? ecanHelper = null; //声明CancellationTokenSource对象
        private int sendIndex = 0;

        private Dictionary<string, string> _addrVariableDic = new Dictionary<string, string>(); //地址和变量映射字典

        public bool startRead = false;
        public bool IsWriting = false;
        public bool IsRepeat = false;
        private uint _id = 0x1FFA5A5; //读写DSP数据指令的id
        private object? ackValue = null;
        private CAN_OBJ _recvData;
        #endregion


        #region 属性
        private ObservableCollection<MemoryModel> dataSource = new ObservableCollection<MemoryModel>();
        public ObservableCollection<MemoryModel> DataSource
        {
            get => dataSource;
            set
            {
                dataSource = value;
                OnPropertyChanged();
            }
        }

        private MemoryModel selectData = null;
        public MemoryModel SelectData
        {
            get => selectData;
            set
            {
                selectData = value;
                OnPropertyChanged();
            }
        }
        private string filePath = "";
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged();
            }
        }
        private string readBtnText = "开始读取";
        public string ReadBtnText
        {
            get => readBtnText;
            set
            {
                readBtnText = value;
                OnPropertyChanged();
            }
        }
        private int intervalVal = 200;
        public int IntervalVal
        {
            get => intervalVal;
            set
            {
                intervalVal = value;
                OnPropertyChanged();
            }
        }
        private bool intervalEnable = true;
        public bool IntervalEnable
        {
            get => intervalEnable;
            set
            {
                intervalEnable = value;
                OnPropertyChanged();
            }
        }
        #endregion


        #region 命令
        public ICommand ImportCommand { get; set; }
        public ICommand AddCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand StartReadCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand ClearCommand { get; set; }
        public ICommand WriteDataCommand { get; set; }
        public ICommand LoadedCommand { get; set; }
        #endregion


        #region 成员方法
        private void Init()
        {
            ImportCommand = new SimpleCommand(Import);
            AddCommand = new SimpleCommand(Add);
            DeleteCommand = new SimpleCommand(Delete);
            StartReadCommand = new SimpleCommand(StartRead);
            SaveCommand = new SimpleCommand(SaveData);
            ClearCommand = new SimpleCommand(Clear);
            WriteDataCommand = new SimpleCommand(WriteData);
            LoadedCommand = new SimpleCommand(Loaded);

            UpdateModel();
        }

        private void Loaded(object obj)
        {
            cancelTokenSource.Cancel();
            startRead = false;
            IntervalEnable = true;
        }

        /// <summary>
        /// 更新model数据
        /// </summary>
        /// <param name="fileCfgModel"></param>
        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel != null)
            {
                startRead = false;
                IntervalEnable = true;
                ReadBtnText = "开始读取";
                DataSource = new ObservableCollection<MemoryModel>(fileCfgModel.MemModels);
            }
        }
        private void Import(object o)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "选择map文件";
                dlg.Filter = "map(*.map)|*.map";
                dlg.Multiselect = false;
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (!File.Exists(dlg.FileName))
                    {
                        throw new FileNotFoundException("文件不存在");
                    }

                    //文件路径
                    FilePath = dlg.FileName;

                    //收集map文件中的地址和变量数据
                    _addrVariableDic = new Dictionary<string, string>();

                    StreamReader file = new StreamReader(FilePath);

                    try
                    {
                        int flagIndex = 0,
                            addrIndex = 0,
                            variableIndex = 0;

                        string line = string.Empty;
                        while ((line = file.ReadLine()) != null)
                        {
                            if (line.Trim() == "")
                                continue;

                            switch (flagIndex)
                            {
                                case 0:
                                    if (line.Contains("SYMBOLS"))
                                    {
                                        //检测已进入第一行Title
                                        flagIndex = 1;
                                    }
                                    break;
                                case 1:
                                    if (line.Contains("page") && line.Contains("address") && line.Contains("name"))
                                    {
                                        //检测已进入第二行Title
                                        flagIndex = 2;

                                        //解析地址和变量所在列号
                                        var strLine = System.Text.RegularExpressions.Regex.Split(line, @"\s{1,}");
                                        for (int i = 0; i < strLine.Length; i++)
                                        {
                                            string s = strLine[i];
                                            if (s.Contains("address"))
                                            {
                                                addrIndex = i;
                                            }
                                            else if (s.Contains("name"))
                                            {
                                                variableIndex = i;
                                            }
                                        }
                                    }
                                    break;
                                case 2:
                                    var strs = System.Text.RegularExpressions.Regex.Split(line, @"\s{1,}");
                                    string addr = strs[addrIndex];
                                    if (addr == "ffffffff" || strs.Length < (variableIndex + 1) || strs[variableIndex] == "")
                                        continue;

                                    string variable = strs[variableIndex];
                                    if (variable[0] == '_')
                                    {
                                        variable = variable.Remove(0, 1);
                                    }
                                    _addrVariableDic[addr] = variable;
                                    break;
                                default:
                                    break;
                            }
                        }

                        flagIndex = 0;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    finally
                    {
                        file.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Add(object o)
        {
            MemoryModel mem = new MemoryModel();
            DataSource.Add(mem);
        }
        private void Delete(object o)
        {
            //移除当前选择项，为空时不会删除掉现有数据
            DataSource.Remove(SelectData);

            //选择最后一个
            if (DataSource.Count > 0)
            {
                SelectData = DataSource.Last();
            }
        }
        private void SaveData(object o)
        {
            fileCfgModel.MemModels = DataSource.ToList();
            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }
        private void Clear(object o)
        {
            if (DataSource.Count <= 0)
                return;

            MessageBoxResult ret = MessageBox.Show("确认清除数据？", "提示", MessageBoxButton.OKCancel);
            if (ret == MessageBoxResult.OK)
            {
                DataSource.Clear();
            }
        }
        private void WriteData(object o)
        {
            StartWriteData(SelectData);
        }
        #region 读
        private void StartRead(object o)
        {
            try
            {
                if (!CheckConnect())
                {
                    MessageBox.Show("未连接设备，请先连接CAN操作", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (string.IsNullOrEmpty(FilePath))
                {
                    MessageBox.Show("未加载文件，请先导入MAP文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (DataSource.Count == 0)
                {
                    MessageBox.Show("未加载数据，请先编辑MAP表", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (DeviceManager.Instance().GetSelectDev() == 0)
                {
                    MessageBox.Show("未选择设备，请先选择设备", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    startRead = !startRead;
                    IntervalEnable = !startRead;

                    if (startRead)
                    {
                        ReadBtnText = "终止读取";

                        cancelTokenSource = new CancellationTokenSource();

                        Task.Factory.StartNew(ReadData, cancelTokenSource.Token);
                    }
                    else
                    {
                        ReadBtnText = "开始读取";

                        cancelTokenSource.Cancel();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行错误，" + ex.Message);
            }
        }//func
        private void ReadData()
        {
            while (!cancelTokenSource.IsCancellationRequested)
            {
                //检查是否为开始读取状态
                if (!startRead)
                    break;

                //处于“正在编辑数据”或者“数据源为空”的时候，暂停读取更新
                if (IsWriting || DataSource.Count == 0)
                    continue;

                try
                {
                    //实时更新当前数据源的情况
                    List<MemoryModel> dataList = new List<MemoryModel>(DataSource);

                    //循环读取每一个变量
                    MemoryModel mem = dataList[sendIndex];
                    byte[] sendBytes = new byte[8] { 0x1, 0, 0, 0, 0, 0, 0, 0 };
                    string addressOrName = mem.AddressOrName;
                    string type = mem.Type;
                    string value = mem.Value;
                    string address = string.Empty;

                    //地址不为空或名称不为空即可执行
                    if (!string.IsNullOrEmpty(addressOrName))
                    {
                        address = checkAddr(addressOrName);

                        if (!string.IsNullOrEmpty(address))
                        {
                            //转义地址为字节数组
                            byte[] addrBytes = HexDataHelper.HexStringToByte2(address);
                            for (int i = 0; i < 4; i++)
                            {
                                sendBytes[i + 4] = addrBytes[i];
                            }

                            //坐标
                            sendBytes[3] = Convert.ToByte(sendIndex);

                            //类型
                            sendBytes[1] = ConvertType(type);

                            //设置指定设备地址
                            uint id = _id;
                            {
                                byte addr = DeviceManager.Instance().GetSelectDev();
                                if (addr != 0)
                                {
                                    sendBytes[2] = addr; //指定发送数据给某台设备
                                }
                            }

                            //1、发送
                            SendFrame(id, sendBytes);
                            DebugTool.OutputBytes($"读请求-{id.ToString("X")}", sendBytes);

                            //2、处理接收
                            {
                                ///在指定内如果收到正确应答则ok
                                Stopwatch timer = new Stopwatch();
                                timer.Start();
                                while (true)
                                {
                                    Thread.Sleep(100);

                                    int result = ReadDataRequestCheck();
                                    if (result == 0)
                                    {
                                        _recvData.ID = 0;
                                        UpdateDataSource(mem.AddressOrName);
                                        break;
                                    }
                                    /*else if (result == 1)
                                    {
                                        UpdateDataSourceTonull(mem.AddressOrName, "Fail:响应为空或长度不等于8");
                                    }
                                    else if (result == 2)
                                    {
                                        UpdateDataSourceTonull(mem.AddressOrName, "Error:响应非读应答帧");
                                    }
                                    else if (result == 3)
                                    {
                                        UpdateDataSourceTonull(mem.AddressOrName, "Error:响应非请求编码");
                                    }*/

                                    if (timer.ElapsedMilliseconds > 100)
                                    {
                                        timer.Stop();
                                        break;
                                    }
                                }
                            }

                            Thread.Sleep(IntervalVal);
                            IsWriting = false;
                        }
                        else
                        {
                            UpdateDataSourceTonull(mem.AddressOrName, $"变量[{addressOrName}]未找到地址！");
                        }
                    }

                    //更新坐标值
                    if (sendIndex == dataList.Count - 1)
                        sendIndex = 0;
                    else
                        sendIndex++;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }//while
        }

        private string checkAddr(string addressOrName)
        {
            try
            {
                string address = "";
                if (addressOrName.Contains("0x") || addressOrName.Contains("0X"))
                {
                    //地址
                    address = addressOrName;
                }
                else
                {
                    //变量名转地址
                    foreach (var pair in _addrVariableDic)
                    {
                        if (pair.Value == addressOrName)
                        {
                            address = pair.Key;
                            break;
                        }
                    }
                }
                return address;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private int ReadDataRequestCheck()
        {
            byte[] datas = _recvData.Data;
            if (_recvData.ID == 0 || datas.Length != 8)
                return 1;

            byte funcCode = datas[0];
            if (funcCode != 1)
                return 2;

            int funcIndex = datas[3];
            if (funcIndex != sendIndex)
                return 3;

            byte type = datas[1];
            List<byte> valueList = new List<byte>(datas.Skip(4).Take(4));
            if (type == 1)
            {
                //i16
                string byte1 = valueList[0].ToString("X2");
                string byte2 = valueList[1].ToString("X2");
                ackValue = Convert.ToInt16(byte2 + byte1, 16);
            }
            else if (type == 2)
            {
                //u16
                ackValue = ackValue = Convert.ToUInt16(valueList[0] | (valueList[1] << 8));
            }
            else if (type == 3)
            {
                //u32
                int b1 = valueList[0];
                int b2 = valueList[1] << 8;
                int b3 = valueList[2] << 16;
                int b4 = valueList[3] << 24;
                ackValue = Convert.ToUInt32(b1 | b2 | b3 | b4);
            }
            else if (type == 4)
            {
                //float
                ackValue = BitConverter.ToSingle(valueList.ToArray());
            }
            else if (type == 5)
            {
                // i32
                string byte1 = valueList[0].ToString("X2");
                string byte2 = valueList[1].ToString("X2");
                string byte3 = valueList[2].ToString("X2");
                string byte4 = valueList[3].ToString("X2");
                ackValue = Convert.ToInt32(byte4 + byte3 + byte2 + byte1, 16);
            }

            return 0;
        }
        #endregion

        #region 写
        public void StartWriteData(MemoryModel mem)
        {
            try
            {
                //写入数据请求
                WriteDataRequest(mem);

                //处理接收
                {
                    ///在指定内如果收到正确应答则ok
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    while (true)
                    {
                        string result = "";
                        if (WriteDataRequestCheck(out result))
                        {
                            WriteData(mem);
                            break;
                        }
                        else
                        {
                            //MessageBox.Show(result, "提示");
                        }

                        if (timer.ElapsedMilliseconds > 100)
                        {
                            timer.Stop();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                IsWriting = false;
            }
        }
        public void WriteDataRequest(MemoryModel mem)
        {
            //if (!CheckConnect())
            //    return;

            if (mem != null)
            {
                /* 检查数据
                 * 1.如果为空return、
                 * 2.如果数值越界return*/
                if (string.IsNullOrEmpty(mem.AddressOrName) || mem.Value == "")
                {
                    MessageBox.Show($"ERROR:写入时地址/变量名和值不能为空", "提示");
                    return;
                }

                byte[] sendBytes = new byte[8] { 0x2, 0, 0, 0, 0, 0, 0, 0 };
                string addressOrName = mem.AddressOrName;
                string type = mem.Type;
                string value = mem.Value;
                string address = string.Empty;

                bool isError = false;
                switch (mem.Type)
                {
                    case "I16":
                        short tempShort = 0;
                        isError = Int16.TryParse(value, out tempShort);
                        break;
                    case "U16":
                        ushort tempUshort = 0;
                        isError = UInt16.TryParse(value, out tempUshort);
                        break;
                    case "I32":
                        int tempInt = 0;
                        isError = Int32.TryParse(value, out tempInt);
                        break;
                    case "U32":
                        uint tempUint = 0;
                        isError = UInt32.TryParse(value, out tempUint);
                        break;
                    case "float":
                        float tempFloat = 0;
                        isError = float.TryParse(value, out tempFloat);
                        break;
                    default:
                        break;
                }

                if (!isError)
                {
                    MessageBox.Show($"ERROR:写入的值为非法数据，请输入数据类型对应的范围值", "提示");
                    return;
                }

                //地址
                address = checkAddr(addressOrName);
                if (string.IsNullOrEmpty(address))
                {
                    UpdateDataSourceTonull(mem.AddressOrName, $"变量[{addressOrName}]未找到地址！");
                    return;
                }

                byte[] addrBytes = HexDataHelper.HexStringToByte2(address);
                for (int i = 0; i < 4; i++)
                {
                    sendBytes[i + 4] = addrBytes[i];
                }

                //类型
                sendBytes[1] = ConvertType(type);

                // 设置id
                uint id = _id;
                {
                    byte addr = DeviceManager.Instance().GetSelectDev();
                    if (addr != 0)
                    {
                        //指定发送数据给某台设备
                        sendBytes[2] = addr;
                    }
                }

                //发送
                SendFrame(id, sendBytes);

                DebugTool.OutputBytes("写数据请求", sendBytes);
            }
            else
            {
                MessageBox.Show("未选择数据，请先选中操作行！", "提示");
            }
        }
        private bool WriteDataRequestCheck(out string strTip)
        {
            strTip = "";

            if (_recvData.ID == 0 || _recvData.DataLen != 8)
            {
                strTip = "Fail:响应为空或长度不等于8";
                return false;
            }

            byte[] datas = _recvData.Data;

            DebugTool.OutputBytes("写数据应答", datas);

            bool result = datas[4] == 1;
            if (!result)
            {
                strTip = "ERROR:当前不能写数据";
            }
            return result;
        }
        private void WriteData(MemoryModel mem)
        {
            byte[] sendBytes = new byte[8] { 0x3, 0, 0, 0, 0, 0, 0, 0 };
            string address = mem.AddressOrName;
            string type = mem.Type;
            string strValue = mem.Value;

            //数据
            List<byte> byteList = new List<byte>();
            if (strValue.Contains("0x") || strValue.Contains("0X"))
            {
                //十六进制数转十进制数处理
                strValue = strValue.Replace("0x", "").Replace("0X", "");
            }

            if (type == "float")
            {
                float v = 0;
                if (float.TryParse(strValue, out v))
                {
                    byteList = new List<byte>(BitConverter.GetBytes(v));
                }
            }
            else if (type == "U16" || type == "U32")
            {
                uint value = 0;
                if (uint.TryParse(strValue, out value))
                {
                    byte[] arr = HexDataHelper.UIntToByte(value, true);
                    byteList = new List<byte>(arr);
                }
            }
            else if (type == "I16" || type == "I32")
            {
                int value = 0;
                if (int.TryParse(strValue, out value))
                {
                    byte[] arr = HexDataHelper.IntToByte(value, true);
                    byteList = new List<byte>(arr);
                }
            }

            for (int i = 0; i < byteList.Count; i++)
            {
                sendBytes[i + 4] = byteList[i];
            }

            //类型
            sendBytes[1] = ConvertType(type);

            //设置id
            uint id = _id;
            {
                byte addr = DeviceManager.Instance().GetSelectDev();
                if (addr != 0)
                {
                    //指定发送数据给某台设备
                    CanFrameID frameId = new CanFrameID(id);
                    //frameId.DstAddr = addr;
                    sendBytes[2] = addr;
                    id = frameId.ID;
                }
            }

            //发送
            SendFrame(id, sendBytes);

            DebugTool.OutputBytes("写数据", sendBytes);
        }
        #endregion

        private void UpdateDataSource(string addressOrName)
        {
            List<MemoryModel> memList = new List<MemoryModel>(DataSource);
            int index = memList.FindIndex((o) =>
            {
                return o.AddressOrName == addressOrName;
            });

            if (index == -1)
                return;

            MemoryModel mem = DataSource[index];

            //将解析数值进行赋值
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mem.Value = ackValue.ToString();
            });

            //变量名转地址
            string address = "";
            foreach (var pair in _addrVariableDic)
            {
                if (pair.Value == addressOrName)
                {
                    address = pair.Key;
                    break;
                }
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mem.Address = address.ToString();
            });

            MultiLanguages.Common.LogHelper.WriteLog($"地址名称：{addressOrName}，数值：{mem.Value}");
        }

        private void UpdateDataSourceTonull(string addressOrName, string content)
        {
            List<MemoryModel> memList = new List<MemoryModel>(DataSource);
            int index = memList.FindIndex((o) =>
            {
                return o.AddressOrName == addressOrName;
            });

            if (index == -1)
                return;

            MemoryModel mem = DataSource[index];
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mem.Value = content;
            });
        }
        private byte ConvertType(string type)
        {
            switch (type)
            {
                case "I16":
                    return 0x1;
                case "U16":
                    return 0x2;
                case "U32":
                    return 0x3;
                case "float":
                    return 0x4;
                case "I32":
                    return 0x05;
                default:
                    return 0x0;
            }
        }

        #region CAN操作
        /// <summary>
        /// 初始化Can设置
        /// </summary>
        public void InitCanHelper()
        {
            ecanHelper.RegisterRecvProcessCan1(RecvProcCan1);
            ecanHelper.RegisterRecvProcessCan2(RecvProcCan2);
        }
        private void RecvProcCan1(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.MapOpt)
                return;

            RecvFrame(recvData);
        }
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.MapOpt)
                return;

            RecvFrame(recvData);
        }
        /// <summary>
        /// 检查Can连接
        /// </summary>
        /// <returns></returns>
        private bool CheckConnect()
        {
            if (!ecanHelper.IsConnected)
            {
                MessageBox.Show("未连接CAN设备，请先连接后再进行操作！", "提示");
                return false;
            }

            if (!ecanHelper.IsCan1Start && !ecanHelper.IsCan2Start)
            {
                MessageBox.Show("CAN通道未打开！", "提示");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 下发CAN数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="datas"></param>
        private void SendFrame(uint id, byte[] datas)
        {
            if (ecanHelper.IsCan1Start)
            {
                ecanHelper.SendCan1(id, datas);
            }
        }
        /// <summary>
        /// 接收CAN处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvFrame(CAN_OBJ recvData)
        {
            //if (startRecv)
            //return;

            //过滤id
            {
                string strId = recvData.ID.ToString("X");

                //过滤心跳帧
                if (strId.Contains("197F"))
                    return;

                if (strId.Contains("1FF"))
                {
                    int stop = 10;
                }

                byte addr = DeviceManager.Instance().GetSelectDev();
                if (addr != 0)
                {
                    //指定接收某台设备
                    CanFrameID frameId = new CanFrameID(_id);
                    //frameId.SrcAddr = addr;
                    if (recvData.ID != frameId.ID)
                        return;
                }
                else
                {
                    //过滤默认帧
                    if (recvData.ID != _id)
                        return;
                }
            }

            byte[] datas = recvData.Data;
            if (datas.Length != 8)
                return;

            _recvData = recvData;
            DebugTool.OutputBytes($"接收信息：canid：{recvData.ID}、data：", recvData.Data);
        }
        #endregion

        #endregion

    }//class

}
