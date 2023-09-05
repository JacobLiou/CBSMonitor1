using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SofarHVMExe.Model;

namespace SofarHVMExe.DbModels
{
    internal class OscilloscopeDal
    {
        public string CoffPath { get; set; } = "";

        public string DwarfXmlPath { get; set; } = "";

        public int UnderSampleScale { get; set; } = 1;

        public int TrigMode { get; set; } = 0;

        public int TrigSource { get; set; } = 0;

        public int TrigYLevel { get; set; } = 1;

        public int TrigXPercent { get; set; } = 1;
    }
}
