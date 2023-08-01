using CanProtocol.ProtocolModel;
using SofarHVMExe.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
    /// FrameDataControl.xaml 的交互逻辑
    /// CAN帧数据配置控件（8个字节）
    /// 和Multy_old配套
    /// </summary>
    public partial class FrameDataControl_old : UserControl
    {
        public FrameDataControl_old()
        {
            InitializeComponent();
        }

        private bool cellValChanged = false;
        public CanFrameDataInfo SelectDataInfo { get; set; }
        public Action<object> DeleteFrameAction { get; set; } //右键删除处理
        public Action<object> ModifyInfoAction { get; set; } //帧数据修改处理（用于更新CRC）

        #region 依赖属性
        public bool ShowTip
        {
            get { return (bool)GetValue(ShowTipProperty); }
            set { SetValue(ShowTipProperty, value); }
        }
        public static readonly DependencyProperty ShowTipProperty =
            DependencyProperty.Register("ShowTip", typeof(bool), typeof(FrameDataControl), new PropertyMetadata(false));
        
        public bool ShowPackageInfo
        {
            get { return (bool)GetValue(ShowPackageInfoProperty); }
            set { SetValue(ShowPackageInfoProperty, value); }
        }
        public static readonly DependencyProperty ShowPackageInfoProperty =
            DependencyProperty.Register("ShowPackageInfo", typeof(bool), typeof(FrameDataControl), new PropertyMetadata(false));
        
        public Visibility ShowShrinkBtn
        {
            get { return (Visibility)GetValue(ShowShrinkBtnProperty); }
            set { SetValue(ShowShrinkBtnProperty, value); }
        }
        public static readonly DependencyProperty ShowShrinkBtnProperty =
            DependencyProperty.Register("ShowShrinkBtn", typeof(Visibility), typeof(FrameDataControl), 
                new PropertyMetadata(Visibility.Visible));

        public Visibility ContextMenuVis
        {
            get { return (Visibility)GetValue(ContextMenuVisProperty); }
            set { SetValue(ContextMenuVisProperty, value); }
        }
        public static readonly DependencyProperty ContextMenuVisProperty =
            DependencyProperty.Register("ContextMenuVis", typeof(Visibility), typeof(FrameDataControl),
                new PropertyMetadata(Visibility.Visible));

        public string PackageIndex
        {
            get { return (string)GetValue(PackageIndexProperty); }
            set { SetValue(PackageIndexProperty, value); }
        }
        public static readonly DependencyProperty PackageIndexProperty =
            DependencyProperty.Register("PackageIndex", typeof(string), typeof(FrameDataControl), new PropertyMetadata("0"));

        public string FrameType
        {
            get { return (string)GetValue(FrameTypeProperty); }
            set { SetValue(FrameTypeProperty, value); }
        }
        public static readonly DependencyProperty FrameTypeProperty =
            DependencyProperty.Register("FrameType", typeof(string), typeof(FrameDataControl), new PropertyMetadata("起始帧"));
        
        //public CanFrameData FrameData
        //{
        //    get { return (CanFrameData)GetValue(FrameDataProperty); }
        //    set { SetValue(FrameDataProperty, value); }
        //}
        //public static readonly DependencyProperty FrameDataProperty =
        //    DependencyProperty.Register("FrameData", typeof(CanFrameData), typeof(FrameDataControl), 
        //        new PropertyMetadata(new CanFrameData()));

#endregion


#region 控件事件处理

        /// <summary>
        /// 表格加载一行数据事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            //增加序号列
            //e.Row.Header = e.Row.GetIndex() + 1;
            //dataGrid.InvalidateMeasure();
            //dataGrid.InvalidateArrange();
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            CanFrameData? frameData = this.DataContext as CanFrameData;
            if (frameData == null)
                return;

            ObservableCollection<CanFrameDataInfo> dataInfos = frameData.DataInfos;

            string newType = "U16";
            //判断数据是否超出8个字节长度
            {
                int count = 0;
                foreach (CanFrameDataInfo info in dataInfos)
                {
                    if (info.Type.Contains("8"))
                    {
                        count += 1;
                    }
                    else if (info.Type.Contains("16"))
                    {
                        count += 2;
                    }
                    else if (info.Type.Contains("32"))
                    {
                        count += 4;
                    }
                }

                if (count == 8)
                {
                    MessageBox.Show("长度最大8个字节，无法增加！", "提示");
                    return;
                }
                else if (count == 7)
                {
                    newType = "U8";
                }
            }

            //增加一条新的字段信息
            string packageIndex = frameData.GetPackageIndex();
            if (packageIndex == "0xff")
            {
                ///连续帧中的结束帧
                CanFrameDataInfo crcInfo = dataInfos.Last();
                dataInfos.Remove(crcInfo);
                CanFrameDataInfo newInfo = new CanFrameDataInfo();
                newInfo.Type = newType;
                dataInfos.Add(newInfo);
                dataInfos.Add(crcInfo);
                dataGrid.SelectedItem = newInfo;

                frameData.DataNum += 1;
            }
            else 
            {
                ///非连续帧、连续帧中的起始帧和中间帧
                CanFrameDataInfo newInfo = new CanFrameDataInfo();
                newInfo.Type = newType;
                dataInfos.Add(newInfo);
                dataGrid.SelectedItem = newInfo;
            }

            ModifyInfoAction?.Invoke(this.DataContext);
        }//func
            
        private void DeleteSelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectDataInfo == null)
            {
                MessageBox.Show("请选择一条数据！", "提示");
                return;
            }

            //检查是否为点表起始地址和个数字段
            {
                string name = SelectDataInfo.Name;
                if (name == "点表起始地址" ||
                    name == "个数" ||
                    name == "包序号" ||
                    name == "CRC校验"
                    )
                {
                    MessageBox.Show("必要字段不允许删除！", "提示");
                    return;
                }
            }

            //删除选择的字段信息
            CanFrameData? frameData = this.DataContext as CanFrameData;
            if (frameData == null)
                return;

            ObservableCollection<CanFrameDataInfo> dataInfos = frameData.DataInfos;
            dataInfos.Remove(SelectDataInfo);

            ///选择一条字段数据
            if (dataInfos.Count > 0)
            {
                string packageIndex = frameData.GetPackageIndex();
                if (packageIndex == "0xff")
                {
                    CanFrameDataInfo info = dataInfos[dataInfos.Count - 2];
                    dataGrid.SelectedItem = info;
                    ModifyInfoAction?.Invoke(this.DataContext);
                }
                else
                {
                    dataGrid.SelectedItem = dataInfos.Last();
                }
            }
        }//func

        /// <summary>
        /// 右键菜单删除处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteFrameDataButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteFrameAction?.Invoke(this.DataContext);
        }

        /// <summary>
        /// 表格单元格编辑完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!(e.EditingElement is TextBox tb))
                return;

            //CanFrameData? frameData = this.DataContext as CanFrameData;
            //string newValue = tb.Text;

            //传递给父级更新CRC校验值（用于连续帧-多包）
            string colName = e.Column.Header.ToString();
            if (colName == "值")
            {
                ModifyInfoAction?.Invoke(this.DataContext);
                cellValChanged = true;
            }
        }

        #endregion

        private string GetPackageIndex(ObservableCollection<CanFrameDataInfo> dataInfos)
        {
            foreach(CanFrameDataInfo info in dataInfos)
            {
                if (info.Name == "包序号")
                {
                    return info.Value;
                }
            }

            return "-1";
        }

    }//class
}
