using CanProtocol.ProtocolModel;
using SofarHVMExe.Model;
using SofarHVMExe.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Utilities
{
    internal class FrameHelper
    {
        /// <summary>
        /// 查找指定Guid的Can帧
        /// </summary>
        /// <param name="fileCfgModel"></param>
        /// <param name="frameGuid"></param>
        /// <returns></returns>
        public static CanFrameModel GetFrame(FileConfigModel fileCfgModel, string frameGuid)
        {
            if (fileCfgModel == null || fileCfgModel.FrameModel == null)
                return null;

            CanFrameModel? findFrame = fileCfgModel.FrameModel.CanFrameModels.Find(frameModel =>
            {
                if (frameModel.Guid== frameGuid) 
                    return true;

                return false;
            });

            return findFrame;
        }

        /// <summary>
        /// 查找指定Guid的Can帧的索引
        /// </summary>
        /// <param name="fileCfgModel"></param>
        /// <param name="frameGuid"></param>
        /// <returns>-1：没找到</returns>
        public static int GetFrameIndex(FileConfigModel fileCfgModel, string frameGuid)
        {
            if (fileCfgModel == null || fileCfgModel.FrameModel == null)
                return -1;

            return fileCfgModel.FrameModel.CanFrameModels.FindIndex(frameModel =>
            {
                if (frameModel.Guid == frameGuid)
                    return true;

                return false;
            });
        }

    }//class
}
