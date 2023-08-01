using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
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

namespace SofarHVMExe.View
{
    /// <summary>
    /// CANFrameCfgPage.xaml 的交互逻辑
    /// </summary>
    public partial class CANFrameCfgPage : UserControl
    {
        public CANFrameCfgPage()
        {
            InitializeComponent();
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //增加序号列
            e.Row.Header = e.Row.GetIndex() + 1;

        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.ChangedButton == MouseButton.Left)
            //{
            //    //this.DragMove();
            //}
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is CANFrameCfgPageVm vm)
            {
                vm.DataSrcChangeAction += RefreshDataGrid;
            }
        }

        private void RefreshDataGrid()
        {
            //拖拽行后，刷新表格，让表格序号正常更新
            dataGrid.Items.Refresh();
        }
    }//class
}
