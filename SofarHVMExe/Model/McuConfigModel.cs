using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Model
{
    public class McuConfigModel
    {
        public McuConfigModel()
        {
            McuEnable = false;
            McuName = "MCU";
            FileType = "0(out)";
            AddrMin = 0x0;
            AddrMax = 0x0;
            MemWidth = 1;
            FwCode = 0xFFFFFFFF;
        }

        public McuConfigModel(bool mcuEnable, string mcuName, string fileType, uint addrMin,
                              uint addrMax, uint memWidth, uint fwCode)
        {
            McuEnable = mcuEnable;
            McuName = mcuName;
            FileType = fileType;
            AddrMin = addrMin;
            AddrMax = addrMax;
            MemWidth = memWidth;
            FwCode = fwCode;
        }

        [JsonProperty("MCU使能")]
        public bool McuEnable { get; set; }
        [JsonProperty("MCU名")]
        public string McuName { get; set; }
        [JsonProperty("AppFlash文件类型")]
        public string FileType { get; set; }
        [JsonProperty("AppFlash AddrMin")]
        public uint AddrMin { get; set; }
        [JsonProperty("AppFlash AddrMax")]
        public uint AddrMax { get; set; }
        public uint MemWidth { get; set; }
        [JsonProperty("AppFwCode")]
        public uint FwCode { get; set; }
    }
}