using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OpenRPA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("OpenRPA"))
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();
                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }
        // static System.Threading.Mutex mutex = new System.Threading.Mutex(false, "OpenRPA");
        public static System.Windows.Forms.NotifyIcon notifyIcon { get; set; }  = new System.Windows.Forms.NotifyIcon();
        public App()
        {
            //if (!mutex.WaitOne(TimeSpan.FromSeconds(2), false))
            //{
            //    Process currentProcess = Process.GetCurrentProcess();
            //    var runningProcess = (from process in Process.GetProcesses()
            //                          where
            //                            process.Id != currentProcess.Id &&
            //                            process.ProcessName.Equals(
            //                              currentProcess.ProcessName,
            //                              StringComparison.Ordinal)
            //                          select process).FirstOrDefault();
            //    if (runningProcess != null)
            //    {
            //        GenericTools.restore(runningProcess.MainWindowHandle);
            //    }
            //    Application.Current.Shutdown(0);
            //}
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

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            //try
            //{
            //    mutex.ReleaseMutex();
            //}
            //catch (Exception)
            //{
            //}
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            nIcon_Click(null, null);
            return true;
        }
    }
}
