using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class CanFrameDataInfoDal
    {
        /// <summary>
        /// DataGuid
        /// </summary>
        public string DataGuid { get; set; }

        /// <summary>
        /// 数据名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据类型（如uint16）
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// //字节范围（如2，3~4）
        /// </summary>
        public int ByteRange { get; set; }   

        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 精度
        /// </summary>
        public decimal? Precision { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 隐藏
        /// </summary>
        public bool Hide { get; set; } 
    }
}
