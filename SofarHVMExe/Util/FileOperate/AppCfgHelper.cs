using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SofarHVMExe.Utilities
{
    /// <summary>
    /// app.config文件操作类
    /// </summary>
    public class AppCfgHelper
    {
        /// <summary>
        /// 读字段
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string ReadField(string key)
        {
            try
            {
                return ConfigurationManager.AppSettings.Get(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReadField: " + ex);
            }

            return "";
        }

        /// <summary>
        /// 写字段
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void WriteField(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppSettingsSection appSettings = (AppSettingsSection)config.GetSection("appSettings");

                if (appSettings.Settings.AllKeys.Contains(key))
                {
                    appSettings.Settings[key].Value = value;
                }
                else
                {
                    appSettings.Settings.Add(key, value);
                }

                //保存和刷新
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings"); 
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WriteField: " + ex);
            }
        }

    }//class
}
