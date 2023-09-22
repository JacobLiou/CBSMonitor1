using CanProtocol;
using CanProtocol.ProtocolModel;
using Org.BouncyCastle.Ocsp;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Communication.Can
{
    public class EcanHelper : CommBase<CAN_OBJ>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public EcanHelper()
        {
        }

        public enum Bauds
        {
            _1000kbps,
            _800kbps,
            _666kbps,
            _500kbps,
            _400kbps,
            _250kbps,
            _200kbps,
            _125kbps,
            _100kbps,
            _80kbps,
            _50kbps,
            _40kbps,
            _20kbps,
            _10kbps,
            _5kbps
        }

        #region 1、字段
        private static UInt32 devType = 1;//设备类型号。USBCAN I 选择3，USBCAN II 选择4。
        private static UInt32 devIndex = 0;//设备索引号
        private static UInt32 canIndex = 0;//第几路CAN  0：第1路  1：第2路
        private UInt32 accCode = 0;//验收码
        private UInt32 accMask = 0xffffffff;//屏蔽码
        private byte filter = 0;//滤波使能。0：不使能  1：使能
        private byte mode = 0;//模式。0：正常模式  1：只听模式  2：自发自收模式
        private static byte sendType = 0;//发送帧类型，0：正常发送  1：单次发送  2：自发自收  3：单次自发自收
        private static byte externFlag = 1;//是否是扩展帧
        private static byte remoteFlag = 0;//是否是远程帧

        private bool isRecvStartCan1 = false; //CAN1接收线程开启
        private bool isRecvStartCan2 = false; //CAN2接收线程开启
        private uint errcode = 0;
        #endregion

        #region 2、属性
        public Bauds BaudCan1 = Bauds._500kbps; //CAN1波特率
        public Bauds BaudCan2 = Bauds._500kbps; //CAN2波特率
        public uint DevType { get => devType; set => devType = value; }
        public uint DevIndex { get => devIndex; set => devIndex = value; }
        public uint CanIndex { get => canIndex; set => canIndex = value; }
        public uint AccCode { get => accCode; set => accCode = value; }
        public uint AccMask { get => accMask; set => accMask = value; }
        public byte Filter { get => filter; set => filter = value; }
        public byte Mode { get => mode; set => mode = value; }
        public byte SendType { get => sendType; set => sendType = value; }
        public byte ExternFlag { get => externFlag; set => externFlag = value; }
        public byte RemoteFlag { get => remoteFlag; set => remoteFlag = value; }

        private bool isConnected = false;
        public override bool IsConnected
        {
            get => isConnected;
            set
            {
                isConnected = value;
                OnPropertyChanged();
            }
        }
        //public Action<CAN_OBJ> OnReceiveCan1 { get; set; }
        //public Action<CAN_OBJ> OnReceiveCan2 { get; set; }
        public List<ConcurrentQueue<CAN_OBJ>> RecvDataQueuesCan1 { get; set; } = new List<ConcurrentQueue<CAN_OBJ>>();
        public List<ConcurrentQueue<CAN_OBJ>> RecvDataQueuesCan2 { get; set; } = new List<ConcurrentQueue<CAN_OBJ>>();
        //public CAN_OBJ RecvDataCan1 { get; set; }
        //public CAN_OBJ RecvDataCan2 { get; set; }
        public bool IsCan1Start { get; set; }
        public bool IsCan2Start { get; set; }
        public CancellationTokenSource CancellationTokenSource1 { get; set; }
        public CancellationTokenSource CancellationTokenSource2 { get; set; }
        #endregion

        #region 3、方法
        [Obsolete("该方法已被弃用，请使用 OpenDevice、StartupCan1/StartupCan2 代替")]
        public override bool Open()
        {
            return false;
        }

        #region CAN设备操作
        /// <summary>
        /// 关闭CAN设备
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public override bool Close()
        {
            try
            {
                //持续发送数据会导致CAN连接不能关闭，先停止相关收发操作
                IsCan1Start = false;
                IsCan2Start = false;
                CancellationTokenSource1?.Cancel();
                CancellationTokenSource2?.Cancel();

                if (EcanMethod.CloseDevice(devType, devIndex) != ECANStatus.STATUS_OK)
                {
                    throw new System.Exception("关闭设备失败！");
                }

                IsConnected = false;

                return true;
            }
            catch (Exception ex)
            {
                throw new System.Exception("ERROR:" + ex.Message);
            }
        }

        /// <summary>
        /// 打开CAN设备
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public bool OpenDevice()
        {
            if (IsConnected)
                return true;

            try
            {
                ECANStatus result = EcanMethod.OpenDevice(devType, devIndex, 0);
                if (result != ECANStatus.STATUS_OK)
                {
                    throw new System.Exception("打开设备失败！");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenDevice: {ex.Message}");
                return false;
            }

            IsConnected = true;
            return true;
        }

        /// <summary>
        /// 启动通道1
        /// 注意：如果修改了通道的波特率，需要重新启动通道
        /// </summary>
        /// <returns></returns>
        public bool StartupCan1()
        {
            if (!IsConnected)
            {
                Debug.WriteLine("CAN设备未启动！");
                return false;
            }

            //if (!IsChannel1Start) //切换CAN1的波特率后，可直接再次启动，不需要关闭通道
            {
                CanIndex = 0;
                return StartupChannel();
            }

            return true;
        }

        /// <summary>
        /// 启动通道2
        /// 注意：如果修改了通道的波特率，需要重新启动通道
        /// </summary>
        /// <returns></returns>
        public bool StartupCan2()
        {
            if (!IsConnected)
            {
                Debug.WriteLine("CAN设备未启动！");
                return false;
            }

            //if (!IsChannel2Start) //切换CAN2的波特率后，可直接再次启动，不需要关闭通道
            {
                CanIndex = 1;
                return StartupChannel();
            }

            return true;
        }

        /// <summary>
        /// 启动所有(2个)通道
        /// 注意：如果修改了通道的波特率，需要重新启动通道
        /// </summary>
        /// <returns></returns>
        public bool StartupAllCan()
        {
            if (StartupCan1() && StartupCan2())
                return true;

            return false;
        }

        /// <summary>
        /// 启动通道
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        private bool StartupChannel()
        {
            if (!IsConnected)
                return false;

            Bauds baud = CanIndex == 0 ? BaudCan1 : BaudCan2;

            try
            {
                INIT_CONFIG INITCONFIG = new INIT_CONFIG();
                INITCONFIG.AccCode = accCode;
                INITCONFIG.AccMask = accMask;
                INITCONFIG.Filter = filter;
                switch (baud)
                {
                    case Bauds._5kbps:
                        INITCONFIG.Timing0 = 0xBF;
                        INITCONFIG.Timing1 = 0xFF;
                        break;
                    case Bauds._10kbps:
                        INITCONFIG.Timing0 = 0x31;
                        INITCONFIG.Timing1 = 0x1C;
                        break;
                    case Bauds._20kbps:
                        INITCONFIG.Timing0 = 0x18;
                        INITCONFIG.Timing1 = 0x1C;
                        break;
                    case Bauds._40kbps:
                        INITCONFIG.Timing0 = 0x87;
                        INITCONFIG.Timing1 = 0xFF;
                        break;
                    case Bauds._50kbps:
                        INITCONFIG.Timing0 = 0x09;
                        INITCONFIG.Timing1 = 0x1C;
                        break;
                    case Bauds._80kbps:
                        INITCONFIG.Timing0 = 0x83;
                        INITCONFIG.Timing1 = 0xFF;
                        break;
                    case Bauds._100kbps:
                        INITCONFIG.Timing0 = 0x04;
                        INITCONFIG.Timing1 = 0x1C;
                        break;
                    case Bauds._125kbps:
                        INITCONFIG.Timing0 = 0x03;
                        INITCONFIG.Timing1 = 0x1C;
                        break;
                    case Bauds._200kbps:
                        INITCONFIG.Timing0 = 0x81;
                        INITCONFIG.Timing1 = 0xFA;
                        break;
                    case Bauds._250kbps:
                        INITCONFIG.Timing0 = 0x01;
                        INITCONFIG.Timing1 = 0x1C;
                        break;
                    case Bauds._400kbps:
                        INITCONFIG.Timing0 = 0x80;
                        INITCONFIG.Timing1 = 0xFA;
                        break;
                    case Bauds._500kbps:
                        INITCONFIG.Timing0 = 0x00;
                        INITCONFIG.Timing1 = 0x1C;
                        break;
                    case Bauds._666kbps:
                        INITCONFIG.Timing0 = 0x80;
                        INITCONFIG.Timing1 = 0xB6;
                        break;
                    case Bauds._800kbps:
                        INITCONFIG.Timing0 = 0x00;
                        INITCONFIG.Timing1 = 0x16;
                        break;
                    case Bauds._1000kbps:
                        INITCONFIG.Timing0 = 0x00;
                        INITCONFIG.Timing1 = 0x14;
                        break;
                }
                INITCONFIG.Mode = mode;

                if (EcanMethod.InitCan(devType, devIndex, canIndex, ref INITCONFIG) != ECANStatus.STATUS_OK)
                {
                    throw new System.Exception($"初始化CAN通道：{canIndex + 1} 失败！");
                }

                if (EcanMethod.StartCAN(devType, devIndex, canIndex) != ECANStatus.STATUS_OK)
                {
                    throw new System.Exception($"打开CAN通道：{canIndex + 1} 失败！");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR:" + ex.Message);
                return false;
            }

            if (canIndex == 0)
            {
                IsCan1Start = true;
                CancellationTokenSource1 = new CancellationTokenSource();
            }
            else
            {
                IsCan2Start = true;
                CancellationTokenSource2 = new CancellationTokenSource();
            }

            return true;
        }

        /// <summary>
        /// 关闭通道1
        /// </summary>
        public void CloseCan1()
        {
            IsCan1Start = false;
        }

        /// <summary>
        /// 关闭通道2
        /// </summary>
        public void CloseCan2()
        {
            IsCan2Start = false;
        }

        public void CloseAllCan()
        {
            CloseCan1();
            CloseCan2();
        }

        /// <summary>
        /// 读取错误信息
        /// </summary>
        public void ReadErrInfo()
        {
            try
            {
                //ECANStatus result = EcanMethod.ReadErrInfo(devType, devIndex, 0);
                //if (result != ECANStatus.STATUS_OK)
                //{
                //    throw new System.Exception("打开设备失败！");
                //}
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 复位Can 状态
        /// </summary>
        public void RsetEcanMode()
        {
            EcanMethod.ResetCAN(devType, devIndex, 0);
        }
        #endregion

        #region 发送操作

        public bool SendCan1(uint id, byte[] data)
        {
            if (IsCan1Start)
            {
                CanIndex = 0;
                return Send(id, data);
            }

            return false;
        }

        public bool SendCan2(uint id, byte[] data)
        {
            if (IsCan2Start)
            {
                CanIndex = 1;
                return Send(id, data);
            }

            return false;
        }

        public bool Send(uint id, byte[] data)
        {
            try
            {
                if (data != null && data.Length > 8)
                {
                    throw new System.Exception("ERROR:数组越界！");
                }

                CAN_OBJ frameinfo = new CAN_OBJ();
                frameinfo.ID = id;
                frameinfo.Data = new byte[8];
                frameinfo.SendType = sendType;
                frameinfo.ExternFlag = externFlag;
                frameinfo.RemoteFlag = remoteFlag;
                int length = data == null ? 0 : data.Length;
                frameinfo.DataLen = Convert.ToByte(length);
                for (int i = 0; i < length; i++)
                {
                    frameinfo.Data[i] = data[i];
                }

                if (EcanMethod.Transmit(devType, devIndex, canIndex, new CAN_OBJ[] { frameinfo }, 1) != ECANStatus.STATUS_OK)
                {
                    if (EcanMethod.ReadErrInfo(devType, devIndex, canIndex, out ERR_INFO errInfo) == ECANStatus.STATUS_OK)
                    {
                        errcode = errInfo.ErrCode;
                        SofarHVMExe.Utilities.Global.GlobalManager.Instance().UpdateStatusBar_CanErrInfo(errInfo.ErrCode == 0 ? "" :
                            SofarHVMExe.Commun.EnumHelper.GetCombinedDescription((ErrCode)errInfo.ErrCode));
                    }
                    throw new System.Exception("发送消息失败！");
                }

                string dataStr = "";
                for (int i = 0; i < data.Length; i++)
                {
                    dataStr += data[i].ToString("X2") + " ";
                }

                //MultiLanguages.Common.LogHelper.WriteLog("发送（全局）：" + $"0x{id:X8}: " + dataStr);

                return true;
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"CAN发送数据错误-通道{canIndex}: " + ex.ToString());
                //return false;

                if (EcanMethod.ReadErrInfo(devType, devIndex, canIndex, out ERR_INFO errInfo) == ECANStatus.STATUS_OK)
                {
                    errcode = errInfo.ErrCode;
                    SofarHVMExe.Utilities.Global.GlobalManager.Instance().UpdateStatusBar_CanErrInfo(errInfo.ErrCode == 0 ? "" :
                        SofarHVMExe.Commun.EnumHelper.GetCombinedDescription((ErrCode)errInfo.ErrCode));
                }
                throw new System.Exception("发送消息失败！" + ex.Message);
            }
        }

        #endregion

        #region 接收操作

        public void StartRecvCan1()
        {
            if (!IsConnected || !IsCan1Start)
                return;

            if (!isRecvStartCan1)
            {
                //再次打开接收前清除接收缓存
                try
                {
                    EcanMethod.ClearBuffer(devType, devIndex, 0);
                }
                catch (Exception)
                {
                }
                CancellationTokenSource1 = new CancellationTokenSource();
                StartRecvThreadCan1();
            }
        }

        public void StartRecvCan2()
        {
            if (!IsConnected || !IsCan2Start)
                return;

            if (!isRecvStartCan2)
            {
                //再次打开接收前清除接收缓存
                try
                {
                    EcanMethod.ClearBuffer(devType, devIndex, 1);
                }
                catch (Exception)
                {
                }

                CancellationTokenSource2 = new CancellationTokenSource();
                StartRecvThreadCan2();
            }
        }

        public void StopRecvCan1()
        {
            isRecvStartCan1 = false;
            CancellationTokenSource1?.Cancel();
        }

        public void StopRecvCan2()
        {
            isRecvStartCan2 = false;
            CancellationTokenSource2?.Cancel();
        }

        /// <summary>
        /// 开始接收线程--Can1
        /// </summary>
        public void StartRecvThreadCan1()
        {
            isRecvStartCan1 = true;

            //设置新的log文件
            LogHelper.SubDirectory = "RealtimeData";
            LogHelper.CreateNewLogger();
            LogHelper.AddLog("**************** 程序运行日志 ****************");

            Task.Run(() =>
            {
                #region
                //调试用自定义接收数据();
                //调试用日志文件作为接收数据();
                //调试用读取配置当作接收数据();
                #endregion

                while (!CancellationTokenSource1.IsCancellationRequested)
                {
                    CAN_OBJ coMsg;
                    ECANStatus eCANStatus = EcanMethod.Receive(devType, devIndex, 0, out coMsg, 1, 1);
                    if (eCANStatus == ECANStatus.STATUS_OK)
                    {
                        coMsg.Data = coMsg.Data.Take(coMsg.DataLen).ToArray();
                        //将消息加入到接收线程中
                        //OnReceiveCan1?.Invoke(coMsg);
                        for (int i = 0; i < RecvDataQueuesCan1.Count; i++)
                        {
                            RecvDataQueuesCan1[i].Enqueue(coMsg);
                        }
                        //接收日志打印
                        string time = DateTime.Now.ToString("HH:mm:ss.fff"); //时分秒毫秒
                        string id = "0x" + coMsg.ID.ToString("X8");
                        string len = coMsg.DataLen.ToString();
                        string data = "";
                        string frame = "";
                        foreach (byte d in coMsg.Data)
                        {
                            data += " " + d.ToString("X2");
                        }

                        frame = $"{time,-15}\t{id,-10}     {len}    {data}\r\n";
                        //LogHelper.AddLog($"[CAN1Received]-{frame}");
                    }

                    if (eCANStatus != ECANStatus.STATUS_OK || errcode != 0)
                    {
                        if (EcanMethod.ReadErrInfo(devType, devIndex, 0, out ERR_INFO errInfo) == ECANStatus.STATUS_OK)
                        {
                            errcode = errInfo.ErrCode;
                            //EcanMethod.ResetCAN(devType, devIndex, 0);
                            //EcanMethod.StartCAN(devType, devIndex, 0);

                            SofarHVMExe.Utilities.Global.GlobalManager.Instance().UpdateStatusBar_CanErrInfo(errInfo.ErrCode == 0 ? "" :
                                SofarHVMExe.Commun.EnumHelper.GetCombinedDescription((ErrCode)errInfo.ErrCode));
                        }
                    }

                    Thread.Sleep(2);
                }
            });
        }

        /// <summary>
        /// 开始接收线程--Can2
        /// </summary>
        private void StartRecvThreadCan2()
        {
            isRecvStartCan2 = true;

            Task.Run(() =>
            {
                while (!CancellationTokenSource2.IsCancellationRequested)
                {
                    CAN_OBJ coMsg = new CAN_OBJ();
                    ECANStatus eCANStatus = EcanMethod.Receive(devType, devIndex, 1, out coMsg, 1, 1);
                    if (eCANStatus == ECANStatus.STATUS_OK)
                    {
                        coMsg.Data = coMsg.Data.Take(coMsg.DataLen).ToArray();
                        //将消息加入到接收线程中
                        //OnReceiveCan2?.Invoke(coMsg);
                        for (int i = 0; i < RecvDataQueuesCan2.Count; i++)
                        {
                            RecvDataQueuesCan2[i].Enqueue(coMsg);
                        }
                        //接收日志打印
                        string time = DateTime.Now.ToString("HH:mm:ss.fff"); //时分秒毫秒
                        string id = "0x" + coMsg.ID.ToString("X8");
                        string len = coMsg.DataLen.ToString();
                        string data = "";
                        string frame = "";
                        foreach (byte d in coMsg.Data)
                        {
                            data += " " + d.ToString("X2");
                        }

                        frame = $"{time,-15}\t{id,-10}     {len}    {data}\r\n";
                        //LogHelper.AddLog($"[CAN2Received]-{frame}");
                    }

                    if (eCANStatus != ECANStatus.STATUS_OK || errcode != 0)
                    {
                        if (EcanMethod.ReadErrInfo(devType, devIndex, 1, out ERR_INFO errInfo) == ECANStatus.STATUS_OK)
                        {
                            errcode = errInfo.ErrCode;
                            SofarHVMExe.Utilities.Global.GlobalManager.Instance().UpdateStatusBar_CanErrInfo(errInfo.ErrCode == 0 ? "" :
                                SofarHVMExe.Commun.EnumHelper.GetCombinedDescription((ErrCode)errInfo.ErrCode));
                        }
                    }

                    Thread.Sleep(2);
                }
            });
        }

        public void RegisterRecvProcessCan1(Action<CAN_OBJ> RecvProcCan1)
        {
            ConcurrentQueue<CAN_OBJ> cAN_OBJs = new ConcurrentQueue<CAN_OBJ>();
            RecvDataQueuesCan1.Add(cAN_OBJs);
            Task.Run(() =>
            {
                while (true)
                {
                    if (cAN_OBJs.TryDequeue(out CAN_OBJ coMsg))
                    {
                        RecvProcCan1(coMsg);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            });
        }

        public void RegisterRecvProcessCan2(Action<CAN_OBJ> RecvProcCan2)
        {
            ConcurrentQueue<CAN_OBJ> cAN_OBJs = new ConcurrentQueue<CAN_OBJ>();
            RecvDataQueuesCan2.Add(cAN_OBJs);
            Task.Run(() =>
            {
                while (true)
                {
                    if (cAN_OBJs.TryDequeue(out CAN_OBJ coMsg))
                    {
                        RecvProcCan2(coMsg);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            });
        }

        /// <summary>
        /// 手动从接收指定ID的数据-CAN1
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public CAN_OBJ RecvMsgCan13()
        {
            //List<CAN_OBJ> result = new List<CAN_OBJ>();
            CAN_OBJ msg = new CAN_OBJ();
            int sCount = 0;
            do
            {
                uint mLen = 1;
                if (!(EcanMethod.Receive(devType, devIndex, 0, out msg, mLen, 1500) == ECANStatus.STATUS_OK))
                {
                    break;
                }
                if (mLen == 0) break;

                sCount++;
            }
            while (sCount < 500);

            return msg;
        }

        /// <summary>
        /// 手动从接收指定ID的数据-CAN1
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<CAN_OBJ> RecvMsgCan1()
        {
            CAN_OBJ msg = new CAN_OBJ();
            List<CAN_OBJ> result = new List<CAN_OBJ>();

            //uint mLen = 1;
            //EcanMethod.Receive(devType, devIndex, 0, out msg, mLen, 1500);

            //if (((EcanMethod.Receive(devType, devIndex, 0, out msg, mLen, 1500) == ECANStatus.STATUS_OK) && (mLen > 0)))
            //{
            //}

            int sCount = 0;
            do
            {
                uint mLen = 1;
                if ((EcanMethod.Receive(devType, devIndex, 0, out msg, mLen, 1) == ECANStatus.STATUS_OK) && (mLen > 0))
                {
                    Debug.WriteLine($"接收一帧数据: 收到");
                    break;
                }

                if (mLen != 1)
                {
                    int stop = 10;
                }
                //if (msg.ID == id)
                //{
                //    result.Add(msg);
                //    break;
                //}
                //break;

                result.Add(msg);
                Debug.WriteLine($"接收一帧数据: 没收到 {sCount}");
                sCount++;
            }
            while (sCount < 1024);
            return result;
            //return msg;
        }
        public bool ClearBufferCan1()
        {
            if (EcanMethod.ClearBuffer(devType, devIndex, 0) == ECANStatus.STATUS_OK)
                return true;

            return false;
        }
        public bool ClearBufferCan2()
        {
            if (EcanMethod.ClearBuffer(devType, devIndex, 1) == ECANStatus.STATUS_OK)
                return true;

            return false;
        }

        //private void 调试用自定义接收数据()
        //{
        //    while (!CancellationTokenSource1.IsCancellationRequested)
        //    {
        //        string inputString = "00 00 00 12 00 04 00 00" +
        //        "01 00 00 00 00 00 00 00" +
        //        "02 00 00 00 00 00 00 00" +
        //        "03 00 00 00 00 00 00 00" +
        //        "04 00 00 00 00 00 00 00" +
        //        "FF 00 00 00 00 F5 D0 00";
        //        int groupSize = 23;
        //        List<string> groups = Enumerable.Range(0, (int)Math.Ceiling((double)inputString.Length / groupSize))
        //            .Select(i => inputString.Substring(i * groupSize, Math.Min(groupSize, inputString.Length - i * groupSize)))
        //            .ToList();
        //        foreach (var hexString in groups)
        //        {
        //            string[] hexValues = hexString.Split(' ');
        //            byte[] data = new byte[hexValues.Length];

        //            for (int i = 0; i < hexValues.Length; i++)
        //            {
        //                data[i] = Convert.ToByte(hexValues[i], 16);
        //            }
        //            CAN_OBJ frameinfo = new CAN_OBJ();
        //            frameinfo.ID = 0x19814121;
        //            frameinfo.Data = new byte[8];
        //            frameinfo.SendType = sendType;
        //            frameinfo.ExternFlag = externFlag;
        //            frameinfo.RemoteFlag = remoteFlag;
        //            int length = data == null ? 0 : data.Length;
        //            frameinfo.DataLen = Convert.ToByte(length);
        //            for (int i = 0; i < length; i++)
        //            {
        //                frameinfo.Data[i] = data[i];
        //            }
        //            CAN_OBJ coMsg = frameinfo;
        //            coMsg.Data = coMsg.Data.Take(coMsg.DataLen).ToArray();
        //            //将消息加入到接收线程中
        //            for (int i = 0; i < RecvDataQueuesCan1.Count; i++)
        //            {
        //                RecvDataQueuesCan1[i].Enqueue(coMsg);
        //            }
        //            Thread.Sleep(100);
        //        }
        //    }
        //}

        //private void 调试用日志文件作为接收数据()
        //{
        //    string fileContent = "18:49:55.584   \t0x197F4121     8     00 00 03 00 01 00 00 00";
        //    System.Windows.Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        OpenFileDialog dlg = new OpenFileDialog();
        //        dlg.Title = "选择日志文件";
        //        dlg.Filter = "(*.txt)|*.txt";
        //        dlg.Multiselect = false;
        //        dlg.RestoreDirectory = true;
        //        if (dlg.ShowDialog() != DialogResult.OK)
        //            return;
        //        fileContent = File.ReadAllText(dlg.FileName);
        //    });
        //    string[] canFrameStrs = fileContent.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        //    List<CAN_OBJ> cAN_OBJs = new List<CAN_OBJ>();
        //    List<int> interval = new List<int>();
        //    foreach (var canFrameStr in canFrameStrs)
        //    {
        //        var canFrameInfo = canFrameStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //        string[] hexValues = canFrameInfo.Skip(3).Take(8).ToArray();
        //        byte[] data = new byte[hexValues.Length];

        //        for (int i = 0; i < hexValues.Length; i++)
        //        {
        //            data[i] = Convert.ToByte(hexValues[i], 16);
        //        }
        //        CAN_OBJ frameinfo = new CAN_OBJ();
        //        frameinfo.ID = Convert.ToUInt32(canFrameInfo[1].Trim('\t'), 16);
        //        frameinfo.Data = new byte[8];
        //        frameinfo.SendType = sendType;
        //        frameinfo.ExternFlag = externFlag;
        //        frameinfo.RemoteFlag = remoteFlag;
        //        int length = data == null ? 0 : data.Length;
        //        frameinfo.DataLen = Convert.ToByte(length);
        //        for (int i = 0; i < length; i++)
        //        {
        //            frameinfo.Data[i] = data[i];
        //        }
        //        CAN_OBJ coMsg = frameinfo;
        //        coMsg.Data = coMsg.Data.Take(coMsg.DataLen).ToArray();
        //        cAN_OBJs.Add(coMsg);

        //        if (canFrameStrs.ToList().IndexOf(canFrameStr) - 1 >= 0)
        //            interval.Add((DateTime.Parse(canFrameInfo[0]) - DateTime.Parse(canFrameStrs[canFrameStrs.ToList().IndexOf(canFrameStr) - 1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])).Milliseconds);
        //        else
        //            interval.Add(100);
        //    }

        //    while (!CancellationTokenSource1.IsCancellationRequested)
        //    {
        //        for (int j = 0; j < cAN_OBJs.Count; j++)
        //        {
        //            for (int i = 0; i < RecvDataQueuesCan1.Count; i++)
        //            {
        //                RecvDataQueuesCan1[i].Enqueue(cAN_OBJs[j]);
        //            }
        //            Thread.Sleep(interval[j]);
        //        }
        //    }
        //}

        //private void 调试用读取配置作为接收数据()
        //{
        //    while (!CancellationTokenSource1.IsCancellationRequested)
        //    {
        //        var frameModels = JsonConfigHelper.ReadConfigFile().FrameModel.CanFrameModels;
        //        //List<CanFrameModel> frameList = frameModels;
        //        List<CanFrameModel> frameList = new List<CanFrameModel>(frameModels);
        //        foreach (CanFrameModel frame in frameList)
        //        {
        //            if (frame.AutoTx)
        //            {
        //                byte addr = DeviceManager.Instance().GetSelectDev();
        //                if (addr != 0)
        //                {
        //                    if (frame.FrameId.FC != 0x39) //组播（功能码）不走地址设置
        //                    {
        //                        frame.FrameId.DstAddr = addr;
        //                    }
        //                }

        //                if (frame.FrameDatas.Count > 0)
        //                {
        //                    uint id = frame.Id;
        //                    CanFrameData frameData = frame.FrameDatas[0];
        //                    if (frame.FrameId.ContinuousFlag == 0)
        //                    {
        //                        //非连续
        //                        //Task.Run(() =>
        //                        //{
        //                        ProtocolHelper.AnalyseFrameModel(frame);
        //                        byte[] data = frameData.Data;
        //                        if (data != null && data.Length > 8)
        //                        {
        //                            throw new System.Exception("ERROR:数组越界！");
        //                        }

        //                        CAN_OBJ frameinfo = new CAN_OBJ();
        //                        frameinfo.ID = id;
        //                        frameinfo.Data = new byte[8];
        //                        frameinfo.SendType = sendType;
        //                        frameinfo.ExternFlag = externFlag;
        //                        frameinfo.RemoteFlag = remoteFlag;
        //                        int length = data == null ? 0 : data.Length;
        //                        frameinfo.DataLen = Convert.ToByte(length);
        //                        for (int i = 0; i < length; i++)
        //                        {
        //                            frameinfo.Data[i] = data[i];
        //                        }

        //                        CAN_OBJ coMsg = frameinfo;
        //                        coMsg.Data = coMsg.Data.Take(coMsg.DataLen).ToArray();
        //                        //将消息加入到接收线程中
        //                        for (int i = 0; i < RecvDataQueuesCan1.Count; i++)
        //                        {
        //                            RecvDataQueuesCan1[i].Enqueue(coMsg);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        List<CanFrameData> frameDataList = ProtocolHelper.AnalyseMultiPackage(frameData);
        //                        foreach (CanFrameData fd in frameDataList)
        //                        {
        //                            byte[] data = fd.Data;
        //                            if (data != null && data.Length > 8)
        //                            {
        //                                throw new System.Exception("ERROR:数组越界！");
        //                            }

        //                            CAN_OBJ frameinfo = new CAN_OBJ();
        //                            frameinfo.ID = id;
        //                            frameinfo.Data = new byte[8];
        //                            frameinfo.SendType = sendType;
        //                            frameinfo.ExternFlag = externFlag;
        //                            frameinfo.RemoteFlag = remoteFlag;
        //                            int length = data == null ? 0 : data.Length;
        //                            frameinfo.DataLen = Convert.ToByte(length);
        //                            for (int i = 0; i < length; i++)
        //                            {
        //                                frameinfo.Data[i] = data[i];
        //                            }

        //                            CAN_OBJ coMsg = frameinfo;
        //                            coMsg.Data = coMsg.Data.Take(coMsg.DataLen).ToArray();
        //                            //将消息加入到接收线程中
        //                            for (int i = 0; i < RecvDataQueuesCan1.Count; i++)
        //                            {
        //                                RecvDataQueuesCan1[i].Enqueue(coMsg);
        //                            }
        //                            Thread.Sleep(100);
        //                        }
        //                    }
        //                }
        //                Thread.Sleep(100);
        //            }
        //        }
        //    }
        //}


        #endregion

        #endregion




    }//class
}
