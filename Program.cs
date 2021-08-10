using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using QQS_UI.Core;
using SharpExtension.Collections;
using SharpExtension;
using System.IO;

namespace QQS_UI
{
    public class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application app = new Application();
            _ = app.Run(new MainWindow());
        }
    }
}
