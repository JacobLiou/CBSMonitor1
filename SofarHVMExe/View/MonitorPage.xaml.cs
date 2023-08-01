using SofarHVMExe.ViewModel;
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
using UserControl = System.Windows.Controls.UserControl;

namespace SofarHVMExe.View
{
    /// <summary>
    /// MonitorPage.xaml 的交互逻辑
    /// </summary>
    public partial class MonitorPage : UserControl
    {
        public MonitorPage()
        {
            InitializeComponent();
        }

        bool isMoveDownNeeded;

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

        private void DataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            MonitorPageVm vm = this.DataContext as MonitorPageVm;
            if (vm != null)
            {
                try
                {
                    if (vm.IsScrollDisplay)
                    {
                        if (isMoveDownNeeded && dataGrid.Items.Count > 0)
                        {
                            isMoveDownNeeded = false;
                            var lastItem = dataGrid.Items[dataGrid.Items.Count - 1];
                            dataGrid.SelectedItem = lastItem;
                            dataGrid.ScrollIntoView(lastItem);
                        }
                    }
                }
                catch { }
            }
        }

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox;
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        private void InnerDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 阻止 DataGrid 自己处理滚轮事件
            e.Handled = true;

            // 将滚轮事件传递给外部的 ScrollViewer
            OuterScrollViewer.RaiseEvent(
                new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = OuterScrollViewer
                });
        }
    }//class
}
