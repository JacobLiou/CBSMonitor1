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

                Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                TaskScheduler.UnobservedTaskException += UnobservedTaskException;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// UI线程上未捕获的异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //MessageBox.Show("当前应用程序遇到一些问题，出现异常须重启软件。");
            LogHelper.Error($"{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}未捕获到的UI线程异常: {e.Exception.Message}", e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// 捕获应用程序域中发生的异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //var exception = e.ExceptionObject as Exception;
            //var terminatingMessage = e.IsTerminating ? " The application is terminating." : string.Empty;
            //var exceptionMessage = exception?.Message ?? "An unmanaged exception occured.";
            //var message = string.Concat(exceptionMessage, terminatingMessage);
            LogHelper.Error($"{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}未捕获到的非UI线程异常！" , (System.Exception)e.ExceptionObject);
        }

        /// <summary>
        /// 当出错的任务的未观察到的异常将要触发异常升级策略时发生，默认情况下，这将终止进程。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogHelper.Error($"{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}未捕获到的Task任务异常: {e.Exception.Message}" + e.Exception);
            e.SetObserved();
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
