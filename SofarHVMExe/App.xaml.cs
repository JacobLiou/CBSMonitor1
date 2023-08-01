using SofarHVMExe.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SofarHVMExe
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        ///下面这一堆仅用来刷新界面，如果有更好的方法，请删除下面的所有东西
        private static DispatcherOperationCallback exitFrameCallback = new DispatcherOperationCallback(ExitFrame);

        public static void DoEvents()
        {
            DispatcherFrame nestedFrame = new DispatcherFrame();
            DispatcherOperation exitOperation = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, exitFrameCallback, nestedFrame);
            Dispatcher.PushFrame(nestedFrame);
            if (exitOperation.Status !=
            DispatcherOperationStatus.Completed)
            {
                exitOperation.Abort();
            }
        }

        private static Object ExitFrame(Object state)
        {
            DispatcherFrame? frame = state as DispatcherFrame;
            if (frame != null)
            {
                frame.Continue = false;
            }
            return null;
        }

        public App()
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            try
            {
                string name = Assembly.GetExecutingAssembly()!.GetName()!.Name!.ToString();
                Process[] pro = Process.GetProcesses();
                var processes = pro.Where(t => t.ProcessName == name);
                int n = processes.Count();
                if (n > 1)
                {
                    var first = processes.OrderBy(t => t.StartTime).FirstOrDefault();
                    ShowWindowTop(first!.MainWindowHandle);
                    App.Current.Shutdown();
                    return;
                }
                TaskScheduler.UnobservedTaskException += UnobservedTaskException;
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                DispatcherUnhandledException += App_DispatcherUnhandledException;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogHelper.Error(e.Exception.ToString());
            MessageBox.Show(e.Exception.ToString(), "上位机软件运行异常（请截图反馈后再关闭）");
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            var terminatingMessage = e.IsTerminating ? " The application is terminating." : string.Empty;
            var exceptionMessage = exception?.Message ?? "An unmanaged exception occured.";
            var message = string.Concat(exceptionMessage, terminatingMessage);
            LogHelper.Error(message);
            MessageBox.Show(message, "上位机软件运行异常（请截图反馈后再关闭）");
        }

        private void UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogHelper.Error(e.Exception.ToString());
            MessageBox.Show(e.Exception.ToString(), "上位机软件运行异常（请截图反馈后再关闭）");
        }

        private static void ShowWindowTop(IntPtr hWnd)
        {
            ShowWindow(hWnd, 1);
            SetForegroundWindow(hWnd);
            FlashWindow(hWnd, true);
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, uint msg);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hWnd, bool bInvert);
    }//class



}
