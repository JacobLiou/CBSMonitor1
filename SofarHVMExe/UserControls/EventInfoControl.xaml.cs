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
    /// EventInfoControl.xaml 的交互逻辑
    /// </summary>
    public partial class EventInfoControl : UserControl
    {
        public EventInfoControl()
        {
            InitializeComponent();
        }


        /// <summary>
        /// 数据源依赖属性
        /// </summary>
        public EventInfoModel Model
        {
            get { return (EventInfoModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(EventInfoModel), typeof(EventInfoControl));
    }//class
}
