using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SofarHVMExe.DbModels
{
    /// <summary>
    /// 
    /// </summary>
    internal class McuConfigDal
    {
        /// <summary>
        /// MCU使能
        /// </summary>
        public bool McuEnable { get; set; }
        /// <summary>
        /// MCU名
        /// </summary>
        public string McuName { get; set; }
        /// <summary>
        /// AppFlash文件类型
        /// </summary>
        public string FileType { get; set; }
        /// <summary>
        /// AppFlash AddrMin
        /// </summary>
        public uint AddrMin { get; set; }
        /// <summary>
        /// AppFlash AddrMax
        /// </summary>
        public uint AddrMax { get; set; }

        public int MemWidth { get; set; }
        /// <summary>
        /// AppFwCode
        /// </summary>
        public uint FwCode { get; set; }
    }
}
