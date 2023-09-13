using CanProtocol.ProtocolModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SofarHVMExe.Model
{
    public class FrameConfigModel
    {
        public FrameConfigModel()
        {
            SrcIdBitNum = 3;
            SrcAddrBitNum = 8 - 3;
            TargetIdBitNum = 3;
            TargetAddrBitNum = 8 - 3;

            CanFrameModels = new List<CanFrameModel>();
        }

        //源设备ID位数
        public byte SrcIdBitNum { get; set; }

        //源设备地址位数
        public byte SrcAddrBitNum { get; set; }

        //目标设备ID位数
        public byte TargetIdBitNum { get; set; }

        //目标设备地址位数
        public byte TargetAddrBitNum { get; set; }

        public List<CanFrameModel> CanFrameModels { get; set; }

        /// <summary>
        /// 查找指定guid的CanFrameModel
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public CanFrameModel FindFrameModel(string guid)
        {
            if (guid == null || guid == "")
                return null;

            return CanFrameModels.Find((model) =>
            {
                return model.Guid == guid;
            });
        }

        /// <summary>
        /// 查找指定guid的CanFrameModel的索引号
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public int FindFrameModelIndex(string guid)
        {
            if (guid == null || guid == "")
                return -1;

            return CanFrameModels.FindIndex((model) =>
            {
                return model.Guid == guid;
            });
        }

        public void ReSortCanFrameModels()
        {
            CanFrameModels = CanFrameModels.OrderBy(t => t.Sort).ToList();
        }
    }//class


}
