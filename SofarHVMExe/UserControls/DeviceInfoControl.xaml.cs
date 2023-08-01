using SofarHVMExe.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SofarHVMExe.UserControls
{
    /// <summary>
    /// 设备信息自定义控件 的交互逻辑
    /// </summary>
    public partial class DeviceInfoControl : UserControl
    {
        public DeviceInfoControl()
        {
            InitializeComponent();
        }

        #region 依赖属性
        public Device DeviceInfo
        {
            get { return (Device)GetValue(DeviceInfoProperty); }
            set { SetValue(DeviceInfoProperty, value); }
        }
        public static readonly DependencyProperty DeviceInfoProperty =
            DependencyProperty.Register("DeviceInfo", typeof(Device), typeof(DeviceInfoControl), new PropertyMetadata(new Device()));

        //public bool Connected
        //{
        //    get { return (bool)GetValue(ConnectedProperty); }
        //    set { SetValue(ConnectedProperty, value); }
        //}
        //public static readonly DependencyProperty ConnectedProperty =
        //    DependencyProperty.Register("Connected", typeof(bool), typeof(DeviceInfoControl), new PropertyMetadata(false));

        //public string DeviceName
        //{
        //    get { return (string)GetValue(DeviceNameProperty); }
        //    set { SetValue(DeviceNameProperty, value); }
        //}
        //public static readonly DependencyProperty DeviceNameProperty =
        //    DependencyProperty.Register("DeviceName", typeof(string), typeof(DeviceInfoControl), new PropertyMetadata(""));

        //public string Address
        //{
        //    get { return (string)GetValue(AddressProperty); }
        //    set { SetValue(AddressProperty, value); }
        //}
        //public static readonly DependencyProperty AddressProperty =
        //    DependencyProperty.Register("Address", typeof(string), typeof(DeviceInfoControl), new PropertyMetadata(""));

        //public string ID
        //{
        //    get { return (string)GetValue(IDProperty); }
        //    set { SetValue(IDProperty, value); }
        //}
        //public static readonly DependencyProperty IDProperty =
        //    DependencyProperty.Register("ID", typeof(string), typeof(DeviceInfoControl), new PropertyMetadata(""));


        #endregion



    }//class
}
