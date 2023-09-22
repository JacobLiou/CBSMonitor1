using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;

namespace SofarHVMExe.View
{
    /// <summary>
    /// LoadWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoadWindow : Window
    {
        public bool ShowLoading { get; set; } = true;

        public Func<bool> DoWork;

        public LoadWindow()
        {
            InitializeComponent();

            this.Loaded += LoadWindow_Loaded;
        }

        private void LoadWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                if (DoWork.Invoke())
                {
                    App.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.Close();
                    });
                }
            });
        }
    }
}
