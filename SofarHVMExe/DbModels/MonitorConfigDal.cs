using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SofarHVMExe.DbModels
{
    internal class MonitorConfigDal
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Title { get; set; }          //主参数名称
        /// <summary>
        /// CAN ID
        /// </summary>
        public string CanText { get; set; }        //CAN_ID+Name
        /// <summary>
        /// 成员
        /// </summary>
        public string MemberText { get; set; }    //成员坐标+Name
        /// <summary>
        /// 上红色警戒值
        /// </summary>
        public double AlertVal1 { get; set; }      //上红色警戒值
        /// <summary>
        /// 上黄色警戒值
        /// </summary>
        public double AlertVal2 { get; set; }      //上黄色警戒值
        /// <summary>
        /// 下黄色警戒值
        /// </summary>
        public double AlertVal3 { get; set; }      //下黄色警戒值
        /// <summary>
        /// 下红色警戒值
        /// </summary>
        public double AlertVal4 { get; set; }      //下红色警戒值
    }
}
