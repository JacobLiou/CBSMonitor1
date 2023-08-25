using CanProtocol.ProtocolModel;
using NPOI.SS.Formula.Functions;
using SofarHVMExe.Model;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;

namespace SofarHVMExe.View
{
    /// <summary>
    /// MapOptPage.xaml 的交互逻辑
    /// </summary>
    public partial class MapOptPage : UserControl
    {
        public MapOptPage()
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

        /// <summary>
        /// 表格开始编辑事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            MapOptPageVm? vm = this.DataContext as MapOptPageVm;
            if (vm == null)
                return;

            vm.IsWriting = true;
        }

        /// <summary>
        /// 表格单元格编辑完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!(e.EditingElement is System.Windows.Controls.TextBox tb))
                return;

            MapOptPageVm? vm = this.DataContext as MapOptPageVm;
            if (vm == null)
                return;

            MemoryModel mem = dataGrid.CurrentItem as MemoryModel;
            if (mem == null)
                return;

            vm.IsWriting = false;
            //vm.StartWriteData(mem);

            //KEY重复检查待追加
            vm.IsRepeat = vm.DataSource.Where(d => d.AddressOrName != string.Empty).GroupBy(d => d.AddressOrName).Where(g => g.Count() > 1).Count() > 0;
        }

        private void MapOptPage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (MapOptPageVm.cancelTokenSource != null)
            {
                MapOptPageVm.cancelTokenSource.Cancel();
            }
        }
    }//class
}
