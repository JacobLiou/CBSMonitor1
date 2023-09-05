using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class CanFrameDataDal
    {
        /// <summary>
        /// CanGuid
        /// </summary>
        public string Guid { get; set; }

        /// <summary>
        /// CanGuid
        /// </summary>
        public string CanGuid { get; set; }

        /// <summary>
        /// 数据长度
        /// </summary>
        public int DataLen { get; set; } //数据长度

        /// <summary>
        /// 数据内容
        /// </summary>
        public byte[] Data { get; set; } //数据内容

        /// <summary>
        /// 有效数据个数（用于连续多包中 结束帧的有效数据 计数）
        /// </summary>
        public int DataNum { get; set; } //有效数据个数（用于连续多包中 结束帧的有效数据 计数）
    }
}
