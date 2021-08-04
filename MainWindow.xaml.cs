using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QQS_UI.Core;
using Path = System.IO.Path;

namespace QQS_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RenderFile file = null;
        private bool isLoading = false;
        private Core.RenderOptions options = Core.RenderOptions.CreateRenderOptions();
        private Renderer renderer = null;
        private readonly Config config;
        private const string DefaultVideoFilter = "视频 (*.mp4, *.avi, *.mov)|*.mp4;*.avi;*.mov",
            TransparentVideoFileter = "视频 (*.mp4, *.mov)|*.mp4, *.mov";
        public MainWindow()
        {
            InitializeComponent();
            config = new Config();
            if (config.GetCachedMidiDirectory() == null)
            {
                config.SetCachedMidiDirectory(new OpenFileDialog().InitialDirectory);
            }
            if (config.GetCachedVideoDirectory() == null)
            {
                config.SetCachedVideoDirectory(new SaveFileDialog().InitialDirectory);
            }
            config.SaveConfig();
            previewColor.Background = new SolidColorBrush(new Color
            {
                R = (byte)(options.LineColor & 0xff),
                G = (byte)((options.LineColor & 0xff00) >> 8),
                B = (byte)((options.LineColor & 0xff0000) >> 16),
                A = 0xff
            });
        }

        private void openMidi_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Midi 文件 (*.mid)|*.mid",
                InitialDirectory = config.GetCachedMidiDirectory()
            };
            if ((bool)dialog.ShowDialog())
            {
                string midiDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
                config.SetCachedMidiDirectory(midiDirectory);
                midiPath.Text = dialog.FileName;
                config.SaveConfig();
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            string fileName = midiPath.Text;
            if (!File.Exists(fileName) || !fileName.EndsWith(".mid"))
            {
                _ = MessageBox.Show("不正确的 Midi 路径!", "无法加载 Midi 文件");
                return;
            }
            trackCount.Content = "加载中...";
            noteCount.Content = "加载中...";
            _ = Task.Run(() =>
            {
                isLoading = true;
                file = new RenderFile(fileName);
                isLoading = false;
                TimeSpan midilen = Global.GetTimeOf(file.MidiTime, file.Division, file.Tempos);
                Dispatcher.Invoke(() =>
                {
                    Resources["midiLoaded"] = true;
                    trackCount.Content = file.TrackCount.ToString();
                    noteCount.Content = file.NoteCount.ToString();
                    midiLen.Content = midilen.ToString("mm\\:ss\\.fff");
                });
            });
        }

        private void unloadButton_Click(object sender, RoutedEventArgs e)
        {
            int gen = GC.GetGeneration(file);
            file = null;
            GC.Collect(gen);
            Resources["midiLoaded"] = false;
            Console.WriteLine("Midi 已经卸载.");
            noteCount.Content = "-";
            trackCount.Content = "-";
            midiLen.Content = "--:--.---";
        }

        private void fpsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (fpsBox.SelectedIndex)
            {
                case 0:
                    options.FPS = 30;
                    break;
                case 1:
                    options.FPS = 60;
                    break;
                case 2:
                    options.FPS = 120;
                    break;
                default:
                    options.FPS = 240;
                    break;
            }
        }

        private void noteSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            options.NoteSpeed = noteSpeed.Value;
        }

        private void renderResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (renderResolution.SelectedIndex)
            {
                case 0:
                    options.Width = 640;
                    options.Height = 480;
                    break;
                case 1:
                    options.Width = 1280;
                    options.Height = 720;
                    break;
                case 2:
                    options.Width = 1920;
                    options.Height = 1080;
                    break;
                case 3:
                    options.Width = 2560;
                    options.Height = 1440;
                    break;
                case 4:
                    options.Width = 3840;
                    options.Height = 2160;
                    break;
                default:
                    break;
            }
            options.KeyHeight = options.Height * 15 / 100;
        }

        private void selectOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = options.PNGEncoder ? TransparentVideoFileter : DefaultVideoFilter,
                Title = "选择保存输出视频的位置",
                InitialDirectory = config.GetCachedVideoDirectory()
            };
            if ((bool)dialog.ShowDialog())
            {
                config.SetCachedVideoDirectory(Path.GetDirectoryName(Path.GetFullPath(dialog.FileName)));
                outputPath.Text = dialog.FileName;
                config.SaveConfig();
            }
        }

        private void startRender_Click(object sender, RoutedEventArgs e)
        {
            if (file == null)
            {
                _ = MessageBox.Show("无法进行渲染: \nMidi 文件为空. 请检查是否加载了 Midi 文件.", "无 Midi 文件");
                return;
            }
            options.Input = midiPath.Text;
            options.Output = outputPath.Text;
            Resources["notRendering"] = false;
            renderer = new Renderer(file, options);
            _ = Task.Run(() =>
            {
                Console.WriteLine("准备渲染...");
                renderer.Render();
                int gen = GC.GetGeneration(renderer);
                Dispatcher.Invoke(() =>
                {
                    renderer = null;
                    Resources["notRendering"] = true;
                });
                GC.Collect(gen);
            });
        }

        private void interruptButton_Click(object sender, RoutedEventArgs e)
        {
            if (renderer != null)
            {
                renderer.Interrupt = true;
            }
        }

        private void crfSelect_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.CRF = (int)crfSelect.Value;
        }

        private void setBarColor_Click(object sender, RoutedEventArgs e)
        {
            string coltxt = barColor.Text;
            if (coltxt.Length != 6)
            {
                _ = MessageBox.Show("当前的颜色代码不符合规范.\n一个颜色代码应当由6位16进制表示的数字组成.", "无法设置颜色");
                return;
            }
            try
            {
                byte r = Convert.ToByte(coltxt.Substring(0, 2), 16);
                byte g = Convert.ToByte(coltxt.Substring(2, 2), 16);
                byte b = Convert.ToByte(coltxt.Substring(4, 2), 16);
                uint col = 0xff000000U | r | (uint)(g << 8) | (uint)(b << 16);
                options.LineColor = col;
                previewColor.Background = new SolidColorBrush(new Color()
                {
                    R = r,
                    G = g,
                    B = b,
                    A = 0xff
                });
            }
            catch
            {
                _ = MessageBox.Show("错误: 无法解析颜色代码.\n请检查输入的颜色代码是否正确.", "无法设置颜色");
            }
        }

        private void usePNGEncoder_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (outputPath.Text != null)
            {
                if (outputPath.Text.EndsWith(".avi") && e.NewValue)
                {
                    _ = MessageBox.Show("注意: 暂不支持以.avi为后缀的 PNG 编码视频.\n请保存为.mp4或者.mov格式.", "无法设置为PNG 编码器");
                    e.Handled = true;
                    usePNGEncoder.IsChecked = false;
                    return;
                }
            }
            options.PNGEncoder = usePNGEncoder.IsChecked;
            if (options.PNGEncoder)
            {
                options.TransparentBackground = false;
            }
        }
    }

    internal class NotValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    internal class AndValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = true;
            foreach (object obj in values)
            {
                b &= (bool)obj;
            }
            return b;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
