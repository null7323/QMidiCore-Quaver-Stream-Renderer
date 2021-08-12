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
using System.Diagnostics;
using System.Threading;

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
        private CommonRenderer renderer = null;
        private readonly Config config;
        private readonly CustomColor customColors;
        private const string DefaultVideoFilter = "视频 (*.mp4, *.avi, *.mov)|*.mp4;*.avi;*.mov",
            PNGVideoFilter = "视频 (*.mp4, *.mov)|*.mp4, *.mov",
            TransparentVideoFilter = "视频 (*.mov)|*.mov";
        public MainWindow()
        {
            InitializeComponent();
            config = new Config();
            customColors = new CustomColor();
            if (config.CachedMIDIDirectory == null)
            {
                config.CachedMIDIDirectory = new OpenFileDialog().InitialDirectory;
            }
            if (config.CachedVideoDirectory == null)
            {
                config.CachedVideoDirectory = new SaveFileDialog().InitialDirectory;
            }
            if (config.CachedColorDirectory == null)
            {
                config.CachedColorDirectory = config.CachedVideoDirectory;
            }
            config.SaveConfig();
            previewColor.Background = new SolidColorBrush(new Color
            {
                R = (byte)(options.LineColor & 0xff),
                G = (byte)((options.LineColor & 0xff00) >> 8),
                B = (byte)((options.LineColor & 0xff0000) >> 16),
                A = 0xff
            });
#if DEBUG
            Title += " (Debug)";
#endif
        }

        private void openMidi_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Midi 文件 (*.mid)|*.mid",
                InitialDirectory = config.CachedMIDIDirectory
            };
            if ((bool)dialog.ShowDialog())
            {
                string midiDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
                config.CachedMIDIDirectory = midiDirectory;
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
                Filter = options.TransparentBackground ? TransparentVideoFilter : (options.PNGEncoder ? PNGVideoFilter : DefaultVideoFilter),
                Title = "选择保存输出视频的位置",
                InitialDirectory = config.CachedVideoDirectory
            };
            if ((bool)dialog.ShowDialog())
            {
                config.CachedVideoDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
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
            options.PreviewMode = false;
            options.AdditionalFFMpegArgument = additionalFFArgs.Text;
            Resources["notRendering"] = Resources["notRenderingOrPreviewing"] = false;
            renderer = new CommonRenderer(file, options);
            _ = Task.Run(() =>
            {
                Console.WriteLine("准备渲染...");
                //_ = Task.Run(RenderProgressCallback);
                renderer.Render();
                int gen = GC.GetGeneration(renderer);
                Dispatcher.Invoke(() =>
                {
                    renderer = null;
                    Resources["notRendering"] = Resources["notRenderingOrPreviewing"] = true;
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

        private void enableTranparentBackground_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.TransparentBackground = enableTranparentBackground.IsChecked;
            if (options.TransparentBackground)
            {
                if (!outputPath.Text.EndsWith(".mov"))
                {
                    outputPath.Text = outputPath.Text.Substring(0, outputPath.Text.Length - 4) + ".mov";
                }
            }
        }

        //private void RenderProgressCallback()
        //{
        //    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
        //    Stopwatch totalTime = Stopwatch.StartNew();
        //    TimeSpan span;
        //    while (!(bool)Resources["notRenderingOrPreviewing"])
        //    {
        //        span = Global.GetTimeOf((uint)Global.CurrentRenderTick, file.Division, file.Tempos);
        //        double rtfps = Math.Round(Global.RealtimeFPS, 3);
        //        double avgfps = Math.Round(Global.RenderedFrameCount * 1000.0 / totalTime.ElapsedMilliseconds, 3);
        //        Dispatcher.Invoke(() =>
        //        {
        //            currentMidiTime.Content = span.ToString("mm\\:ss\\.fff");
        //            percentage.Content = Math.Round(Global.CurrentRenderTick * 100 / file.MidiTime, 3).ToString();
        //            realtimeFPS.Content = $"{rtfps} ({Math.Round(rtfps / options.FPS, 3)}x)";
        //            avgFPS.Content = $"{avgfps} ({Math.Round(avgfps / options.FPS, 3)}x)";
        //        });
        //        _ = SpinWait.SpinUntil(() => false, 20);
        //    }
        //    Dispatcher.Invoke(() =>
        //    {
        //        currentMidiTime.Content = "--:--.---";
        //        percentage.Content = "0.0";
        //        realtimeFPS.Content = "0.0 (0.000x)";
        //        avgFPS.Content = "0.0 (0.000x)";
        //    });
        //}

        private void startPreview_Click(object sender, RoutedEventArgs e)
        {
            if (file == null)
            {
                _ = MessageBox.Show("无法进行预览: \nMidi 文件为空. 请检查是否加载了 Midi 文件.", "无 Midi 文件");
                return;
            }
            if (usePNGEncoder.IsChecked)
            {
                _ = MessageBox.Show("无法进行预览: \n不支持使用 PNG 序列进行预览.", "无法预览");
                return;
            }
            options.Input = midiPath.Text;
            options.Output = outputPath.Text;
            options.PreviewMode = true;
            options.AdditionalFFMpegArgument = additionalFFArgs.Text;
            Resources["notPreviewing"] = Resources["notRenderingOrPreviewing"] = false;
            renderer = new CommonRenderer(file, options);
            _ = Task.Run(() =>
            {
                Console.WriteLine("准备预览...");
                //_ = Task.Run(RenderProgressCallback);
                renderer.Render();
                int gen = GC.GetGeneration(renderer);
                Dispatcher.Invoke(() =>
                {
                    renderer = null;
                    Resources["notPreviewing"] = Resources["notRenderingOrPreviewing"] = true;
                });
                GC.Collect(gen);
            });
        }

        private void useDefaultColors_Click(object sender, RoutedEventArgs e)
        {
            customColors.UseDefault();
            customColors.SetGlobal();
            _ = MessageBox.Show("颜色重设完成.", "颜色重置完成");
        }

        private void loadColors_Click(object sender, RoutedEventArgs e)
        {
            string filePath = colorPath.Text;
            if (!filePath.EndsWith(".json"))
            {
                _ = MessageBox.Show("无法加载颜色文件.\n当前仅支持.json格式的颜色文件.", "无法加载颜色");
                return;
            }
            if (!File.Exists(filePath))
            {
                _ = MessageBox.Show("无法加载颜色文件: 文件不存在.", "无法加载颜色");
                return;
            }
            int errCode = customColors.Load(filePath);
            if (errCode == 1)
            {
                _ = MessageBox.Show("加载颜色文件时发生了错误: 此文件格式不与支持的颜色文件兼容.", "无法加载颜色");
                return;
            }
            errCode = customColors.SetGlobal();
            if (errCode != 0)
            {
                _ = MessageBox.Show("设置颜色时发生了错误: 颜色为空.", "无法设置颜色");
                return;
            }
            _ = MessageBox.Show("颜色加载成功. 一共加载了: " + customColors.Colors.Length + " 种颜色.", "颜色加载完成");
        }

        private void openColorFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json",
                InitialDirectory = config.CachedColorDirectory
            };
            if ((bool)dialog.ShowDialog())
            {
                string colorDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
                config.CachedColorDirectory = colorDirectory;
                colorPath.Text = dialog.FileName;
                config.SaveConfig();
            }
        }

        private void enableRandomColor_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (enableRandomColor.IsChecked)
            {
                _ = customColors.Shuffle().SetGlobal();
            }
            else
            {
                _ = customColors.SetGlobal();
            }
        }

        private void limitPreviewFPS_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.LimitPreviewFPS = e.NewValue;
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
            if (!options.PNGEncoder)
            {
                enableTranparentBackground.IsChecked = false;
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
