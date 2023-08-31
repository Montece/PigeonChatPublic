using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Pigeon_Client
{
    [Serializable]
    public class Config
    {
        [JsonIgnore]
        private const string CONFIG_FILENAME = "config.json";
        [JsonIgnore]
        public static Config CurrentConfig = null;

        [JsonProperty("server_ip")]
        public string ServerIP { get; set; }

        public static Config GetDefault()
        {
            Config config = new Config()
            {
                ServerIP = "127.0.0.1"
            };

            return config;
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(CONFIG_FILENAME))
                {
                    string text = File.ReadAllText(CONFIG_FILENAME);
                    CurrentConfig = JsonConvert.DeserializeObject<Config>(text);
                }
                else Save();
            }
            catch (Exception x)
            {
                MessageBox.Show("Ошибка загрузки конфига!", x.ToString());
            }

            if (CurrentConfig == null) Save();
        }

        public static void Save()
        {
            try
            {
                if (CurrentConfig == null) CurrentConfig = GetDefault();

                if (File.Exists(CONFIG_FILENAME)) File.Delete(CONFIG_FILENAME);
                string text = CurrentConfig.ToString();
                File.WriteAllText(CONFIG_FILENAME, text);
            }
            catch (Exception x)
            {
                MessageBox.Show("Ошибка сохранения конфига!", x.ToString());
            }
        }

        public override string ToString()
        {
            try
            {
                string text = JsonConvert.SerializeObject(this, Formatting.Indented);
                return text;
            }
            catch (Exception x)
            {
                MessageBox.Show("Ошибка!", x.ToString());
                return "error";
            }
        }
    }
}
