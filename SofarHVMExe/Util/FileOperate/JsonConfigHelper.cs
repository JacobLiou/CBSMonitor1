using CanProtocol.ProtocolModel;
using Newtonsoft.Json;
using SofarHVMExe.DbModels;
using SofarHVMExe.Model;
using SofarHVMExe.Util;
using SofarHVMExe.ViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SofarHVMExe.Utilities
{
    public class JsonConfigHelper
    {
        private static readonly object _lockObj = new object();

        private static bool isOperater = true;

        static JsonConfigHelper()
        {
            //增加对GBK编码的支持
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// 配置文件信息
        /// </summary>
        public static FileConfigModel? FileConfig;

        /// <summary>
        /// 设置默认配置文件
        /// </summary>
        public static void SetDefaultCfgPath()
        {
            return;
            //默认配置文件设置
            string exePath = System.AppDomain.CurrentDomain.BaseDirectory;
            string defualtPathDir = exePath + @"Config";
            string defualtPath = defualtPathDir + @"\Config.json";

            try
            {
                if (!Directory.Exists(defualtPathDir))
                {
                    Directory.CreateDirectory(defualtPathDir);
                }

                if (!File.Exists(defualtPath))
                {
                    FileInfo file = new FileInfo(defualtPath);
                    FileStream fs = file.Create();
                    fs.Close();
                }

                AppCfgHelper.WriteField("配置文件路径", defualtPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"默认配置文件设置失败：{ex.Message}");
            }
        }//func

        /// <summary>
        /// 读取配置文件到model对象
        /// 文件格式：GBK
        /// </summary>
        /// <returns></returns>
        public static FileConfigModel? ReadConfigFile()
        {
            if (FileConfig == null)
            {
                //lock (_lockObj)
                //{
                //    string cfgFilePath = AppCfgHelper.ReadField("配置文件路径");
                //    FileConfig = ReadConfigFile(cfgFilePath);
                //}

                // 查找有没有数据库数据--项目配置一定要有
                if (DataManager.SelectPrjConfigModel())
                {
                    // 读取数据库
                    FileConfig = DataManager.ReadData();
                }
            }

            return FileConfig;
        }

        /// <summary>
        /// 读取配置文件到model对象,临时
        /// 文件格式：GBK
        /// </summary>
        /// <returns></returns>
        public static FileConfigModel? ReadConfigFile_Temp()
        {
            string cfgFilePath = AppCfgHelper.ReadField("配置文件路径");
            var ret = ReadConfigFile(cfgFilePath); // 读取文件数据
            DataManager.ClearDataBaseData(); // 情况数据库数据
            WirteConfigFile_Data(ret); // 写入文件数据
            FileConfig = null; // 情况缓存
            return ReadConfigFile(); // 读取缓存
        }

        /// <summary>
        /// 写入model对象到配置文件中
        /// 覆盖写入
        /// 文件格式：GBK
        /// </summary>
        /// <param name="configModel">配置model</param>
        /// <returns></returns>
        public static bool WirteConfigFile_Data(FileConfigModel configModel)
        {
            return DataManager.WriteData(configModel);
        }

        /// <summary>
        /// 写入model对象到配置文件中
        /// 覆盖写入
        /// 文件格式：GBK
        /// </summary>
        /// <param name="configModel">配置model</param>
        /// <returns></returns>
        public static bool WirteConfigFile(FileConfigModel configModel)
        {
            bool ret = false;
            lock (_lockObj)
            {
                string cfgFilePath = AppCfgHelper.ReadField("配置文件路径");
                ret = WirteConfigFile(configModel, cfgFilePath);
            }

            return ret;
        }


        /// <summary>
        /// 读取配置文件到model对象
        /// 文件格式：GBK
        /// </summary>
        /// <param name="cfgFilePath">配置文件路径</param>
        /// <returns></returns>
        private static FileConfigModel? ReadConfigFile(string cfgFilePath)
        {
            if (cfgFilePath == string.Empty || cfgFilePath == null)
                return null;

            FileConfigModel? result = null;

            try
            {
                string jsonStr = "";
                using (StreamReader sr = new StreamReader(cfgFilePath, Encoding.GetEncoding("GBK")))
                {
                    jsonStr = sr.ReadToEnd();
                    sr.Close();
                }
                result = JsonConvert.DeserializeObject<FileConfigModel>(jsonStr);
                CorrectConfig(result);
            }
            catch (Exception ex)
            {
                string msg = $"读Json文件到Model错误：{ex.Message}";
                Debug.WriteLine(msg);
                MessageBox.Show(msg, "提示");
                return null;
            }


            return result;
        }

        /// <summary>
        /// 纠正配置文件中的错误
        /// </summary>
        /// <param name="fileConfigModel"></param>
        private static void CorrectConfig(FileConfigModel fileConfigModel)
        {
            if (fileConfigModel.EventModels.GroupBy(e => e.Group).Any(g => g.Count() > 1))
            {
                fileConfigModel.EventModels = fileConfigModel.EventModels.GroupBy(e => e.Group)
                                              .Select(g => g.First())
                                              .ToList();
                WirteConfigFile(fileConfigModel, AppCfgHelper.ReadField("配置文件路径"));
            }
            //TODO:配置去重
        }

        /// <summary>
        /// 写入model对象到配置文件中
        /// 覆盖写入
        /// 文件格式：GBK
        /// </summary>
        /// <param name="configModel">配置model</param>
        /// <param name="cfgFilePath">配置文件路径</param>
        /// <returns></returns>
        private static bool WirteConfigFile(FileConfigModel configModel, string cfgFilePath)
        {
            if (cfgFilePath == string.Empty || cfgFilePath == null)
                return false;

            try
            {
                string jsonStr = JsonConvert.SerializeObject(configModel, Formatting.Indented);

                //用下面的方法，文件json内容末尾会出现多出一个右大括号的问题
                //FileStreamOptions fsOpt = new FileStreamOptions();
                //fsOpt.Access= FileAccess.Write;
                //fsOpt.Mode= FileMode.OpenOrCreate;
                //using (StreamWriter sw = new StreamWriter(cfgFilePath, Encoding.GetEncoding("GBK"), fsOpt))
                //{
                //    sw.Write(jsonStr);
                //    sw.Close();
                //}

                File.WriteAllText(cfgFilePath, jsonStr, Encoding.GetEncoding("GBK"));

                // 插入数据

            }
            catch (Exception ex)
            {
                string msg = $"写Model到Json文件错误：{ex.Message}";
                Debug.WriteLine(msg);
                MessageBox.Show(msg, "提示");
                return false;
            }

            return true;
        }
    }
}//class