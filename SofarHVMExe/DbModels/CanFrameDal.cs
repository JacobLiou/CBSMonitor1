using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SofarHVMExe.DbModels
{
    internal class CanFrameDal
    {
        public string Guid { get; set; }

        /// <summary>
        /// ID帧
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// 帧名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否自动下发
        /// </summary>
        public bool AutoTx { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 源设备地址
        /// </summary>
        public byte SrcAddr { get; set; }

        /// <summary>
        /// 源设备类型/id
        /// </summary>
        public byte SrcType { get; set; }

        /// <summary>
        /// 目标设备地址
        /// </summary>
        public byte DstAddr { get; set; }

        /// <summary>
        /// 目标设备类型/id
        /// </summary>
        public byte DstType { get; set; }

        /// <summary>
        /// 功能码
        /// </summary>
        public byte FC { get; set; }

        public byte PF { get; set; }

        /// <summary>
        /// 连续标识
        /// 0：非连续；
        /// 1：连续
        /// </summary>
        public byte ContinuousFlag { get; set; }

        /// <summary>
        /// 帧类型
        /// 0：J1939标准帧;
        /// 1：数据帧；
        /// 2：请求帧；
        /// 3：应答帧；
        /// </summary>
        public byte FrameType { get; set; }

        /// <summary>
        /// 优先级
        /// 3：控制帧；
        /// 6：数据帧；
        /// 7：boot帧
        /// </summary>
        public byte Priority { get; set; }
    }
}
