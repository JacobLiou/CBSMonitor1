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
    /// CommandInfoControl.xaml 的交互逻辑
    /// </summary>
    public partial class CommandInfoControl : UserControl
    {
        public CommandInfoControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// cmd名，如"cmd0"
        /// </summary>
        public string CmdName
        {
            get { return (string)GetValue(CmdNameProperty); }
            set { SetValue(CmdNameProperty, value); }
        }
        public static readonly DependencyProperty CmdNameProperty =
            DependencyProperty.Register("CmdName", typeof(string), typeof(CommandInfoControl));

        /// <summary>
        /// 数据源依赖属性
        /// </summary>
        public CommandInfoModel DataSource
        {
            get { return (CommandInfoModel)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }
        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(CommandInfoModel), typeof(CommandInfoControl));
    }//class
}
