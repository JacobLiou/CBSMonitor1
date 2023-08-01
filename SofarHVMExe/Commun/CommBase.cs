
using System;
using System.Threading;

namespace Communication
{
    /// <summary>
    /// 通讯抽象类
    /// </summary>
    public abstract class CommBase<T>
    {
        /// <summary>
        /// 设备连接状态
        /// </summary>
        public abstract bool IsConnected { get; set; }

        /// <summary>
        /// 接收线程运行状态（已启动/未启动）
        /// </summary>
        public virtual bool IsReceiveState { get; set; }

        /// <summary>
        /// 接收线程运行状态（已启动，是否取消线程）
        /// </summary>
        public virtual CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// 接收委托
        /// </summary>
        public virtual Action<T> OnReceive { get; set; }

        /// <summary>
        /// 打开设备
        /// </summary>
        /// <returns>执行结果</returns>
        public abstract bool Open();

        /// <summary>
        /// 关闭设备
        /// </summary>
        /// <returns>执行状态</returns>
        public abstract bool Close();

        /// <summary>
        /// 接收线程
        /// </summary>
        public virtual void StartReceiveThread() { }

        /// <summary>
        /// 发送数据，应用于TCP/IP协议
        /// </summary>
        /// <param name="data">下发的字符串</param>
        /// <returns>执行结果</returns>
        public virtual bool Send(string data) { return true; }

        /// <summary>
        /// 发送数据，支持大部分协议，排除CAN协议
        /// </summary>
        /// <param name="bytes">下发的byte数组</param>
        /// <returns>执行结果</returns>
        public virtual bool Send(byte[] bytes) { return true; }

        /// <summary>
        /// 发送数据，支持ECAN,ZLGCAN协议
        /// </summary>
        /// <param name="id">帧ID</param>
        /// <param name="bytes">帧数据</param>
        /// <returns>执行结果</returns>
        public virtual bool Send(uint id, byte[] bytes) { return true; }

        /// <summary>
        /// 发送数据，支持ECAN,ZLGCAN协议
        /// </summary>
        /// <param name="id">帧ID</param>
        /// <param name="bytes">帧数据</param>
        /// <returns>执行结果</returns>
        public virtual bool Send(uint id, string data) { return true; }
    }
}