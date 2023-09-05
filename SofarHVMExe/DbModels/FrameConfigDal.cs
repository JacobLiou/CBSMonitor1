using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class FrameConfigDal
    {
        /// <summary>
        /// 源设备ID位数
        /// </summary>
        public byte SrcIdBitNum { get; set; }

        /// <summary>
        /// 源设备地址位数
        /// </summary>
        public byte SrcAddrBitNum { get; set; }

        /// <summary>
        /// 目标设备ID位数
        /// </summary>
        public byte TargetIdBitNum { get; set; }

        /// <summary>
        /// 目标设备地址位数
        /// </summary>
        public byte TargetAddrBitNum { get; set; }
    }
}
