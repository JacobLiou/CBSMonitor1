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
using System.Windows.Shapes;

namespace SofarHVMExe.View
{
    /// <summary>
    /// DownloadDebugInfoWnd.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadDebugInfoWnd : Window
    {
        public DownloadDebugInfoWnd()
        {
            InitializeComponent();
        }

        private StringBuilder sb = new StringBuilder();

        public void SetTitle(string title)
        {
            titleTextBlock.Text = title;
        }
        public void UpdateInfo(string text)
        {
            this.textBox.Text = text;
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Hide(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Button_Clear(object sender, RoutedEventArgs e)
        {
            textBox.Text = "";
        }

        private void UpdateMsg()
        {
            double boxHeight = textBox.ActualHeight;
            textBox.ScrollToVerticalOffset(boxHeight);


        }

        private void textBox_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            //e.NewValue;


        }
    }
}
