using SofarHVMExe.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace SofarHVMExe.View
{
    /// <summary>
    /// CANFrameDataEditWnd.xaml 的交互逻辑
    /// </summary>
    public partial class CANFrameDataEditWnd : Window
    {

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="selectModel"></param>
        /// <param name="originDataSource"></param>
        public CANFrameDataEditWnd()
        {
            InitializeComponent();

            //设置父窗口
            //{
            //    MainWindow? mainWnd = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
            //    if (mainWnd != null)
            //    {
            //        this.Owner = mainWnd;
            //        this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            //    }
            //}
            //this.Closing += Window_Closing;
        }

        private bool isClosed = false;


        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

       /// <summary>
       /// 表格加载一行数据事件
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //增加序号列
            e.Row.Header = e.Row.GetIndex() + 1;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 窗口失去激活/焦点事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            return; //关闭此效果
            if (isClosed)
                return;

            //设置窗口抖动效果
            Rect curRt = new Rect(this.Left, this.Top, this.ActualWidth, this.ActualHeight);
            Rect ownerRt = new Rect(Owner.Left, Owner.Top, Owner.ActualWidth, Owner.ActualHeight);

            var currentGraphics = Graphics.FromHwnd(new WindowInteropHelper(Application.Current.MainWindow).Handle);
            ///计算系统Dpi
            var DpiX = currentGraphics.DpiX / 96;
            var DpiY = currentGraphics.DpiY / 96;

            ///计算包括了系统dpi的鼠标坐标
            int mousePtX = (int)(System.Windows.Forms.Control.MousePosition.X / DpiX);
            int mousePtY = (int)(System.Windows.Forms.Control.MousePosition.Y / DpiY);
            Point mousePt = new Point(mousePtX, mousePtY);

            ///点击软件外面和当前窗口里面不做操作
            ///只有点击父窗口和当前窗口的差集区域才有抖动效果
            if (!ownerRt.Contains(mousePt) || curRt.Contains(mousePt))
                return;

            ///窗口抖动效果
            int offset = 8;
            for (int i = 0; i < 20; i++)
            {
                this.Left += offset;
                Thread.Sleep(10);
                this.Left -= offset;
                Thread.Sleep(10);
            }
        }

    }//class
}
