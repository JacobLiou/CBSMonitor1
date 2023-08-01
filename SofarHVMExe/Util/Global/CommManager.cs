using Communication.Can;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SofarHVMExe.Utilities.Global
{
    /// <summary>
    /// 通讯管理器单例类
    /// </summary>
    public class CommManager
    {
        private EcanHelper ecanHelper = null;


        #region 单例初始化
        private CommManager()
        {
            ecanHelper = new EcanHelper();
        }

        private static class SingletonInstance
        {
            public static CommManager INSTANCE = new CommManager();
        }
        public static CommManager Instance()
        {
            return SingletonInstance.INSTANCE;
        }
        #endregion


        #region 操作
        public EcanHelper GetCanHelper()
        {
            return ecanHelper;
        }

        public void SetCanHelper(EcanHelper helper)
        {
            ecanHelper = helper;
        }

        public bool CheckConnect(bool hint = true)
        {
            if (!ecanHelper.IsConnected)
            {
                if (hint)
                {
                    MessageBox.Show("未连接CAN设备！", "提示");
                }
                return false;
            }

            if (!ecanHelper.IsCan1Start && !ecanHelper.IsCan2Start)
            {
                if (hint)
                {
                    MessageBox.Show("通道未打开！", "提示");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 注册接收处理方法
        /// </summary>
        /// <param name="recvProcCan1"></param>
        /// <param name="recvProcCan2"></param>
        public void RegistRecvProc(Action<CAN_OBJ> recvProcCan1, Action<CAN_OBJ> recvProcCan2)
        {
            if (recvProcCan1 != null)
            {
                //ecanHelper.OnReceiveCan1 += recvProcCan1;
                ecanHelper.RegisterRecvProcessCan1(recvProcCan1);
            }

            if (recvProcCan2 != null)
            {
                //ecanHelper.OnReceiveCan2 += recvProcCan2;
                ecanHelper.RegisterRecvProcessCan2(recvProcCan2);
            }
        }
        #endregion

    }//class
}
