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
        private int keyHeightPercentage = 15;
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
                R = (byte)(options.DivideBarColor & 0xff),
                G = (byte)((options.DivideBarColor & 0xff00) >> 8),
                B = (byte)((options.DivideBarColor & 0xff0000) >> 16),
                A = 0xff
            });
            previewBackgroundColor.Background = new SolidColorBrush(new Color
            {
                R = 0,
                G = 0,
                B = 0,
                A = 255
            });

            if (!PFAConfigrationLoader.IsConfigurationAvailable)
            {
                loadPFAColors.IsEnabled = false;
            }

            renderWidth.Value = 1920;
            renderHeight.Value = 1080;
            noteSpeed.Value = 1.5;
#if DEBUG
            Title += " (Debug)";
#endif
            unpressedKeyboardGradientStrength.Value = Global.DefaultUnpressedWhiteKeyGradientScale;
            pressedKeyboardGradientStrength.Value = Global.DefaultPressedWhiteKeyGradientScale;
            noteGradientStrength.Value = Global.DefaultNoteGradientScale;
            separatorGradientStrength.Value = Global.DefaultSeparatorGradientScale;

            int processorCount = Environment.ProcessorCount;
            maxMidiLoaderConcurrency.Value = processorCount;
            maxRenderConcurrency.Value = processorCount;
            Global.MaxMIDILoaderConcurrency = -1;
            Global.MaxRenderConcurrency = -1;

            options.VideoQuality = 17;
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
            midiLen.Content = "加载中...";
            midiPPQ.Content = "加载中...";
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
                    midiPPQ.Content = file.Division;
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
            midiPPQ.Content = '-';
        }

        private void noteSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            options.NoteSpeed = noteSpeed.Value;
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
            options.VideoQuality = (int)crfSelect.Value;
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

        private void limitPreviewFPS_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.LimitPreviewFPS = e.NewValue;
        }

        private void loadPFAColors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RGBAColor[] colors = PFAConfigrationLoader.LoadPFAConfigurationColors();
                customColors.Colors = colors;
                _ = customColors.SetGlobal();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"加载 PFA 配置颜色时出现了错误: \n{ex.Message}\n栈追踪: \n{ex.StackTrace}", "无法加载 PFA 配置");
            }
        }

        private void setbgColor_Click(object sender, RoutedEventArgs e)
        {
            string coltxt = bgColor.Text;
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
                options.BackgroundColor = col;
                previewBackgroundColor.Background = new SolidColorBrush(new Color()
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

        private void drawGreySquare_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.DrawGreySquare = e.NewValue;
        }

        private void enableNoteColorGradient_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.Gradient = e.NewValue;
        }

        private void shuffleColor_Click(object sender, RoutedEventArgs e)
        {
            customColors.Shuffle().SetGlobal();
        }

        private void enableSeparator_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.DrawSeparator = e.NewValue;
        }

        private void thinnerNotes_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.ThinnerNotes = e.NewValue;
        }

        private void fps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.FPS = (int)e.NewValue;
        }

        private void renderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.Width = (int)e.NewValue;
        }

        private void renderHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.Height = (int)e.NewValue;
            options.KeyHeight = options.Height * keyHeightPercentage / 100;
        }

        private void presetResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (presetResolution.SelectedIndex)
            {
                case 0:
                    renderWidth.Value = 640;
                    renderHeight.Value = 480;
                    break;
                case 1:
                    renderWidth.Value = 1280;
                    renderHeight.Value = 720;
                    break;
                case 2:
                    renderWidth.Value = 1920;
                    renderHeight.Value = 1080;
                    break;
                case 3:
                    renderWidth.Value = 2560;
                    renderHeight.Value = 1440;
                    break;
                default:
                    renderWidth.Value = 3840;
                    renderHeight.Value = 2160;
                    break;
            }
        }

        private void keyboardHeightPercentage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            keyHeightPercentage = (int)e.NewValue;
            options.KeyHeight = options.Height * keyHeightPercentage / 100;
        }

        private void delayStart_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.DelayStartSeconds = (double)e.NewValue;
        }

        private void resetGradientScale_Click(object sender, RoutedEventArgs e)
        {
            unpressedKeyboardGradientStrength.Value = Global.DefaultUnpressedWhiteKeyGradientScale;
            pressedKeyboardGradientStrength.Value = Global.DefaultPressedWhiteKeyGradientScale;
            noteGradientStrength.Value = Global.DefaultNoteGradientScale;
            separatorGradientStrength.Value = Global.DefaultSeparatorGradientScale;

            unpressedKeyboardGradientStrength.slider.Value = Global.DefaultUnpressedWhiteKeyGradientScale;
            pressedKeyboardGradientStrength.slider.Value = Global.DefaultPressedWhiteKeyGradientScale;
            noteGradientStrength.slider.Value = Global.DefaultNoteGradientScale;
            separatorGradientStrength.slider.Value = Global.DefaultSeparatorGradientScale;

            options.KeyboardGradientDirection = VerticalGradientDirection.FromButtomToTop;
            options.SeparatorGradientDirection = VerticalGradientDirection.FromButtomToTop;
            options.NoteGradientDirection = HorizontalGradientDirection.FromLeftToRight;

            keyboardGradientDirection.SelectedIndex = 0;
            noteGradientDirection.SelectedIndex = 0;
            barGradientDirection.SelectedIndex = 0;
        }

        private void noteGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.NoteGradientScale = e.NewValue;
        }

        private void unpressedKeyboardGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.UnpressedWhiteKeyGradientScale = e.NewValue;
        }

        private void pressedKeyboardGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.PressedWhiteKeyGradientScale = e.NewValue;
        }

        private void separatorGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.SeparatorGradientScale = e.NewValue;
        }

        private void noteGradientDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            options.NoteGradientDirection = (HorizontalGradientDirection)noteGradientDirection.SelectedIndex;
        }

        private void keyboardGradientDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            options.KeyboardGradientDirection = (VerticalGradientDirection)keyboardGradientDirection.SelectedIndex;
        }

        private void barGradientDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            options.SeparatorGradientDirection = (VerticalGradientDirection)barGradientDirection.SelectedIndex;
        }

        private void betterBlackKeys_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.BetterBlackKeys = e.NewValue;
        }

        private void drawKeyboard_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (e.NewValue)
            {
                options.KeyHeight = options.Height * keyHeightPercentage / 100;
                if (enableSeparator != null)
                {
                    options.DrawSeparator = enableSeparator.IsChecked;
                }
                if (drawGreySquare != null)
                {
                    options.DrawGreySquare = drawGreySquare.IsChecked;
                }

            }
            else
            {
                options.KeyHeight = 0;
                options.DrawGreySquare = false;
                options.DrawSeparator = false;
            }
        }

        private void maxRenderConcurrency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            Global.MaxRenderConcurrency = !useDefaultRenderConcurrency.IsChecked ? (int)e.NewValue : -1;
        }

        private void maxMidiLoaderConcurrency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            Global.MaxMIDILoaderConcurrency = !useDefaultMidiLoaderConcurrency.IsChecked ? (int)e.NewValue : -1;
        }

        private void useDefaultRenderConcurrency_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.MaxRenderConcurrency = e.NewValue ? -1 : (int)maxRenderConcurrency.Value;
        }

        private void useDefaultMidiLoaderConcurrency_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.MaxMIDILoaderConcurrency = e.NewValue ? -1 : (int)maxMidiLoaderConcurrency.Value;
        }

        private void videoQualityOptions_RadioChanged(object sender, RoutedEventArgs e)
        {
            if (sender == crfOptions)
            {
                options.QualityOptions = VideoQualityOptions.CRF;
                options.VideoQuality = crfSelect != null ? (int)crfSelect.Value : 17;
            }
            else
            {
                options.QualityOptions = VideoQualityOptions.Bitrate;
                options.VideoQuality = videoBitrate != null ? (int)videoBitrate.Value : 50000;
            }
        }

        private void videoBitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.VideoQuality = (int)e.NewValue;
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
                uint col = 0xFF000000U | r | (uint)(g << 8) | (uint)(b << 16);
                options.DivideBarColor = col;
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
