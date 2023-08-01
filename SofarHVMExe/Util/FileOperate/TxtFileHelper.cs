using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SofarHVMExe.Utilities.FileOperate
{
    public class TxtFileHelper
    {
        static TxtFileHelper()
        {
            //增加对GBK编码的支持
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static object _fileLock = new object();
        public static ReaderWriterLockSlim _logWriteLock = new ReaderWriterLockSlim();

        #region 读文件
        public static string ReadFile(string path)
        {
            string result = "";
            try
            {
                lock (_fileLock) 
                {
                    using (FileStream fsRead = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        int len = (int)fsRead.Length;
                        byte[] buffers = new byte[len];
                        int r = fsRead.Read(buffers, 0, len);
                        Encoding coding = Encoding.GetEncoding("GBK");
                        result = coding.GetString(buffers);
                    }

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Debug.WriteLine("Executing finally block.");
            }

            return result;
        }
        public static string[] ReadFileLines(string path)
        {
            string fileText = ReadFile(path);
            return fileText.Split("\r\n");
        }
        #endregion

        public static bool ClearFile(string path)
        {
            try
            {
                lock (_fileLock)
                {
                    using (FileStream fsWrite = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        fsWrite.Seek(0, SeekOrigin.Begin);
                        fsWrite.SetLength(0);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
                return false;
            }
            finally
            {
                Debug.WriteLine("Executing finally block.");
            }

            return true;
        }

        public static string Save2FileAutoPath(string text, string fileName)
        {
            if (text == null)
                return null;

            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            string pathDir = exePath + @"Work";
            string time = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            string totalPath = pathDir + $"\\{fileName}_{time}" + @".txt";

            if (!Directory.Exists(pathDir))
            {
                Directory.CreateDirectory(pathDir);
            }

            if (!File.Exists(totalPath))
            {
                FileInfo file = new FileInfo(totalPath);
                FileStream fs = file.Create();
                fs.Close();
            }

            if (Save2File(text, totalPath))
            {
                return totalPath;
            }

            return null;
        }
        public static bool Save2File(string text, string path)
        {
            if (text == null)
                return false;

            File.WriteAllText(path, text, Encoding.Unicode);

            //using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            //{

            //    fs.Write(text, 0);
            //}
            return true;
        }


        #region 多线程操作
        private bool IsOccupied(string path)
        {
            FileStream stream = null;
            try
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch
            {
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        #endregion
    }//class
}
