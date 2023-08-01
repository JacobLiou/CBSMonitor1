using Communication.Can;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SofarHVMExe.Utilities.Global
{
    public class GlobalManager
    {
        public enum Page
        {
            None,
            Can_Connect,
            HeartBeat,
            Monitor,
            MapOpt,
            Oscilloscope,
            Download,
            Config,
            FileOpt,
            Safety
        }

        public enum StatusBarOption
        {
            None = 0,   
            ConnectStatus,
            ConnectNum,
            ConnectDevs,
            SelectDev,
            CanErrInfo
        }

        public Page CurrentPage = Page.Can_Connect;
        public Action<StatusBarOption, string> UpdataStatusAction = null;
        public Action<bool> UpdataUILock = null;

        #region 单例初始化
        private GlobalManager()
        {
        }
        
        private static class SingletonInstance
        {
            public static GlobalManager INSTANCE = new GlobalManager();
        }
        public static GlobalManager Instance()
        {
            return SingletonInstance.INSTANCE;
        }
        #endregion


        #region 操作
        #region 状态栏
        /// <summary>
        /// 更新连接状态
        /// </summary>
        public void UpdateStatusBar_ConnectStatus(string value)
        {
            UpdataStatusAction?.Invoke(StatusBarOption.ConnectStatus, value);
        }
        /// <summary>
        /// 更新CAN错误状态
        /// </summary>
        public void UpdateStatusBar_CanErrInfo(string value)
        {
            UpdataStatusAction?.Invoke(StatusBarOption.CanErrInfo, value);
        }
        /// <summary>
        /// 更新选择的设备
        /// </summary>
        /// <param name="value"></param>
        public void UpdateStatusBar_SelectDev(string value)
        {
            UpdataStatusAction?.Invoke(StatusBarOption.SelectDev, value);
        }
        /// <summary>
        /// 更新连接的设备
        /// </summary>
        /// <param name="value"></param>
        public void UpdateStatusBar_ConnectDevs(string devs)
        {
            UpdataStatusAction?.Invoke(StatusBarOption.ConnectDevs, devs);
        }

        #endregion
        #endregion

    }//class
}
