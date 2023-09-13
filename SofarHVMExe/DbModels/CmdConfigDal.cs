using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class CmdConfigDal
    {
        /// <summary>
        /// CanID
        /// </summary>
        public uint CmdID { get; set; }
        public int CmdType { get; set; }
        public string FrameGuid { get; set; }   //Can帧的唯一id标识

        public string SetValue { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// guid 唯一识别
        /// </summary>
        public string Guid { get; set; }
    }
}
