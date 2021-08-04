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
                string jsonData = File.ReadAllText(configName);
                ConfigPath = JsonConvert.DeserializeObject<DialogPath>(jsonData);
            }
            else
            {
                File.Create(ConfigName).Close();
                ConfigPath = new DialogPath();
            }
        }
        public string GetCachedMidiDirectory()
        {
            return ConfigPath.MidiDirectory;
        }
        public string GetCachedVideoDirectory()
        {
            return ConfigPath.VideoDirectory;
        }

        public void SetCachedMidiDirectory(string path)
        {
            ConfigPath.MidiDirectory = path;
        }

        public void SetCachedVideoDirectory(string path)
        {
            ConfigPath.VideoDirectory = path;
        }

        public void SaveConfig()
        {
            File.WriteAllText(ConfigName, JsonConvert.SerializeObject(ConfigPath));
        }
    }
}
