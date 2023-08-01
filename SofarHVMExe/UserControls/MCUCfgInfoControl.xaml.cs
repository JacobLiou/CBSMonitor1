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
    /// MCUCfgInfoControl.xaml 的交互逻辑
    /// </summary>
    public partial class MCUCfgInfoControl : UserControl
    {
        public MCUCfgInfoControl()
        {
            InitializeComponent();
        }

        public string Desc
        { 
            get { return (string)GetValue(DescProperty); }
            set { SetValue(DescProperty, value); }
        }
        public static readonly DependencyProperty DescProperty =
            DependencyProperty.Register("Desc", typeof(string), typeof(MCUCfgInfoControl));


        /// <summary>
        /// 数据源依赖属性
        /// </summary>
        public McuConfigModel DataSource
        {
            get { return (McuConfigModel)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(McuConfigModel), typeof(MCUCfgInfoControl));

    }//class
}
