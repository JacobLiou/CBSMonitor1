using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CanProtocol.ProtocolModel;
using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Cmp;
using ScottPlot;
using ScottPlot.Plottable;
using ScottPlot.Renderable;
using SofarHVMExe.Util.TI;
using CanProtocol.Utilities;
using System.Windows.Threading;
using System.Windows.Interop;
using NPOI.OpenXml4Net.Util;
using SofarHVMExe.Utilities;
using Org.BouncyCastle.Asn1.Ocsp;
using MultiLanguages;
using SofarHVMExe.Model;
using NPOI.POIFS.NIO;
using NPOI.OpenXmlFormats.Dml.Chart;
using ScottPlot.Drawing.Colormaps;
using System.Collections;
using System.IO;
using FontAwesome.Sharp;
using NPOI.SS.Formula.Atp;
using NPOI.Util;
using SofarHVMExe.Utilities.Global;

namespace SofarHVMExe.ViewModel
{
    public class OscilloscopePageVM {

        public OscilloscopePageVM(EcanHelper? ecanHelper)
        {
            this._ecanHelper = ecanHelper;
            OscilloscopeVm = new OscilloscopeVM(ecanHelper);
            FaultWaveRecordVm= new FaultWaveRecordVM(ecanHelper);

            this._ecanHelper.RegisterRecvProcessCan1(OnReceiveFrame);
        }

        
        private EcanHelper? _ecanHelper = null;
        private FileConfigModel? fileCfgModel = null;
        
        public OscilloscopeVM OscilloscopeVm { set; get; } 

        public FaultWaveRecordVM FaultWaveRecordVm { set; get; }

        public void UpdateModel()
        {
            OscilloscopeVm.UpdateModel();  
        }

        public void OnPageLoadedJobs()
        {
            IsWorking = true;
        }

        public void OnPagesUnloadedJobs()
        {
            // 
            OscilloscopeVm.ClosedJobs();
            //FaultWaveRecordVm.ClosedJobs();
            //IsWorking = false;
        }
        

        #region CAN操作（自己造的轮子）
        public bool IsWorking { get; set; }

        /// <summary>
        /// 接收事件响应，帧筛选
        /// </summary>
        private void OnReceiveFrame(CAN_OBJ rawCanObj)
        {
            if (!IsWorking)
            {
                return;
            }

            var canID = new CanFrameID(rawCanObj.ID);
            rawCanObj.Data = rawCanObj.Data.Take(rawCanObj.DataLen).ToArray();
            // 功能码30~50为示波器保留
            if (canID.FC is >= 30 and <= 35)
            {
                OscilloscopeVm.RxQueue.Enqueue(rawCanObj);

            }
            else if (canID.FC == 41)
            {
                FaultWaveRecordVm.RxQueue.Enqueue(rawCanObj);
            }
            else
            {
                return;
            }

            // MultiLanguages.Common.LogHelper.WriteLog($"接收: " + $"0x{rawCanObj.ID:X8}: " + $"{BytesToString(rawCanObj.Data)}");
        }
        

        /// <summary>
        /// 将连续数据帧去除包序号并拼接在一起
        /// </summary>
        /// <param name="rxQueue"></param>
        /// <param name="rxCANID"></param>
        /// <param name="dataBytes"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public static bool ReadDataFrames(ConcurrentQueue<CAN_OBJ> rxQueue,CanFrameID rxCANID, 
                                            out List<byte> dataBytes, int timeoutMs)
        {
            dataBytes = new List<byte>();
            int nextPackSn = 0;

            bool isAllReceived = false;
            var sw = Stopwatch.StartNew();

            //MultiLanguages.Common.LogHelper.WriteLog($"连续数据帧接收开始【{sw.ElapsedMilliseconds}ms】");

            do
            {
                if (rxQueue.TryDequeue(out var rawCanFrame))
                {
                    var canIDinfo = new CanFrameID(rawCanFrame.ID);
                    var data = rawCanFrame.Data;

                    if (canIDinfo.FC != rxCANID.FC)
                    {
                        // CANID只核对功能码
                        break;
                    }

                    //MultiLanguages.Common.LogHelper.WriteLog("获取帧：" + canIDinfo.ID.ToString("X8") + ": " + BytesToString(data));

                    if (canIDinfo.ContinuousFlag == 0)
                    {
                        // 单数据帧
                        dataBytes.AddRange(data);
                        isAllReceived = true;
                        break;
                    }

                    byte packSn = data[0];
                    if (packSn == nextPackSn)
                    {
                        // 数据帧头、中间帧
                        dataBytes.AddRange(data[1..^0]);
                        nextPackSn++;
                    }
                    else if (packSn == 0xFF && nextPackSn > 0)
                    {
                        // 数据帧尾
                        dataBytes.AddRange(data[1..^0]);
                        isAllReceived = true;
                        break;
                    }
                    else
                    {
                        // 非数据帧、缺帧
                        MultiLanguages.Common.LogHelper.WriteLog("错误数据帧");
                        return false;
                    }

                }

            } while (sw.ElapsedMilliseconds < timeoutMs);

            sw.Stop();
            
            if (isAllReceived)
            {
                //MultiLanguages.Common.LogHelper.WriteLog($"连续数据帧接收完成，接收时长【{sw.ElapsedMilliseconds}ms】");
                return true;
            }

            //MultiLanguages.Common.LogHelper.WriteLog($"连续数据帧接收不完整，接收时长【{sw.ElapsedMilliseconds}ms】");
            return false;
        }

        /// <summary>
        /// 点表连续数据帧
        /// </summary>
        /// <param name="rxQueue"></param>
        /// <param name="rxCANID"></param>
        /// <param name="address"></param>
        /// <param name="number"></param>
        /// <param name="dataBytes"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="CheckCRC"></param>
        /// <returns></returns>
        public static bool ReadAddressDataFrames(ConcurrentQueue<CAN_OBJ> rxQueue, CanFrameID rxCANID, int address, int number, 
                                                out List<byte> dataBytes, int timeoutMs = 500, bool CheckCRC = false)
        {
            dataBytes = new List<byte>();

            if (!ReadDataFrames(rxQueue, rxCANID, out var rawDataBytes, timeoutMs))
            {
                return false;
            }

            int addrRx = rawDataBytes[1] << 8 | rawDataBytes[0];
            int byteNumRx = rawDataBytes[2];    // 字长16bits
            int crc = rawDataBytes.Count > 8 ?  rawDataBytes[2 + byteNumRx * 2 + 2] << 8 | rawDataBytes[2 + byteNumRx * 2 + 1] : 0;

            if (addrRx != address || byteNumRx != number)
            {
                return false;
            }
            ////MultiLanguages.Common.LogHelper.WriteLog("完整数据包：" + FrameDataToString(rawDataBytes));

            dataBytes = rawDataBytes.GetRange(3, byteNumRx * 2);
            if (CheckCRC)
            {
                if (CRCHelper.ComputeCrc16(dataBytes.ToArray(), dataBytes.Count) != (ushort)crc)
                {
                    return false;
                }
            }

            // //MultiLanguages.Common.LogHelper.WriteLog("数据包：" + BytesToString(dataBytes));

            return true;
        }

        /// <summary>
        /// 文件请求连续数据帧
        /// </summary>
        /// <param name="rxQueue"></param>
        /// <param name="rxCANID"></param>
        /// <param name="offset"></param>
        /// <param name="dataLen"></param>
        /// <param name="dataBytes"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="CheckCRC"></param>
        /// <returns></returns>
        public static bool ReadFileDataFrames(ConcurrentQueue<CAN_OBJ> rxQueue, CanFrameID rxCANID, int offset, int dataLen,
                                        out List<byte> dataBytes, int timeoutMs = 500, bool CheckCRC = false)
        {
            dataBytes = new List<byte>();
            if (!ReadDataFrames(rxQueue, rxCANID, out var rawDataBytes, timeoutMs))
            {
                return false;
            }

            int offsetRx = rawDataBytes[1] << 8 | rawDataBytes[0];
            int dataLenRx = rawDataBytes[3] << 8 | rawDataBytes[2]; // 字长8bits
            int crc = rawDataBytes[3 + dataLenRx + 2] << 8 | rawDataBytes[3 + dataLenRx + 1];

            if (offsetRx != offset || dataLenRx != dataLen)
            {
                return false;
            }

            // MultiLanguages.Common.LogHelper.WriteLog("完整数据包：" + FrameDataToString(rawDataBytes));

            dataBytes = rawDataBytes.GetRange(4, dataLenRx);
            if (CheckCRC)
            {
                if (CRCHelper.ComputeCrc16(dataBytes.ToArray(), dataBytes.Count) != (ushort)crc)
                {
                    return false;
                }
            }

            // MultiLanguages.Common.LogHelper.WriteLog("数据包：" + BytesToString(dataBytes));

            return true;
        }


        public static bool SendFrameCan1(EcanHelper ecanHelper, uint CanID, IList<byte> txData)
        {
            MultiLanguages.Common.LogHelper.WriteLog("发送："+ $"0x{CanID:X8}: " + BytesToString(txData));
            //LogHelper.AddLog("发送：" + requestCanID.ID.ToString("X8") + ": " + OscilloscopePageVM.BytesToString(txData));
            if (!ecanHelper.SendCan1(CanID, txData.ToArray()))
            {
                MultiLanguages.Common.LogHelper.WriteLog("发送失败：" + $"0x{CanID:X8}: " + BytesToString(txData));
                return false;
            }

            return true;
        }

        public static bool SendDataFramesCan1(EcanHelper ecanHelper, uint CANID, List<byte> dataBytes)
        {
            // 数据量
            if (dataBytes.Count % 2 == 1)
            {
                dataBytes.Add(0);  // 字长为16bit，不足时补一位
            }
            byte byteNum = (byte)(dataBytes.Count / 2);

            // CRC
            ushort crc = CRCHelper.ComputeCrc16(dataBytes.ToArray(), dataBytes.Count);
            dataBytes.Add((byte)(crc & 0xFF));
            dataBytes.Add((byte)(crc >> 8));

            var frameBytes = new List<byte>() { 0, 0, 0, 0, 0, 0, 0, 0 };
            int remains = dataBytes.Count;
            byte packSn = 0;

            // 帧头
            frameBytes[0] = packSn++;
            frameBytes[1] = 0;
            frameBytes[2] = 0;
            frameBytes[3] = byteNum;
            frameBytes[4] = dataBytes[0];
            frameBytes[5] = dataBytes[1];
            frameBytes[6] = dataBytes[2];
            frameBytes[7] = dataBytes[3];

            //MultiLanguages.Common.LogHelper.WriteLog("连续数据帧发送开始：");

            if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
            {
                return false;
            }

            remains -= 4;

            // 中间帧
            int p = 4;
            while (remains > 7)
            {
                frameBytes.Clear();
                frameBytes.Add(packSn++);
                frameBytes.AddRange(dataBytes.GetRange(p, 7));

                if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
                {
                    return false;
                }

                p += 7;
                remains -= 7;
            }

            // 帧尾
            frameBytes.Clear();
            frameBytes.Add(0XFF);
            frameBytes.AddRange(dataBytes.GetRange(p, remains));
            while (frameBytes.Count < 8)
            {
                frameBytes.Add(0);
            }
            
            if (!SendFrameCan1(ecanHelper, CANID, frameBytes))
            {
                return false;
            }

            //MultiLanguages.Common.LogHelper.WriteLog("连续数据帧发送结束\r\n");
            return true;
        }

        public static string BytesToString(IList<byte> byteData)
        {
            string dataStr = "";
            for (int i = 0; i < byteData.Count; i++)
            {
                dataStr += byteData[i].ToString("X2") + " ";
            }
            return dataStr;
        }


        #endregion

    }

    public partial class OscilloscopeVM : ObservableObject {

        private FileConfigModel? fileCfgModel = null;

        private const int MaxChannelNum = 6;
        private const int MaxPlotDataLen = 8192;

        private TIAddressChecker _addressChecker = new TIAddressChecker();

        private ScottPlot.WpfPlot _plotCtrl = new WpfPlot();
        public ScottPlot.WpfPlot PlotCtrl => _plotCtrl;

        public bool IsStopped { get; set; }


        private string _statusMsg = "";
        public string StatusMsg
        {
            get => _statusMsg;
            set { _statusMsg = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.SolidColorBrush _statusMsgColor =
            new SolidColorBrush(System.Windows.Media.Colors.Black);

        public System.Windows.Media.SolidColorBrush StatusMsgColor
        {
            get => _statusMsgColor;
            set { _statusMsgColor = value; OnPropertyChanged(); }
        }

        public EcanHelper? _ecanHelper = null;
        public ConcurrentQueue<CAN_OBJ> RxQueue = new();


        public void ShowStatusMsg(string msg, string type = "default", int timeMs = -1)
        {
            Application.Current.Dispatcher.BeginInvoke(async () =>
            {
                switch (type.ToUpper())
                {
                    case "ERROR":
                        StatusMsgColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                        break;
                    default:
                        StatusMsgColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                        break;

                }

                StatusMsg = msg;
                MultiLanguages.Common.LogHelper.WriteLog(msg);

                if (timeMs > 0)
                {
                    await Task.Delay(timeMs);
                    if (StatusMsg == msg)
                        StatusMsg = "";
                }
            });


        }


        public void ClosedJobs()
        {
            SaveSettings();
            // StopButton();
        }

        public void SaveSettings()
        {
            fileCfgModel.OscilloscopeModel.FilesPath.DwarfXmlPath = DwarfXmlPath;
            fileCfgModel.OscilloscopeModel.FilesPath.CoffPath = (CoffPath.Contains(".out")) ? CoffPath : "";
            fileCfgModel.OscilloscopeModel.UnderSampleScale = UnderSampleScale;
            fileCfgModel.OscilloscopeModel.TrigMode = TrigMode;
            fileCfgModel.OscilloscopeModel.TrigSource = TrigSource;
            fileCfgModel.OscilloscopeModel.TrigYLevel = TrigYLevel;

            fileCfgModel.OscilloscopeModel.ChannelInfoList = new();
            for (int i = 0; i < ChannelsSettings.Count; i++)
            {
                fileCfgModel.OscilloscopeModel.ChannelInfoList.Add(new()
                {
                    VariableName = ChannelsSettings[i].VariableName,
                    DataType = ChannelsSettings[i].DataType,
                    FloatDataScale = ChannelsSettings[i].FloatDataScale,
                    Comment = ChannelsSettings[i].Comment,
                });
            }

            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {

            }
        }

        public void UpdateModel()
        {
            fileCfgModel = JsonConfigHelper.ReadConfigFile();
            if (fileCfgModel != null)
            {
                DwarfXmlPath = fileCfgModel.OscilloscopeModel.FilesPath.DwarfXmlPath;
                CoffPath = fileCfgModel.OscilloscopeModel.FilesPath.CoffPath;
                UnderSampleScale = fileCfgModel.OscilloscopeModel.UnderSampleScale;
                TrigMode = fileCfgModel.OscilloscopeModel.TrigMode;
                TrigSource = fileCfgModel.OscilloscopeModel.TrigSource;
                TrigYLevel = fileCfgModel.OscilloscopeModel.TrigYLevel;
                for (int i = 0; i < fileCfgModel.OscilloscopeModel.ChannelInfoList.Count; ++i)
                {
                    ChannelsSettings[i].VariableName = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].VariableName;
                    ChannelsSettings[i].DataType = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].DataType;
                    ChannelsSettings[i].FloatDataScale = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].FloatDataScale;
                    ChannelsSettings[i].Comment = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].Comment;
                }
            }
        }

        public OscilloscopeVM(EcanHelper? ecanHelper)
        {
            this._ecanHelper = ecanHelper;
            InitializeChannelsSettings();
            InitializePlotCtrl();
        }
        
        #region 导入.out文件
        
        private string _coffPath = string.Empty;

        public string CoffPath
        {
            get => _coffPath;
            set { _coffPath = value; OnPropertyChanged(); }
        }

        private const int DwarfXmlValidHours = 12;

        public string DwarfXmlPath
        {
            get => _addressChecker.DwarfXmlPath;
            set { _addressChecker.DwarfXmlPath = value; }
        }

        [RelayCommand]
        private async void ImportCoff() 
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择COFF(.out)文件";
            dlg.Filter = "out(*.out)|*.out";
            dlg.Multiselect = false;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != true)
                return;

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string ofdExePath = System.IO.Path.Combine(currentDir, "Oscilloscope", "OFD", "ofd2000.exe");
            if (!System.IO.File.Exists(ofdExePath))
            {
                MessageBox.Show("找不到程序：\n" + ofdExePath, "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dwarfDirInfo = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(currentDir, "Oscilloscope", "DWARF"));
            string objFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            string dwarfXmlSavePath = System.IO.Path.Combine(dwarfDirInfo.FullName, objFileName + "_DWARF_" +
                DateTime.Now.ToString("yyyyMMddhhmmss") + AppDomain.CurrentDomain.Id + ".xml");

            try
            {
                foreach (var fileInfo in dwarfDirInfo.GetFiles())
                {
                    // 删除所有旧文件
                    fileInfo.Delete();
                }

                CoffPath = "加载中...";
                IsStartAllowed = false;  // 加载中禁止启动
                await TIAddressChecker.GetXmlByTiOfdTask(ofdExePath, dlg.FileName, dwarfXmlSavePath, 25);
                MessageBox.Show($"DWARF.xml保存文件路径(有效时间{DwarfXmlValidHours}小时):\n" + dwarfXmlSavePath, 
                      "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                CoffPath = dlg.FileName;
                DwarfXmlPath = dwarfXmlSavePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsStartAllowed = true;
            }
        }

        #endregion

        #region 通道变量参数
        // 下位机采样参数：
        public double SampleFreq { get; set; }
        public int SamplePointNum { get; set; }
        public int TotalStorageDepth { get; set; }

        
        // 通道变量参数
        public ObservableCollection<ChannelModel> ChannelsSettings { get; set; } = new ();

        private void InitializeChannelsSettings()
        {
            List<System.Drawing.Color> channelColors = new List<System.Drawing.Color>()
            {
                System.Drawing.Color.DodgerBlue,
                System.Drawing.Color.Red,
                System.Drawing.Color.Green,
                System.Drawing.Color.BlueViolet,
                System.Drawing.Color.Fuchsia,
                System.Drawing.Color.Goldenrod,
            };
            

            for (int i = 0; i < MaxChannelNum; i++)
            {
                ChannelsSettings.Add(new ChannelModel
                {
                    TagName = $"CH{i+1}",
                    LineColor = channelColors[i],
                    ID = i + 1,
                });
                TrigSourceComboItems.Add($"CH{i + 1}");
            }
        }

        public ObservableCollection<string> DataTypeComboItems { get; } = new ()
        {
            "Bool", "U8", "I8", "U16", "I16", "U32", "I32", "Float" 
        };

        public ObservableCollection<string> FloatDataScaleComboItems { get; } = new()
        {
            "1", "0.1", "0.01"
        };


        // 用户采样设置
        private int _underSampleScale = 1;
        public int UnderSampleScale 
        { 
            get => _underSampleScale;
            set
            {
                if (value < 0 || value > 20)
                {
                    throw new InvalidDataException();
                }
                _underSampleScale= value; 
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> TrigModeComboItems { get; } = new()
        {
            "FreeTrig", "Rising", "Falling", "Rising/Falling"
        };

        private int _trigMode = 0;
        public int TrigMode
        {
            get => _trigMode;
            set
            {
                _trigMode = value;
                TrigXPercent = 0;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> TrigSourceComboItems { get; set; } = new();

        private int _trigSource = 0;
        public int TrigSource
        {
            get=> _trigSource;
            set { _trigSource = value; OnPropertyChanged(); }
        }
        
        private int _trigYLevel = 1;
        public int TrigYLevel 
        { 
            get => _trigYLevel;
            set { _trigYLevel = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> TrigXPercentComboItems { get; set; } = new()
        {
            "Free", "0% TimeAxis", 
            "10% TimeAxis", "20% TimeAxis", "30% TimeAxis", "40% TimeAxis", "50% TimeAxis",
            "60% TimeAxis", "70% TimeAxis", "80% TimeAxis", "90% TimeAxis", "100% TimeAxis"
        };

        private int _trigXPercent = 0;
        public int TrigXPercent 
        { 
            get => _trigXPercent;
            set
            {
                _trigXPercent = TrigMode == 0 ? 0 : value;
                OnPropertyChanged();
            } 
        }

        public string StartMode { get; set; } = "SingleTrig";

        public ObservableCollection<string> StartModeComboItems { get; } = new()
        {
            "SingleTrig", "ContinuousTrig", "Rolling", "StartOnly",
        };
        
        #endregion

        #region 运行命令

        private bool _isStartAllowed = true;
        public bool IsStartAllowed
        {
            get => _isStartAllowed;
            set => SetProperty(ref _isStartAllowed, value);
        }

        [RelayCommand]
        private void StartButton()
        {
            // 禁止重复按下
            IsStartAllowed = false;

            // 保存配置
            SaveSettings();

            // 清空波形图
            ClearSignalText();
            for (int i = 0; i < _signalXYPlots.Count; i++)
            {
                _signalXYPlots[i].Ys[0] = 0;
                _signalXYPlots[i].MaxRenderIndex = 0;
            }
            PlotCtrl.Plot.BottomAxis.Dims.SetAxis(0, 1);
            PlotCtrl.Refresh();
            _fillBeginIndex = 0;
            _timeX = 0;
            

            // 加载dwarf.xml文件，查找变量地址
            if (string.IsNullOrEmpty(DwarfXmlPath) || !CheckDwarfXmlDate())
            {
                MessageBox.Show("DWARF.xml文件不存在或已过期，请重新加载.out文件", "文件过期",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                IsStartAllowed = true;
                return;
            }

            if (!CheckAddresses(ChannelsSettings))
            {
                IsStartAllowed = true;
                return;
            }

            // 
            for (int i = 0; i < MaxChannelNum; i++)
            { 
                _signalAxis[i].Hide((ChannelsSettings[i].ID < 0));
            }
            PlotCtrl.Refresh();

            // 保存配置
            SaveSettings();

            MultiLanguages.Common.LogHelper.WriteLog($"**************** 示波器CAN帧日志({DateTime.Now.ToString()}) ****************");

            byte dstAddr = DeviceManager.Instance().GetSelectDev();
            if (dstAddr == 0)
            {
                ShowStatusMsg("未选择设备，请在界面下方中选择一台设备", "ERROR");
                IsStartAllowed = true;
                return;
            }

            switch (StartMode)
            {
                case "StartOnly":
                    Task.Run(() => StartWithoutReadData(dstAddr));
                    break;

                case "SingleTrig":
                    Task.Run(() => SingleTrigStart(dstAddr));
                    break;

                case "ContinuousTrig":
                    Task.Run(() => ContinuousTrigStart(dstAddr));
                    break;

                case "Rolling":
                    break;
                default:
                    break;
            }
        }

        private bool CheckDwarfXmlDate()
        {
            // 删除过期文件
            var dwarfDirInfo = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Oscilloscope", "DWARF"));

            foreach (var fileInfo in dwarfDirInfo.GetFiles())
            {
                var lastWriteTime = fileInfo.LastWriteTime;
                var currentTime = DateTime.Now;
                if (currentTime - lastWriteTime > TimeSpan.FromHours(DwarfXmlValidHours))
                {
                    fileInfo.Delete();
                }
            }
            
            var dwarfFileInfo = new System.IO.FileInfo(DwarfXmlPath);
            return dwarfFileInfo.Exists;
        }

        private bool CheckAddresses<T>(ObservableCollection<T> varCollection) where T : VariableModelBase, new()
        {
            if (!_addressChecker.IsDwarfXmlLoaded())
            {
                if (!_addressChecker.LoadDwarfXml(DwarfXmlPath))
                {
                    MessageBox.Show("加载COFF(.out)或DWARF(.xml)文件出错，请尝试重新导入COFF(.out)文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
               
            var addrCheckTasks = new List<Task>();
            

            string errInfo = "";

            for (int i = 0; i < varCollection.Count; i++)
            {
                varCollection[i].VariableAddress = 0;

                if (string.IsNullOrWhiteSpace(varCollection[i].VariableName))
                {
                    varCollection[i].ID = -1;
                    varCollection[i].IsInvalid = false;
                }
                else
                {
                    int sn = i;
                    addrCheckTasks.Add(Task.Run(() => {
                        UInt32 addrRes = _addressChecker.SearchAddressByName(varCollection[sn].VariableName, sn + 1);
                        varCollection[sn].VariableAddress = addrRes;
                        varCollection[sn].ID = sn + 1;
                        varCollection[sn].IsInvalid = false;
                    }));
                }
            }


            try
            {
                Task.WhenAll(addrCheckTasks).Wait();
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.Flatten().InnerExceptions)
                {
                    string exMsg = ex.Message;
                    int idStartIdx = exMsg.IndexOf("变量");
                    int idEndIdx = exMsg.IndexOf(": ");
                    if (idStartIdx >= 0 && idEndIdx > 0)
                    {
                        idStartIdx += "变量".Length;
                        int varId = int.Parse(exMsg.Substring(idStartIdx, idEndIdx - idStartIdx));
                        varCollection[varId - 1].IsInvalid = true;
                        varCollection[varId - 1].ID = -1;
                        errInfo += exMsg;
                    }
                    else
                    {
                        throw;
                    }
                    
                }

            }
            finally
            {
                _addressChecker.UnloadDwarfXml();
            }

            if (!string.IsNullOrEmpty(errInfo))
            {
                MessageBox.Show(errInfo, "寻址错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                return true;
            }

        }


        private void StartWithoutReadData(byte dstAddr)
        {
            //ShowStatusMsg("确认关闭示波器");
            //if (!WriteStopRequest(dstAddr))
            //{
            //    ShowStatusMsg("确认关闭示波器失败");
            //    IsStartAllowed = true;
            //    return;
            //}

            ShowStatusMsg("读取性能参数...");
            if (!ReadOscilloscopeProperty(dstAddr))
            {
                ShowStatusMsg("读取性能参数失败");
                IsStartAllowed = true;
                return;
            }
            ShowStatusMsg("读取性能参数成功");

            Thread.Sleep(800);

            ShowStatusMsg("配置并启动示波器...");
            if (!WriteOscilloscopeSettings(dstAddr))
            {
                ShowStatusMsg("配置并启动示波器失败");
                IsStartAllowed = true;
                return;
            }
            ShowStatusMsg("配置并启动示波器成功");
        }

        private void SingleTrigStart(byte dstAddr)
        {
            ShowStatusMsg("读取性能参数...");
            if (!ReadOscilloscopeProperty(dstAddr))
            {
                ShowStatusMsg("读取性能参数失败");
                Stop();
                return;
            }
            ShowStatusMsg("读取性能参数成功");

            Thread.Sleep(800);

            ShowStatusMsg("配置并启动示波器...");
            if (!WriteOscilloscopeSettings(dstAddr))
            {
                ShowStatusMsg("配置并启动示波器失败");
                Stop();
                return;
            }
            ShowStatusMsg("配置并启动示波器成功");

            Thread.Sleep(800);

            ShowStatusMsg("单次触发开始");

            Thread.Sleep(500);

            
            IsStopped = false;

            if (!WaitForTrig(dstAddr))
            {
                Stop();
                ShowStatusMsg("触发等待超时，示波器已关闭");
                return;
            }

            ShowStatusMsg("触发成功，正在读取波形数据...");

            List<List<short>> chDataCollection = new();
            int storageDepth = TotalStorageDepth / MaxChannelNum;

            double timeX = 0; 
            var newTimeXs = new List<double>();

            PlotCtrl.Plot.BottomAxis.Dims.SetAxis(-5 * UnderSampleScale / SampleFreq, SamplePointNum * UnderSampleScale / SampleFreq);
            

            //var swTotal = Stopwatch.StartNew();
            //var sw = Stopwatch.StartNew();
            int waveIndex = 0;
            // int failureCnt = 0;
            while (!IsStopped && waveIndex < SamplePointNum)
            {
                if (ReadChannelData(dstAddr, waveIndex, out List<short> chData))
                {
                    newTimeXs.Add(timeX);
                    chDataCollection.Add(chData);
                    //if ((waveIndex + 1) % 200 == 0)
                    //{
                    //    //sw.Restart();

                    //    UpdatePlot(chDataCollection, newTimeXs);
                    //    chDataCollection.Clear();
                    //    newTimeXs.Clear();
                    //}
                    ShowStatusMsg($"正在读取波形数据({waveIndex}/{SamplePointNum})");

                }
                else
                {
                    // failureCnt++;
                    ShowStatusMsg($"读取波形数据失败：WaveIndex={waveIndex}");
                }

                timeX += UnderSampleScale / SampleFreq;
                waveIndex++;
            }
            UpdatePlot(chDataCollection, newTimeXs);
            chDataCollection.Clear();
            newTimeXs.Clear();

            // 正常流程结束
            if (!IsStopped)
            {
                //MultiLanguages.Common.LogHelper.WriteLog($"单次触发耗时（ms）：{swTotal.ElapsedMilliseconds}");
                ShowStatusMsg(Stop() ? "单次触发结束, 示波器已关闭" : "单次触发结束, 示波器未成功关闭");
            }
            IsStopped = true;
            IsStartAllowed = true;
        }

        private void ContinuousTrigStart(byte dstAddr)  
        {
            
        }

        private void RollingStart()
        {

        }

        private void UpdatePlot(List<List<short>> channelDataCollection, List<double> newTimeXs)
        {
            for (int i = 0; i < channelDataCollection.Count; i++)
            {
                for (int j = 0; j < MaxChannelNum; j++)
                {
                    
                    if (ChannelsSettings[j].ID < 0)
                    {
                        continue;
                    }

                    _signalXYPlots[j].Ys[_fillBeginIndex] = ChannelsSettings[j].DataType switch
                    {
                        0 => (byte)channelDataCollection[i][j],  // bool
                        1 => (byte)channelDataCollection[i][j],  // uint8
                        2 => (sbyte)channelDataCollection[i][j],  // int8
                        3 => (ushort)channelDataCollection[i][j],  // uint16
                        4 => (short)channelDataCollection[i][j],  // int16
                        5 => (uint)channelDataCollection[i][j],  // uint32
                        6 => (int)channelDataCollection[i][j],  // int32
                        7 => (float)channelDataCollection[i][j] / Math.Pow(10, ChannelsSettings[j].FloatDataScale),  // float
                    };

                    if (_signalXYPlots[j].MaxRenderIndex < MaxPlotDataLen - 1 && _fillBeginIndex > 0)
                    {
                        _signalXYPlots[j].MaxRenderIndex += 1;
                    }
                }
                
                _fillBeginIndex++;
            }

            // Update TimeXs
            Array.Copy(newTimeXs.ToArray(), 0,_timeXs, _fillBeginIndex - newTimeXs.Count,newTimeXs.Count);

            // Refresh
            Application.Current.Dispatcher.Invoke(() => {
                for (int i = 0; i < MaxChannelNum; i++)
                {
                    if (ChannelsSettings[i].ID < 0)
                        continue;
                    _signalAxis[i].LockLimits(false);
                    //
                    PlotCtrl.Plot.AxisAutoY(yAxisIndex: i + 2);
                    _signalAxis[i].LockLimits(true);
                }
                
                PlotCtrl.Refresh();
            });
        }

        private void UpdateRollingPlotXY(List<List<short>> channelDataCollection, List<double> newTimeXs)
        {
            // 超过最大容纳量时舍弃较早的点
            if (_fillBeginIndex + newTimeXs.Count > MaxPlotDataLen)
            {
                int moveLen = _fillBeginIndex + newTimeXs.Count - MaxPlotDataLen;
                for (int i = 0; i < MaxChannelNum; i++)
                {
                    if (ChannelsSettings[i].ID > 0)
                    {
                        Array.Copy(_signalXYPlots[i].Ys, moveLen, _signalXYPlots[i].Ys, 0, _fillBeginIndex - moveLen);
                        Array.Copy(_timeXs, moveLen, _timeXs, 0, _fillBeginIndex - moveLen);
                    }
                }

                _fillBeginIndex -= moveLen;
            }

            // 数据分发
            for (int i = 0; i < channelDataCollection.Count; i++)
            {
                for (int j = 0; j < MaxChannelNum; j++)
                {
                    if (ChannelsSettings[j].ID < 0)
                        continue;

                    _signalXYPlots[j].Ys[_fillBeginIndex] = ChannelsSettings[j].DataType switch
                    {
                        0 => (byte)channelDataCollection[i][j],  // bool
                        1 => (byte)channelDataCollection[i][j],  // uint8
                        2 => (sbyte)channelDataCollection[i][j],  // int8
                        3 => (ushort)channelDataCollection[i][j],  // uint16
                        4 => (short)channelDataCollection[i][j],  // int16
                        5 => (uint)channelDataCollection[i][j],  // uint32
                        6 => (int)channelDataCollection[i][j],  // int32
                        7 => (float)channelDataCollection[i][j] / Math.Pow(10, ChannelsSettings[j].FloatDataScale),  // float
                    };

                    
                    if (_signalXYPlots[j].MaxRenderIndex < MaxPlotDataLen - 1 && _fillBeginIndex > 0)
                    {
                        _signalXYPlots[j].MaxRenderIndex += 1;
                    }
                    
                }

                _fillBeginIndex++;
            }

            // Update TimeXs
            Array.Copy(newTimeXs.ToArray(), 0, _timeXs, _fillBeginIndex - newTimeXs.Count, newTimeXs.Count);


        }

        private bool ReadOscilloscopeProperty(byte dstAddr, int timeoutMs=5000)
        {
            var txCanID = new CanFrameID()
            {
                // 请求帧，功能码30
                Priority = 3,
                FrameType = 2,
                ContinuousFlag = 0,
                PF = 30,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            var rxCanID = new CanFrameID()
            {
                // 数据帧，功能码30
                Priority = 6,
                FrameType = 1,
                ContinuousFlag = 1,
                PF = 30,
                DstType = 2,
                DstAddr = 1,
                SrcType = 1,
                SrcAddr = dstAddr,
            };

            var txData = new List<byte>()
            {
                0, 0, 4, 0,
                0, 0, 0, 0
            };

            // 清空接收队列
            RxQueue.Clear();

            // 发送请求帧
            if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txCanID.ID, txData))
            {
                return false;
            }

            // 接收数据
            if (!OscilloscopePageVM.ReadAddressDataFrames(RxQueue, rxCanID, 0, 4, out var rxData, timeoutMs) || rxData.Count != 8)
            {
                return false;
            }

            // 解析数据
            SampleFreq = (rxData[1] << 8) | rxData[0];
            SamplePointNum = (rxData[3] << 8) | rxData[2];
            TotalStorageDepth = (rxData[7] << 24) | (rxData[6] << 16) |
                                (rxData[5] << 8) | rxData[4];
            
            return true;
        }
        
        private bool WriteOscilloscopeSettings(byte dstAddr, int timeoutMs=3000)
        {
            var txCanID = new CanFrameID()
            {
                // 数据帧，功能码31
                Priority = 6,
                FrameType = 1,
                ContinuousFlag = 1,
                PF = 31,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            var rxCanID = new CanFrameID()
            {
                // 应答帧，功能码31
                Priority = 3,
                FrameType = 3,
                ContinuousFlag = 0,
                PF = 31,
                DstType = 2,
                DstAddr = 1,
                SrcType = 1,
                SrcAddr = dstAddr,
            };

            var txData = new List<byte>();

            var chAddr = new UInt32[MaxChannelNum];
            var chVarType = new byte[MaxChannelNum];
            var chFloatScale = new byte[MaxChannelNum];
            for (int i = 0; i < ChannelsSettings.Count; i++)
            {
                if (ChannelsSettings[i].ID > 0)
                {
                    chAddr[i] = ChannelsSettings[i].VariableAddress;
                    chVarType[i] = (byte)ChannelsSettings[i].DataType;
                    chFloatScale[i] = (byte)ChannelsSettings[i].FloatDataScale;
                }
                else
                {
                    chAddr[i] = 0;
                    chVarType[i] = 0;
                    chFloatScale[i] = 0;
                }
            }


            // 变量地址
            for (int i = 0; i < chAddr.Length; i++)
            {
                txData.AddRange(HexDataHelper.UIntToByte(chAddr[i], true));
            }
            // 变量类型
            for (int i = 0; i < chVarType.Length; i += 2)
            {
                var data = chVarType[i + 1] << 4 | chVarType[i];
                txData.Add((byte)data);
            }
            // 触发参数、浮点倍数
            if (StartMode == "Rolling")
            {
                txData.Add((byte)((0 << 4) | (MaxChannelNum - 1)));
            }
            else
            {
                txData.Add((byte)(((TrigMode+1) << 4) | (MaxChannelNum - 1)));
            }

            var chFloatScaleCombined = new byte[MaxChannelNum / 2];  // 先合并成4bit一对
            for (int i = 0; i < chFloatScaleCombined.Length; i++)
            {
                chFloatScaleCombined[i] = (byte)(chFloatScale[2 * i + 1] << 2 | chFloatScale[2 * i]);
            }
            txData.Add((byte)(chFloatScaleCombined[0] << 4 | TrigSource));
            txData.Add((byte)(chFloatScaleCombined[2] << 4 | chFloatScaleCombined[1]));

            txData.AddRange(HexDataHelper.UShortToByte((ushort)UnderSampleScale));

            txData.AddRange(HexDataHelper.UShortToByte((ushort)(TrigXPercent * 10)));

            txData.AddRange(HexDataHelper.IntToByte(TrigYLevel, true));

            // Send request and get response
            RxQueue.Clear();
            if (!OscilloscopePageVM.SendDataFramesCan1(_ecanHelper, txCanID.ID, txData))
                return false;

            var sw = Stopwatch.StartNew();
            do
            {
                if (RxQueue.TryDequeue(out var rawFrame) && new CanFrameID(rawFrame.ID).FC == rxCanID.FC)
                {
                    var result = rawFrame.Data[3];
                    if (result == 0)
                    {                        
                        return true;
                    }
                }
            } while (sw.ElapsedMilliseconds < timeoutMs);

            return false;
        }

        private bool WaitForTrig(byte dstAddr, int timeoutMs=60_000*2)
        {
            bool isReady = false;
            var sw = Stopwatch.StartNew();
            var countdownSpan = TimeSpan.FromMilliseconds(timeoutMs);
            while (!IsStopped && countdownSpan.TotalSeconds > 0 && !isReady)
            {
                isReady = ReadChannelData(dstAddr, 0, out var channelData);
                ShowStatusMsg("等待触发: " + countdownSpan.Minutes.ToString("D2") + ":" + countdownSpan.Seconds.ToString("D2"));
                Thread.Sleep(1000);
                countdownSpan -= TimeSpan.FromSeconds(1);
            }
            return isReady;
            
            
            //var countdownTimer = new System.Timers.Timer(1000);
            //countdownTimer.AutoReset = true;
            //countdownTimer.Elapsed += (s, e) =>
            //{
            //    if (IsStopped) return;


                
                
            //};
            //countdownTimer.Start();




            //while (countdownSpan.Seconds > 0)
            //{
            //    if (isReady) return true;
            //}

            //countdownTimer.Stop();
            return false;
        }

        private bool ReadChannelData(byte dstAddr, int waveIdx, out List<short> channelData)
        {
            var txCanID = new CanFrameID()
            {
                // 数据短帧，功能码32
                Priority = 3,
                FrameType = 1,
                ContinuousFlag = 0,
                PF = 32,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            var rxCanID = new CanFrameID()
            {
                // 数据帧，功能码32
                Priority = 3,
                FrameType = 1,
                ContinuousFlag = 1,
                PF = 32,
                DstType = 2,
                DstAddr = 1,
                SrcType = 1,
                SrcAddr = dstAddr,
            };

            var txData = new List<byte>()
            {
                0, 0, 
                1,
                (byte)(waveIdx & 0xFF),
                (byte)(waveIdx >> 8),
                0, 0, 0
            };


            // Send request and get response
            channelData = new List<short>();
            RxQueue.Clear();
            if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txCanID.ID, txData))
            {
                return false;
            }

            //MultiLanguages.Common.LogHelper.WriteLog($"读取WaveIndex：{waveIdx}");

            var sw = Stopwatch.StartNew();
            if (!OscilloscopePageVM.ReadAddressDataFrames(RxQueue, rxCanID, 0, 7,out var rxData, 250)
                || rxData.Count != 2 + 2 * MaxChannelNum)
            {
                return false;
            }

            int waveIdxResponse = rxData[1] << 8 | rxData[0];
            if (waveIdxResponse != waveIdx)
            {
                // MultiLanguages.Common.LogHelper.WriteLog("");
                return false;
            }
            

            for (int i = 2; i < rxData.Count; i += 2)
            {
                var chData = (short)(rxData[i + 1] << 8 | rxData[i]);
                channelData.Add(chData);
            }

            string channelDataStr = "";
            for (int i = 0;  i < channelData.Count; ++i)
            {
                channelDataStr += channelData[i].ToString() + " ";
            }
            MultiLanguages.Common.LogHelper.WriteLog($"WaveData({waveIdx}) {channelDataStr}");

            //var rxDelay = sw.ElapsedMilliseconds;
            //MultiLanguages.Common.LogHelper.WriteLog($"6点数据接收时长(ms)：{sw.ElapsedMilliseconds}");
            return true;

        }

        
        [RelayCommand]
        private void StopButton()
        {
            // ShowStatusMsg("示波器请求关闭");

            ShowStatusMsg(Stop() ? "示波器关闭成功" : "示波器关闭失败");
        }

        private bool Stop()
        {
            IsStopped = true;
            IsStartAllowed = true;
            byte dstAddr = DeviceManager.Instance().GetSelectDev();
            return WriteStopRequest(dstAddr);
        }

        private bool WriteStopRequest(byte dstAddr, int timeoutMs=500)
        {
            var txCanID = new CanFrameID()
            {
                // 请求帧，功能码33
                Priority = 3,
                FrameType = 2,
                ContinuousFlag = 0,
                PF = 33,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            var rxCanID = new CanFrameID()
            {
                // 应答帧，功能码33
                Priority = 3,
                FrameType = 3,
                ContinuousFlag = 0,
                PF = 33,
                DstType = 2,
                DstAddr = 1,
                SrcType = 1,
                SrcAddr = dstAddr,
            };

            var txData = new List<byte>()
            {
                0, 0, 0, 0,
                0, 0, 0, 0
            };

            // Send request and get response
            RxQueue.Clear();
            if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txCanID.ID, txData))
            {
                return false;
            }

            var sw = Stopwatch.StartNew();
            do
            {
                if (RxQueue.TryDequeue(out var rawFrame) && new CanFrameID(rawFrame.ID).FC == rxCanID.FC)
                {
                    var result = rawFrame.Data[3];
                    if (result == 0)
                    {                        
                        return true;
                    }
                }
            } while (sw.ElapsedMilliseconds < timeoutMs);

            return false;

        }

        private void ClearSignalText()
        {
            for (int i = 0; i < SignalTextList.Count; i++)
            {
                SignalTextList[i].SignalText = $"CH{i + 1}: ";
            }
        }

        #endregion


        #region Plot

        private double[] _timeXs = new double[MaxPlotDataLen];
        private List<ScottPlot.Plottable.SignalPlotXY> _signalXYPlots = new();

        //private List<ScottPlot.Plottable.SignalPlot> _signalPlots = new ();
        private List<ScottPlot.Renderable.Axis> _signalAxis = new();
        
        private List<double[]> _signalDataList = new();
        public ObservableCollection<SignalTextModel> SignalTextList { get; set; }= new ();

        private double _timeX = 0;
        private double _offsetX = 0;
        private int _fillBeginIndex = 0;

        private void InitializePlotCtrl()
        {
            // Style and Axis
            _plotCtrl.Configuration.DoubleClickBenchmark = false;
            _plotCtrl.Plot.Style(ScottPlot.Style.Black);

            _plotCtrl.Plot.BottomAxis.Dims.SetAxis(0, 1);
            //_plotCtrl.Plot.BottomAxis.Dims.SpanMaximum = 1;
            //_plotCtrl.Plot.BottomAxis.Dims.SpanMinimum = 1;
            _plotCtrl.Plot.BottomAxis.TickMarkDirection(false);
            _plotCtrl.Plot.BottomAxis.Layout(maximumSize: 15);
            
            _plotCtrl.Plot.LeftAxis.Dims.SetAxis(-5, 5);
            _plotCtrl.Plot.LeftAxis.MinimumTickSpacing(1);
            _plotCtrl.Plot.LeftAxis.LockLimits(true);
            _plotCtrl.Plot.LeftAxis.Hide();

            _plotCtrl.Plot.RightAxis.Hide();

            // 
            for (int i = 0; i < ChannelsSettings.Count; i++) 
            {
                _signalAxis.Add(_plotCtrl.Plot.AddAxis(edge: (i % 2 == 0) ? Edge.Left : Edge.Right,
                                                            axisIndex: i + 2));
                _signalAxis[i].Color(ChannelsSettings[i].LineColor);
                _signalAxis[i].LockLimits(true);

                _signalDataList.Add(new double[MaxPlotDataLen]);
                
                _signalXYPlots.Add(_plotCtrl.Plot.AddSignalXY(xs: _timeXs, ys: _signalDataList[i]));
                _signalXYPlots[i].YAxisIndex = i + 2;
                _signalXYPlots[i].LineColor = ChannelsSettings[i].LineColor;
                _signalXYPlots[i].LineWidth = 3.2;
                _signalXYPlots[i].MaxRenderIndex = 0;

                SignalTextList.Add(new SignalTextModel()
                {
                    SignalText = $"CH{ChannelsSettings[i].ID}: ",
                    TextColor = ChannelsSettings[i].LineColor,
                });
            }
            
            _plotCtrl.Refresh();
        }

        #endregion


        #region Plot Display
        public ObservableCollection<string> DataCursorComboItems { get; } = new()
        {
            "None", "One", "Multiple"
        };

        private string _dataCursorMode = "None";
        public string DataCursorMode
        {
            get => _dataCursorMode;
            set
            {
                _dataCursorMode = value;
                OnPropertyChanged();
                ChangeDataCursorMode(value);
            }
        }

        private List<ScottPlot.Plottable.VLine> _dataCursors = new ();

        private void ChangeDataCursorMode(string mode)
        {
            switch (mode)
            {
                case "None":
                {
                    for (int i = 0; i < _dataCursors.Count; i++)
                    {
                        PlotCtrl.Plot.Remove(_dataCursors[i]);
                    }
                    _dataCursors.Clear();
                    break;
                }

                case "One":
                {
                    for(int i = 0; i < _dataCursors.Count; i++)
                    {
                        PlotCtrl.Plot.Remove(_dataCursors[i]);
                    }
                    _dataCursors.Clear();

                    double xVLine = PlotCtrl.Plot.BottomAxis.Dims.Center;
                    var dataCursor = PlotCtrl.Plot.AddVerticalLine(x: xVLine, color: System.Drawing.Color.White, width: 2.0f);
                    dataCursor.DragEnabled = true;
                    dataCursor.PositionLabel = true;
                    dataCursor.PositionLabelOppositeAxis = true;
                    dataCursor.Dragged += UpdateSignalText;
                    dataCursor.DragLimitMin = -0.01;
                    dataCursor.DragLimitMax = 1.01;
                    dataCursor.PositionFormatter = d => d.ToString("0.0000E0");
                    _dataCursors.Add(dataCursor);

                    break;
                }

                case "Multiple":
                    break;

            }

            PlotCtrl.Refresh();
        }

       
        [RelayCommand]
        private void HideYAxis(bool isHiden)
        {
            for (int i = 0; i < MaxChannelNum; i++)
            {
                if (ChannelsSettings[i].ID > 0)
                {
                    _signalAxis[i].Hide(isHiden);
                }
            }
            PlotCtrl.Refresh();
        }

        [RelayCommand]
        private void ZoomXAxis(bool isEnabled)
        {
            if (isEnabled)
            {
                PlotCtrl.Plot.XAxis.Dims.SpanMaximum = null;
                PlotCtrl.Plot.XAxis.Dims.SpanMinimum = null;
            }
            else
            {
                double xSpan = PlotCtrl.Plot.XAxis.Dims.Span;
                PlotCtrl.Plot.XAxis.SetZoomInLimit(xSpan);
                PlotCtrl.Plot.XAxis.SetZoomOutLimit(xSpan);
            }
        }

        [RelayCommand]
        private void ChannelVisible()
        {
            for (int i = 0; i < MaxChannelNum; i++)
            {
                if (ChannelsSettings[i].ID > 0)
                {
                    _signalXYPlots[i].IsVisible = ChannelsSettings[i].IsVisible;
                }                
            }
            PlotCtrl.Refresh();
        }

        [RelayCommand]
        private void ChannelSelected()
        {
            for (int i = 0; i < MaxChannelNum; i++)
            {
                if (ChannelsSettings[i].ID > 0)
                {
                    _signalAxis[i].LockLimits(!ChannelsSettings[i].IsSelected);
                }
            }
            PlotCtrl.Refresh();
        }
        
        private void UpdateSignalText(object sender, EventArgs args)
        {
            switch (DataCursorMode)
            {
                case "None":
                    break;
                case "One":
                    {
                        double curX = _dataCursors[0].X;

                        for (int i = 0; i < MaxChannelNum; i++)
                        {
                            string dataText = ChannelsSettings[i].TagName + ": "; // "CHx: "
                            if (ChannelsSettings[i].IsVisible 
                                && curX >= _timeXs[0] - 0.001
                                && curX  <= _timeXs[_signalXYPlots[i].MaxRenderIndex] + 0.001)
                            {
                                double signalY;
                                (_, signalY, _) = _signalXYPlots[i].GetPointNearestX(curX);

                                switch (ChannelsSettings[i].DataType)
                                {
                                    case 0:
                                        {
                                            // bool
                                            dataText += (byte)signalY;
                                            break;
                                        }

                                    case 1:
                                    case 2:
                                        {
                                            // (u)int8
                                            dataText += $"{(int)signalY} " +
                                                        $"(0x{Convert.ToString((int)signalY).PadLeft(2, '0')})";
                                            string binStr = $"b{Convert.ToString((int)signalY, 2).PadLeft(8, '0')}";
                                            for (int p = 5; p < binStr.Length; p += 5)
                                            {
                                                binStr = binStr.Insert(p, "_");
                                            }
                                            dataText += "\n   " + binStr;
                                            break;
                                        }

                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                        {
                                            // (u)int16, (u)int32
                                            dataText += $"{(int)signalY}" +
                                                        $"(0x{Convert.ToString((int)signalY).PadLeft(4, '0')})";
                                            string binStr = $"b{Convert.ToString((int)signalY, 2).PadLeft(16, '0')}";
                                            for (int p = 5; p < binStr.Length; p += 5)
                                            {
                                                binStr = binStr.Insert(p, "_");
                                            }
                                            dataText += binStr;
                                            break;
                                        }

                                    case 7:
                                        {
                                            // float
                                            dataText += signalY;
                                            break;
                                        }

                                }

                            }

                            SignalTextList[i].SignalText = dataText;
                        }

                        break;
                    }

                case "Multiple":
                    break;
            }
        }
        #endregion

    }

    public class VariableModelBase : ObservableObject
    {

        private string _variableName = "";
        public string VariableName
        {
            get => _variableName;
            set { _variableName = value; OnPropertyChanged(); }
        }

        private uint _variableAddress = 0;
        public uint VariableAddress
        {
            get => _variableAddress;
            set { _variableAddress = value; OnPropertyChanged(); }
        }

        private int _dataType = 7;
        public int DataType
        {
            get => _dataType;
            set { _dataType = value; OnPropertyChanged(); }
        }

        private int _floatDataScale = 0;
        public int FloatDataScale
        {
            get => _floatDataScale;
            set { _floatDataScale = value; OnPropertyChanged(); }
        }

        private bool _isInvalid = false;
        public bool IsInvalid
        {
            get => _isInvalid;
            set {  _isInvalid = value;  OnPropertyChanged(); }
        }
        
        private int _id = -1;
        public int ID
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private string _comment = "";
        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(); }
        }

    }

    public class ChannelModel : VariableModelBase
    {
        private string _tagName = "";
        public string TagName 
        { 
            get => _tagName;
            set { _tagName = value; OnPropertyChanged();} 
        }

        private System.Drawing.Color _lineColor = System.Drawing.Color.White;
        public System.Drawing.Color LineColor { 
            get => _lineColor;
            set { _lineColor = value; OnPropertyChanged();}
        }

        private bool _isVisible = true;
        public bool IsVisible 
        { 
            get => _isVisible; 
            set { _isVisible = value; OnPropertyChanged();}
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged();}
        }
        

    }

    public class VariableToWriteModel : VariableModelBase
    {
        public string DataValueString { get; set; } = ""; 
    }

    public class SignalTextModel : ObservableObject
    {

        private string _signalText = "";
        public string SignalText
        {
            get { return _signalText; }
            set { _signalText = value; OnPropertyChanged(); }
        }

        private System.Drawing.Color _textColor = System.Drawing.Color.White;
        public System.Drawing.Color TextColor
        {
            get => _textColor;
            set { _textColor = value; OnPropertyChanged(); }
        }
        
    }


    public partial class FaultWaveRecordVM : ObservableObject
    {
        private const ushort FaultWaveInfoSize = 8;  // 字长16bits
        private const ushort FaultWavePackSize = 8 * 12; // 字长16bits，一个数据块接收12列数据
        private const int FaultWaveNum = 8;   
        private const double SamplingRate = 1;     // 4050;
        private const int FaultWaveFileBegin = 200;

        private string _statusMsg = "";
        public string StatusMsg
        {
            get => _statusMsg;
            set { _statusMsg = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.SolidColorBrush _statusMsgColor =
            new SolidColorBrush(System.Windows.Media.Colors.Black);

        public System.Windows.Media.SolidColorBrush StatusMsgColor
        {
            get => _statusMsgColor;
            set { _statusMsgColor = value; OnPropertyChanged(); }
        }

        public EcanHelper? _ecanHelper = null;
        public ConcurrentQueue<CAN_OBJ> RxQueue = new();


        public void ShowStatusMsg(string msg, string type = "default", int timeMs = -1)
        {
            Application.Current.Dispatcher.BeginInvoke(async () =>
            {
                switch (type.ToUpper())
                {
                    case "ERROR":
                        StatusMsgColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                        break;
                    default:
                        StatusMsgColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                        break;

                }

                StatusMsg = msg;

                if (timeMs > 0)
                {
                    await Task.Delay(timeMs);
                    if (StatusMsg == msg)
                        StatusMsg = "";
                }
            });
            

        }

        public FaultWaveRecordVM(EcanHelper ecanHelper)
        {
            InitializePlot();
            InitializeSelectedWavesItems();
            this._ecanHelper = ecanHelper;
            InitializeDemoWaves();
        }

        private void InitializeSelectedWavesItems()
        {
            List<System.Drawing.Color> waveColors = new List<System.Drawing.Color>()
            {
                System.Drawing.Color.Red,
                System.Drawing.Color.Green,
                System.Drawing.Color.DodgerBlue,
                

                System.Drawing.Color.Fuchsia,
                System.Drawing.Color.Goldenrod,

                System.Drawing.Color.OrangeRed,
                System.Drawing.Color.Gray,
                System.Drawing.Color.Blue,
            };

            List<string> tagNameList = new List<string>()
            {
                // 显示单下划线
                "AD__R__Line__Current", "AD__S__Line__Current", "AD__T__Line__Current",
                "AD__RS__Line__Voltage", "AD__ST__Line__Voltage", 
                "AD__DC__BUS__Positive__Voltage", "AD__DC__BUS__Negative__Voltage", "AD__DC__BUS__Battery__Voltage",
            }; 

            for (int i = 0; i < FaultWaveNum; i++)
            {
                var wavePlot = _plotCtrl.Plot.AddSignal(ys: new double[1]);
                wavePlot.LineWidth = 2.0;
                wavePlot.Color = waveColors[i];
                wavePlot.YAxisIndex = (i < 3) ? 0 : 1;
                wavePlot.SampleRate = SamplingRate;
                
                SelectedFaultWaves.Add(new FaultWavePlotVM()
                {
                    LineColor = waveColors[i],
                    TagName = tagNameList[i],
                    WaveText = tagNameList[i],
                    WavePlot = wavePlot
                });
                
            }
        }

        private void InitializeDemoWaves()
        {
            var testWaves1 = new List<List<double>>();

            var testWaves2 = new List<List<double>>();

            for (int i = 0; i < 8; i++)
            {
                testWaves1.Add(new List<double>());
                testWaves2.Add(new List<double>());
                for (int j = 0; j < 600; j++)
                {
                    testWaves1[i].Add(Math.Sin(0.01 * j + 0.2 * i));
                    testWaves2[i].Add(Math.Cos(0.01 * j + 0.2 * i));
                }
            }

            FaultWavesInfoList.Add(new FaultWavesInfoVM()
            {
                Index = 4,
                TrigIndex = 150,
                IsSelectedToPlot = false,
                FaultType = (FaultWavesInfoVM.FaultTypeEnum)5,
                RecordTime = DateTime.MaxValue.ToString("yyyy/MM/dd-HH:mm:ss"),
                FaultWavesData = testWaves1,
            });

            FaultWavesInfoList.Add(new FaultWavesInfoVM()
            {
                Index = 3,
                TrigIndex = 50,
                IsSelectedToPlot = false,
                FaultType = FaultWavesInfoVM.FaultTypeEnum.GRID_PRD_BUS_VOLT_UNBALANCE_AVG,
                RecordTime = DateTime.MinValue.ToString("yyyy/MM/dd-HH:mm:ss"),
                FaultWavesData = testWaves2,
            });

            FaultWavesInfoList = new ObservableCollection<FaultWavesInfoVM>(FaultWavesInfoList.OrderBy(elem => elem.Index).ToList());

        }

        public void ClosedJobs()
        {
            ExecuteStopReadFaultWaves();
        }

        #region Settings
        public ObservableCollection<int> FetchIndexComboItems { get; } = new()
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
        };
        

        private int _fetchBeginIndex = 1;

        public int FetchBeginIndex
        {
            get => _fetchBeginIndex;
            set
            {
                _fetchBeginIndex = value;

                if (_fetchBeginIndex > _fetchEndIndex)
                {
                    FetchEndIndex = _fetchBeginIndex;
                }
                OnPropertyChanged();
            }
        }

        private int _fetchEndIndex = 1;

        public int FetchEndIndex
        {
            get => _fetchEndIndex;
            set
            {
                _fetchEndIndex = value;

                if (_fetchBeginIndex > _fetchEndIndex)
                {
                    FetchBeginIndex = _fetchEndIndex;
                }
                OnPropertyChanged();
            }
        }

        private ObservableCollection<FaultWavesInfoVM> _faultWavesInfoList = new();
        public ObservableCollection<FaultWavesInfoVM> FaultWavesInfoList
        {
            get => _faultWavesInfoList;
            set { _faultWavesInfoList = value; OnPropertyChanged();}
        } 


        #endregion

        #region Plot
        private ScottPlot.WpfPlot _plotCtrl = new WpfPlot();
        public ScottPlot.WpfPlot PlotCtrl => _plotCtrl;

        private ScottPlot.Plottable.HLine _currentHLine;
        private ScottPlot.Plottable.HLine _voltageHLine;

        public ObservableCollection<FaultWavePlotVM> SelectedFaultWaves { get; set; } = new();

        private void InitializePlot()
        {
            PlotCtrl.Plot.Title("故障录波");
            PlotCtrl.Configuration.DoubleClickBenchmark = false;

            
            PlotCtrl.Plot.LeftAxis.Label("电流(A)");
            PlotCtrl.Plot.LeftAxis.Color(System.Drawing.Color.Red);
            

            PlotCtrl.Plot.RightAxis.Ticks(true);
            PlotCtrl.Plot.RightAxis.LockLimits(false);
            PlotCtrl.Plot.RightAxis.SetBoundary();
            PlotCtrl.Plot.RightAxis.Label("电压(V)");
            PlotCtrl.Plot.RightAxis.Color(System.Drawing.Color.Fuchsia);
            PlotCtrl.Plot.RightAxis.LabelStyle(rotation:90);
            
            _currentHLine = PlotCtrl.Plot.AddHorizontalLine(y: 0, color: System.Drawing.Color.Red);
            _currentHLine.YAxisIndex = PlotCtrl.Plot.LeftAxis.AxisIndex;
            _currentHLine.PositionLabel = true;
            _currentHLine.DragEnabled = true;
            _currentHLine.PositionFormatter = d => d.ToString("0.000E0");
            _currentHLine.IsVisible = false;
            
            _voltageHLine = PlotCtrl.Plot.AddHorizontalLine(y: -0, color: System.Drawing.Color.Fuchsia);
            _voltageHLine.YAxisIndex = PlotCtrl.Plot.RightAxis.AxisIndex;
            _voltageHLine.PositionLabel = true;
            _voltageHLine.DragEnabled = true;
            _voltageHLine.PositionFormatter = d => d.ToString("0.000E0");
            _voltageHLine.PositionLabelOppositeAxis = true;
            _voltageHLine.IsVisible = false;
           

            PlotCtrl.Render();
        }

        [RelayCommand]
        private void VisualizeWave()
        {
            //for (int i = 0; i < SelectedFaultWaves.Count; i++) {}
            //PlotCtrl.Plot.AxisAuto();
            PlotCtrl.Refresh();
        }

        [RelayCommand]
        private void VisualizeDataCursor(bool isVisible)
        {
            _currentHLine.IsVisible = isVisible;
            _voltageHLine.IsVisible = isVisible;
            PlotCtrl.Refresh();
        }

        [RelayCommand]
        private void UnlockCurrentAxis(bool isUnlocked)
        {
            PlotCtrl.Plot.LeftAxis.LockLimits(!isUnlocked);
        }

        [RelayCommand]
        private void UnlockVoltageAxis(bool isUnlocked)
        {
            PlotCtrl.Plot.RightAxis.LockLimits(!isUnlocked);
        }

        [RelayCommand]
        private void SwitchFaultWavePlot(int idx)
        {
            for (int i = 0; i < FaultWavesInfoList.Count; i++)
            {
                if (FaultWavesInfoList[i].IsSelectedToPlot)
                {
                    var faultWavesData = FaultWavesInfoList[i].FaultWavesData;
                    for (int k = 0; k < SelectedFaultWaves.Count; k++)
                    {
                        SelectedFaultWaves[k].WavePlot.Ys = faultWavesData[k].ToArray();
                        SelectedFaultWaves[k].WavePlot.MaxRenderIndex = faultWavesData[k].Count - 1;
                    }
                    
                }
            }
            PlotCtrl.Plot.AxisAuto();
            PlotCtrl.Refresh();
        }

        #endregion

        #region Communication

        private bool _isReceiving = false;

        public bool IsReceiving
        {
            get => _isReceiving; 
            set { _isReceiving = value; OnPropertyChanged();}
        }

        [RelayCommand]
        private void ReadFaultWaves(string mode = "Replace")
        {
            MultiLanguages.Common.LogHelper.WriteLog($"**************** 故障录波CAN帧日志({DateTime.Now.ToString()}) ****************");
            
            // 选择目标设备
            byte dstAddr = DeviceManager.Instance().GetSelectDev();
            if (dstAddr == 0)
            {
                ShowStatusMsg("未选择目标设备，请在界面下方选择一台设备", "ERROR");
                return;
            }

            // 分析需要读取的编号
            var fetchIndexList = new List<int>();
            for (int waveIdx = FetchBeginIndex; waveIdx < FetchEndIndex + 1; waveIdx++)
            {
                fetchIndexList.Add(waveIdx);
            }

            if (mode == "Append")
            {
                // 追加读取
                for (int i = 0; i < FaultWavesInfoList.Count; i++)
                {
                    if (fetchIndexList.Contains(FaultWavesInfoList[i].Index))
                    {
                        fetchIndexList.Remove(FaultWavesInfoList[i].Index);
                    }
                }
                // 清空波形
                ClearPlot();
            }
            else
            {
                // 清空读取
                // 清空波形和数据
                if (MessageBox.Show("确认要清除当前故障录波数据？", "警告",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    FaultWavesInfoList.Clear();
                    ClearPlot();
                }
                else
                {
                    return;
                }
            }

            // 开始请求/接收
            IsReceiving = true;
            Task.Run(() =>
            {
                for (int i = 0; i < fetchIndexList.Count; i++)
                {
                    if (!IsReceiving)
                    {
                        continue;
                    }

                    if (ReadFaultWavesByIndex(dstAddr, fetchIndexList[i], out int faultTypeIndex, out int trigIndex, 
                            out string recordTime, out List<List<double>> wavesList))
                    {
                        if (!IsReceiving)
                        {
                            return;
                        }
                        var curWaveIdx = fetchIndexList[i];
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            var newFaultWavesInfo = new FaultWavesInfoVM()
                            {
                                Index = curWaveIdx,
                                FaultType = (FaultWavesInfoVM.FaultTypeEnum)faultTypeIndex,
                                TrigIndex = trigIndex,
                                RecordTime = recordTime,
                                FaultWavesData = wavesList,
                            };

                            // 数据重排列
                            RearrangeWavesData(newFaultWavesInfo);

                            FaultWavesInfoList.Add(newFaultWavesInfo);

                            PlotCtrl.Refresh();
                            ShowStatusMsg($"读取第{curWaveIdx}个故障录波成功", timeMs:5000);
                        });
                    }
                    else
                    {
                        if (IsReceiving)
                        {
                            ShowStatusMsg($"读取第{fetchIndexList[i]}个故障录波失败");
                        }
                    }
                }

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (FaultWavesInfoList.Count > 1)
                    {
                        FaultWavesInfoList = new (FaultWavesInfoList.OrderBy(elem => elem.Index).ToList());
                    }
                    IsReceiving = false;
                });
                
            });
            

        }
        

        private bool ReadFaultWavesByIndex(byte dstAddr, int waveIndex, out int faultTypeIndex, out int trigIndex, 
                                                out string recordTime, out List<List<double>> wavesList)
        {
            faultTypeIndex = -1;
            trigIndex = -1;
            recordTime = DateTime.MinValue.ToString("yyyy/MM/dd-HH:mm:ss");
            wavesList = new List<List<double>>();
            for (int i = 0; i < FaultWaveNum; i++)
            {
                wavesList.Add(new List<double>());
            }

            // <1> 请求录波文件
            int fileSize = 0;
            var txFileCanID = new CanFrameID()
            {
                // 请求帧，功能码41
                Priority = 3,
                FrameType = 2,
                ContinuousFlag = 0,
                PF = 41,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            var rxFileCanID = new CanFrameID()
            {
                // 应答帧，功能码41
                Priority = 3,
                FrameType = 3,
                ContinuousFlag = 0,
                PF = 41,
                DstType = 2,
                DstAddr = 1,
                SrcType = 1,
                SrcAddr = dstAddr,
            };

            var txFileData = new List<byte>()
            {
                // FileIndex = waveIdx + FaultWaveFileBegin, ReadMode = 1
                (byte)(waveIndex + FaultWaveFileBegin), 1,
                0, 0, 0, 0, 0, 0
            };

            // Send request and get response
            RxQueue.Clear();
            if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txFileCanID.ID, txFileData))
            {
                return false;
            }
            
            MultiLanguages.Common.LogHelper.WriteLog($"已请求第{waveIndex}号录波");
            ShowStatusMsg($"已请求第{waveIndex}号录波");

            var sw = Stopwatch.StartNew();
            do
            {
                if (RxQueue.TryDequeue(out var rawFrame) && new CanFrameID(rawFrame.ID).FC == rxFileCanID.FC)
                {
                    var fileIndex = rawFrame.Data[0];
                    var result = rawFrame.Data[1];
                    fileSize = rawFrame.Data[3] << 8 | rawFrame.Data[2];
                    if (fileIndex != waveIndex + FaultWaveFileBegin || result != 0)
                    {
                        return false;
                    }
                    sw.Stop();
                    break;
                }
            } while (sw.ElapsedMilliseconds < 1000);
            if (sw.ElapsedMilliseconds >= 1000)
            {
                return false;
            }

            // <2> 读取录波
            int remainSize = fileSize;
            var txFaultWaveCanID = new CanFrameID()
            {
                // 请求帧，功能码41
                Priority = 3,
                FrameType = 2,
                ContinuousFlag = 0,
                PF = 41,
                DstType = 1,
                DstAddr = dstAddr,
                SrcType = 2,
                SrcAddr = 1,
            };

            var rxFaultWaveCanID = new CanFrameID()
            {
                // 数据帧，功能码41
                Priority = 3,
                FrameType = 1,
                ContinuousFlag = 1,
                PF = 41,
                DstType = 2,
                DstAddr = 1,
                SrcType = 1,
                SrcAddr = dstAddr,
            };

            List<byte> txFaultWaveData;
            int readOffset = 0;

            // <2.1> 请求/接收录波信息
            txFaultWaveData = new List<byte>()
            {
                // FileIndex = waveIndex + FaultWaveFileBegin, Offset = 0, ReadOffset = FaultWaveInfoSize
                (byte)(waveIndex + FaultWaveFileBegin),
                (byte)(readOffset & 0xFF), (byte)(readOffset >> 8),
                (byte)(FaultWaveInfoSize & 0xFF), (byte)(FaultWaveInfoSize >> 8),
                0, 0, 0
            };

            // Send request and get response
            RxQueue.Clear();
            if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txFaultWaveCanID.ID, txFaultWaveData))
            {
                return false;
            }

            MultiLanguages.Common.LogHelper.WriteLog($"已请求第{waveIndex}号录波的录波信息");
            

            if (!OscilloscopePageVM.ReadFileDataFrames(RxQueue, rxFaultWaveCanID, 0, FaultWaveInfoSize * 2,
                                                        out var rxWaveInfoData, 10000))
            {
                return false;
            }

            faultTypeIndex = rxWaveInfoData[1] << 8 | rxWaveInfoData[0];
            trigIndex = rxWaveInfoData[3] << 8 | rxWaveInfoData[2];
            var year = 2000 + (rxWaveInfoData[5] << 8 | rxWaveInfoData[4]);
            var month = rxWaveInfoData[7] << 8 | rxWaveInfoData[6];
            var day = rxWaveInfoData[9] << 8 | rxWaveInfoData[8];
            var hour = rxWaveInfoData[11] << 8 | rxWaveInfoData[10];
            var minute = rxWaveInfoData[13] << 8 | rxWaveInfoData[12];
            var second = rxWaveInfoData[15] << 8 | rxWaveInfoData[14];
            
            recordTime = $"{year:D4}/{month:D2}/{day:D2}-{hour:D2}:{minute:D2}:{second:D2}";

            MultiLanguages.Common.LogHelper.WriteLog($"第{waveIndex}号录波的录波信息: " 
                                                     + $"故障类型【{faultTypeIndex}】；"
                                                     + $"故障起始索引号【{trigIndex}】；"
                                                     + $"录波时间【{recordTime.ToString()}】");


            remainSize -= FaultWaveInfoSize;
            ShowStatusMsg($"读取第{waveIndex}号录波的录波信息成功");

            // <2.2> 请求/接收录波数据
            int readTotalCount = remainSize / FaultWavePackSize;
            if (remainSize % FaultWavePackSize > 0)
            {
                readTotalCount += 1;
            }

            for (int i = 0; i < readTotalCount; i++)
            {
                if (!IsReceiving)
                {
                    ShowStatusMsg("已停止读取数据");
                    break;
                }

                readOffset = i + 1;
                txFaultWaveData = new List<byte>()
                {
                    (byte)(waveIndex + FaultWaveFileBegin),
                    (byte)(readOffset & 0xFF), (byte)(readOffset >> 8),
                    (byte)((FaultWavePackSize * 2) & 0xFF), (byte)((FaultWavePackSize * 2 ) >> 8),
                    0, 0, 0
                };

                // Send request and get response
                RxQueue.Clear();
                if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txFaultWaveCanID.ID, txFaultWaveData))
                {
                    return false;
                }

                MultiLanguages.Common.LogHelper.WriteLog($"已请求第{waveIndex}号录波的录波数据块{readOffset}");


                if (!OscilloscopePageVM.ReadFileDataFrames(RxQueue, rxFaultWaveCanID, readOffset, FaultWavePackSize * 2,
                                                            out var rxWaveData, 10000)
                    || rxWaveData.Count != FaultWavePackSize * 2)
                {
                    return false;
                }

                

                string dataStr = "";
                for (int p = 0; p < rxWaveData.Count; p += FaultWaveNum * 2)
                {
                    for (int k = 0; k < FaultWaveNum; k++)
                    {
                        short data = (short)(rxWaveData[p + 2 * k + 1] << 8 | rxWaveData[p + 2 * k]);
                        wavesList[k].Add(data);
                        dataStr += data.ToString() + " "; 
                    }
                    dataStr += "\n";
                }

                MultiLanguages.Common.LogHelper.WriteLog($"第{waveIndex}号录波的录波数据块{readOffset}解析数据: \n" + dataStr);
                ShowStatusMsg($"已读取第{waveIndex}号录波的第{readOffset}/{readTotalCount}个录波数据块...");
            }
            
            return true;
        }
        
        /// <summary>
        /// 根据TrigIndex对数据进行重排
        /// </summary>
        /// <param name="faultWavesInfo"></param>
        private bool RearrangeWavesData(FaultWavesInfoVM faultWavesInfo)
        {
            int trigIndex = faultWavesInfo.TrigIndex;

            for (int i = 0; i < faultWavesInfo.FaultWavesData.Count; i++)
            {
                if (trigIndex >= faultWavesInfo.FaultWavesData[i].Count)
                {
                    return false;
                }

                var len = faultWavesInfo.FaultWavesData[i].Count;
                var part1 = faultWavesInfo.FaultWavesData[i].GetRange(trigIndex, len - trigIndex);
                var part2 = faultWavesInfo.FaultWavesData[i].GetRange(0, trigIndex);
                faultWavesInfo.FaultWavesData[i].Clear();
                faultWavesInfo.FaultWavesData[i].AddRange(part1);
                faultWavesInfo.FaultWavesData[i].AddRange(part2);
            }

            return true;
        }

        [RelayCommand]
        private void StopReadFaultWaves()
        {
            if (MessageBox.Show("确认要停止读取？", "警告",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                ExecuteStopReadFaultWaves();
            }
        }

        private void ExecuteStopReadFaultWaves()
        {
            if (IsReceiving)
            {
                IsReceiving = false;
                ShowStatusMsg("已停止读取数据");
            }
        }

        [RelayCommand]
        private void Clear()
        {
            if (MessageBox.Show("确认要清除当前故障录波数据？", "警告", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                ClearPlot();
                FaultWavesInfoList.Clear();
            }
        }

        private void ClearPlot()
        {
            for (int i = 0; i < SelectedFaultWaves.Count; i++)
            {
                SelectedFaultWaves[i].WavePlot.Ys = new double[1];
                SelectedFaultWaves[i].WavePlot.MaxRenderIndex = 0;
            }
            PlotCtrl.Refresh();
        }

        #endregion

    }

    public class FaultWavePlotVM : ObservableObject 
    {
        public string TagName { get; set; }
        public ScottPlot.Plottable.SignalPlot WavePlot { get; set; }
        public string WaveText { get; set; } = "";
        public System.Drawing.Color LineColor { get; set; }

        public bool IsVisible { get; set; } = true;

    }

    public class FaultWavesInfoVM : ObservableObject
    {
        public int Index { get; set; }

        public bool IsSelectedToPlot { get; set; } = false;

        public string RecordTime { get; set; } = "";

        public FaultTypeEnum FaultType { get; set; }
        
        public int TrigIndex { get; set; } = 0;

        public List<List<double>> FaultWavesData { get; set; }

        public enum FaultTypeEnum
        {
            // -------CRITICAL FAULT--------
            CTRL_BUS_OVP_HW = 0,
            CTRL_OCP_HW = 1,
            CTRL_GRID_OVP_INST_I,
            CTRL_GRID_OVP_INST_II,
            CTRL_BUS_OVP_INST,
            CTRL_BUS_UVP_INST,
            CTRL_AC_OCP_INST,
            CTRL_CBC,

            // -------FAST FAULT--------
            FAST_ISO_DET_FAULT,
            FAST_ISO_DET_WARNING,
            FAST_CONSISTENT_UGRID,
            FAST_GFCI_FAULT,
            FAST_GFCI_DEVICE_FAULT_AC,
            FAST_ANTI_ISLAND_FAULT,

            // -------RMS/AVG FAULT-------
            GRID_PRD_GRID_OVP_RMS,
            GRID_PRD_GRID_UVP_RMS,
            GRID_PRD_GRID_OFP,
            GRID_PRD_GRID_UFP,
            GRID_PRD_BUS_VOLT_UNBALANCE_AVG,
            GRID_PRD_BUS_UVP_AVG,
            GRID_PRD_BUS_OVP_AVG,
            GRID_PRD_AC_OCP_RMS,
            GRID_PRD_AC_CUR_UNBALANCE_RMS,
            GRID_PRD_AMB_OTP,
            GRID_PRD_IGBT_RA_OTP,
            GRID_PRD_IGBT_SA_OTP,
            GRID_PRD_IGBT_TA_OTP,
            GRID_PRD_IGBT_RB_OTP,
            GRID_PRD_IGBT_SB_OTP,
            GRID_PRD_IGBT_TB_OTP,
            GRID_PRD_IGBT_ANTI_SC_OTP,
            GRID_PRD_IGBT_TEMP_UNBALANCE,
            GRID_PRD_AD_CALI_FAULT,
            GRID_PRD_ANTI_SC_PROTECT,
            GRID_PRD_ANTI_SC_FAULT,
            GRID_PRD_AC_SPD_FAIL,
            GRID_PRD_DC_SPD_FAIL,
            GRID_PRD_12V_LOW_WARNING,
            GRID_PRD_12V_LOW_FAULT,
            GRID_LOST,
            GRID_PRD_BAT_UVP,
            GRID_PRD_BAT_OVP,
            GRID_PRD_DCI_OCP,
            GRID_PRD_OVERTEMP_DERATING,
            GRID_PRD_FREQ_DERATING,
            GRID_PRD_GRID_UNBALANCE,
            GRID_PRD_DC_RELAY_FEEDBACK_ERR,
            GRID_PRD_N_TO_PE_VOLT_FAULT,
            GRID_PRD_AMB_LOW_TEMP_FAULT,
            GRID_PRD_AMB_LOW_TEMP_WARN,
            GRID_PRD_OVER_MODULATION_FAULT,
            GRID_PRD_LVRT_OVER_TIME_FAULT,
            GRID_PRD_HVRT_OVER_TIME_FAULT,

            // -------SLOW FAULT-------
            SLOW_DC_CONTACTOR_RELAY_FAULT,
            SLOW_BRIDGE_ARM_SHORT_FAULT,
            SLOW_AC_RELAY_FAULT,
            SLOW_SOFT_START_FAULT,
            SLOW_AC_START_FAULT,
            SLOW_DEVICE_ID_CONFLICT,
            SLOW_INTERNAL_CAN_LOST,
            SLOW_INNER_FAN_WARNING,
            SLOW_OUTER_FAN_WARNING,
            SLOW_INNER_FAN_FAULT,
            SLOW_OUTER_FAN_FAULT,
            SLOW_HARDWARE_VERSION_FAULT,
            SLOW_PHASE_SEQUANCE_FAULT,
            SLOW_BAT_REVERSE_FAULT,
        }

    }


    #region ValueConverters
    public class ChannelColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // System.Drawing.Color -> System.Windows.Media.Color
            System.Drawing.Color color = (System.Drawing.Color)value;
            var brush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            return brush;
            
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
                throw new InvalidOperationException("The target must be a boolean");

            return !(bool)value;
        }
    }

    public class UInt32ToHexStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "0x" + ((uint)value).ToString("X8");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

}
