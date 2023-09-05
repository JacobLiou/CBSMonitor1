using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SofarHVMExe.DbModels
{
    internal class PrjConfigDal
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } //项目名称
        /// <summary>
        /// 工作路径
        /// </summary>
        public string WorkPath { get; set; }    //工作路径
        /// <summary>
        /// 设备索引号
        /// </summary>
        public int DeviceInx { get; set; }       //设备索引号
        /// <summary>
        /// Can1波特率
        /// </summary>
        public int Baudrate1 { get; set; }       //Can1波特率
        /// <summary>
        /// Can2波特率
        /// </summary>
        public int Baudrate2 { get; set; }       //Can1波特率
        /// <summary>
        /// 发送间隔
        /// </summary>
        public int SendInterval { get; set; }   //发送间隔
        /// <summary>
        /// 地址掩码
        /// </summary>
        public int AddrMark { get; set; }       //地址掩码
        /// <summary>
        /// 心跳帧滤波
        /// </summary>
        public int HeartbeatFrame { get; set; } //心跳帧滤波
        /// <summary>
        /// 起始帧滤波
        /// </summary>
        public int StartFrame { get; set; }     //起始帧滤波
        /// <summary>
        /// 数据帧滤波
        /// </summary>
        public int DataFrame { get; set; }      //数据帧滤波
        /// <summary>
        /// 路由超时
        /// </summary>
        public int RouteTimeout { get; set; }   //路由超时
        /// <summary>
        /// 线程超时
        /// </summary>
        public int ThreadTimeout { get; set; }  //线程超时
        /// <summary>
        /// 主机心跳帧Guid
        /// </summary>
        public string HostFrameGuid { get; set; }  //主机心跳帧Guid（上位机心跳）
        /// <summary>
        /// 模块心跳帧Guid
        /// </summary>
        public string ModuleFrameGuid { get; set; } //模块心跳帧Guid（设备心跳）
    }
}
