using Communication.Can;
using SofarHVMExe.Utilities.Global;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SofarHVMExe.Utilities
{
    public class DebugTool
    {
        public static bool cancel { get; set; } = true;

        public static void Output(string msg, bool writeLine = true, bool showTime = true)
        {
            if (showTime)
            {
                msg = DateTime.Now.ToString("yy:mm:dd HH:mm:ss.fff ") + msg;
            }

            if (writeLine)
                Debug.WriteLine(msg);
            else
                Debug.Write(msg);
        }
        public static void OutputBytes(string hint, byte[] bytes, bool showTime = true)
        {
            if (bytes == null)
                return;

            string recvVal = "";
            foreach (byte v in bytes)
            {
                recvVal += v.ToString("X2") + " ";
            }

            string msg = (hint == "") ? recvVal : (hint + "：" + recvVal);
            string time = DateTime.Now.ToString("yy:mm:dd HH:mm:ss.fff");
            if (showTime)
            {
                msg = time + " : " + msg;
            }
            Debug.WriteLine(msg);
            MultiLanguages.Common.LogHelper.WriteLog(msg);
        }


        #region 心跳功能调试
        /// <summary>
        /// 开始模拟设备心跳
        /// </summary>
        /// <param name="ecanHelper"></param>
        public static void StartSimulateDevice(EcanHelper ecanHelper)
        {
            if (ecanHelper.IsCan2Start)
            {
                cancel = false;

                Task.Run(() =>
                {
                    while (!cancel)
                    {
                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4121, new byte[] { }); //1

                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4122, new byte[] { }); //2

                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4123, new byte[] { }); //3

                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4124, new byte[] { }); //4

                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4125, new byte[] { }); //5

                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4126, new byte[] { }); //6

                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4127, new byte[] { }); //7

                        Thread.Sleep(10);
                        ecanHelper.SendCan2(0x197F4128, new byte[] { }); //8
                    }
                });
            }
        }

        /// <summary>
        /// 停止模拟设备心跳
        /// </summary>
        /// <param name="ecanHelper"></param>
        public static void StopSimulateDevice(EcanHelper ecanHelper)
        {
            if (ecanHelper.IsCan2Start)
            {
                cancel = true;
            }
        }
        #endregion


        #region 程序更新功能调试
        public static void DebugShakeHand()
        {
            //Can2接收处理
            CommManager.Instance().RegistRecvProc(null, recvData =>
            {
                uint id = recvData.ID;
                byte[] datas = recvData.Data;
                if (id != 0x1e322041)
                    return;

                EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
                if (ecanHelper.IsCan2Start)
                {
                    byte[] send = new byte[8] { 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00};
                    ecanHelper.SendCan2(0x1F324121, send);
                }
            });
        }
        #endregion


        #region 监控功能调试
        public static void DebugRecvData()
        {
            //Can2接收处理
            CommManager.Instance().RegistRecvProc(null, recvData =>
            {
                uint id = recvData.ID;
                byte[] datas = recvData.Data;
                if (id != 0x1A002041)
                    return;

                EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
                if (ecanHelper.IsCan2Start)
                {
                    byte[] send = new byte[8] { 0x00, 0x00, 0x03, 255, 0x7D, 0x7F, 0x56, 0x42 };
                    ecanHelper.SendCan2(0x19004321, send);
                }
            });
        }


        #endregion

    }//class
}
