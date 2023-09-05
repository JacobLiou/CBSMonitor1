using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class EventGroupDal
    {
        /// <summary>
        /// 组号
        /// </summary>
        public int Group { get; set; } = 0;

        /// <summary>
        /// 事件使能
        /// </summary>
        public bool Enable { get; set; } = false;

        /// <summary>
        /// Can帧Guid
        /// </summary>
        public string FrameGuid { get; set; } = "";
        public int MemberIndex { get; set; } = -1;
    }
}
