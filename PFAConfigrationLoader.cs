using QQS_UI.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QQS_UI
{
    public static class PFAConfigrationLoader
    {
        public static string ConfigurationPath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                path += "\\Piano From Above\\Config.xml";
                return File.Exists(path) ? path : null;
            }
        }

        /// <summary>
        /// 判断PFA配置文件是否可用.<br/>
        /// Determines whether PFA configuration is available.
        /// </summary>
        public static bool IsConfigurationAvailable => ConfigurationPath != null;

        /// <summary>
        /// 加载 PFA Config的颜色.<br/>
        /// Load colors from PFA configuration if possible.
        /// </summary>
        /// <returns>
        /// 如果无法加载配置, 返回<see langword="null"/>;<br/>
        /// 如果加载成功, 则以数组的形式返回这些颜色.<br/>
        /// If it fails to load PFA configuration, <see langword="null"/> will be returned.<br/>
        /// If it succeeds in loading config, then an array containing these colors will be returned.
        /// </returns>
        public static RGBAColor[] LoadPFAConfigurationColors()
        {
            if (!IsConfigurationAvailable)
            {
                return null;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(ConfigurationPath);
            XmlNode rootNode = doc.SelectSingleNode("PianoFromAbove");
            XmlNode visualNode = rootNode.SelectSingleNode("Visual");
            XmlNode colors = visualNode.SelectSingleNode("Colors");
            XmlNodeList actualColors = colors.SelectNodes("Color");
            List<RGBAColor> retColors = new List<RGBAColor>();
            foreach (XmlNode node in actualColors)
            {
                byte r = byte.Parse(node.Attributes[0].Value);
                byte g = byte.Parse(node.Attributes[1].Value);
                byte b = byte.Parse(node.Attributes[2].Value);
                retColors.Add(new RGBAColor
                {
                    R = r,
                    G = g,
                    B = b,
                    A = 0xFF
                });
            }
            Console.WriteLine("PFA 配置颜色解析完成. 一共 {0} 种颜色.", retColors.Count);
            return retColors.ToArray();
        }
    }
}
