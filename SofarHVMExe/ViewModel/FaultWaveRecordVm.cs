using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CanProtocol.ProtocolModel;
using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ScottPlot;
using SofarHVMExe.Utilities.Global;

namespace SofarHVMExe.ViewModel
{
    public partial class FaultWaveRecordVm : ObservableObject
    {
        private const ushort FaultWaveInfoSize = 6;       // 录波信息大小，字长16bits
        private const int FaultWaveNum = 8;               // 录波波形数量
        private const ushort FaultWavePackSize = FaultWaveNum * 12;  // 一个数据块接收12列数据，字长16bits

        private const double SamplingRate = 1;            // 采样频率, 4050Hz;
        private const int FaultWaveFileBegin = 200;       // 录波数据文件起始编号

        private const int FaultWaveTotalLength = 600;
        private const int FaultTrigPosition = 400;        // 每个波形600点数据，前400为故障前数据，后200为故障后数据

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


        public void ShowStatusMsg(string msg, string type = "default", bool toLog = true, int timeMs = -1)
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

        public FaultWaveRecordVm(EcanHelper ecanHelper)
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
                "AD__R__Phase__Current", "AD__S__Phase__Current", "AD__T__Phase__Current",
                "AD__RS__Phase__Voltage", "AD__ST__Phase__Voltage",
                "AD__DC__BUS__Positive__Voltage", "AD__DC__BUS__Negative__Voltage", "AD__DC__BUS__Battery__Voltage",
            };

            for (int i = 0; i < 8; i++)
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
                IsSelectedToPlot = false,
                FaultType = (FaultWavesInfoVM.FaultTypeEnum)5,
                RecordTime = DateTime.MaxValue.ToString("yyyy/MM/dd-HH:mm:ss.fff"),
                FaultWavesData = testWaves1,
            });

            FaultWavesInfoList.Add(new FaultWavesInfoVM()
            {
                Index = 3,
                IsSelectedToPlot = false,
                FaultType = FaultWavesInfoVM.FaultTypeEnum.GRID_PRD_BUS_VOLT_UNBALANCE_AVG,
                RecordTime = DateTime.MinValue.ToString("yyyy/MM/dd-HH:mm:ss.fff"),
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

        public ObservableCollection<FaultWavesInfoVM> FaultWavesInfoList { get; set; } = new();


        #endregion

        #region Plot
        private ScottPlot.WpfPlot _plotCtrl = new WpfPlot();
        public ScottPlot.WpfPlot PlotCtrl => _plotCtrl;

        private ScottPlot.Plottable.VLine _faultBeginVLine;
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
            PlotCtrl.Plot.RightAxis.LabelStyle(rotation: 90);

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

            _faultBeginVLine = PlotCtrl.Plot.AddVerticalLine(x: 0, color: System.Drawing.Color.Red, style: LineStyle.Dash);
            _faultBeginVLine.PositionLabel = true;
            _faultBeginVLine.PositionLabelOppositeAxis = true;
            _faultBeginVLine.PositionFormatter = d => d.ToString("N4");
            _faultBeginVLine.X = FaultTrigPosition / SamplingRate;
            _faultBeginVLine.IsVisible = false;

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
            _faultBeginVLine.IsVisible = true;
            PlotCtrl.Plot.AxisAuto();
            PlotCtrl.Refresh();
        }

        [RelayCommand]
        private void UnlockCurrentAxis(bool unlock)
        {
            PlotCtrl.Plot.LeftAxis.LockLimits(!unlock);
        }

        [RelayCommand]
        private void UnlockVoltageAxis(bool unlock)
        {
            PlotCtrl.Plot.RightAxis.LockLimits(!unlock);
        }

        #endregion

        #region Communication

        private bool _isReceiving = false;
        public bool IsReceiving
        {
            get => _isReceiving;
            set { _isReceiving = value; OnPropertyChanged(); }
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

                    if (ReadFaultWavesByIndex(dstAddr, fetchIndexList[i], out int faultTypeIndex, out int recordIndex,
                            out string recordTime, out List<List<double>> wavesList))
                    {
                        if (!IsReceiving)
                        {
                            return;
                        }

                        if (!RearrangeWaveData(ref wavesList, recordIndex))
                        {
                            ShowStatusMsg($"解析第{fetchIndexList[i]}个故障录波失败");
                            continue;
                        }

                        int curWaveIdx = fetchIndexList[i];
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            FaultWavesInfoList.Add(new FaultWavesInfoVM()
                            {
                                Index = curWaveIdx,
                                FaultType = (FaultWavesInfoVM.FaultTypeEnum)faultTypeIndex,
                                RecordTime = recordTime,
                                FaultWavesData = wavesList,
                            });
                            PlotCtrl.Refresh();
                            ShowStatusMsg($"读取第{curWaveIdx}个故障录波成功", timeMs: 5000);
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
                        FaultWavesInfoList = new ObservableCollection<FaultWavesInfoVM>(FaultWavesInfoList.OrderBy(elem => elem.Index).ToList());
                    IsReceiving = false;
                });

            });


        }


        private bool ReadFaultWavesByIndex(byte dstAddr, int waveIndex, out int faultTypeIndex, out int recordIndex,
                                                out string recordTime, out List<List<double>> wavesList)
        {
            faultTypeIndex = -1;
            recordIndex = -1;
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
            recordIndex = rxWaveInfoData[3] << 8 | rxWaveInfoData[2];
            int year = 2000 + rxWaveInfoData[4];
            int month = rxWaveInfoData[5];
            int day = rxWaveInfoData[6];
            int hour = rxWaveInfoData[7];
            int minute = rxWaveInfoData[8];
            
            int milliseconds = rxWaveInfoData[10] << 8 | rxWaveInfoData[9];
            int second = milliseconds / 1000;
            int millisecond = milliseconds % 1000;

            recordTime = $"{year:D4}/{month:D2}/{day:D2}-{hour:D2}:{minute:D2}:{second:D2}.{millisecond:D3}";

            MultiLanguages.Common.LogHelper.WriteLog($"第{waveIndex}号录波的录波信息: "
                                                     + $"故障类型【{faultTypeIndex}】；"
                                                     + $"故障起始索引号【{recordIndex}】；"
                                                     + $"录波时间【{recordTime.ToString()}】");
            remainSize -= FaultWaveInfoSize;
            ShowStatusMsg($"读取第{waveIndex}号录波的录波信息成功");

            // <2.2> 请求/接收录波数据
            int readTotalCount = remainSize / FaultWavePackSize;
            if (remainSize % FaultWavePackSize > 0)
            {
                readTotalCount += 1;
            }

            for (int i = 0; i < readTotalCount; i++)  // 请求数据块
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

                sbyte retry = 5;
                do
                {
                    retry--;

                    // Send request and get response
                    RxQueue.Clear();
                    if (!OscilloscopePageVM.SendFrameCan1(_ecanHelper, txFaultWaveCanID.ID, txFaultWaveData))
                    {
                        continue;
                    }

                    MultiLanguages.Common.LogHelper.WriteLog($"已请求第{waveIndex}号录波的录波数据块{readOffset}");


                    if (!OscilloscopePageVM.ReadFileDataFrames(RxQueue, rxFaultWaveCanID, readOffset,
                            FaultWavePackSize * 2,
                            out var rxWaveData, 5000)
                        || rxWaveData.Count != FaultWavePackSize * 2)
                    {
                        continue;
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
                    break;

                } while (retry > 0);

            }

            return true;
        }


        /// <summary>
        /// 在环形数组中整理录波前/后数据
        /// </summary>
        /// <param name="wavesDataList"></param>
        /// <param name="recordIndex"></param>
        private bool RearrangeWaveData(ref List<List<double>> wavesDataList, int recordIndex)
        {
            try
            {
                foreach (var wave in wavesDataList)
                {
                    var beforeFault = new List<double>(400);
                    var afterFault = new List<double>(200);
                    int faultWaveLen = FaultWaveTotalLength - FaultTrigPosition;

                    if (recordIndex + faultWaveLen > FaultWaveTotalLength)
                    {
                        afterFault.AddRange(wave.GetRange(recordIndex, FaultWaveTotalLength - recordIndex));
                        afterFault.AddRange(wave.GetRange(0, recordIndex + faultWaveLen - FaultWaveTotalLength));
                        beforeFault.AddRange(wave.GetRange(recordIndex + faultWaveLen - FaultWaveTotalLength, FaultTrigPosition));
                    }
                    else
                    {
                        afterFault.AddRange(wave.GetRange(recordIndex, faultWaveLen));
                        beforeFault.AddRange(wave.GetRange(recordIndex + faultWaveLen, FaultWaveTotalLength - recordIndex - faultWaveLen));
                        beforeFault.AddRange(wave.GetRange(0, recordIndex));
                    }

                    wave.Clear();
                    wave.AddRange(beforeFault);
                    wave.AddRange(afterFault);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

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

        [RelayCommand]
        private void ImportFaultWaves()
        {
            if (FaultWavesInfoList.Any() &&
                MessageBox.Show("导入故障录波文件将清空当前数据列表，是否继续？", "警告", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            FaultWavesInfoList.Clear();

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "选择故障录波文件";
            dlg.Filter = "fw(*.fw)|*.fw";
            dlg.Multiselect = true;
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != true)
                return;

            Task.Run(() => ParseFaultWavesData(dlg.FileNames));
        }

        private void ParseFaultWavesData(string[] faultWavesFiles)
        {
            var failedFiles = new List<string>();

            for (int n = 0; n < faultWavesFiles.Length; n++)
            {
                try
                {
                    var fwDataBytes = File.ReadAllBytes(faultWavesFiles[n]);

                    // 录波信息
                    var fwInfo = fwDataBytes.Take(FaultWaveInfoSize * 2).ToArray();
                    int faultTypeIndex = fwInfo[1] << 8 | fwInfo[0];
                    int recordIndex = fwInfo[3] << 8 | fwInfo[2];
                    int year = 2000 + fwInfo[4];
                    int month = fwInfo[5];
                    int day = fwInfo[6];
                    int hour = fwInfo[7];
                    int minute = fwInfo[8];

                    int milliseconds = fwInfo[10] << 8 | fwInfo[9];
                    int second = milliseconds / 1000;
                    int millisecond = milliseconds % 1000;
                    string recordTime = $"{year:D4}/{month:D2}/{day:D2}-{hour:D2}:{minute:D2}:{second:D2}.{millisecond:D3}";


                    // 录波数据点
                    fwDataBytes = fwDataBytes.Skip(FaultWaveInfoSize * 2).ToArray();

                    var wavesDataList = new List<List<double>>();
                    for (int i = 0; i < FaultWaveNum; i++)
                    {
                        wavesDataList.Add(new List<double>());
                    }

                    int readTotalCount = fwDataBytes.Length / (FaultWavePackSize * 2);
                    if (fwDataBytes.Length % FaultWavePackSize > 0)
                    {
                        readTotalCount += 1;
                    }

                    for (int i = 0; i < readTotalCount; i++)
                    {
                        var singlePack = fwDataBytes.Skip(i * FaultWavePackSize * 2).Take(FaultWavePackSize * 2).ToArray();
                        for (int p = 0; p < singlePack.Length; p += FaultWaveNum * 2)
                        {
                            for (int k = 0; k < FaultWaveNum; k++)
                            {
                                short data = (short)(singlePack[p + 2 * k + 1] << 8 | singlePack[p + 2 * k]);
                                wavesDataList[k].Add(data);
                            }
                        }
                    }

                    int curWaveIdx = n;

                    // 数据整理
                    if (!RearrangeWaveData(ref wavesDataList, recordIndex))
                    {
                        failedFiles.Add(faultWavesFiles[n]);
                        continue;
                    }

                    // 数据录入
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        FaultWavesInfoList.Add(new FaultWavesInfoVM()
                        {
                            Index = curWaveIdx,
                            FaultType = (FaultWavesInfoVM.FaultTypeEnum)faultTypeIndex,

                            RecordTime = recordTime,
                            FaultWavesData = wavesDataList,
                        });
                        PlotCtrl.Refresh();
                        ShowStatusMsg($"故障录波文件{Path.GetFileName(faultWavesFiles[curWaveIdx])}加载成功", timeMs: 1000);
                    });



                }
                catch (Exception ex)
                {
                    failedFiles.Add(faultWavesFiles[n]);

                }
            }

            if (failedFiles.Count > 0)
            {
                string failureMsg = "以下文件导入失败:\n";

                foreach (var filename in failedFiles)
                {
                    failureMsg += $"\t{Path.GetFileName(filename)}\n";
                }

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(failureMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }


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

        public DateTime RecordDateTime { get; set; }


        public FaultTypeEnum FaultType
        {
            get => _faultType;
            set
            {
                _faultType = value;
                OnPropertyChanged();
            }
        }
        private FaultTypeEnum _faultType;

        public string FaultTypeString
        {
            get => $"{FaultType.ToString()}[{((int)FaultType)}]";
        }

        public List<List<double>> FaultWavesData { get; set; }

        public enum FaultTypeEnum
        {  // -------CRITICAL FAULT--------
            CTRL_BUS_OVP_HW = 0,
            CTRL_OCP_HW,
            CTRL_GRID_OVP_INST_I,
            CTRL_GRID_OVP_INST_II,
            CTRL_BUS_OVP_INST,
            CTRL_BUS_UVP_INST,
            CTRL_AC_OCP_INST,
            CTRL_CBC,
            CTRL_SYNC_CARRIER,
            CTRL_MIDBALANCE_OCP_INST,
            CTRL_MIDBALANCE_OCP_HW,
            // -------FAST FAULT--------
            FAST_ISO_DET_FAULT,
            FAST_ISO_DET_WARNING,
            FAST_GFCI_FAULT,
            FAST_GFCI_DEVICE_FAULT_AC,
            FAST_ANTI_ISLAND_FAULT,
            FAST_BMS_EPO_FAULT,
            // -------RMS/AVG FAULT-------
            GRID_PRD_GRID_OVP_RMS_I,
            GRID_PRD_GRID_OVP_RMS_II,
            GRID_PRD_GRID_OVP_RMS_III,
            GRID_PRD_GRID_UVP_RMS_I,
            GRID_PRD_GRID_UVP_RMS_II,
            GRID_PRD_GRID_UVP_RMS_III,
            GRID_PRD_GRID_OFP_I,
            GRID_PRD_GRID_OFP_II,
            GRID_PRD_GRID_OFP_III,
            GRID_PRD_GRID_UFP_I,
            GRID_PRD_GRID_UFP_II,
            GRID_PRD_GRID_UFP_III,
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
            GRID_PRD_BUS_UG_DERATING,
            GRID_PRD_OBUS_DERATING,
            GRID_PRD_UBUS_DERATING,
            GRID_PRD_FAN_DERATING,
            GRID_PRD_OVOLT_DERATING,
            GRID_PRD_GRID_UNBALANCE,
            GRID_PRD_DC_RELAY_FEEDBACK_ERR,
            GRID_PRD_N_TO_PE_VOLT_FAULT,
            GRID_PRD_AMB_LOW_TEMP_FAULT,
            GRID_PRD_AMB_LOW_TEMP_WARN,
            GRID_PRD_OVER_MODULATION_FAULT,
            GRID_PRD_LVRT_OVER_TIME_FAULT,
            GRID_PRD_HVRT_OVER_TIME_FAULT,
            GRID_PRD_AD_DCI_CALI_FAULT,
            GRID_PRD_AD_IGRID_CALI_FAULT,
            GRID_PRD_AD_UGRID_CALI_FAULT_AC,
            GRID_PRD_IGBT_MIDBALANCE_OTP,
            GRID_PRD_MIDBALANCE_OCP_AVG,
            // -------SLOW FAULT-------
            SLOW_DC_RELAY_FAULT,
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
            SLOW_INNER_COMM_LOSE_FAULT,
            SLOW_EXTERNAL_CAN_LOST,
            SLOW_BLACK_START_FAIL_FAULT,
            SLOW_10MIN_GRID_OVP_FAULT,
            SLOW_CONSISTENT_UGRID,
            SLOW_BMS_WARNING,
            SLOW_BMS_FAULT,
            SLOW_SW_VERSION_FAULT,
            SLOW_BLACKSTART_OVERTIME_FAULT,
            SLOW_HWTOCPLD_VERSION_FAULT,
            SLOW_GRIDOFF_OVERLOAD_FAULT,
            SLOW_EEPROM_READING_FAULT,
            DIAG_ARRAY_END,
            DIAG_ARRAY_SIZE = DIAG_ARRAY_END,
        }

    }
}
