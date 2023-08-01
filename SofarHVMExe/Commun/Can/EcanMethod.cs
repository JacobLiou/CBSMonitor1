using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Communication.Can
{
    [Flags]
    public enum ECANStatus : uint
    {
        /// <summary>
        ///  error
        /// </summary>
        STATUS_ERR = 0x00000,
        /// <summary>
        /// No error
        /// </summary>
        STATUS_OK = 0x00001,
    }
    public enum ErrCode
    {
        /// <summary>
        /// CAN控制器内部FIFO溢出
        /// </summary>
        [Description("CAN控制器内部FIFO溢出")]
        ERR_CAN_OVERFLOW = 0x00000001,
        /// <summary>
        /// CAN控制器错误告警
        /// </summary>
        [Description("CAN控制器错误告警")]
        ERR_CAN_ERRALARM = 0x00000002,
        /// <summary>
        /// CAN控制器消极错误
        /// </summary>
        [Description("CAN控制器消极错误")]
        ERR_CAN_PASSIVE = 0x00000004,
        /// <summary>
        /// CAN控制器仲裁丢失
        /// </summary>
        [Description("CAN控制器仲裁丢失")]
        ERR_CAN_LOSE = 0x00000008,
        /// <summary>
        /// CAN控制器总线错误
        /// </summary>
        [Description("CAN控制器总线错误")]
        ERR_CAN_BUSERR = 0x00000010,
        /// <summary>
        /// CAN接收寄存器满
        /// </summary>
        [Description("CAN接收寄存器满")]
        ERR_CAN_REG_FULL = 0x00000020,
        /// <summary>
        /// CAN接收寄存器溢出
        /// </summary>
        [Description("CAN接收寄存器溢出")]
        ERR_CAN_REC_OVER = 0x00000040,
        /// <summary>
        /// CAN控制器主动错误
        /// </summary>
        [Description("CAN控制器主动错误")]
        ERR_CAN_ACTIVE = 0x00000080,
        /// <summary>
        /// 设备已经打开
        /// </summary>
        [Description("设备已经打开")]
        ERR_DEVICEOPENED = 0x00000100,
        /// <summary>
        /// 打开设备错误
        /// </summary>
        [Description("打开设备错误")]
        ERR_DEVICEOPEN = 0x00000200,
        /// <summary>
        /// 设备没有打开
        /// </summary>
        [Description("设备没有打开")]
        ERR_DEVICENOTOPEN = 0x00000400,
        /// <summary>
        /// 缓冲区溢出
        /// </summary>
        [Description("缓冲区溢出")]
        ERR_BUFFEROVERFLOW = 0x00000800,
        /// <summary>
        /// 此设备不存在
        /// </summary>
        [Description("此设备不存在")]
        ERR_DEVICENOTEXIST = 0x00001000,
        /// <summary>
        /// 装载动态库失败
        /// </summary>
        [Description("装载动态库失败")]
        ERR_LOADKERNELDLL = 0x00002000,
        /// <summary>
        /// 执行命令失败错误码
        /// </summary>
        [Description("执行命令失败")]
        ERR_CMDFAILED = 0x00004000,
        /// <summary>
        /// 内存不足
        /// </summary>
        [Description("内存不足")]
        ERR_BUFFERCREATE = 0x00008000,
        /// <summary>
        /// 端口已经被打开
        /// </summary>
        [Description("端口已经被打开")]
        ERR_CANETE_PORTOPENED = 0x00010000,
        /// <summary>
        /// 设备索引号已经被占用
        /// </summary>
        [Description("设备索引号已经被占用")]
        ERR_CANETE_INDEXUSED = 0x00020000
    }

    /// <summary>
    /// 包含 ECAN 系列接口卡的设备信息
    /// </summary>
    public struct BOARD_INFO
    {
        /* 注解
        hw_Version 
        硬件版本号，用16进制表示。比如0x0100表示V1.00。 
        fw_Version 
        固件版本号，用16进制表示。 
        dr_Version 
        驱动程序版本号，用16进制表示。 
        in_Version 
        接口库版本号，用16进制表示。 
        irq_Num 
        板卡所使用的中断号。 
        can_Num 
        表示有几路CAN通道。 
        str_Serial_Num 
        此板卡的序列号。 
        str_hw_Type 
        硬件类型，比如“USBCAN V1.00”（注意：包括字符串结束符’\0’）。 
        Reserved 
        系统保留。*/
        public ushort hw_Version;
        public ushort fw_Version;
        public ushort dr_Version;
        public ushort in_Version;
        public ushort irq_Num;
        public byte can_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] str_Serial_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] str_hw_Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] Reserved;
    }

    /// <summary>
    /// 在Transmit和Receive函数中被用来传送CAN信息帧
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CAN_OBJ
    {
        /* 注解
         ID 
        报文ID。 
        TimeStamp 
        接收到信息帧时的时间标识，从CAN控制器初始化开始计时，单位微秒。 
        TimeFlag 
        是否使用时间标识，为1时TimeStamp有效，TimeFlag和TimeStamp只在此帧为接
        收帧时有意义。 
        SendType 
        发送帧类型，=0时为正常发送，=1时为单次发送，=2时为自发自收，=3时为
        单次自发自收，只在此帧为发送帧时有意义。 
        RemoteFlag 
        是否是远程帧。 
        ExternFlag 
        是否是扩展帧。 
        DataLen 
        数据长度(<=8)，即Data的长度。 
        Data 
        报文的数据。 
        Reserved
        系统保留。*/
        public uint ID;
        public uint TimeStamp;
        public byte TimeFlag;
        public byte SendType;
        public byte RemoteFlag;
        public byte ExternFlag;
        public byte DataLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;
    }

    /// <summary>
    /// 包含CAN控制器状态信息。结构体将在ReadCanStatus函数中被填充
    /// </summary>
    public struct CAN_STATUS
    {
        /* 注解
         ErrInterrupt 
        中断记录，读操作会清除。 
        regMode 
        CAN控制器模式寄存器。 
        regStatus 
        CAN控制器状态寄存器。 
        regALCapture 
        CAN控制器仲裁丢失寄存器。 
        regECCapture 
        CAN控制器错误寄存器。 
        regEWLimit 
        CAN控制器错误警告限制寄存器。 
        regRECounter 
        CAN控制器接收错误寄存器。 
        regTECounter 
        CAN控制器发送错误寄存器。*/
        public byte ErrInterrupt;
        public byte regMode;
        public byte regStatus;
        public byte regALCapture;
        public byte regECCapture;
        public byte regEWLimit;
        public byte regRECounter;
        public byte regTECounter;
    }

    /// <summary>
    /// 用于装载VCI 库运行时产生的错误信息。结构体将在ReadErrInfo函数中被填充
    /// </summary>
    public struct ERR_INFO
    {
        /* 注解
         ErrCode 
        错误码。 
        Passive_ErrData 
        当产生的错误中有消极错误时表示为消极错误的错误标识数据。 
        ArLost_ErrData 
        当产生的错误中有仲裁丢失错误时表示为仲裁丢失错误的错误标识数据。*/
        public uint ErrCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Passive_ErrData;
        public byte ArLost_ErrData;
    }

    /// <summary>
    /// 定义了初始化CAN的配置。结构体将在InitCan函数中被填充
    /// </summary>
    public struct INIT_CONFIG
    {
        /* 注解
         AccCode
        验收码。SJA1000的帧过滤验收码。
        AccMask
        屏蔽码。SJA1000的帧过滤屏蔽码。屏蔽码推荐设置为0xFFFF FFFF，即
        全部接收。
        Reserved
        保留。
        Filter
        滤波使能。0=不使能，1=使能。使能时，请参照SJA1000验收滤波器设置
        验收码和屏蔽码。
        Timing0
        波特率定时器0（BTR0）。设置值见下表。
        Timing1
        波特率定时器1（BTR1）。设置值见下表。
        Mode
        模式。=0为正常模式，=1为只听模式，=2为自发自收模式。*/
        public uint AccCode;
        public uint AccMask;
        public uint Reserved;
        public byte Filter;
        public byte Timing0;
        public byte Timing1;
        public byte Mode;
    }

    /// <summary>
    /// FILTER_RECORD结构体定义了CAN滤波器的滤波范围。结构体将在SetReference函数中被填充。
    /// </summary>
    public struct FILTER_RECORD {
        /*
         ExtFrame
          过滤的帧类型标志，为1代表要过滤的为扩展帧，为0代表要过滤的为标准帧。
         Start
          滤波范围的起始帧ID。
         End
          滤波范围的结束帧ID。
         */
        public uint ExtFrame;
        public uint Start;
        public uint End;
    }


    /// <summary>
    /// 返回值：为1表示操作成功，0表示操作失败。
    /// </summary>
    public static class EcanMethod
    {
        /// <summary>
        /// 打开设备 
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="Reserved">参数无意义</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "OpenDevice")]
        public static extern ECANStatus OpenDevice(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 Reserved);

        /// <summary>
        /// 关闭设备
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <returns></returns>

        [DllImport("ECanVci64.dll", EntryPoint = "CloseDevice")]
        public static extern ECANStatus CloseDevice(
            UInt32 DevType,
            UInt32 DevIndex);

        /// <summary>
        /// 初始化指定的CAN
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <param name="InitConfig">初始化参数结构</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "InitCAN")]
        public static extern ECANStatus InitCan(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex,
            ref INIT_CONFIG InitConfig);

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="INFO">用来存储设备信息的BOARD_INFO结构指针</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "ReadBoardInfo")]
        public static extern ECANStatus ReadBoardInfo(
            UInt32 DevType,
            UInt32 DevIndex,
            ref BOARD_INFO INFO);

        /// <summary>
        /// 获取最后一次错误信息
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <param name="ErrInfo">用来存储错误信息的ERR_INFO结构指针</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "ReadErrInfo")]
        public static extern ECANStatus ReadErrInfo(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex,
            out ERR_INFO ErrInfo);

        /// <summary>
        /// 获取CAN状态
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex"></param>
        /// <param name="CANSTATUS"></param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "ReadCanStatus")]
        public static extern ECANStatus ReadCanStatus(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex,
            CAN_STATUS CANSTATUS);

        /// <summary>
        /// 获取设备的相应参数
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <param name="RefType">参数类型</param>
        /// <param name="DATA">用来存储参数有关数据缓冲区地址首指针</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "GetReference")]
        public static extern ECANStatus GetReference(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex,
            UInt32 RefType,
            byte DATA);

        /// <summary>
        /// 设置设备的相应参数
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <param name="RefType">参数类型</param>
        /// <param name="DATA">用来存储参数有关数据缓冲区地址首指针</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "SetReference")]
        public static extern ECANStatus SetReference(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex,
            UInt32 RefType,
            byte DATA);

        /// <summary>
        /// 获取指定接收缓冲区中接收到但尚未被读取的帧数
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <returns>返回尚未被读取的帧数</returns>
        [DllImport("ECanVci64.dll", EntryPoint = "GetReceiveNum")]
        public static extern ECANStatus GetReceiveNum(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex);

        /// <summary>
        /// 清空指定缓冲区
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "ClearBuffer")]
        public static extern ECANStatus ClearBuffer(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex);

        /// <summary>
        /// 启动CAN
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "StartCAN")]
        public static extern ECANStatus StartCAN(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex);

        /// <summary>
        /// 返回实际发送的帧数
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <param name="send">要发送的数据帧数组的首指针</param>
        /// <param name="Len">要发送的数据帧数组的长度</param>
        /// <returns>返回实际发送的帧数</returns>
        [DllImport("ECanVci64.dll", EntryPoint = "Transmit")]
        public static extern ECANStatus Transmit(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex,
            CAN_OBJ[] send,
            ulong Len);

        /// <summary>
        /// 从指定的设备读取数据
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <param name="Receive">用来接收的数据帧数组的首指针</param>
        /// <param name="Len">用来接收的数据帧数组的长度</param>
        /// <param name="WaitTime">等待超时时间，以毫秒为单位</param>
        /// <returns>返回实际读取到的帧数。如果返回值为0xFFFFFFFF，则表示读取数据失败，有错误发生，请调用ReadErrInfo函数来获取错误码</returns>
        [DllImport("ECanVci64.dll", EntryPoint = "Receive")]
        public static extern ECANStatus Receive(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex,
            out CAN_OBJ Receive,
            ulong Len,
            int WaitTime);

        /// <summary>
        /// 复位CAN
        /// </summary>
        /// <param name="DevType">设备类型号</param>
        /// <param name="DevIndex">设备索引号，比如当只有一个设备时，索引号为0，有两个时可以为0或1</param>
        /// <param name="CANIndex">第几路CAN</param>
        /// <returns></returns>
        [DllImport("ECanVci64.dll", EntryPoint = "ResetCAN")]
        public static extern ECANStatus ResetCAN(
            UInt32 DevType,
            UInt32 DevIndex,
            UInt32 CANIndex);

    }
}
