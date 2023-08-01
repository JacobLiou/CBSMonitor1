using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Windows.Forms;
using System.Configuration;
using MultiLanguages.I18N;
using MultiLanguages.Common;

namespace MultiLanguages.Bll
{
    /// <summary>
    /// 整体思路
    /// 1.语言Json文件
    ///分两层 第一层类名，第二层 类下的属性名。
    /// 属性名下面是语言数组 第一个是中文、第二个是英文、以此类推可以扩展其他语言
    /// 
    /// 2.更改语言配置
    /// 提供一个接口，配置当前语言
    /// 
    /// 3.获取语言值
    /// 提供一个接口，接口有两个参数:类名、属性名，再根据配置的当前语言序号 就能获取当语言配置的值
    /// </summary>
  public  class MultiLangApi
    {
        /// <summary>
        /// 获取语言值
        /// </summary>
        /// <param name="className"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string GetLangValue(string className, string propertyName)
        {
            LangResource multLnagDb = LangResource.CreateMultLnagDb();

            if (multLnagDb.I18nJObject == null)
            {
                LogHelper.WriteLog("Error: i18n JObject is null");

                return "";
            }

            if (multLnagDb.I18nJObject.SelectToken(
                  $"{className}.{propertyName}") == null)
            {
                LogHelper.WriteLog("Error: value is null");

                return "";
            }

            if (multLnagDb.CurrentLangIndex >= multLnagDb.I18nJObject.SelectToken(
                  $"{className}.{propertyName}").ToArray().Length)
            {
                LogHelper.WriteLog("Error: the current language index is out of array");

                return "";
            }

            return (string)multLnagDb.I18nJObject.SelectToken(
                $"{className}.{propertyName}[{multLnagDb.CurrentLangIndex}]");

        }
        public bool SetLang(string langName, int langIndex)
        {
            try
            {
                SetConfigValue("LangName", langName);
                SetConfigValue("LangIndex", langIndex.ToString());

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message);

                throw ex;
            }
        }

        /// <summary>
        /// 修改AppSettings中配置
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="value">相应值</param>
        public static bool SetConfigValue(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                    config.AppSettings.Settings[key].Value = value;
                else
                    config.AppSettings.Settings.Add(key, value);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 更改语言配置
        /// </summary>
        /// <param name="langName">选择的语言</param>
        /// <param name="langIndex">选择语言的序号</param>
        /// <returns>配置成功 返回 true 失败 返回 false</returns>
        public bool SetLang2(string langName, int langIndex)
        {
            try
            {
                string jsonFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"i18n\LangConfig.json");

                if (!File.Exists(jsonFile))
                {
                    return false;
                }

                string jsonString = File.ReadAllText(jsonFile, Encoding.Default);//读取文件
                JObject jObject = JObject.Parse(jsonString);//解析成json

                jObject["currentLang"]["name"] = langName;
                jObject["currentLang"]["index"] = langIndex;

                WriteJson(jsonFile, jObject);

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void WriteJson(string jsonFile, JObject jObject)
        {
            using (StreamWriter file = new StreamWriter(jsonFile))
            {
                file.Write(jObject.ToString());
            }
        }


    }
}
