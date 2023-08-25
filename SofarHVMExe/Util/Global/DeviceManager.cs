using SofarHVMExe.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SofarHVMExe.Utilities.Global
{
    /// <summary>
    /// 设备管理器
    /// 单例类
    /// </summary>
    public class DeviceManager
    {
        public List<Device> Devices = new List<Device>();
        public bool UpdateDev = true;


        #region 单例初始化
        private DeviceManager() { }
        private static class SingletonInstance
        {
            public static DeviceManager INSTANCE = new DeviceManager();
        }
        public static DeviceManager Instance()
        {
            return SingletonInstance.INSTANCE;
        }
        #endregion


        #region 操作
        public void InitDevice()
        {
            for (int i = 0; i < 10; i++)
            {
                Device dev = new Device();
                dev.Name = "设备" + (i + 1).ToString();
                dev.Address = (i + 1);
                dev.SelectAction = new Action<Device>(ClearSelect);
                Devices.Add(dev);
            }
        }

        public void ClearSelect(Device selectDev)
        {
            foreach (Device dev in Devices)
            {
                if (dev != selectDev && dev.Selected)
                {
                    dev.UpdateStatusBar = false;
                    dev.Selected = false;
                }
            }
        }
        /// <summary>
        /// 获取选择的设备地址
        /// 0：没有选择设备 其他：设备地址
        /// </summary>
        /// <returns></returns>
        public byte GetSelectDev()
        {
            Device? findDev = Devices.Find((dev) =>
            {
                return dev.Selected == true;
            });

            if (findDev != null)
            {
                return (byte)findDev.address;
            }

            return 0;
        }

        /// <summary>
        /// 订阅设备选择动作
        /// </summary>
        /// <param name="method"></param>
        public void SubscribDevSelect(Action<Device> method)
        {
            foreach (Device dev in Devices)
            {
                dev.SelectAction += method;
            }
        }

        public List<Device> GetConnectDevs()
        {
            return Devices.FindAll((dev) => dev.Connected == true);
        }
        #endregion

    }//class
}
