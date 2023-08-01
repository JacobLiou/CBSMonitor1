using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    public class OscilloscopeModel
    {
        public OscilloscopeFileModel FilesPath { get; set; } = new();
        
        public List<ChannelInfoModel> ChannelInfoList { get; set; } = new();

        public int UnderSampleScale { get; set; } = 1;

        public int TrigMode { get; set; } = 0;

        public int TrigSource { get; set; } = 0;

        public int TrigYLevel { get; set; } = 1;
        
    }

    

    public class OscilloscopeFileModel
    {
        public string CoffPath { get; set; } = "";

        public string DwarfXmlPath { get; set; } = "";
    }

    public class ChannelInfoModel
    {
        public string VariableName { get; set; } = "";

        public int DataType { get; set; } = 0;

        public int FloatDataScale { get; set; } = 0;

        public string Comment { get; set; } = "";
    }
}
