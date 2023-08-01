using SofarHVMExe.Utilities;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SofarHVMExe.Model
{
    public class Device : ViewModelBase
    {
        public Device()
        {

        }

        private readonly object locker = new object();
        private Stopwatch timer = new Stopwatch();
        public Action<Device> SelectAction = null;

        private string name = "设备1";
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        private string id = "";
        public string ID
        {
            get => id;
            set
            {
                id = value;
                OnPropertyChanged();
            }
        }

        private bool selected = false;
        public bool Selected
        {
            get => selected;
            set
            {
                if (selected == value)
                    return;

                if (UpdateStatusBar)
                {
                    //同时更新状态栏
                    DeviceManager.Instance().UpdateDev = false;//不再更新设备

                    if (value)
                    {
                        SelectAction?.Invoke(this); //取消其他设备的勾选
                        GlobalManager.Instance().UpdateStatusBar_SelectDev($"设备{address}");
                    }
                    else
                    {
                        GlobalManager.Instance().UpdateStatusBar_SelectDev("无");
                    }
                }
                else
                {
                    UpdateStatusBar = true;

                    //不更新状态栏
                    if (value)
                    {
                        SelectAction?.Invoke(this); //取消其他设备的勾选
                    }
                }

                selected = value;
                OnPropertyChanged();
            }
        }

        private bool connected = false;
        public bool Connected
        {
            get => connected;
            set
            {
                lock (this.locker)
                {
                    //后期是否可以取消该位置的锁
                    //MultiLanguages.Common.LogHelper.WriteLog("Device_进入Lock锁：变更连接状态");
                    connected = value;
                    if (!connected)
                    {
                        Selected = false;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public int address = 1;
        public int Address
        {
            get => address;
            set
            {
                address = value;
                OnPropertyChanged();
            }
        }

        private string status = "";
        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }

        private string fault = "";
        public string Fault
        {
            get => fault;
            set
            {
                fault = value;
                OnPropertyChanged();
            }
        }

        private string mode = "";
        public string Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 更新状态栏
        /// </summary>
        public bool UpdateStatusBar = true;

        public void ResetTimer()
        {
            timer.Restart();
        }

        public void StartDetect(Func<string, bool> callBack)
        {
            //持续检测 10s没收到该设备心跳，则判定为掉线
            Task.Run(() =>
            {
                timer.Restart();

                while (true)
                {
                    if (timer.ElapsedMilliseconds > 10000)
                    {
                        lock (this.locker)
                        {
                            //关闭连接
                            Application.Current?.Dispatcher.Invoke(() =>
                            {
                                connected = false;
                                OnPropertyChanged("Connected");
                                id = "";
                                OnPropertyChanged("ID");
                                status = "";
                                OnPropertyChanged("Status");
                                fault = "";
                                OnPropertyChanged("Fault");
                                mode = "";
                                OnPropertyChanged("Mode");
                            });
                            //更新连接信息
                            //string time = DateTime.Now.ToString("HH:mm:ss.fff");
                            //string msg = $"{time}  设备[{address}]断开，连接超时！";
                           string msg = $"设备[{address}]断开，连接超时！";
                            callBack.Invoke(msg);
                            break;
                        }
                    }

                    Thread.Sleep(1000);
                }//while
            });
        }//func
    }//class
}
