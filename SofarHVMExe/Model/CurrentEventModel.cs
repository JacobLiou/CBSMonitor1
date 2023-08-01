using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    public class CurrentEventModel
    {
        public string DatetimeStr { get { return System.DateTime.Now.ToString(); } }
        public uint CANId { get; set; }
        public EventInfoModel eventInfoModel { get; set; }
    }
}
