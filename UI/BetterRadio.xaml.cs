using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QQS_UI.UI
{
    /// <summary>
    /// Interaction logic for BetterRadio.xaml
    /// </summary>
    public partial class BetterRadio : UserControl
    {
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(BetterRadio), new PropertyMetadata(false, (s, e) => ((BetterRadio)s).OnCheckChanged()));


        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(BetterRadio), new PropertyMetadata(""));


        public int ParentDepth
        {
            get { return (int)GetValue(ParentDepthProperty); }
            set { SetValue(ParentDepthProperty, value); }
        }

        public static readonly DependencyProperty ParentDepthProperty =
            DependencyProperty.Register("ParentDepth", typeof(int), typeof(BetterRadio), new PropertyMetadata(1));


        public static readonly RoutedEvent RadioCheckedEvent = EventManager.RegisterRoutedEvent(
            "RadioChecked", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(BetterRadio));

        public event RoutedEventHandler RadioChecked
        {
            add { AddHandler(RadioCheckedEvent, value); }
            remove { RemoveHandler(RadioCheckedEvent, value); }
        }


        void OnCheckChanged()
        {
            if (IsChecked)
            {
                if (ParentDepth != 0)
                {
                    FrameworkElement p = (FrameworkElement)Parent;
                    for (int i = 1; i < ParentDepth; i++)
                    {
                        p = (FrameworkElement)p.Parent;
                    }
                    RecursiveUncheck(p);
                }
                RaiseEvent(new RoutedEventArgs(RadioCheckedEvent));
            }
        }


        public BetterRadio()
        {
            InitializeComponent();
            new InplaceConverter(new[] {
                new BBinding(IsCheckedProperty, this)
            }, v => (bool)v[0] ? Visibility.Visible : Visibility.Hidden)
                .Set(checkedBox, VisibilityProperty);
        }

        private void RecursiveUncheck(FrameworkElement p)
        {
            if (p is Panel panel)
            {
                foreach (object c in panel.Children)
                {
                    if (c is FrameworkElement element)
                    {
                        RecursiveUncheck(element);
                    }
                }
            }

            if (p is BetterRadio radio && p != this)
            {
                radio.IsChecked = false;
            }
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsChecked = true;

            double ExpandTime = 0.1;
            double FadeTime = 0.1;

            double o = 0.7;

            double targetWidth = rippleBox.ActualWidth;

            Ellipse ellipse = new Ellipse()
            {
                Fill = (Brush)Resources["PrimaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = o
            };
            _ = ellipse.SetBinding(HeightProperty, new Binding("Width") { Source = ellipse });

            Storyboard storyboard = new Storyboard();

            DoubleAnimation expand = new DoubleAnimation(10, targetWidth, new Duration(TimeSpan.FromSeconds(ExpandTime + FadeTime)));
            storyboard.Children.Add(expand);
            Storyboard.SetTarget(expand, ellipse);
            Storyboard.SetTargetProperty(expand, new PropertyPath(WidthProperty));

            DoubleAnimation opacity = new DoubleAnimation(o, 0, new Duration(TimeSpan.FromSeconds(FadeTime)))
            {
                BeginTime = TimeSpan.FromSeconds(ExpandTime)
            };
            storyboard.Children.Add(opacity);
            Storyboard.SetTarget(opacity, ellipse);
            Storyboard.SetTargetProperty(opacity, new PropertyPath(Ellipse.OpacityProperty));

            rippleBox.Children.Add(ellipse);

            storyboard.Begin();

            double waitTime = ExpandTime + FadeTime;
            _ = Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(waitTime));
                Dispatcher.Invoke(() =>
                {
                    rippleBox.Children.Remove(ellipse);
                });
            });
        }
    }
}
