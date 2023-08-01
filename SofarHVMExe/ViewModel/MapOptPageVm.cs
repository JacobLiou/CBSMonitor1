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
        private Dictionary<string, string> _addrVariableDic = new Dictionary<string, string>(); //地址和变量映射字典
        public static bool _second = false;
        public static bool _frist = false;
        public static bool _start = false;

        public bool startRead = false;
        public bool IsWriting = false;
        private uint _id = 0x1FFA5A5; //读写DSP数据指令的id
        private int _ackValue1; //dsp应答数据 int类型
        private uint _ackValue; //dsp应答数据 uint类型
        private float _ackFltValue; //dsp应答数据 float类型
        private CAN_OBJ _recvData;
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
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择map文件";
            dlg.Filter = "map(*.map)|*.map";
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            FilePath = dlg.FileName;

            if (!File.Exists(FilePath))
            {
                MessageBox.Show("文件不存在");
                return;
            }

            //打开文件
            string line;
            int counter = 0;
            int addrIndex = 0;
            int variableIndex = 0;

            //收集map文件中的地址和变量数据
            _addrVariableDic = new Dictionary<string, string>();

            StreamReader file = new StreamReader(FilePath);
            try
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Trim() == "")
                        continue;

                    if (/*line.Contains("GLOBAL") && */line.Contains("SYMBOLS"))
                    {
                        _frist = true;
                    }
                    else if (_frist && (line.Contains("page") && line.Contains("address") && line.Contains("name")))
                    {
                        _frist = false;
                        _second = true;

                        //解析地址和变量所在列号
                        {
                            var strs = System.Text.RegularExpressions.Regex.Split(line, @"\s{1,}");

                            for (int i = 0; i < strs.Length; i++)
                            {
                                string s = strs[i];
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
                    }
                    else if (_second)
                    {
                        _second = false;
                        _start = true;
                    }
                    else if (_start)
                    {
                        try
                        {
                            var strs = System.Text.RegularExpressions.Regex.Split(line, @"\s{1,}");
                            string addr = strs[addrIndex];
                            if (addr == "ffffffff" ||
                                strs.Length < (variableIndex + 1))
                                continue;

                            string variable = strs[variableIndex];
                            if (variable.StartsWith("__"))
                            {
                                int aa = 10;
                            }

                            if (variable[0] == '_')
                            {
                                variable = variable.Remove(0, 1);
                            }
                            _addrVariableDic[addr] = variable;
                        }
                        catch (Exception ex)
                        {
                            int stop = 10;
                        }

                    }
                    counter++;
                }

                _start = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR:" + ex.ToString());
                return;
            }
            finally
            {
                file.Close();
                Console.WriteLine("There were {0} lines.", counter);
            }

            return;
        }
        private void Add(object o)
        {
            MemoryModel mem = new MemoryModel();
            DataSource.Add(mem);
        }
        private void Delete(object o)
        {
            if (SelectData == null)
            {
                MessageBox.Show("请选择一条数据！", "提示");
                return;
            }

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
            //if (!CheckConnect())
            //    return;

            if (FilePath == null || FilePath == "")
            {
                MessageBox.Show("未加载文件，请先导入map文件！", "提示");
                return;
            }

            startRead = !startRead;
            IntervalEnable = !startRead;

           //更新界面
           {
               if (startRead)
               {
                   cancelTokenSource = new CancellationTokenSource();
                   //停止按钮
                   ReadBtnText = "停止";
               }
               else
               {
                   cancelTokenSource.Cancel();
                   //开始读取按钮
                   ReadBtnText = "开始读取";
               }
           }

           //读取数据
           {
               if (startRead)
               {
                   Task.Factory.StartNew(ReadData, cancelTokenSource.Token);
               }
           }

        }//func
        private void ReadData()
        {
            List<MemoryModel> dataList = new List<MemoryModel>(DataSource);

            while (!cancelTokenSource.IsCancellationRequested)
            {
                if (!startRead)
                    break;

                if (dataList.Count == 0)
                    continue;

                //正在写的时候暂停读取更新
                if (IsWriting)
                    continue;

                //循环读取每一个变量
                MemoryModel mem = dataList[sendIndex];
                byte[] sendBytes = new byte[8] { 0x1, 0, 0, 0, 0, 0, 0, 0 };
                string addressOrName = mem.AddressOrName;
                string type = mem.Type;
                string value = mem.Value;
                string address = "";

                    if (string.IsNullOrEmpty(addressOrName))
                        continue;

                //地址
                if (addressOrName.Contains("0x") || addressOrName.Contains("0X"))
                {
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

                    if (address == "")
                    {
                        //MessageBox.Show($"变量[{addressOrName}]未找到地址！", "提示");
                        continue;
                    }
                }

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
                        //指定发送数据给某台设备
                        sendBytes[2] = addr;
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
                        if (ReadDataRequestCheck())
                        {
                            _recvData.ID = 0;
                            UpdateDataSource(mem.AddressOrName);
                            break;
                        }

                        if (timer.ElapsedMilliseconds > 100)
                        {
                            timer.Stop();
                            break;
                        }
                    }
                }

                Thread.Sleep(IntervalVal);

                //更新坐标值
                if (sendIndex == dataList.Count - 1)
                    sendIndex = 0;
                else
                    sendIndex++;
            }//while
        }

        private bool ReadDataRequestCheck()
        {
            byte[] datas = _recvData.Data;
            if (_recvData.ID == 0 || datas.Length != 8)
                return false;

            byte funcCode = datas[0];
            if (funcCode != 1)
                return false;

            int funcIndex = datas[3];
            if (funcIndex != sendIndex)
                return false;

            byte type = datas[1];
            List<byte> valueList = new List<byte>(datas.Skip(4).Take(4));
            if (type == 1)
            {
                //u8
                //_ackValue = valueList[0];
                string byte1 = valueList[0].ToString("X2");
                string byte2 = valueList[1].ToString("X2");
                _ackValue1 = Convert.ToInt16(byte2 + byte1, 16);
            }
            else if (type == 2)
            {
                //u16
                _ackValue = (uint)(valueList[0] | (valueList[1] << 8));
            }
            else if (type == 3)
            {
                //u32
                int b1 = valueList[0];
                int b2 = valueList[1] << 8;
                int b3 = valueList[2] << 16;
                int b4 = valueList[3] << 24;
                _ackValue = (uint)(b1 | b2 | b3 | b4);
            }
            else if (type == 4)
            {
                //float
                _ackFltValue = BitConverter.ToSingle(valueList.ToArray());
            }
            else if (type == 5)
            {
                // i32
                string byte1 = valueList[0].ToString("X2");
                string byte2 = valueList[1].ToString("X2");
                string byte3 = valueList[2].ToString("X2");
                string byte4 = valueList[3].ToString("X2");
                _ackValue1 = Convert.ToInt32(byte4 + byte3 + byte2 + byte1, 16);
            }
            return true;
        }
        #endregion

        #region 写
        public void StartWriteData(MemoryModel mem)
        {
            WriteDataRequest(mem);

            //处理接收
            {
                ///在指定内如果收到正确应答则ok
                Stopwatch timer = new Stopwatch();
                timer.Start();
                while (true)
                {
                    if (WriteDataRequestCheck())
                    {
                        WriteData(mem);
                        break;
                    }

                    if (timer.ElapsedMilliseconds > 100)
                    {
                        timer.Stop();
                        break;
                    }
                }
            }

            IsWriting = false;
        }
        public void WriteDataRequest(MemoryModel mem)
        {
            //if (!CheckConnect())
            //    return;

            if (mem == null)
            {
                MessageBox.Show("未选择数据，请先选中操作行！", "提示");
                return;
            }

            byte[] sendBytes = new byte[8] { 0x2, 0, 0, 0, 0, 0, 0, 0 };
            string addressOrName = mem.AddressOrName;
            string type = mem.Type;
            string value = mem.Value;
            string address = "";

            //检查数据
            switch (mem.Type)
            {
                case "I16":
                    
                    break;
                case "U16":break;
                case "U32":break;
                case "float":break;
                case "I32":break;
                default:
                    break;
            }

            if (string.IsNullOrEmpty(addressOrName) || value == "")
                return;

            //地址
            if (addressOrName.Contains("0x") || addressOrName.Contains("0X"))
            {
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

                if (address == "")
                {
                    MessageBox.Show($"变量[{addressOrName}]未找到地址！", "提示");
                    return;
                }
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
                    //CanFrameID frameId = new CanFrameID(id);
                    //frameId.DstAddr = addr;
                    //id = frameId.ID;
                }
            }

            //发送
            SendFrame(id, sendBytes);

            DebugTool.OutputBytes("写数据请求", sendBytes);
        }
        private bool WriteDataRequestCheck()
        {
            if (_recvData.ID == 0 || _recvData.DataLen != 8)
                return false;

            byte[] datas = _recvData.Data;

            DebugTool.OutputBytes("写数据应答", datas);

            return datas[4] == 1;
        }
        private void WriteData(MemoryModel mem)
        {
            byte[] sendBytes = new byte[8] { 0x3, 0, 0, 0, 0, 0, 0, 0 };
            string address = mem.AddressOrName;
            string type = mem.Type;
            string strValue = mem.Value;

            if (address == null ||
                address == "" ||
                strValue == ""
                )
            {
                return;
            }

            //数据
            List<byte> byteList = new List<byte>();
            if (strValue.Contains("0x") || strValue.Contains("0X"))
            {
                //十六进制数转十进制数处理
                strValue = strValue.Replace("0x", "").Replace("0X", "");
            }

            if (type == "float")
            {
                float v;
                if (float.TryParse(strValue, out v))
                {
                    byteList = new List<byte>(BitConverter.GetBytes(v));
                }
            }
            else if (type == "U16" || type == "U32")
            {
                //uint
                //byte[] arr = HexDataHelper.HexStringToByte2(strValue);
                uint value = 0;
                if (!uint.TryParse(strValue, out value))
                {
                    return;
                }
                byte[] arr = HexDataHelper.UIntToByte(value, true);
                byteList = new List<byte>(arr);
            }

            else if (type == "I16" || type == "I32")
            {
                int value = 0;
                if (!int.TryParse(strValue, out value))
                {
                    return;
                }
                byte[] arr = HexDataHelper.IntToByte(value, true);
                byteList = new List<byte>(arr);

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
            if (mem.Type == "float")
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    mem.Value = _ackFltValue.ToString();
                });
            }
            else if (mem.Type == "I16" || mem.Type == "I32")
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    mem.Value = _ackValue1.ToString();
                });
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    mem.Value = _ackValue.ToString();
                });
            }

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
        private void SendFrame(uint id, byte[] datas)
        {
            if (ecanHelper.IsCan1Start)
            {
                ecanHelper.SendCan1(id, datas);
            }
        }
        private void RecvFrame(CAN_OBJ recvData)
        {
            //if (startRecv)
            //return;

            //过滤id
            {
                uint id = recvData.ID;
                string strId = id.ToString("X");

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
        private void RecvProcCan1(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.MapOpt)
                return;

            if (recvData.ID == 0x197f4121)
                return;

            RecvFrame(recvData);
        }
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.MapOpt)
                return;

            RecvFrame(recvData);
        }
        #endregion

        #endregion

    }//class

}
