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

        public static bool IsConfigurationAvailable => ConfigurationPath != null;

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
