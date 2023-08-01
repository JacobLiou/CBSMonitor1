using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CanProtocol.ProtocolModel
{
    public class CanFrameID
    {
        public CanFrameID() 
        { 
            
        }

        public CanFrameID(uint id)
        {
            ID = id;
        }

        /// <summary>
        /// 复制构造
        /// </summary>
        /// <param name="other"></param>
        public CanFrameID(CanFrameID other) 
        {
            srcAddr = other.srcAddr;
            srcType = other.srcType;
            dstAddr = other.dstAddr;
            dstType = other.dstType;
            pf = other.pf;
            continuousFlag = other.continuousFlag;
            frameType = other.frameType;
            priority = other.priority;
        }

        public uint ID
        {
            get
            {
                byte[] bytes = new byte[4];

                bytes[0] = (byte)(srcAddr & 0x1F);
                bytes[0] |= (byte)((srcType & 0x07) << 5);
                bytes[1] = (byte)(dstAddr & 0x1F);
                bytes[1] |= (byte)((dstType & 0x07) << 5);
                bytes[2] = (byte)(pf & 0x7F);
                bytes[2] |= (byte)((continuousFlag & 0x1) << 7);
                bytes[3] = (byte)(frameType & 0x03);
                bytes[3] |= (byte)((priority & 0x07) << 2);

                return BitConverter.ToUInt32(bytes);
            }

            set
            {
                uint id = value;
                srcAddr = (byte)(id & 0x1f);
                srcType = (byte)((id >> 5) & 0x7);
                dstAddr = (byte)((id >> 8) & 0x1f);
                dstType = (byte)((id >> 13) & 0x7);
                pf = (byte)((id >> 16) & 0x7f);
                continuousFlag = (byte)((id >> 23) & 0x1);
                frameType = (byte)((id >> 24) & 0x3);
                priority = (byte)((id >> 26) & 0x7);
            }
        }

        private byte srcAddr;       //源设备地址
        private byte srcType;       //源设备类型/id
        private byte dstAddr;       //目标设备地址
        private byte dstType;       //目标设备类型/id
        private byte pf;            //功能类型/功能码
        private byte continuousFlag;   //连续标识
        private byte frameType;     //帧类型
        private byte priority;          //优先级

        /// <summary>
        /// 源设备地址
        /// </summary>
        public byte SrcAddr { get => srcAddr; set { srcAddr = value; } }

        /// <summary>
        /// 源设备类型/id
        /// </summary>
        public byte SrcType { get => srcType; set { srcType = value; } }

        /// <summary>
        /// 目标设备地址
        /// </summary>
        public byte DstAddr { get => dstAddr; set { dstAddr = value; } }

        /// <summary>
        /// 目标设备类型/id
        /// </summary>
        public byte DstType { get => dstType; set { dstType = value; } }         

        /// <summary>
        /// 功能码
        /// </summary>
        public byte FC { get => pf; set { pf = value; } }

        public byte PF { get => pf; set { pf = value; } }

        /// <summary>
        /// 连续标识
        /// 0：非连续；
        /// 1：连续
        /// </summary>
        public byte ContinuousFlag { get => continuousFlag; set { continuousFlag = value; } }         

        /// <summary>
        /// 帧类型
        /// 0：J1939标准帧;
        /// 1：数据帧；
        /// 2：请求帧；
        /// 3：应答帧；
        /// </summary>
        public byte FrameType { get => frameType; set { frameType = value; } }         

        /// <summary>
        /// 优先级
        /// 3：控制帧；
        /// 6：数据帧；
        /// 7：boot帧
        /// </summary>
        public byte Priority { get => priority; set { priority = value; } }         
    }//class
}
