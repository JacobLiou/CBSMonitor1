using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MultiLanguages.Common
{
   public class LogHelper
    {
        public static readonly object locker = new object();
        public static void WriteLog(string msg)
        {
            lock (locker)
            {
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "log";
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string logPath = AppDomain.CurrentDomain.BaseDirectory + "log\\" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                try
                {
                    using (StreamWriter sw = File.AppendText(logPath))
                    {
                        sw.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}消息：{msg}");
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                    }
                }
                catch (IOException e)
                {
                    using (StreamWriter sw = File.AppendText(logPath))
                    {
                        sw.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}异常：{e.Message}");
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();
                    }
                }
            }
        }
    }
}
