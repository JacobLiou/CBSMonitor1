using SofarHVMExe.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.SubPubEvent.Events
{
    public class LogInfiEvent
    {
        //public List<object> Infos { get; set; }
        public List<CurrentEventModel> CurrentEventList { get; set; } = new List<CurrentEventModel>();
        public LogInfiEvent(List<CurrentEventModel> currentEventList)
        {
            this.CurrentEventList = currentEventList;
        }
    }
}
