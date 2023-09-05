using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class ChannelInfoDal
    {
        public string VariableName { get; set; } = "";

        public int DataType { get; set; } = 0;

        public int FloatDataScale { get; set; } = 0;

        public string Comment { get; set; } = "";
    }
}
