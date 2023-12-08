using CanProtocol.ProtocolModel;
using Communication.Can;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace SofarHVMExe.ViewModel
{
    class HeartBeatPageVm : ViewModelBase
    {
        public HeartBeatPageVm(EcanHelper? helper)
        {
            ecanHelper = helper;
            Init();
        }

        public EcanHelper? ecanHelper = null;
        /// <summary>
        /// 消息框中消息文本
        /// </summary>
        private string message = "";
        public string Message
        {
            get => message;
            set { message = value; OnPropertyChanged(); }
        }
        public List<Device> DataSource
        {
            get
            {
                return DeviceManager.Instance().Devices;
            }
            set
            {

                DeviceManager.Instance().Devices = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartBroadcastCommand { get; set; }
        public ICommand StopBroadcastCommand { get; set; }

        private void Init()
        {
            StartBroadcastCommand = new SimpleCommand(StartBroadcast);
            StopBroadcastCommand = new SimpleCommand(StopBroadcast);

            //CAN
            DeviceManager.Instance().InitDevice();
        }


        /// <summary>
        /// 开始广播
        /// </summary>
        /// <param name="o"></param>
        private void StartBroadcast(object o)
        {
            //调用can发送广播帧
            if (!CheckConnect())
                return;

            DebugTool.StartSimulateDevice(ecanHelper);
        }

        /// <summary>
        /// 停止广播
        /// </summary>
        /// <param name="o"></param>
        private void StopBroadcast(object o)
        {
            DebugTool.StopSimulateDevice(ecanHelper);
        }

        #region CAN操作
        /// <summary>
        /// 初始化Can设置
        /// </summary>
        public void InitCanHelper()
        {
            //接收处理
            ecanHelper.RegisterRecvProcessCan1(RecvProcCan1);
            ecanHelper.RegisterRecvProcessCan2(RecvProcCan2);
        }
        /// <summary>
        /// 检查Can连接
        /// </summary>
        /// <returns></returns>
        private bool CheckConnect()
        {
            if (!ecanHelper.IsConnected)
            {
                MessageBox.Show("未连接CAN设备，请先连接后再进行操作！", "提示");
                return false;
            }

            if (!ecanHelper.IsCan1Start && !ecanHelper.IsCan2Start)
            {
                MessageBox.Show("CAN通道未打开！", "提示");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 发送单帧数据
        /// </summary>
        /// <param name="frame"></param>
        private void SendFrame(uint id, byte[] datas)
        {
            if (ecanHelper.IsCan1Start)
            {
                ecanHelper.SendCan1(id, datas);
            }

            if (ecanHelper.IsCan2Start)
            {
                ecanHelper.SendCan2(id, datas);
            }
        }
        private void RecvFrame(CAN_OBJ recvData)
        {
            string strId = recvData.ID.ToString("X");
            Debug.WriteLine(strId);

            if (!strId.Contains("197F")) //非心跳帧过滤
                return;

            CanFrameID frameId = new CanFrameID();
            frameId.ID = recvData.ID;
            int devId = frameId.SrcType;
            int addr = frameId.SrcAddr;

            //指定时间内检测到设备
            Device findDev = DataSource.Find((dev) => (dev.address == addr));

            if (findDev != null)
            {
                if (findDev.Connected)
                {
                    //已连接 重新启动定时器
                    findDev.ResetTimer();
                    UpdateDevData(findDev, recvData.Data);
                }
                else
                {
                    //未连接
                    //1、高亮设备显示
                    findDev.Connected = true;
                    findDev.ID = "0x" + strId;
                    findDev.Name = $"设备{(SrcType)devId}-{findDev.address}";
                    findDev.address = addr;
                    UpdateDevData(findDev, recvData.Data);
                    findDev.StartDetect(UpdateMsg);
                    //2、更新信息框信息
                    string msg = $"{DateTime.Now.ToString("HH:mm:ss.fff")}  设备[{addr}]已连接！";
                    UpdateMsg(msg);
                }
            }
            else
            {
                //设备地址不在已有的范围 设置到最后一个未连接的设备上
                Device unConnectedDev = DataSource.FindLast((dev) => (!dev.Connected));
                if (unConnectedDev != null)
                {
                    //1、高亮设备显示
                    unConnectedDev.Name = $"设备{(SrcType)devId}-{addr.ToString()}"; 
                    unConnectedDev.address = addr;
                    unConnectedDev.Connected = true;
                    unConnectedDev.ID = "0x" + strId;
                    UpdateDevData(unConnectedDev, recvData.Data);
                    unConnectedDev.StartDetect(UpdateMsg);
                    //2、更新信息框信息
                    string msg = $"{DateTime.Now.ToString("HH:mm:ss.fff")}  设备[{addr}]已连接！";
                    UpdateMsg(msg);

                    //按设备地址大小调整顺序
                    DataSource.Sort((dev1, dev2) => dev1.address.CompareTo(dev2.address));
                }
            }

            //更新状态栏已连接设备集合
            List<Device> devList = DeviceManager.Instance().GetConnectDevs();
            //List<string> devs = new List<string>();
            string devs = "";
            devList.ForEach((dev) =>
            {
                string tmp = dev.Name.ToString();
                devs += tmp + ",";
            });

            devs = devs.TrimEnd(',');
            GlobalManager.Instance().UpdateStatusBar_ConnectDevs(devs);
        }
        /// <summary>
        /// 通道1接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan1(CAN_OBJ recvData)
        {
            RecvFrame(recvData);
        }
        /// <summary>
        /// 通道2接收处理
        /// </summary>
        /// <param name="recvData"></param>
        private void RecvProcCan2(CAN_OBJ recvData)
        {
            RecvFrame(recvData);
        }
        #endregion

        private bool UpdateMsg(string msg)
        {
            string tmp = Message;
            tmp += ">" + msg + "\r\n";

            Message = tmp;
            return true;
        }
        private void UpdateDevData(Device dev, byte[] data)
        {
            dev.Status = GetStatus(data);
            dev.Fault = GetFault(data);
            dev.Mode = GetMode(data);
            dev.DischargePower = GetDischargePower(data);
            dev.ChargePower = ChargePower(data);
        }
        private string GetStatus(byte[] datas)
        {
            if (datas.Length < 4)
                return "";

            return datas[3].ToString();
        }
        private string GetFault(byte[] datas)
        {
            if (datas.Length < 5)
                return "";

            //return datas[4] == 1 ? "有" : "无";
            return datas[2] == 1 ? "有" : "无";
        }
        private string GetMode(byte[] datas)
        {
            if (datas.Length < 6)
                return "";

            //return datas[5].ToString();
            return BitConverter.ToInt16(datas, 0).ToString();
        }
        private string GetDischargePower(byte[] datas)
        {
            if (datas.Length < 6)
                return "";
            return (BitConverter.ToInt16(datas, 4) * 0.01).ToString("F2") + "kW";
        }
        private string ChargePower(byte[] datas)
        {
            if (datas.Length < 8)
                return "";
            return (BitConverter.ToInt16(datas, 6) * 0.01).ToString("F2") + "kW";
        }
    }//class


    public enum SrcType
    {
        PCS = 1,
        CSU_MCU1 = 2,
        CSU_MCU2 = 3,
        BCU = 4,
        BMU = 5,
        DCDC = 6
    }
}
