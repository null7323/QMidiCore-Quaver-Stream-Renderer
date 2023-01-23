using System;
using System.Windows;

namespace QQS_UI
{
    public class Program
    {
        [STAThread]
        private static unsafe void Main(string[] args)
        {
            Console.Title = "QMIDICore Quaver Stream Renderer";
#if !DEBUG
            try
            {
#endif
                Application app = new();
                _ = app.Run(new MainWindow());
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred:\n{ex.Message}\nStack Trace:\n{ex.StackTrace}");
            }
#endif
        }
    }
}
