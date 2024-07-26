using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CanProtocol.ProtocolModel;
using CanProtocol.Utilities;
using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Renderable;
using SofarHVMExe.Model;
using SofarHVMExe.Util.TI;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;

namespace SofarHVMExe.ViewModel
{
    public partial class OscilloscopeVm : ObservableObject
    {

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
            Stop();
        }

        public void SaveSettings()
        {
            fileCfgModel.OscilloscopeModel.FilesPath.DwarfXmlPath = DwarfXmlPath;
            fileCfgModel.OscilloscopeModel.FilesPath.CoffPath = (CoffPath.Contains(".out")) ? CoffPath : "";
            fileCfgModel.OscilloscopeModel.UnderSampleScale = UnderSampleScale;
            fileCfgModel.OscilloscopeModel.TrigMode = TrigMode;
            fileCfgModel.OscilloscopeModel.TrigSource = TrigSource;
            fileCfgModel.OscilloscopeModel.TrigYLevel = TrigYLevel;
            fileCfgModel.OscilloscopeModel.TrigXPercent = TrigXPercent;

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

            //if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            //{

            //}
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
                TrigXPercent = fileCfgModel.OscilloscopeModel.TrigXPercent;
                for (int i = 0; i < fileCfgModel.OscilloscopeModel.ChannelInfoList.Count; ++i)
                {
                    ChannelsSettings[i].VariableName = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].VariableName;
                    ChannelsSettings[i].DataType = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].DataType;
                    ChannelsSettings[i].FloatDataScale = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].FloatDataScale;
                    ChannelsSettings[i].Comment = fileCfgModel.OscilloscopeModel.ChannelInfoList[i].Comment;
                }
            }
        }

        public OscilloscopeVm(EcanHelper? ecanHelper)
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

            var saveDir = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "DWARF"));
            string objFileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);

            string copyObjPath = System.IO.Path.Combine(saveDir.FullName, objFileName + "(copy).out");
            System.IO.File.Copy(dlg.FileName, copyObjPath);

            string dwarfXmlSavePath = System.IO.Path.Combine(saveDir.FullName, objFileName + "_DWARF_" +
                                                                               DateTime.Now.ToString("yyyyMMddhhmmss") + AppDomain.CurrentDomain.Id + ".xml");

            try
            {
                CoffPath = "加载中...";
                IsStartAllowed = false;  // 加载中禁止启动
                await TIPluginHelper.ConvertCoffToXmlAsync(copyObjPath, dwarfXmlSavePath, saveDir.FullName);
                await Task.Run(() => TIAddressChecker.TrimDwarfXml(dwarfXmlSavePath));

                MessageBox.Show($"Debug信息文件DWARF.xml保存路径（有效时间{DwarfXmlValidHours}小时）:\n" + dwarfXmlSavePath,
                    "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                CoffPath = dlg.FileName;
                DwarfXmlPath = dwarfXmlSavePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                CoffPath = "";
            }
            finally
            {
                System.IO.File.Delete(copyObjPath);
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
        public ObservableCollection<ChannelModel> ChannelsSettings { get; set; } = new();

        private void InitializeChannelsSettings()
        {
            List<System.Drawing.Color> channelColors = new List<System.Drawing.Color>()
            {
                System.Drawing.Color.DodgerBlue,
                System.Drawing.Color.Red,
                System.Drawing.Color.Green,
                System.Drawing.Color.BlueViolet,
                System.Drawing.Color.OrangeRed,
                System.Drawing.Color.Goldenrod,
            };


            for (int i = 0; i < MaxChannelNum; i++)
            {
                ChannelsSettings.Add(new ChannelModel
                {
                    TagName = $"CH{i + 1}",
                    LineColor = channelColors[i],
                    ID = i + 1,
                });
                TrigSourceComboItems.Add($"CH{i + 1}");
            }
        }

        public ObservableCollection<string> DataTypeComboItems { get; } = new()
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
                _underSampleScale = value;
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
            get => _trigSource;
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
            "0% TimeAxis",
            "10% TimeAxis", "20% TimeAxis", "30% TimeAxis", "40% TimeAxis", "50% TimeAxis",
            "60% TimeAxis", 
            //"70% TimeAxis", "80% TimeAxis", "90% TimeAxis", "100% TimeAxis"
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
            "SingleTrig", "ContinuousTrig", "Rolling"
        };

        #endregion

        #region Communication

        private bool _isStartAllowed = true;
        public bool IsStartAllowed
        {
            get => _isStartAllowed;
            set => SetProperty(ref _isStartAllowed, value);
        }

        [RelayCommand]
        private void Start()
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
                _signalAxis[i].Hide(ChannelsSettings[i].ID < 0);
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
                case "SingleTrig":
                    Task.Run(() => StartWithoutReadData(dstAddr));
                    // Task.Run(() => SingleTrigStartXY(dstAddr));
                    break;
                case "ContinuousTrig":
                    //Task.Run(() => ContinuousTrigStart());
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
            var dwarfDirInfo = System.IO.Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp", "DWARF"));

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
                _addressChecker.LoadDwarfXml(DwarfXmlPath);
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

            if (string.IsNullOrEmpty(errInfo))
            {
                //string addrResDebug = "寻址结果：\n";
                //for (int i = 0; i < varCollection.Count; i++)
                //{
                //    if (varCollection[i].VariableName.StartsWith("0x") || varCollection[i].VariableName.StartsWith("0X"))
                //    {

                //    }
                //    addrResDebug += $"{varCollection[i].VariableName}: 0x{varCollection[i].VariableAddress:x8} \n";
                //}
                //MessageBox.Show(addrResDebug, "寻址成功", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return true;
            }
            else
            {
                MessageBox.Show(errInfo, "寻址错误", MessageBoxButton.OK, MessageBoxImage.Error);

                return false;

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

        private void SingleTrigStartXY(byte dstAddr)
        {
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

            Thread.Sleep(800);

            ShowStatusMsg("单次触发开始");


            IsStopped = false;


            List<List<short>> chDataCollection = new();
            int storageDepth = TotalStorageDepth / MaxChannelNum;

            int maxWaveNum = (int)(SamplePointNum * (1 - TrigXPercent / 10.0));
            double timeX = 0;
            var newTimeXs = new List<double>();
            for (int i = 0; i < SamplePointNum - maxWaveNum; i++)
            {
                // 触发位置偏移填充
                newTimeXs.Add(timeX);
                chDataCollection.Add(new List<short>() { 0, 0, 0, 0, 0, 0 });
                timeX += UnderSampleScale / SampleFreq;
            }

            PlotCtrl.Plot.BottomAxis.Dims.SetAxis(-5 * UnderSampleScale / SampleFreq, SamplePointNum * UnderSampleScale / SampleFreq);


            var swTotal = Stopwatch.StartNew();
            var sw = Stopwatch.StartNew();
            int waveIndex = 0;
            int failureCnt = 0;
            while (!IsStopped && waveIndex < maxWaveNum && failureCnt < 10)
            {
                if (ReadChannelData(dstAddr, waveIndex, out List<short> chData))
                {
                    newTimeXs.Add(timeX);
                    chDataCollection.Add(chData);
                    if ((waveIndex + 1) % 200 == 0)
                    {
                        //MultiLanguages.Common.LogHelper.WriteLog($"取400点耗时（ms）：{sw.ElapsedMilliseconds}");
                        sw.Restart();

                        UpdatePlotXY(chDataCollection, newTimeXs);
                        chDataCollection.Clear();
                        newTimeXs.Clear();

                    }
                }
                else
                {
                    failureCnt++;
                    // ShowStatusMsg($"读取波形数据失败：WaveIndex={waveIndex}");
                }

                timeX += UnderSampleScale / SampleFreq;
                waveIndex++;
            }
            UpdatePlotXY(chDataCollection, newTimeXs);
            chDataCollection.Clear();
            newTimeXs.Clear();

            // 正常流程结束
            if (!IsStopped)
            {
                MultiLanguages.Common.LogHelper.WriteLog($"单次触发耗时（ms）：{swTotal.ElapsedMilliseconds}");
                if (WriteStopRequest(dstAddr))
                {
                    ShowStatusMsg("单次触发结束, 示波器已关闭");
                }
                else
                {
                    ShowStatusMsg("单次触发结束, 示波器未成功关闭");
                }
            }


            IsStopped = true;
            IsStartAllowed = true;

        }

        private void ContinuousTrigStart()
        {

        }

        private void RollingStart()
        {

        }

        private void UpdatePlotXY(List<List<short>> channelDataCollection, List<double> newTimeXs)
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
            Array.Copy(newTimeXs.ToArray(), 0, _timeXs, _fillBeginIndex - newTimeXs.Count, newTimeXs.Count);

            // Refresh
            App.Current.Dispatcher.Invoke(() => {
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

        private bool ReadOscilloscopeProperty(byte dstAddr)
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

            RxQueue.Clear();
            if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txCanID.ID, txData))
            {
                return false;
            }

            if (!OscilloscopePageVM.ReadAddressDataFrames(RxQueue, rxCanID, 0, 4, out var rxData, 5000) || rxData.Count != 8)
            {
                return false;
            }

            SampleFreq = (rxData[1] << 8) | rxData[0];
            SamplePointNum = (rxData[3] << 8) | rxData[2];
            TotalStorageDepth = (rxData[7] << 24) | (rxData[6] << 16) |
                                (rxData[5] << 8) | rxData[4];

            return true;
        }

        private bool WriteOscilloscopeSettings(byte dstAddr)
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
                txData.Add((byte)(((TrigMode + 1) << 4) | (MaxChannelNum - 1)));
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
            if (!OscilloscopePageVM.SendAddressDataFramesCan1(_ecanHelper, txCanID.ID, txData))
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
            } while (sw.ElapsedMilliseconds < 3000);

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
            if (!OscilloscopePageVM.ReadAddressDataFrames(RxQueue, rxCanID, 0, 7, out var rxData, 250)
                || rxData.Count != 2 + 2 * MaxChannelNum)
            {
                return false;
            }

            int waveIdxResponse = rxData[1] << 8 | rxData[0];
            if (waveIdxResponse != waveIdx)
            {
                //MultiLanguages.Common.LogHelper.WriteLog("");
                return false;
            }


            for (int i = 2; i < rxData.Count; i += 2)
            {
                var chData = (short)(rxData[i + 1] << 8 | rxData[i]);
                channelData.Add(chData);
            }

            string channelDataStr = "";
            for (int i = 0; i < channelData.Count; ++i)
            {
                channelDataStr += channelData[i].ToString() + " ";
            }
            MultiLanguages.Common.LogHelper.WriteLog($"WaveData({waveIdx}) {channelDataStr}");

            //var rxDelay = sw.ElapsedMilliseconds;
            //MultiLanguages.Common.LogHelper.WriteLog($"6点数据接收时长(ms)：{sw.ElapsedMilliseconds}");
            return true;

        }


        [RelayCommand]
        private void Stop()
        {
            IsStopped = true;
            IsStartAllowed = true;

            ShowStatusMsg("示波器请求关闭");
            byte dstAddr = DeviceManager.Instance().GetSelectDev();

            if (!WriteStopRequest(dstAddr))
            {
                ShowStatusMsg("示波器关闭失败");
            }
            else
            {
                ShowStatusMsg("示波器关闭成功");
            }


        }

        private bool WriteStopRequest(byte dstAddr)
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
            } while (sw.ElapsedMilliseconds < 500);

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
        public ObservableCollection<SignalTextModel> SignalTextList { get; set; } = new();

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

        private List<ScottPlot.Plottable.VLine> _dataCursors = new();

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
                        for (int i = 0; i < _dataCursors.Count; i++)
                        {
                            PlotCtrl.Plot.Remove(_dataCursors[i]);
                        }
                        _dataCursors.Clear();

                        double xVLine = PlotCtrl.Plot.BottomAxis.Dims.Center;
                        var dataCursor = PlotCtrl.Plot.AddVerticalLine(x: xVLine, color: System.Drawing.Color.White, width: 2.0f);
                        dataCursor.DragEnabled = true;
                        dataCursor.PositionLabel = true;
                        dataCursor.PositionLabelOppositeAxis = true;
                        dataCursor.Dragged += UpdateSignalXYText;
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

        private void UpdateSignalXYText(object sender, EventArgs args)
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
                                && curX <= _timeXs[_signalXYPlots[i].MaxRenderIndex] + 0.001)
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
                                            dataText += $"{(int)signalY}" +
                                                        $"(0x{Convert.ToString((int)signalY).PadLeft(2, '0')})";
                                            string binStr = $"b{Convert.ToString((int)signalY, 2).PadLeft(8, '0')}";
                                            for (int p = 5; p < binStr.Length; p += 5)
                                            {
                                                binStr = binStr.Insert(p, "_");
                                            }
                                            dataText += binStr;
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
            set { _isInvalid = value; OnPropertyChanged(); }
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
            set { _tagName = value; OnPropertyChanged(); }
        }

        private System.Drawing.Color _lineColor = System.Drawing.Color.White;
        public System.Drawing.Color LineColor
        {
            get => _lineColor;
            set { _lineColor = value; OnPropertyChanged(); }
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
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

    #region ValueConverters
    public class DrawingColorToMediaColorConverter : IValueConverter
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

    #endregion
}
