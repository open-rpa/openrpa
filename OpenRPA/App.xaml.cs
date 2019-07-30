using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRPA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static System.Windows.Forms.NotifyIcon notifyIcon { get; set; }  = new System.Windows.Forms.NotifyIcon();
        public App()
        {
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/open_rpa.ico")).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Visible = false;
            //notifyIcon.ShowBalloonTip(5000, "Title", "Text", System.Windows.Forms.ToolTipIcon.Info);
            notifyIcon.Click += nIcon_Click;
        }

        void nIcon_Click(object sender, EventArgs e)
        {
            //events comes here
            MainWindow.Visibility = Visibility.Visible;
            //MainWindow.WindowState = WindowState.Normal;
            notifyIcon.Visible = false;
            Interfaces.GenericTools.restore(Interfaces.GenericTools.mainWindow);
        }
    }
}
