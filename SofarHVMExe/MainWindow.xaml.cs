using SixLabors.ImageSharp;
using SofarHVMExe.Model;
using SofarHVMExe.Utilities.Global;
using SofarHVMExe.View;
using SofarHVMExe.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SofarHVMExe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SetVersionTitle("CAN协议调试上位机-T1.0.1.23.20240722");

            AdjustMainWindow();
        }

        private void SetVersionTitle(string version)
        {
            // 判断是否是Debug模式
            bool isDebugBuild = false;
            var assembly = Assembly.GetExecutingAssembly();
            var debugAttributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);
            if (debugAttributes.Length > 0 && debugAttributes[0] is DebuggableAttribute debuggable)
            {
                isDebugBuild = (debuggable.DebuggingFlags & DebuggableAttribute.DebuggingModes.Default) ==
                                   DebuggableAttribute.DebuggingModes.Default;
            }

            if (isDebugBuild)
            {
                // Debug模式下的Title
                var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                var fileInfo = new FileInfo(assembly.Location);
                Title = "CAN协议调试上位机(临时调试)" + $"编译时间 {fileInfo.LastWriteTime.ToString()}";
            }
            else
            {
                // Release模式下的Title
                Title = version;
            }
        }


        private void AdjustMainWindow()
        {
            if (Height > SystemParameters.WorkArea.Height)
                Height = SystemParameters.WorkArea.Height;
        }

        private enum Theme
        {
            White = 0,
            Dark
        }

        private Theme CurrentTheme = Theme.Dark;
        int passwordProgress = 0;

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    //this.DragMove();
                }
            }
        }

        /// <summary>
        /// 切换主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonTheme_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentTheme == Theme.Dark)
            {
                //切换主题

                //切换主题按钮图标
                this.themeIcon.Icon = FontAwesome.Sharp.IconChar.Moon;
                this.themeIcon.Foreground = new SolidColorBrush(Colors.Black);
                CurrentTheme = Theme.White;
            }
            else
            {
                //切换主题

                //切换主题按钮图标
                this.themeIcon.Icon = FontAwesome.Sharp.IconChar.Sun;
                this.themeIcon.Foreground = new SolidColorBrush(Colors.White);
                CurrentTheme = Theme.Dark;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.J:
                        passwordProgress = 1;
                        break;
                    case Key.K:
                        passwordProgress = (passwordProgress == 1)? 2 : 0;
                        break;
                    case Key.L:
                        passwordProgress = (passwordProgress == 2)? 3 : 0;
                        break;
                    default:
                        passwordProgress = 0;
                        break;
                }

                if (passwordProgress == 3)
                {
                    DebugPanel wnd = new DebugPanel();
                    wnd.Topmost = true;
                    wnd.Show();
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            Application.Current.Shutdown();
            Environment.Exit(0);
        }

    }//class
}
