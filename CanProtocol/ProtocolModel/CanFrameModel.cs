using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanProtocol.ProtocolModel
{
    /// <summary>
    /// 表示一个CAN帧
    /// </summary>
    public class CanFrameModel
    {
        public CanFrameModel()
        {
            FrameId = new CanFrameID();
            FrameId.FrameType = 1; //默认为数据帧
            FrameDatas = new List<CanFrameData>();
        }

        /// <summary>
        /// 复制构造
        /// </summary>
        /// <param name="other"></param>
        public CanFrameModel(CanFrameModel other)
        {
            Guid = other.Guid;
            Name = other.Name;
            AutoTx = other.AutoTx;

            FrameId = new CanFrameID(other.FrameId);
            Id = FrameId.ID;

            FrameDatas = new List<CanFrameData>();
            foreach (var otherData in other.FrameDatas)
            {
                CanFrameData newData = new CanFrameData(otherData);
                FrameDatas.Add(newData);
            }
            //for (int i = 0; i < other.FrameDatas.Count; i++)
            //{
            //    CanFrameData otherData = other.FrameDatas[i];
            //    CanFrameData newData = new CanFrameData(otherData);
            //    FrameDatas.Add(newData);
            //}
        }

        public CanFrameModel(uint id, string name, bool autoTx)
        {
            FrameId = new CanFrameID();
            FrameDatas = new List<CanFrameData>();

            Id = id;
            Name = name;
            AutoTx = autoTx;
        }

        //唯一id标识，用于区分不同的帧（下面的id是CAN id, 这个id是Guid，注意区分）
        public string Guid { get; set; }
        public uint Id //ID帧
        {
            get => FrameId.ID;
            set
            {
                FrameId.ID = value;
            }
        }
        public string Name { get; set; } //帧名称
        public bool AutoTx { get; set; } //是否自动下发

        public CanFrameID FrameId { get; set; } //id详细信息
        public List<CanFrameData> FrameDatas { get; set; } //多包数据集合（非连续时只有一包数据）

        /// <summary>
        /// 获取地址点表地址
        /// </summary>
        /// <returns></returns>
        public string GetAddr()
        {
            if (FrameId.ContinuousFlag == 0)
            {
                //非连续
                return FrameDatas[0].DataInfos[0].Value;
            }
            else
            {
                //连续
                return FrameDatas[0].DataInfos[1].Value;
            }
        }

        /// <summary>
        /// 获取地址点表地址
        /// </summary>
        /// <returns></returns>
        public int GetAddrInt()
        {
            string strAddr = GetAddr();
            if (strAddr.Contains("0x") || strAddr.Contains("0X"))
            {
                strAddr = strAddr.Replace("0x", "").Replace("0X", "");
                return int.Parse(strAddr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            else 
            {
                return int.Parse(strAddr);
            }
        }

        /// <summary>
        /// 获取包序号
        /// 只给连续多包使用
        /// </summary>
        /// <returns></returns>
        public string GetPackageNum()
        {
            return FrameDatas[0].DataInfos[0].Value;
        }
    }//class
}
