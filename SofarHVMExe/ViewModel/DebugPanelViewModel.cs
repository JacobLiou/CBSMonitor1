using Communication.Can;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FontAwesome.Sharp;
using SofarHVMExe.Utilities.Global;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SofarHVMExe.ViewModel
{
    internal partial class DebugPanelViewModel : ObservableObject
    {
        public DebugPanelViewModel()
        {
            EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
            if (ecanHelper.IsCan2Start)
            {
                //ecanHelper.OnReceiveCan2 += OnReceiveCan2;
                ecanHelper.RegisterRecvProcessCan2(OnReceiveCan2);
            }
        }
        bool cancel1 = false;
        bool cancel2 = false;
        bool cancelSendHb1 = false;
        bool cancelSendHb2 = false;


        [ObservableProperty]
        string deviceNumber = "";
        
        [ObservableProperty]
        string unContinueId = "0x19004121";

        [ObservableProperty]
        string unContinueDatas = "00 00 02 01 02 AA 55 00";

        [ObservableProperty]
        string continueId = "0x19804121";

        [ObservableProperty]
        string continueDatas1 = "00 00 00 04 AA BB 12 34";

        [ObservableProperty]
        string continueDatas2 = "01 56 78 99 88 88 16 00";

        [ObservableProperty]
        string continueDatas3 = "ff 55 AA C0 B0 88 16 00";

        [ObservableProperty]
        bool sendMap1 = false;

        [ObservableProperty]
        bool sendMap2 = false;

        [ObservableProperty]
        string mapDatas1 = "01 02 00 00 01 00 00 00";

        [ObservableProperty]
        string mapDatas2 = "01 02 00 00 01 00 00 00";

        bool hbSendHeartbeat1 = false;
        public bool HbSendHeartbeat1
        {
            get => hbSendHeartbeat1;
            set
            {
                if (hbSendHeartbeat1 != value)
                {
                    hbSendHeartbeat1 = value;
                    OnPropertyChanged();
                    SendHeartbeat1();
                }
            }
        }

        [ObservableProperty]
        int hbPeriod1 = 1000;

        [ObservableProperty]
        string hbId1 = "0x197F4121";

        [ObservableProperty]
        string hbDatas1 = "00 00 03 01 02 03 00 00";

        bool hbSendHeartbeat2 = false;
        public bool HbSendHeartbeat2
        {
            get => hbSendHeartbeat2;
            set
            {
                if (hbSendHeartbeat2 != value)
                {
                    hbSendHeartbeat2 = value;
                    OnPropertyChanged();
                    SendHeartbeat2();
                }
            }
        }

        [ObservableProperty]
        int hbPeriod2 = 1000;

        [ObservableProperty]
        string hbId2 = "0x197F4122";

        [ObservableProperty]
        string hbDatas2 = "00 00 03 01 02 03 00 00";




        [RelayCommand]
        private void ConnectDevice1()
        {
            EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
            if (ecanHelper.IsCan2Start)
            {
                cancel1 = false;
                cancel2 = true;

                Task.Run(() =>
                {
                    while (!cancel1)
                    {
                        Thread.Sleep(1000);
                        ecanHelper.SendCan2(0x197F4121, new byte[] { }); //3
                    }
                });

                DeviceNumber = "1";
            }
        }

        [RelayCommand]
        private void ConnectDevice2()
        {
            EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
            if (ecanHelper.IsCan2Start)
            {
                cancel1 = true;
                cancel2 = false;

                Task.Run(() =>
                {
                    while (!cancel2)
                    {
                        Thread.Sleep(1000);
                        ecanHelper.SendCan2(0x197F4122, new byte[] { }); //3
                    }
                });
                DeviceNumber = "2";
            }
        }

        [RelayCommand]
        private void NotConnectDevice()
        {
            cancel1 = true;
            cancel2 = true;
            DeviceNumber = "";
        }

        [RelayCommand]
        private void SendDevInfoUnContinue()
        {
            EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
            if (ecanHelper.IsCan2Start)
            {
                //id
                uint id = 0;
                string strId = unContinueId.Replace("0x", "").Replace("0X", "");
                if (!uint.TryParse(strId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
                    return;

                //数据
                string[] byteArr = unContinueDatas.Trim().Split(" ");
                byte[] datas = new byte[8];
                for (int i = 0; i < byteArr.Length; i++)
                {
                    byte.TryParse(byteArr[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out datas[i]);
                }

                ecanHelper.SendCan2(id, datas);
            }
        }

        [RelayCommand]
        private void SendDevInfoContinue()
        {
            EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
            if (ecanHelper.IsCan2Start)
            {
                //id
                uint id = 0;
                string strId = continueId.Replace("0x", "").Replace("0X", "");
                if (!uint.TryParse(strId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
                    return;

                //数据1
                string[] byteArr = continueDatas1.Trim().Split(" ");
                byte[] datas = new byte[8];
                for (int i = 0; i < byteArr.Length; i++)
                {
                    byte.TryParse(byteArr[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out datas[i]);
                }

                ecanHelper.SendCan2(id, datas);

                Thread.Sleep(20);

                //数据2
                byteArr = continueDatas2.Trim().Split(" ");
                datas = new byte[8];
                for (int i = 0; i < byteArr.Length; i++)
                {
                    byte.TryParse(byteArr[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out datas[i]);
                }

                ecanHelper.SendCan2(id, datas);

                Thread.Sleep(20);

                //数据3
                byteArr = continueDatas3.Trim().Split(" ");
                datas = new byte[8];
                for (int i = 0; i < byteArr.Length; i++)
                {
                    byte.TryParse(byteArr[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out datas[i]);
                }

                ecanHelper.SendCan2(id, datas);
            }
        }

        [RelayCommand]
        private void MapSend1()
        {
            //sendMap1
        }

        [RelayCommand]
        private void MapSend2()
        {
            //SendFrame(mapId, mapDatas2);
        }

        private void SendHeartbeat1()
        {
            if (hbSendHeartbeat1)
            {
                cancelSendHb1 = false;
                Task.Run(() =>
                {
                    while (!cancelSendHb1)
                    {
                        Thread.Sleep(hbPeriod1);
                        SendFrame(hbId1, hbDatas1);
                    }
                });
            }
            else
            {
                cancelSendHb1 = true;
            }
        }

        private void SendHeartbeat2()
        {
            if (hbSendHeartbeat2)
            {
                cancelSendHb2 = false;
                Task.Run(() =>
                {
                    while (!cancelSendHb2)
                    {
                        Thread.Sleep(hbPeriod2);
                        SendFrame(hbId2, hbDatas2);
                    }
                });
            }
            else
            {
                cancelSendHb2 = true;
            }
        }

        private void SendFrame(string strId, string strDatas)
        {
            EcanHelper ecanHelper = CommManager.Instance().GetCanHelper();
            if (ecanHelper.IsCan2Start)
            {
                //id
                uint id = 0;
                strId = strId.Replace("0x", "").Replace("0X", "");
                if (!uint.TryParse(strId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out id))
                    return;

                //数据
                string[] byteArr = strDatas.Trim().Split(" ");
                byte[] datas = new byte[8];
                for (int i = 0; i < byteArr.Length; i++)
                {
                    byte.TryParse(byteArr[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out datas[i]);
                }

                ecanHelper.SendCan2(id, datas);
            }
        }
        
        private void OnReceiveCan2(CAN_OBJ canObj)
        {
            uint id = canObj.ID;
            byte[] datas = canObj.Data;

            if (sendMap1)
            {
                if (datas[4] == 0x16 &&
                    datas[5] == 0x02 &&
                    datas[6] == 0x01)
                {
                    SendFrame("0x1FFA5A5", mapDatas1);
                }
            }

            if (sendMap2)
            {
                if (datas[4] == 0x15 &&
                    datas[5] == 0x02 &&
                    datas[6] == 0x01)
                {
                    SendFrame("0x1FFA5A5", mapDatas2);
                }
            }
        }
    }//class
}
