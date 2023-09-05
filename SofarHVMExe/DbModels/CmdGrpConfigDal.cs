using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.DbModels
{
    internal class CmdGrpConfigDal
    {
        /// <summary>
        /// 编号
        /// </summary>
        public int Index { get; set; } = 0;

        /// <summary>
        /// 命令组名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否为广播命令
        /// </summary>
        public bool IsBroadcast { get; set; } = false;  
    }
}
