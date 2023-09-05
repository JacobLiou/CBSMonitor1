using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class EventInfoDal
    {
        /// <summary>
        /// 组号
        /// </summary>
        public int GroupID { get; set; } = 0;
        public bool Enable { get; set; }
        public int Bit { get; set; } = 0;
        public string Type { get; set; }
        public string Name { get; set; } = "";
        public string Mark { get; set; } = "";
    }
}
