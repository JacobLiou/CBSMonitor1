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

namespace SofarHVMExe.View
{
    /// <summary>
    /// CommandGroupPage.xaml 的交互逻辑
    /// </summary>
    public partial class CommandGroupPage : UserControl
    {
        public CommandGroupPage()
        {
            InitializeComponent();
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

        private void Id_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            CommandGroupPageVm vm = DataContext as CommandGroupPageVm;
            if (vm != null)
            {
                vm.UpdateFrameModel(cb.SelectedIndex);
            }
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is CommandGroupPageVm vm)
            {
                vm.DataSrcChangeAction += RefreshDataGrid;
            }
        }

        private void RefreshDataGrid()
        {
            //拖拽行后，刷新表格，让表格序号正常更新
            try
            {
                dataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
            }
        }
    }//class
}
