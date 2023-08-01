using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    //[JsonObject(Title = "项目配置")] //无效果
    public class PrjConfigModel
    {
        public PrjConfigModel() 
        {
            DeviceInx = 0;
            Baudrate1 = 500;
            Baudrate2 = 500;
            SendInterval = 1;
        }



        [JsonProperty("项目名称")]
        public string ProjectName { get; set; } //项目名称
        [JsonProperty("工作路径")]
        public string WorkPath { get; set; }    //工作路径
        [JsonProperty("设备索引号")]
        public uint DeviceInx { get; set; }       //设备索引号
        [JsonProperty("Can1波特率")]
        public int Baudrate1 { get; set; }       //Can1波特率
        [JsonProperty("Can2波特率")]
        public int Baudrate2 { get; set; }       //Can1波特率
        [JsonProperty("发送间隔")]
        public int SendInterval { get; set; }   //发送间隔
        [JsonProperty("地址掩码")]
        public int AddrMark { get; set; }       //地址掩码
        [JsonProperty("心跳帧滤波")]
        public int HeartbeatFrame { get; set; } //心跳帧滤波
        [JsonProperty("起始帧滤波")]
        public int StartFrame { get; set; }     //起始帧滤波
        [JsonProperty("数据帧滤波")]
        public int DataFrame { get; set; }      //数据帧滤波
        [JsonProperty("路由超时")]
        public int RouteTimeout { get; set; }   //路由超时
        [JsonProperty("线程超时")]
        public int ThreadTimeout { get; set; }  //线程超时
        [JsonProperty("主机心跳帧Guid")]
        public string HostFrameGuid { get; set; }  //主机心跳帧Guid（上位机心跳）
        [JsonProperty("模块心跳帧Guid")]
        public string ModuleFrameGuid { get; set; } //模块心跳帧Guid（设备心跳）

        //[JsonProperty("主机心跳")]
        //public uint HostId { get; set; }         //主机心跳（上位机心跳）
        //[JsonProperty("模块心跳")]
        //public uint ModuleId { get; set; }       //模块心跳（设备心跳）
    }//class
}
