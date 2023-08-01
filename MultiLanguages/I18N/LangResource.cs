using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MultiLanguages.I18N
{
    public class LangResource
    {



        public JObject I18nJObject = null;
        public int CurrentLangIndex;
        private LangResource()
        {
            LoadI18nJson();
            LoadCurrentLang();
        }

        private static LangResource MultLnagDbSingleton;

        private static readonly object olocker = new object();
        public static LangResource CreateMultLnagDb()
        {
            if (MultLnagDbSingleton == null)
            {
                lock (olocker)
                {
                    if (MultLnagDbSingleton == null)
                    {
                        MultLnagDbSingleton = new LangResource();
                    }
                }
            }

            return MultLnagDbSingleton;
        }

        private void LoadI18nJson()
        {
            string langFile = GetAppSetting("LangFile");
            string jsonFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, langFile);//JSON文件路径

            if (!File.Exists(jsonFile))
            {
                return;
            }

            using (StreamReader file = File.OpenText(jsonFile))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    I18nJObject = (JObject)JToken.ReadFrom(reader);
                }
            }
        }
        private void LoadCurrentLang()
        {

            int.TryParse(GetAppSetting("LangIndex"), out CurrentLangIndex);

        }

        static string configPath = System.Reflection.Assembly.GetExecutingAssembly().Location.ToString() + ".config";
        static Configuration MyConfiguration = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
        {
            ExeConfigFilename = configPath
        }, ConfigurationUserLevel.None);

        /// <summary>
        /// 获取指定节点的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAppSetting(string key)
        {
            string Value = MyConfiguration.AppSettings.Settings[key].Value; //ConfigurationManager.AppSettings[key];

            return Value;
        }

        private void LoadCurrentLang2()
        {
            string jsonFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"i18n\LangConfig.json");

            if (!File.Exists(jsonFile))
            {
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(jsonFile, Encoding.Default);//读取文件
                JObject jObject = JObject.Parse(jsonString);//解析成json
                CurrentLangIndex = (int)jObject["currentLang"]["index"];
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

    }
}
