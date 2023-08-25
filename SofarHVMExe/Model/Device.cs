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
        /// PCSM降额后的发电能力
        /// </summary>
        private string dischargePower = "";
        public string DischargePower
        {
            get => dischargePower;
            set
            {
                dischargePower = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// PCSM降额后的充电能力
        /// </summary>
        private string chargePower = "";
        public string ChargePower
        {
            get => chargePower;
            set
            {
                chargePower = value;
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
            //持续检测 20s没收到该设备心跳，则判定为掉线
            Task.Run(() =>
            {
                timer.Restart();

                while (true)
                {
                    if (timer.ElapsedMilliseconds > 20000)
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
                            string msg = $"{DateTime.Now.ToString("HH:mm:ss.fff")}  设备[{address}]断开，连接超时！";
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
