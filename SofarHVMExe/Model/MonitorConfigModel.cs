using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    public class MonitorConfigModel
    {
        public MonitorConfigModel() { }

        public MonitorConfigModel(string title, string canText, string memberText, double alertVal1, double alertVal2, double alertVal3, double alertVal4)
        {
            Title = title;
            CanText = canText;
            MemberText = memberText;
            AlertVal1 = alertVal1;
            AlertVal2 = alertVal2;
            AlertVal3 = alertVal3;
            AlertVal4 = alertVal4;
        }

        [JsonProperty("名称")]
        public string Title { get; set; }          //主参数名称
        [JsonProperty("CAN ID")]
        public string CanText { get; set; }        //CAN_ID+Name
        [JsonProperty("成员")]
        public string MemberText { get; set; }    //成员坐标+Name
        [JsonProperty("上红色警戒值")]
        public double AlertVal1 { get; set; }      //上红色警戒值
        [JsonProperty("上黄色警戒值")]
        public double AlertVal2 { get; set; }      //上黄色警戒值
        [JsonProperty("下黄色警戒值")]
        public double AlertVal3 { get; set; }      //下黄色警戒值
        [JsonProperty("下红色警戒值")]
        public double AlertVal4 { get; set; }      //下红色警戒值
    }
}
