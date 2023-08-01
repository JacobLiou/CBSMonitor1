using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Communication.Can
{
    public class ZlgcanBase
    {
        public ZCAN_ReceiveFD_Data[] CANFD_Data { get; set; }
        public ZCAN_Receive_Data[] CAN_Data { get; set; }
    }

    public class ZlgcanHelper : CommBase<ZlgcanBase>
    {
        private const int NULL = 0;
        private const int CANFD_BRS = 0x01; /* bit rate switch (second bitrate for payload data) */
        private const int CANFD_ESI = 0x02; /* error state indicator of the transmitting node */

        /* CAN payload length and DLC definitions according to ISO 11898-1 */
        private const int CAN_MAX_DLC = 8;
        private const int CAN_MAX_DLEN = 8;

        /* CAN FD payload length and DLC definitions according to ISO 11898-7 */
        private const int CANFD_MAX_DLC = 15;
        private const int CANFD_MAX_DLEN = 64;

        private const int TYPE_CAN = 0;
        private const int TYPE_CANFD = 1;

        private const uint CAN_EFF_FLAG = 0x80000000U; /* EFF/SFF is set in the MSB */
        private const uint CAN_RTR_FLAG = 0x40000000U; /* remote transmission request */
        private const uint CAN_ERR_FLAG = 0x20000000U; /* error message frame */
        private const uint CAN_ID_FLAG = 0x1FFFFFFFU; /* id */

        DeviceInfo[] kDeviceType =
        {
            new DeviceInfo(Define.ZCAN_USBCAN1, 1),
            new DeviceInfo(Define.ZCAN_USBCAN2, 2),
            new DeviceInfo(Define.ZCAN_USBCAN_E_U, 1),
            new DeviceInfo(Define.ZCAN_USBCAN_2E_U, 2),
            new DeviceInfo(Define.ZCAN_PCIECANFD_100U, 1),
            new DeviceInfo(Define.ZCAN_PCIECANFD_200U, 2),
            new DeviceInfo(Define.ZCAN_PCIECANFD_400U, 4),
            new DeviceInfo(Define.ZCAN_USBCANFD_200U, 2),
            new DeviceInfo(Define.ZCAN_USBCANFD_100U, 1),
            new DeviceInfo(Define.ZCAN_USBCANFD_MINI, 1),
            new DeviceInfo(Define.ZCAN_CANETTCP, 1),
            new DeviceInfo(Define.ZCAN_CANETUDP, 1),
            new DeviceInfo(Define.ZCAN_CANFDNET_400U_TCP, 4),
            new DeviceInfo(Define.ZCAN_CANFDNET_400U_UDP, 4),
            new DeviceInfo(Define.ZCAN_CLOUD, 1)
        };

        uint[] kbaudrate =
        {
            1000000,//1000kbps
            800000,//800kbps
            500000,//500kbps
            250000,//250kbps
            125000,//125kbps
            100000,//100kbps
            50000,//50kbps
            20000,//20kbps
            10000,//10kbps
            5000 //5kbps
        };
        uint[] kUSBCANFDabit =
        {
            1000000, // 1Mbps
            800000, // 800kbps
            500000, // 500kbps
            250000, // 250kbps
            125000, // 125kbps
            100000, // 100kbps
            50000, // 50kbps
            800000, // 800kbps
        };
        uint[] kUSBCANFDdbit =
        {
            5000000, // 5Mbps
            4000000, // 4Mbps
            2000000, // 2Mbps
            1000000, // 1Mbps
            800000, // 800kbps
            500000, // 500kbps
            250000, // 250kbps
            125000, // 125kbps
            100000, // 100kbps
        };
        uint[] kPCIECANFDabit =
        {
            1000000, // 1Mbps
            800000, // 800kbps
            500000, // 500kbps
            250000, // 250kbps
        };
        uint[] kPCIECANFDdbit =
        {
            8000000, // 8Mbps
            4000000, // 4Mbps
            2000000, // 2Mbps
        };

        IntPtr device_handle;
        IntPtr channel_handle;
        IProperty property;
        //recvdatathread recv_data_thread;
        List<string> list_box_data = new List<string>();
        static object lock_obj = new object();

        bool m_bCloud = false;

        private UInt32 device_type_index = 0;//设备类型
        private UInt32 device_index = 0;//设备索引
        private UInt32 channel_index = 0;//设备通道
        private UInt32 abit = 0;//仲裁域波特率
        private UInt32 dbit = 0;//数据域波特率
        private UInt32 netmode = 0;//模式
        private int baud = 0;//波特率
        private int mode = 0;//工作模式
        private int standard = 0;//CANFD标准
        private int resistance = 0;//终端电阻使能，勾选1，不勾选0
        private int customizeabit = 0;//自定义波特率，勾选1，不勾选0
        private String customizeabitval;//自定义波特率，手动写
        private String localport;//本地端口
        private String remoteaddr;//远程地址
        private String remoteport;//远程端口
        private String startid;//起始ID
        private String endid;//结束ID
        private String standard2;//滤波模式

        public override bool IsConnected { get; set; }
        public override Action<ZlgcanBase> OnReceive { get; set; }
        public override CancellationTokenSource CancellationTokenSource { get => base.CancellationTokenSource; set => base.CancellationTokenSource = value; }

        public uint Device_Type_Index { get => device_type_index; set => device_type_index = value; }
        public uint Device_index { get => device_index; set => device_index = value; }
        public uint Channel_index { get => channel_index; set => channel_index = value; }
        public uint Abit { get => abit; set => abit = value; }
        public uint Dbit { get => dbit; set => dbit = value; }
        public uint Netmode { get => netmode; set => netmode = value; }
        public int Baud { get => baud; set => baud = value; }
        public int Mode { get => mode; set => mode = value; }
        public int Standard { get => standard; set => standard = value; }
        public int Resistance { get => resistance; set => resistance = value; }
        public int CustomizeAbit { get => customizeabit; set => customizeabit = value; }
        public string CustomizeAbitVal { get => customizeabitval; set => customizeabitval = value; }
        public string Localport { get => localport; set => localport = value; }
        public string Remoteaddr { get => remoteaddr; set => remoteaddr = value; }
        public string Remoteport { get => remoteport; set => remoteport = value; }
        public string Startid { get => startid; set => startid = value; }
        public string Endid { get => endid; set => endid = value; }
        public string Standard2 { get => standard2; set => standard2 = value; }

        public override bool Close()
        {
            try
            {
                ZlgcanMethod.ZCAN_CloseDevice(device_handle);
                CancellationTokenSource.Cancel();
                IsConnected= false;
                return true;
            }
            catch (Exception ex)
            {
                throw new System.Exception("ERROR:" + ex.Message);
            }
        }

        public override bool Open()
        {
            try
            {
                #region 打开设备
                uint device_type_index_ = (uint)device_type_index;
                uint device_index_;
                if (kDeviceType[device_type_index_].device_type == Define.ZCAN_CLOUD) // CLOUD
                {
                    /*ZLGOUD dlg = new ZLGOUD();
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        device_index_ = dlg.deviceIndex();
                        m_bCloud = true;
                    }
                    else
                    {
                        return;
                    }*/
                    device_index_ = 0;
                }
                else
                {
                    device_index_ = (uint)device_index;
                }
                device_handle = ZlgcanMethod.ZCAN_OpenDevice(kDeviceType[device_type_index_].device_type, device_index_, 0);
                if (NULL == (int)device_handle)
                {
                    throw new System.Exception("打开设备失败,请检查设备类型和设备索引号是否正确");
                }
                #endregion

                #region 初始化CAN
                uint type = kDeviceType[device_type_index].device_type;
                bool netDevice = type == Define.ZCAN_CANETTCP || type == Define.ZCAN_CANETUDP ||
                    type == Define.ZCAN_CANFDNET_400U_TCP || type == Define.ZCAN_CANFDNET_400U_UDP;
                bool pcieCanfd = type == Define.ZCAN_PCIECANFD_100U ||
                    type == Define.ZCAN_PCIECANFD_200U ||
                    type == Define.ZCAN_PCIECANFD_400U;
                bool usbCanfd = type == Define.ZCAN_USBCANFD_100U ||
                    type == Define.ZCAN_USBCANFD_200U ||
                    type == Define.ZCAN_USBCANFD_MINI;
                bool canfdDevice = usbCanfd || pcieCanfd;
                if (!m_bCloud)
                {
                    IntPtr ptr = ZlgcanMethod.GetIProperty(device_handle);
                    if (NULL == (int)ptr)
                    {
                        throw new System.Exception("设置指定路径属性失败");
                    }

                    property = (IProperty)Marshal.PtrToStructure((IntPtr)((UInt32)ptr), typeof(IProperty));

                    if (netDevice)
                    {
                        bool tcpDevice = type == Define.ZCAN_CANETTCP;
                        bool server = tcpDevice && netmode == 0;//comboBox_netmode.SelectedIndex
                        if (tcpDevice)
                        {
                            setnetmode();
                            if (server)
                            {
                                setLocalPort();
                            }
                            else
                            {
                                setRemoteAddr();
                                setRemotePort();
                            }
                        }
                        else
                        {
                            setLocalPort();
                            setRemoteAddr();
                            setRemotePort();
                        }
                    }
                    else
                    {
                        if (usbCanfd)
                        {
                            if (!setCANFDStandard(standard)) //设置CANFD标准standard:comboBox_standard.SelectedIndex
                            {
                                throw new System.Exception("设置CANFD标准失败");
                            }
                        }
                        if (abit == 1)//设置波特率
                        {
                            if (!setCustombaudrate())
                            {
                                throw new System.Exception("设置自定义波特率失败");
                            }
                        }
                        else
                        {
                            if (!canfdDevice)
                            {
                                if (!setbaudrate(kbaudrate[baud]))
                                {
                                    throw new System.Exception("设置波特率失败");
                                }
                            }
                            else
                            {
                                bool result = true;
                                if (usbCanfd)
                                {
                                    result = setFdbaudrate(kUSBCANFDabit[abit], kUSBCANFDdbit[dbit]);
                                }
                                else if (pcieCanfd)
                                {
                                    result = setFdbaudrate(kPCIECANFDabit[abit], kPCIECANFDdbit[dbit]);
                                }
                                if (!result)
                                {
                                    throw new System.Exception("设置波特率失败");
                                }
                            }
                        }
                    }
                }

                ZCAN_CHANNEL_INIT_CONFIG config_ = new ZCAN_CHANNEL_INIT_CONFIG();
                if (!m_bCloud && !netDevice)
                {
                    config_.canfd.mode = (byte)mode;
                    if (usbCanfd)
                    {
                        config_.can_type = Define.TYPE_CANFD;
                    }
                    else if (pcieCanfd)
                    {
                        config_.can_type = Define.TYPE_CANFD;
                        config_.can.filter = 0;
                        config_.can.acc_code = 0;
                        config_.can.acc_mask = 0xFFFFFFFF;
                    }
                    else
                    {
                        config_.can_type = Define.TYPE_CAN;
                        config_.can.filter = 0;
                        config_.can.acc_code = 0;
                        config_.can.acc_mask = 0xFFFFFFFF;
                    }
                }
                IntPtr pConfig = Marshal.AllocHGlobal(Marshal.SizeOf(config_));
                Marshal.StructureToPtr(config_, pConfig, true);

                //int size = sizeof(ZCAN_CHANNEL_INIT_CONFIG);
                //IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(size);
                //System.Runtime.InteropServices.Marshal.StructureToPtr(config_, ptr, true);
                channel_handle = ZlgcanMethod.ZCAN_InitCAN(device_handle, (uint)channel_index, pConfig);
                Marshal.FreeHGlobal(pConfig);

                //Marshal.FreeHGlobal(ptr);

                if (NULL == (int)channel_handle)
                {
                    throw new System.Exception("初始化CAN失败");
                }

                if (!m_bCloud && !netDevice)
                {
                    if (usbCanfd && resistance == 1)//checkBox_resistance.Checked
                    {
                        if (!setResistanceEnable())
                        {
                            throw new System.Exception("使能终端电阻失败");
                        }
                    }
                    if (!canfdDevice && !setFilter())
                    {
                        throw new System.Exception("设置滤波失败");
                    }
                    if (usbCanfd && !setFilter())
                    {
                        throw new System.Exception("设置滤波失败");
                    }
                }
                #endregion

                #region 启动CAN
                if (ZlgcanMethod.ZCAN_StartCAN(channel_handle) != Define.STATUS_OK)
                {
                    throw new System.Exception("启动CAN失败");
                }
                #endregion

                CancellationTokenSource = new CancellationTokenSource();
                IsConnected=true;
                return true;
            }
            catch (Exception ex)
            {
                throw new System.Exception("ERROR:" + ex.Message);
            }
        }

        public override bool Send(uint id, string data)
        {
            try
            {
                //全局变量
                int protocol_index = 0;
                int frame_type_index = 1;
                int send_type_index = 0;
                int canfd_exp_index = 0;

                uint result; //发送的帧数
                if (0 == protocol_index)
                {
                    if (data.Length > CAN_MAX_DLEN)
                    {
                        throw new System.Exception("数组越界!");
                    }

                    ZCAN_Transmit_Data can_data = new ZCAN_Transmit_Data();
                    can_data.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    can_data.frame.data = new byte[8];
                    can_data.frame.can_dlc = (byte)SplitData(data, ref can_data.frame.data, CAN_MAX_DLEN);
                    can_data.transmit_type = (uint)send_type_index;
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(can_data));
                    Marshal.StructureToPtr(can_data, ptr, true);
                    result = ZlgcanMethod.ZCAN_Transmit(channel_handle, ptr, 1);
                    Marshal.FreeHGlobal(ptr);
                }
                else
                {
                    if (data.Length > CANFD_MAX_DLEN)
                    {
                        throw new System.Exception("数组越界!");
                    }

                    ZCAN_TransmitFD_Data canfd_data = new ZCAN_TransmitFD_Data();
                    canfd_data.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    canfd_data.frame.data = new byte[64];
                    canfd_data.frame.len = (byte)SplitData(data, ref canfd_data.frame.data, CANFD_MAX_DLEN);
                    canfd_data.transmit_type = (uint)send_type_index;
                    canfd_data.frame.flags = (byte)((canfd_exp_index != 0) ? CANFD_BRS : 0);
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(canfd_data));
                    Marshal.StructureToPtr(canfd_data, ptr, true);
                    result = ZlgcanMethod.ZCAN_TransmitFD(channel_handle, ptr, 1);
                    Marshal.FreeHGlobal(ptr);
                }

                if (result != 1)
                {
                    throw new System.Exception(AddErr());
                }
            }
            catch (Exception ex)
            {
                throw new System.Exception("ERROR:" + ex.Message);
            }
            return true;
        }

        public override void StartReceiveThread()
        {
            Task.Run(() =>
            {
                ZlgcanBase zlgcanBase = new ZlgcanBase();
                ZCAN_Receive_Data[] can_data = new ZCAN_Receive_Data[10000];
                ZCAN_ReceiveFD_Data[] canfd_data = new ZCAN_ReceiveFD_Data[10000];
                uint len;
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    lock (lock_obj)
                    {
                        len = ZlgcanMethod.ZCAN_GetReceiveNum(channel_handle, TYPE_CAN);
                        if (len > 0)
                        {
                            int size = Marshal.SizeOf(typeof(ZCAN_Receive_Data));
                            IntPtr ptr = Marshal.AllocHGlobal((int)len * size);
                            len = ZlgcanMethod.ZCAN_Receive(channel_handle, ptr, len, 50);
                            for (int i = 0; i < len; ++i)
                            {
                                can_data[i] = (ZCAN_Receive_Data)Marshal.PtrToStructure(
                                    (IntPtr)((Int64)ptr + i * size), typeof(ZCAN_Receive_Data));
                            }
                            
                            zlgcanBase.CAN_Data = can_data;
                            OnReceive?.Invoke(zlgcanBase);
                            Marshal.FreeHGlobal(ptr);
                        }

                        len = ZlgcanMethod.ZCAN_GetReceiveNum(channel_handle, TYPE_CANFD);
                        if (len > 0)
                        {
                            int size = Marshal.SizeOf(typeof(ZCAN_ReceiveFD_Data));
                            IntPtr ptr = Marshal.AllocHGlobal((int)len * size);
                            len = ZlgcanMethod.ZCAN_ReceiveFD(channel_handle, ptr, len, 50);
                            for (int i = 0; i < len; ++i)
                            {
                                canfd_data[i] = (ZCAN_ReceiveFD_Data)Marshal.PtrToStructure(
                                    (IntPtr)((UInt32)ptr + i * size), typeof(ZCAN_ReceiveFD_Data));
                            }

                            zlgcanBase.CANFD_Data = canfd_data;
                            OnReceive?.Invoke(zlgcanBase);
                            Marshal.FreeHGlobal(ptr);
                        }
                    }
                    Thread.Sleep(10);
                }
            });
        }

        #region 辅助函数
        private void AddData(ZCAN_Receive_Data[] data, uint len)
        {
            string text = "";
            for (uint i = 0; i < len; ++i)
            {
                ZCAN_Receive_Data can = data[i];
                uint id = data[i].frame.can_id;
                string eff = IsEFF(id) ? "扩展帧" : "标准帧";
                string rtr = IsRTR(id) ? "远程帧" : "数据帧";
                text = String.Format("接收到CAN ID:0x{0:X8} {1:G} {2:G} 长度:{3:D1} 数据:", GetId(id), eff, rtr, can.frame.can_dlc);

                for (uint j = 0; j < can.frame.can_dlc; ++j)
                {
                    text = String.Format("{0:G}{1:X2} ", text, can.frame.data[j]);
                }
                lock (lock_obj)
                {
                    list_box_data.Add(text);
                }
            }

            //Object[] list = { this, System.EventArgs.Empty };
            //this.listBox.BeginInvoke(new EventHandler(SetListBox), list);
        }

        private void AddData(ZCAN_ReceiveFD_Data[] data, uint len)
        {
            string text = "";
            for (uint i = 0; i < len; ++i)
            {
                ZCAN_ReceiveFD_Data canfd = data[i];
                uint id = data[i].frame.can_id;
                string eff = IsEFF(id) ? "扩展帧" : "标准帧";
                string rtr = IsRTR(id) ? "远程帧" : "数据帧";
                text = String.Format("接收到CANFD ID:0x{0:X8} {1:G} {2:G} 长度:{3:D1} 数据:", GetId(id), eff, rtr, canfd.frame.len);
                for (uint j = 0; j < canfd.frame.len; ++j)
                {
                    text = String.Format("{0:G}{1:X2} ", text, canfd.frame.data[j]);
                }
                lock (lock_obj)
                {
                    list_box_data.Add(text);
                }
            }

            //Object[] list = { this, System.EventArgs.Empty };
            //this.listBox.BeginInvoke(new EventHandler(SetListBox), list);
        }

        private String AddErr()
        {
            ZCAN_CHANNEL_ERROR_INFO pErrInfo = new ZCAN_CHANNEL_ERROR_INFO();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(pErrInfo));
            Marshal.StructureToPtr(pErrInfo, ptr, true);
            if (ZlgcanMethod.ZCAN_ReadChannelErrInfo(channel_handle, ptr) != Define.STATUS_OK)
            {
                throw new System.Exception("获取错误信息失败");
            }
            Marshal.FreeHGlobal(ptr);

            string errorInfo = String.Format("错误码：{0:D1}", pErrInfo.error_code);
            return errorInfo;
            //int index = listBox.Items.Add(errorInfo);
            //listBox.SelectedIndex = index;
        }

        //1:extend frame 0:standard frame || 1：扩展帧 0：标准帧
        public uint MakeCanId(uint id, int eff, int rtr, int err)
        {
            uint ueff = (uint)(!!(Convert.ToBoolean(eff)) ? 1 : 0);
            uint urtr = (uint)(!!(Convert.ToBoolean(rtr)) ? 1 : 0);
            uint uerr = (uint)(!!(Convert.ToBoolean(err)) ? 1 : 0);
            return id | ueff << 31 | urtr << 30 | uerr << 29;
        }

        public bool IsEFF(uint id)//1:extend frame 0:standard frame
        {
            return !!Convert.ToBoolean((id & CAN_EFF_FLAG));
        }

        public bool IsRTR(uint id)//1:remote frame 0:data frame
        {
            return !!Convert.ToBoolean((id & CAN_RTR_FLAG));
        }

        public bool IsERR(uint id)//1:error frame 0:normal frame
        {
            return !!Convert.ToBoolean((id & CAN_ERR_FLAG));
        }

        public uint GetId(uint id)
        {
            return id & CAN_ID_FLAG;
        }

        //设置波特率
        private bool setbaudrate(UInt32 baud)
        {
            string path = channel_index + "/baud_rate";
            string value = baud.ToString();
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(value));
        }

        private bool setFdbaudrate(UInt32 abaud, UInt32 dbaud)
        {
            string path = channel_index + "/canfd_abit_baud_rate";
            string value = abaud.ToString();
            if (1 != property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }
            path = channel_index + "/canfd_dbit_baud_rate";
            value = dbaud.ToString();
            if (1 != property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }
            return true;
        }

        //设置CANFD标准
        private bool setCANFDStandard(int canfd_standard)
        {
            string path = channel_index + "/canfd_standard";
            string value = canfd_standard.ToString();
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(value));
        }

        //设置自定义波特率, 需要从CANMaster目录下的baudcal生成字符串
        private bool setCustombaudrate()
        {
            string path = channel_index + "/baud_rate_custom";
            string baudrate = customizeabitval;
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(baudrate).ToPointer();
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(baudrate));
        }

        //设置终端电阻使能
        private bool setResistanceEnable()
        {
            string path = channel_index + "/initenal_resistance";
            string value = resistance.ToString();//(checkBox_resistance.Checked ? "1" : "0");
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(value));
        }

        //设置滤波
        private bool setFilter()
        {
            string path = channel_index + "/filter_clear";//清除滤波
            string value = "0";
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index + "/filter_mode";
            value = standard2;
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index + "/filter_start";
            value = startid;
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index + "/filter_end";
            value = endid;
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index + "/filter_ack";//滤波生效
            value = "0";
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            //如果要设置多条滤波，在清除滤波和滤波生效之间设置多条滤波即可
            return true;
        }

        private bool setFilterCode()
        {
            string path = channel_index + "/filter";
            string value = "0";
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }
            path = channel_index + "/acc_code";
            value = "0x0000";
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }
            path = channel_index + "/acc_mask";
            value = "0xFFFFFFFF";
            if (0 == property.SetValue(path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }
            return true;
        }

        //设置网络工作模式
        private bool setnetmode()
        {
            string path = channel_index + "/work_mode";
            string value = netmode == 0 ? "1" : "0";
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(value));
        }

        //设置本地端口
        private bool setLocalPort()
        {
            string path = channel_index + "/local_port";
            string value = localport;
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(value));
        }

        //设置远程地址
        private bool setRemoteAddr()
        {
            string path = channel_index + "/ip";
            string value = remoteaddr;
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(value));
        }

        //设置远程端口
        private bool setRemotePort()
        {
            string path = channel_index + "/work_port";
            string value = remoteport;
            return 1 == property.SetValue(path, Encoding.ASCII.GetBytes(value));
        }

        //拆分text到发送data数组
        private int SplitData(string data, ref byte[] transData, int maxLen)
        {
            string[] dataArray = data.Split(' ');
            for (int i = 0; (i < maxLen) && (i < dataArray.Length); i++)
            {
                transData[i] = Convert.ToByte(dataArray[i].Substring(0, 2), 16);
            }

            return dataArray.Length;
        }
        #endregion
    }
}
