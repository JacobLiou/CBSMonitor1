using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Utilities
{
    /// <summary>
    /// 发送/接收的帧信息类
    /// </summary>
    internal class SendRecvFrameInfo
    {
        public SendRecvFrameInfo() { }

        public SendRecvFrameInfo(SendRecvFrameInfo other) 
        { 
            IsSend = other.IsSend;
            IsContinue = other.IsContinue;
            Time = other.Time;
            ID = other.ID;
            Datas = other.Datas;
            Info1= other.Info1;
            Value1 = other.Value1;
            Info2 = other.Info2;
            Value2 = other.Value2;
            Info3 = other.Info3;
            Value3 = other.Value3;
            Info4 = other.Info4;
            Value4 = other.Value4;
            Info5 = other.Info5;
            Value5 = other.Value5;
        }


        public bool IsSend { get; set; }
        public bool IsContinue { get; set; }
        public string Time { get; set; }
        public string ID { get; set; }
        public string Addr { get; set; }
        public string PackageNum { get; set; }
        public string Datas { get; set; }
        public string Info1 { get; set; }
        public string Value1 { get; set; }
        public string Info2 { get; set; }
        public string Value2 { get; set; }
        public string Info3 { get; set; }
        public string Value3 { get; set; }
        public string Info4 { get; set; }
        public string Value4 { get; set; }
        public string Info5 { get; set; }
        public string Value5 { get; set; }
    }
}
