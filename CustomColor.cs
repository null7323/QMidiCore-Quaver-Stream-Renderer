using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QQS_UI.Core;

namespace QQS_UI
{
    public class CustomColor
    {
        public RGBAColor[] Colors;
        public CustomColor(string colorFileName = "colors.json")
        {
            if (!File.Exists(colorFileName))
            {
                string colors = JsonConvert.SerializeObject(Global.Colors);
                File.WriteAllText(colorFileName, colors);
                Colors = new RGBAColor[96];
                Array.Copy(Global.DefaultColors, Colors, 96);
            }
            else
            {
                try
                {
                    string colorData = File.ReadAllText(colorFileName);
                    Colors = JsonConvert.DeserializeObject<RGBAColor[]>(colorData);
                }
                catch
                {
                    Console.WriteLine("加载颜色配置时出现错误, 将使用默认颜色...");
                    Colors = new RGBAColor[96];
                    Array.Copy(Global.DefaultColors, Colors, 96);
                }
            }
            Console.WriteLine("颜色加载完成. 共 {0} 种颜色.", Colors.Length);
        }

        private CustomColor()
        {

        }
        /// <summary>
        /// 将指定的文件的颜色加载到当前实例中.
        /// </summary>
        /// <param name="colorFileName">颜色文件路径.</param>
        /// <returns>如果文件不存在, 返回-1; 如果加载时出现问题, 返回1; 如果没有错误, 返回0.</returns>
        public int Load(string colorFileName)
        {
            if (!File.Exists(colorFileName))
            {
                return -1;
            }
            string colorData = File.ReadAllText(colorFileName);
            RGBAColor[] lastColors = Colors;
            try
            {
                Colors = JsonConvert.DeserializeObject<RGBAColor[]>(colorData);
            }
            catch
            {
                Colors = lastColors;
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// 将当前实例的颜色拷贝到<see cref="Global.Colors"/>中.
        /// </summary>
        /// <remarks>
        /// 这不是一个线程安全操作.
        /// </remarks>
        /// <returns>
        /// 如果当前颜色为<see langword="null"/>, 返回-1;
        /// 如果当前颜色不为<see langword="null"/>, 但是长度为0, 返回1;
        /// 如果操作无异常, 返回0.
        /// </returns>
        public int SetGlobal()
        {
            if (Colors == null)
            {
                return -1;
            }
            if (Colors.Length == 0)
            {
                return 1;
            }
            Global.Colors = new RGBAColor[Colors.Length];
            Array.Copy(Colors, Global.Colors, Colors.Length);
            return 0;
        }

        public void UseDefault()
        {
            Colors = new RGBAColor[96];
            Array.Copy(Global.DefaultColors, Colors, 96);
            Global.Colors = Colors;
        }

        public CustomColor Shuffle()
        {
            CustomColor shuffled = new CustomColor
            {
                Colors = new RGBAColor[Colors.Length]
            };
            Array.Copy(Colors, shuffled.Colors, Colors.Length);
            Random rand = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < shuffled.Colors.Length; i++)
            {
                int x, y;
                RGBAColor col;
                x = rand.Next(0, shuffled.Colors.Length);
                do
                {
                    y = rand.Next(0, shuffled.Colors.Length);
                } while (y == x);

                col = shuffled.Colors[x];
                shuffled.Colors[x] = shuffled.Colors[y];
                shuffled.Colors[y] = col;
            }

            return shuffled;
        }
    }
}
