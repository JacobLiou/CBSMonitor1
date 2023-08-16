using CanProtocol;
using CanProtocol.ProtocolModel;
using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontAwesome.Sharp;
using Microsoft.Xaml.Behaviors;
using SofarHVMDAL;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.FileOperate;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using static SofarHVMExe.Utilities.Global.GlobalManager;
using static System.Net.Mime.MediaTypeNames;
using MessageBox = System.Windows.Forms.MessageBox;
using SofarHVMExe.SubPubEvent;
using SofarHVMExe.SubPubEvent.Events;
using NPOI.SS.Formula.Functions;
using System.Windows.Threading;
using NPOI.Util;
using ScottPlot.Drawing.Colormaps;

namespace SofarHVMExe.ViewModel
{
    partial class MonitorPageVm : ObservableObject
    {
        /// <summary>
        /// 监控界面ViewModel
        /// </summary>
        public MonitorPageVm(EcanHelper? helper, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            ecanHelper = helper;
            Init();
        }


        #region 字段 
        private readonly IEventAggregator _eventAggregator;
        private int sendInterval = 500; //发送帧间隔，默认1ms
        private FileConfigModel? fileCfgModel = null;
        private FrameConfigModel? frameCfgModel = null;
        public EcanHelper? ecanHelper = null;
        private List<CancellationTokenSource> sendCancellList = new List<CancellationTokenSource>(); //发送取消标志
        private StringBuilder msgInfoSb = new StringBuilder();
        private DownloadDebugInfoWnd? msgInfoWnd = null;
        private string recvTime = "";
        //private List<string> faultWarningBuffers = new List<string>(); 
        private List<CurrentEventModel> faultWarningBuffers = new List<CurrentEventModel>();
        #endregion


        #region 属性
        public bool IsShowSend { get; set; }    //显示发送
        public bool IsShowRecieve { get; set; }  //显示接收
        public bool IsScrollDisplay { get; set; } //滚动显示

        [ObservableProperty]
        private string faultInfo = "";

        //public int cmdGroupSelectedIndex = -1;
        //public int CmdGroupSelectedIndex
        //{
        //    get => cmdGroupSelectedIndex;
        //    set
        //    {
        //        if (cmdGroupSelectedIndex != value && value >= 0)
        //        {
        //            cmdGroupSelectedIndex = value;
        //            OnPropertyChanged();
        //            UpdateCmdDataSrc(cmdGroupSelectedIndex);
        //        }
        //    }
        //}

        //private List<string> cmdGroupList = new List<string>();
        //public List<string> CmdGroupList
        //{
        //    get => cmdGroupList;
        //    set
        //    {
        //        cmdGroupList = value;
        //        OnPropertyChanged();
        //    }
        //}


        [ObservableProperty]
        private List<CmdGrpConfigModel> cmdGroupList = new List<CmdGrpConfigModel>();

        private CmdGrpConfigModel selectCmdGrpModel = null;
        public CmdGrpConfigModel SelectCmdGrpModel
        {
            get => selectCmdGrpModel;
            set
            {
                if (selectCmdGrpModel != value)
                {
                    selectCmdGrpModel = value;
                    OnPropertyChanged();
                    UpdateCmdDataSrc();
                }
            }
        }

        public CmdConfigModel? SelectCmdCfgModel { get; set; }

        private BindingList<CmdConfigModel> cmdDataSource = null;
        public BindingList<CmdConfigModel> CmdDataSource
        {
            get => cmdDataSource;
            set
            {
                cmdDataSource = value;
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        bool sortByCfg = true;

        [ObservableProperty]
        private ObservableCollection<CanFrameDataInfo> specialFrameInfo = new ObservableCollection<CanFrameDataInfo>();

        [ObservableProperty]
        private ObservableCollection<SendRecvFrameInfo> frameInfoDataSrc = new ObservableCollection<SendRecvFrameInfo>();

        private List<CurrentEventModel> _currentEventList = new List<CurrentEventModel>();
        public List<CurrentEventModel> CurrentEventList
        {
            get { return _currentEventList; }
            set { _currentEventList = value; OnPropertyChanged(); }
        }
        #endregion


        #region 命令
        public ICommand StartSendCommand { get; set; }      //开始发送
        public ICommand StopSendCommand { get; set; }       //停止发送
        public ICommand SendSelDataCommand { get; set; }    //发送选择的命令
        public ICommand SetDataCommand { get; set; }        //设置选择命令的数据
        public ICommand ShowMsgInfoCommand { get; set; }    //显示报文信息
        public ICommand SaveSetValueCommand { get; set; }   //保存设置值
        #endregion


        #region 成员方法
        private void Init()
        {
            StartSendCommand = new SimpleCommand(StartSend);
            StopSendCommand = new SimpleCommand(StopSend);
            SendSelDataCommand = new SimpleCommand(SendSelData);
            SetDataCommand = new SimpleCommand(SetData);
            ShowMsgInfoCommand = new SimpleCommand(ShowMsgInfo);
            SaveSetValueCommand = new SimpleCommand(SaveSetValue);

            IsShowSend = true;
            IsShowRecieve = true;
            IsScrollDisplay = true;

            UpdateModel();
            StartAutoSend(); //自动发送线程
            StartWriteFaultLog(); //自动写故障告警信息线程

            //StartFaultTask(); //自动清故障显示信息线程
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
                frameCfgModel = fileCfgModel.FrameModel;
                sendInterval = fileCfgModel.PrjModel.SendInterval;

                //临时启用，给旧的json配置设置命令组索引，在更高版本删除
                fileCfgModel.UpdateCmdIndex();

                //更新命令组
                //List<string> tmpList = new List<string>();
                List<CmdGrpConfigModel> tmpList = new List<CmdGrpConfigModel>();
                foreach (CmdGrpConfigModel model in fileCfgModel.CmdModels)
                {
                    if (model.Name != "")
                    {
                        tmpList.Add(model);
                    }
                }
                CmdGroupList = tmpList;
                //命令组更新，选中命令须引用新的命令组元素
                SelectCmdGrpModel = CmdGroupList.FirstOrDefault(model => model.Index == SelectCmdGrpModel?.Index);
            }

            UpdateCmdDataSrc();
        }
        /// <summary>
        /// 更新命令表数据源
        /// </summary>
        /// <param name="cmdGrpIndex">命令组索引</param>
        private void UpdateCmdDataSrc()
        {
            if (fileCfgModel == null || selectCmdGrpModel == null)
            {
                CmdDataSource = null;
                return;
            }

            List<CmdConfigModel> list = new List<CmdConfigModel>();
            CmdGrpConfigModel grpCfgModel = fileCfgModel.CmdModels[selectCmdGrpModel.Index];
            foreach (CmdConfigModel model in grpCfgModel.cmdConfigModels)
            {
                //禁止命令不加入
                if (model.CmdType != 0)
                {
                    list.Add(model);
                }
            }

            CmdDataSource = new BindingList<CmdConfigModel>(list.ToArray());
            CmdDataSource.ListChanged += CmdDataSource_ListChanged;
        }
        private void CmdDataSource_ListChanged(object? sender, ListChangedEventArgs e)
        {
            SaveData();
        }
        private void StartSend(object o)
        {
            if (!CheckConnect())
                return;

            if (cmdDataSource == null)
                return;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            sendCancellList.Add(cancellationTokenSource);

            Task.Run(() =>
            {
                foreach (CmdConfigModel cmd in cmdDataSource)
                {
                    cmd.SetValue2FrameDatas(); //设置值转字段信息
                    CanFrameModel frame = cmd.FrameModel;
                    SendFrame(frame);
                    Thread.Sleep(sendInterval);

                    if (cancellationTokenSource.IsCancellationRequested)
                        break;
                }
            });
        }
        private void StopSend(object o)
        {
            foreach (CancellationTokenSource cancell in sendCancellList)
            {
                cancell?.Cancel();
            }

            sendCancellList.Clear();
        }
        private void SendSelData(object o)
        {
            //if (!CheckConnect())
            //    return;

            if (SelectCmdCfgModel == null || SelectCmdCfgModel.FrameModel == null)
                return;


            ////打开数据设置对话框
            //if (SelectCmdCfgModel.FrameModel.FrameId.ContinuousFlag==1)
            //{
            //    CANFrameDataEditVm vm = new CANFrameDataEditVm(fileCfgModel, SelectCmdCfgModel);
            //    CANFrameDataEditWnd wnd = new CANFrameDataEditWnd();
            //    byte continueFlg = SelectCmdCfgModel.FrameModel.FrameId.ContinuousFlag;
            //    vm.isContinue = continueFlg == 0 ? false : true;
            //    wnd.DataContext = vm;
            //    wnd.ShowDialog();
            //}

            var frameId = SelectCmdCfgModel.FrameModel.FrameId;
            if (SelectCmdCfgModel.FrameModel.Name.Contains("校时"))
            {
                string dateStr = System.DateTime.Now.ToString("yy-MM-dd HH:mm:ss");
                string[] date = dateStr.Split(new char[] { ' ', '-', ':' });

                string setVal = "";
                for (int i = 0; i < date.Length; i++)
                {
                    setVal += date[i].ToString();
                    if (i != date.Length - 1)
                        setVal += ",";
                }
                SelectCmdCfgModel.SetValue = setVal;
            }
            SelectCmdCfgModel.SetValue2FrameDatas(); //设置值转字段信息
            CanFrameModel frame = new CanFrameModel(SelectCmdCfgModel.FrameModel);

            //指定发送数据给某台设备
            if (selectCmdGrpModel?.IsBroadcast == false) //广播命令不指定设备地址
            {
                byte addr = DeviceManager.Instance().GetSelectDev();
                if (addr != 0)
                {
                    if (frame.FrameId.FC != 0x39) //组播（功能码）不走地址设置
                    {
                        frame.FrameId.DstAddr = addr;
                    }
                }
            }

            SendFrame(frame);
        }
        private void SetData(object o)
        {
            if (SelectCmdCfgModel == null)
                return;

            if (SelectCmdCfgModel.FrameModel == null)
            {
                MessageBox.Show("该命令没有对应帧！", "提示");
                return;
            }

            //打开数据设置对话框
            CANFrameDataEditVm vm = new CANFrameDataEditVm(fileCfgModel, SelectCmdCfgModel);
            CANFrameDataEditWnd wnd = new CANFrameDataEditWnd();
            byte continueFlg = SelectCmdCfgModel.FrameModel.FrameId.ContinuousFlag;
            vm.isContinue = continueFlg == 0 ? false : true;
            wnd.DataContext = vm;
            wnd.ShowDialog();
        }

        [RelayCommand]
        private void ClearData()
        {
            FrameInfoDataSrc.Clear();
            SpecialFrameInfo.Clear();
        }
        public void ClearDataByUnSelectDev(Device dev)
        {
            //if (!dev.Selected)
            {
                FrameInfoDataSrc.Clear();
            }
        }
        private void ShowMsgInfo(object o)
        {
            if (msgInfoWnd == null)
            {
                msgInfoWnd = new DownloadDebugInfoWnd();
                msgInfoWnd.SetTitle("收发报文信息");
            }

            string debugText = msgInfoSb.ToString();
            msgInfoWnd.UpdateInfo(debugText);
            msgInfoWnd.Topmost = true;
            msgInfoWnd.Show();
        }
        private void SaveSetValue(object o)
        {
            CmdGrpConfigModel cmdGrpConfigModel = fileCfgModel.CmdModels[selectCmdGrpModel.Index];
            cmdGrpConfigModel.cmdConfigModels = CmdDataSource.ToList();

            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                MessageBox.Show("保存失败！", "提示");
            }
        }
        private void StartAutoSend()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    //fileCfgModel = JsonConfigHelper.ReadConfigFile();
                    if (fileCfgModel == null || ecanHelper == null || !CheckConnect(false))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    //如果自动发送的帧，在命令中，要使用命令设置的值
                    var frameModels = fileCfgModel.FrameModel.CanFrameModels;
                    //List<CanFrameModel> frameList = frameModels;
                    List<CanFrameModel> frameList = new List<CanFrameModel>(frameModels);
                    foreach (CanFrameModel frame in frameList)
                    {
                        if (frame.AutoTx)
                        {
                            //指定发送数据给某台设备
                            byte addr = DeviceManager.Instance().GetSelectDev();
                            if (addr != 0)
                            {
                                if (frame.FrameId.FC != 0x39) //组播（功能码）不走地址设置
                                {
                                    frame.FrameId.DstAddr = addr;
                                }
                            }

                            SendFrame(frame);
                            Thread.Sleep(sendInterval);
                        }
                    }

                    Thread.Sleep(100);
                }
            });
        }
        private void AddDebugInfo(string info, bool addArrow = true, bool isUpdate = false)
        {
            if (addArrow)
            {
                info = ">" + info + "\r\n";
            }
            else
            {
                info = info + "\r\n";
            }

            if (isUpdate)
            {
                msgInfoSb.Clear();
            }

            msgInfoSb.Append(info);
        }
        private void StartWriteFaultLog()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (faultWarningBuffers.Count > 0)
                    {
                        bool locked = false;

                        try
                        {
                            lock (TxtFileHelper._fileLock)
                            {
                                //写入故障告警信息到log文件中
                                for (int i = 0; i < faultWarningBuffers.Count; i++)
                                {
                                    string text = WriteFault(faultWarningBuffers[i]);
                                    LogHelper.FaultAndWarning(text);
                                    _eventAggregator.Publish(text + "\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), new string[] { "FaultAndWarning" });


                                    faultWarningBuffers.Remove(faultWarningBuffers[i]);
                                }
                                //List<string> buffers = new List<string>(faultWarningBuffers);
                                //faultWarningBuffers.Clear();
                                //foreach (var text in buffers)
                                //{
                                //    LogHelper.FaultAndWarning(text);
                                //}
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                        }
                    }

                    Thread.Sleep(5000);
                }
            });
        }



        private string WriteFault(CurrentEventModel pmodel)
        {
            string text = "";

            //通过CANID来解析ID
            CanFrameID frameId = new CanFrameID();
            frameId.ID = pmodel.CANId;
            text += frameId.SrcAddr;

            EventInfoModel model = pmodel.eventInfoModel;
            switch (model.Type)
            {
                case EventType.None:
                    break;
                case EventType.Status:
                    text += $"[状态] {model.Name} {model.Mark}";
                    break;
                case EventType.Exception:
                    text += $"[告警] {model.Name} {model.Mark}";
                    break;
                case EventType.Fault:
                    text += $"[故障] {model.Name} {model.Mark}";
                    break;
                default:
                    break;
            }

            return text;
        }

        [RelayCommand]
        private void SaveData()
        {
            fileCfgModel.CmdModels[selectCmdGrpModel.Index].cmdConfigModels = CmdDataSource.ToList();
            if (JsonConfigHelper.WirteConfigFile(fileCfgModel))
            {
                //MessageBox.Show("保存成功！", "提示");
            }
            else
            {
                //MessageBox.Show("保存失败！", "提示");
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
                    //Task.Run(() =>
                    //{
                    ProtocolHelper.AnalyseFrameModel(frame);
                    SendSingleFrame(id, frameData.Data, frameData);
                    //});
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

        private void RecvProcCan1(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Monitor)
                return;

            if (!IsShowRecieve)
                return;

            RecvFrame(recvData);
        }
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Monitor)
                return;

            if (!IsShowRecieve)
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

            if ((id >> 16) == 0x197F)//心跳帧特殊处理
            {
                if ((devAddr != 0 && frameId.SrcAddr != devAddr) || recvData.Data.Length != 8)
                {
                    return;
                }
                for (int i = 0; i < 2; i++)
                {
                    SendRecvFrameInfo newFrameInfo = new SendRecvFrameInfo();
                    newFrameInfo.IsSend = false;
                    newFrameInfo.Time = currentTime;
                    newFrameInfo.ID = "0x" + id.ToString("X8");
                    newFrameInfo.IsContinue = false;
                    newFrameInfo.Addr = "0";
                    newFrameInfo.PackageNum = i.ToString();
                    if (i == 0)
                    {
                        newFrameInfo.Info1 = "PCS运行模式";
                        newFrameInfo.Value1 = BitConverter.ToInt16(recvData.Data, 0).ToString();
                        newFrameInfo.Info2 = "故障信息";
                        newFrameInfo.Value2 = recvData.Data[2] == 1 ? "有" : "无";
                        newFrameInfo.Info3 = "状态机主状态";
                        newFrameInfo.Value3 = recvData.Data[3].ToString();
                    }
                    else
                    {
                        newFrameInfo.Info1 = "降额后的发电能力";
                        newFrameInfo.Value1 = (BitConverter.ToInt16(recvData.Data, 4) * 0.01).ToString() + " kW";
                        newFrameInfo.Info2 = "降额后的充电能力";
                        newFrameInfo.Value2 = (BitConverter.ToInt16(recvData.Data, 6) * 0.01).ToString() + "kW";
                    }
                    UpdateContinueRecv(newFrameInfo);
                }
            }
            else if (frameId.ContinuousFlag == 0)
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
            }
        }
        #endregion

        private void UpdateRecv(CAN_OBJ recvData, CanFrameModel frame)
        {
            if (frame.FrameDatas.Count <= 0)
                return;

            CanFrameData frameData = frame.FrameDatas[0];
            ProcPresion(frameData);
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

            UpdateRecv(newFrameInfo);

            //处理错误信息
            ProcFaultInfo(frame);
        }
        private void UpdateRecv(SendRecvFrameInfo newFrameInfo)
        {
            List<SendRecvFrameInfo> frameInfoList = new List<SendRecvFrameInfo>(FrameInfoDataSrc);
            List<SendRecvFrameInfo> sendList = new List<SendRecvFrameInfo>();
            List<SendRecvFrameInfo> recvList = new List<SendRecvFrameInfo>();
            foreach (var frameInfo in frameInfoList)
            {
                if (frameInfo.IsSend)
                {
                    sendList.Add(frameInfo);
                }
                else
                {
                    recvList.Add(frameInfo);
                }
            }

            List<SendRecvFrameInfo> operateList = newFrameInfo.IsSend ? sendList : recvList;

            //对于单包 相同id和点表起始地址的就替换 否则增加
            string id = newFrameInfo.ID;
            string addr = newFrameInfo.Addr;
            int findIndex = operateList.FindIndex((o) =>
            {
                if (id == o.ID && addr == o.Addr)
                    return true;
                else
                    return false;
            });

            if (findIndex != -1)
            {
                operateList[findIndex] = newFrameInfo;
            }
            else
            {
                operateList.Add(newFrameInfo);
            }

            frameInfoList.Clear();
            frameInfoList.AddRange(recvList);
            frameInfoList.AddRange(sendList);

            UpdateMsgDisplay(frameInfoList);
        }//func
        private void UpdateContinueRecv(CanFrameModel frame)
        {
            if (frame.FrameDatas.Count <= 0)
                return;

            //List<byte> datas = new List<byte>(frame.FrameDatas[0].Data);
            CanFrameData frameData = frame.FrameDatas[0];
            ProcPresion(frameData);
            List<CanFrameDataInfo> dataInfos = new List<CanFrameDataInfo>(frameData.DataInfos);  //使用第一包数据
            string addr = frame.GetAddrInt().ToString();

            //去除包序号、起始地址、个数、CRC不显示
            {
                dataInfos.RemoveAt(0); //包序号
                //datas.RemoveAt(0);
                dataInfos.RemoveAt(0); //起始地址
                //datas.RemoveRange(0,2);
                dataInfos.RemoveAt(0); //个数
                //datas.RemoveAt(0);

                //CRC
                CanFrameDataInfo? crcInfo = dataInfos.Find((o) =>
                {
                    return (o.Name.ToLower().Contains("crc"));
                });

                if (crcInfo != null)
                {
                    dataInfos.Remove(crcInfo);
                }

                //合并同类字段
                for (int i = 0; i < dataInfos.Count - 1; i++)
                {
                    if (dataInfos[i].Type == "string" && !dataInfos[i].Hide
                        && dataInfos[i].Name.Trim().EndsWith('1'))
                    {
                        int index = 2;
                        string name1 = dataInfos[i].Name.Trim();
                        string name2 = dataInfos[i + 1].Name.Trim();
                        string name = name1.Substring(0, name1.Length - 1);
                        while (i + 1 < dataInfos.Count && name2.EndsWith(index.ToString())
                            && name2.TrimEnd(index.ToString().ToCharArray()) == name)
                        {
                            dataInfos[i].Value += dataInfos[i + 1].Value;
                            dataInfos[i].ByteRange += 2;
                            dataInfos.RemoveAt(i + 1);
                            name2 = dataInfos[i + 1].Name.Trim();
                            index++;
                        }
                        if (index > 2)
                        {
                            dataInfos[i].Name = name;
                            bool found = false;
                            for (int j = 0; j < SpecialFrameInfo.Count; j++)
                            {
                                if (SpecialFrameInfo[j].Name == name)
                                {
                                    found = true;
                                    if (SpecialFrameInfo[j].Value != dataInfos[i].Value)
                                    {
                                        SpecialFrameInfo[j] = dataInfos[i];
                                    }
                                    break;
                                }
                            }
                            if (!found)
                            {
                                SpecialFrameInfo.Add(dataInfos[i]);
                            }
                            SpecialFrameInfo = new ObservableCollection<CanFrameDataInfo>(SpecialFrameInfo);
                            dataInfos.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            uint id = frame.Id;
            byte[] datas = frame.FrameDatas[0].Data;
            int showInfoNum = 4; //一行显示的字段数量
            int num = dataInfos.Count / showInfoNum;
            num += (dataInfos.Count % showInfoNum) > 0 ? 1 : 0;
            for (int i = 0; i < num; i++)
            {
                CanFrameDataInfo[] infos = dataInfos.Skip(i * showInfoNum).Take(showInfoNum).ToArray();

                SendRecvFrameInfo newFrameInfo = new SendRecvFrameInfo();
                newFrameInfo.IsSend = false;
                newFrameInfo.Time = recvTime;
                newFrameInfo.ID = "0x" + id.ToString("X8");
                newFrameInfo.Datas = BitConverter.ToString(datas);

                newFrameInfo.IsContinue = frame.FrameId.ContinuousFlag == 1;
                newFrameInfo.Addr = addr;
                newFrameInfo.PackageNum = i.ToString();

                int count = infos.Length;
                if (count > 0 && !infos[0].Hide)
                {
                    newFrameInfo.Info1 = infos[0].Name;
                    newFrameInfo.Value1 = $"{infos[0].Value} {infos[0].Unit}";
                }
                if (count > 1 && !infos[1].Hide)
                {
                    newFrameInfo.Info2 = infos[1].Name;
                    newFrameInfo.Value2 = $"{infos[1].Value} {infos[1].Unit}";
                }
                if (count > 2 && !infos[2].Hide)
                {
                    newFrameInfo.Info3 = infos[2].Name;
                    newFrameInfo.Value3 = $"{infos[2].Value} {infos[2].Unit}";
                }
                if (count > 3 && !infos[3].Hide)
                {
                    newFrameInfo.Info4 = infos[3].Name;
                    newFrameInfo.Value4 = $"{infos[3].Value} {infos[3].Unit}";
                }

                UpdateContinueRecv(newFrameInfo);
            }

            //处理错误信息
            ProcFaultInfo(frame);
        }
        private void UpdateContinueRecv(SendRecvFrameInfo newFrameInfo)
        {
            List<SendRecvFrameInfo> frameInfoList = new List<SendRecvFrameInfo>(FrameInfoDataSrc);
            List<SendRecvFrameInfo> sendList = new List<SendRecvFrameInfo>();
            List<SendRecvFrameInfo> recvList = new List<SendRecvFrameInfo>();
            foreach (var frameInfo in frameInfoList)
            {
                if (frameInfo.IsSend)
                {
                    sendList.Add(frameInfo);
                }
                else
                {
                    recvList.Add(frameInfo);
                }
            }

            List<SendRecvFrameInfo> operateList = newFrameInfo.IsSend ? sendList : recvList;

            //对于单包 相同id和点表起始地址的就替换 否则增加
            string id = newFrameInfo.ID;
            string addr = newFrameInfo.Addr;
            string packageNum = newFrameInfo.PackageNum;
            int findIndex = operateList.FindIndex((o) =>
            {
                if (id == o.ID &&
                    addr == o.Addr &&
                    packageNum == o.PackageNum)
                    return true;
                else
                    return false;
            });

            if (findIndex != -1)
            {
                operateList[findIndex] = newFrameInfo;
            }
            else
            {
                operateList.Add(newFrameInfo);
            }

            frameInfoList.Clear();
            frameInfoList.AddRange(recvList);
            frameInfoList.AddRange(sendList);

            //变更源数据，DESC排序
            frameInfoList = frameInfoList.OrderBy(X => X.ID).ToList();
            UpdateMsgDisplay(frameInfoList);
        }//func

        /// <summary>
        /// 更新界面帧数据显示
        /// </summary>
        /// <param name="frameInfos"></param>
        private void UpdateMsgDisplay(List<SendRecvFrameInfo> frameInfos)
        {
            try
            {
                //不是当前界面则不更新显示
                if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Monitor)
                    return;

                List<SendRecvFrameInfo> sortFrameInfos = frameInfos;
                if (sortByCfg)
                {
                    sortFrameInfos = SortByFrameCfg(frameInfos);
                }

                //子线程更新发送/接收信息数据表
                //Task.Factory.StartNew(() => //这里不能用多线程和BeginInvoke，会导致多帧的接收数据在界面重叠到一行
                //{

                if (!CheckConnect())
                    return;

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    FrameInfoDataSrc = new ObservableCollection<SendRecvFrameInfo>(sortFrameInfos);
                });
                //});

            }
            catch (Exception ex)
            {
                Console.WriteLine("error:" + ex.Message);
            }
        }

        /// <summary>
        /// 显示数据重新排序
        /// 按照命令顺序
        /// </summary>
        /// <param name="frameInfos"></param>
        /// <returns></returns>
        private List<SendRecvFrameInfo> SortByFrameCfg(List<SendRecvFrameInfo> frameInfos)
        {
            List<SendRecvFrameInfo> results = new List<SendRecvFrameInfo>();
            if (fileCfgModel == null)
                return results;

            var frameModels = fileCfgModel.FrameModel.CanFrameModels;
            if (frameModels.Count == 0)
                return frameInfos;

            List<CanFrameModel> fms = new List<CanFrameModel>(frameModels);

            //按帧配置顺序收集接收信息
            foreach (var fm in fms)
            {
                string modelId = ("0x" + fm.Id.ToString("X8")).Substring(0, 6);
                for (int i = 0; i < frameInfos.Count; i++)
                {
                    var sendRecvFrameInfo = frameInfos[i];
                    string frameInfoId = sendRecvFrameInfo.ID.Substring(0, 6);
                    if (modelId == frameInfoId)
                    {
                        results.Add(sendRecvFrameInfo);
                        frameInfos.RemoveAt(i);
                        i--;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 处理精度
        /// </summary>
        /// <param name="frameData"></param>
        private void ProcPresion(CanFrameData frameData)
        {
            foreach (var dataInfo in frameData.DataInfos)
            {
                if (dataInfo.Type.Contains("float") && dataInfo.Precision != null)
                {
                    //按精度有几位小数，则值也保留几位小数
                    string value = dataInfo.Value;
                    string presion = dataInfo.Precision.ToString();
                    string[] presions = presion.Split('.');
                    if (presions.Length != 2)
                        continue;

                    int num = presions[1].Length;
                    string[] values = value.Split(".");
                    if (values.Length == 2)
                    {
                        dataInfo.Value = $"{values[0]}.{values[1].Substring(0, num)}";
                    }
                }
            }
        }

        /// <summary>
        /// 处理故障信息
        /// </summary>
        /// <param name="frame"></param>
        private void ProcFaultInfo(CanFrameModel frame)
        {
            //UpdateFaultDisplay("");

            //1、查找该帧是否记录在事件组中
            string guid = frame.Guid;
            List<EventGroupModel> findModels = fileCfgModel.EventModels.FindAll(model =>
            {
                return (model.FrameGuid == guid);
            });

            if (findModels.Count == 0)
                return;

            //2、收集多个故障告警信息
            List<EventInfoModel> events = new List<EventInfoModel>();
            foreach (var model in findModels)
            {
                events.AddRange(CollectFualtInfo(model, frame));
            }
            if (events.Count == 0)
            {
                UpdateFaultDisplay("");
                return;
            }

            //3、收集和显示故障告警信息到界面
            string faultMsg = "";
            events.ForEach(model => { if (model.Type == EventType.Fault) faultMsg += $"{model.Name}, "; });
            faultMsg = faultMsg.TrimEnd().TrimEnd(',');
            UpdateFaultDisplay(faultMsg);

            //4、保存故障告警到log文件
            List<EventInfoModel> _eventInfos = new List<EventInfoModel>();
            foreach (var model in events)
            {
                //添加到当前事件列表
                bool _find = false;
                foreach (var item in CurrentEventList)
                {
                    if (item.eventInfoModel == model)
                    {
                        _find = true;
                        break;
                    }
                }
                if (!_find)
                {
                    CurrentEventModel _model = new CurrentEventModel();
                    _model.CANId = frame.Id;
                    _model.eventInfoModel = model;
                    CurrentEventList.Add(_model);

                    faultWarningBuffers.Add(_model);
                }


                //string text = "";

                //switch (model.Type)
                //{
                //    case EventType.None:
                //        break;
                //    case EventType.Status:
                //        text += $"[状态] {model.Name} {model.Mark}";
                //        break;
                //    case EventType.Exception:
                //        text += $"[告警] {model.Name} {model.Mark}";
                //        break;
                //    case EventType.Fault:
                //        text += $"[故障] {model.Name} {model.Mark}";
                //        break;
                //    default:
                //        break;
                //}

                //if (text != "")
                //{
                //    faultWarningBuffers.Add(text);
                //}
            }

            //5、排除现有的类别故障已恢复，与上次上报数据信息比对
            for (int i = 0; i < CurrentEventList.Count; i++)//foreach (var item in CurrentEventList)
            {
                var reuslt = events.Exists(c => c == CurrentEventList[i].eventInfoModel);
                if (!reuslt)
                {
                    CurrentEventList.Remove(CurrentEventList[i]);
                }
            }
            //订阅传参
            _eventAggregator.Publish(new LogInfiEvent(CurrentEventList));
        }

        private List<EventInfoModel> CollectFualtInfo(EventGroupModel eventGrpModel, CanFrameModel frame)
        {
            List<EventInfoModel> result = new List<EventInfoModel>();
            if (!eventGrpModel.Enable || eventGrpModel.MemberIndex == -1)
                return result;

            //1、解析字段信息中的故障（该位为1，并且设置了事件故障）
            ObservableCollection<CanFrameDataInfo> dataInfos = frame.FrameDatas[0].DataInfos;
            if (eventGrpModel.MemberIndex >= dataInfos.Count) //记录成员超出字段信息范围（当修改帧字段后会出现这种情况）
                return result;

            CanFrameDataInfo dataInfo = dataInfos[eventGrpModel.MemberIndex];
            List<byte> datas = ProtocolHelper.AnalyseDataInfo(dataInfo);
            int value = 0;
            for (int i = 0; i < datas.Count; i++)
            {
                byte d = datas[i];
                value += d << (i * 8);
            }

            int bitCount = eventGrpModel.InfoModels.Count;
            if (bitCount > (datas.Count * 8))
            {
                bitCount = datas.Count * 8;
            }

            //2、比对字节中的位，是否匹配事件故障 收集故障告警信息
            for (int i = 0; i < bitCount; i++)
            {
                EventInfoModel model = eventGrpModel.InfoModels[i];
                if (model.Enable)
                {
                    int d = value & (0x1 << i);
                    if (d > 0)
                    {
                        result.Add(model);
                    }
                }
            }

            return result;
        }//func

        /// <summary>
        /// 更新界面故障信息显示
        /// </summary>
        /// <param name="faultMsg"></param>
        public void UpdateFaultDisplay(string faultMsg)
        {
            //不是当前界面则不更新显示
            if (GlobalManager.Instance().CurrentPage != GlobalManager.Page.Monitor)
                return;

            if (FaultInfo == faultMsg) return;// && FaultInfo == faultMsg
            //if (faultMsg == "") return;// && FaultInfo == faultMsg

            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                FaultInfo = faultMsg;
            });
        }

        private void StartFaultTask()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    UpdateFaultDisplay("");
                }
            });
        }


        #endregion

    }//class
}
