using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI
{
    public struct DialogPath
    {
        public string MidiDirectory;
        public string VideoDirectory;
        public string ColorDirectory;
    }
    /// <summary>
    /// 表示用于存储Midi文件夹路径和视频文件夹路径.
    /// </summary>
    public class Config
    {
        private DialogPath ConfigPath;
        private readonly string ConfigName;
        /// <summary>
        /// 初始化一个新的 <see cref="Config"/> 实例.
        /// </summary>
        /// <param name="configName">配置文件的文件名, 需要".json"后缀.</param>
        public Config(string configName = "config.json")
        {
            ConfigName = configName;
            if (File.Exists(configName))
            {
                try
                {
                    string jsonData = File.ReadAllText(configName);
                    ConfigPath = JsonConvert.DeserializeObject<DialogPath>(jsonData);
                }
                catch
                {
                    File.Create(ConfigName).Close();
                    ConfigPath = new DialogPath();
                }
            }
            else
            {
                File.Create(ConfigName).Close();
                ConfigPath = new DialogPath();
            }
        }
        public string CachedVideoDirectory
        {
            get => ConfigPath.VideoDirectory;
            set => ConfigPath.VideoDirectory = value;
        }

        public string CachedColorDirectory
        {
            get => ConfigPath.ColorDirectory;
            set => ConfigPath.ColorDirectory = value;
        }

        public string CachedMIDIDirectory
        {
            get => ConfigPath.MidiDirectory;
            set => ConfigPath.MidiDirectory = value;
        }

        public void SaveConfig()
        {
            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(ConfigPath));
        }
    }
}
