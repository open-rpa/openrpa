using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
            InitializeCefSharp();
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/open_rpa.ico")).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Visible = false;
            //notifyIcon.ShowBalloonTip(5000, "Title", "Text", System.Windows.Forms.ToolTipIcon.Info);
            notifyIcon.Click += nIcon_Click;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitializeCefSharp()
        {
            //Perform dependency check to make sure all relevant resources are in our output directory.
            var settings = new CefSharp.Wpf.CefSettings();
            settings.CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache");
            // We need to update useragent to be of one of the supported browsers on googles signin page
            settings.UserAgent = Config.local.cef_useragent;
            if (string.IsNullOrEmpty(Config.local.cef_useragent))
            {
                // We use firefox, since google often check if chrome is uptodate
                // Syntax
                // Mozilla/5.0 (Windows NT x.y; Win64; x64; rv:10.0) Gecko/20100101 Firefox/10.0
                // example
                // Mozilla/5.0 (Windows NT 6.1; WOW64; rv:54.0) Gecko/20100101 Firefox/72.0
                // latest ?
                settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:54.0) Gecko/20100101 Firefox/72.0";
            }
            CefSharp.Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }
        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }

            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(System.IO.Path.Combine(target.FullName, file.Name));
            }
        }
        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("CefSharp"))
            {
                string assemblyName = args.Name.Split(new[] { ',' }, 2)[0] + ".dll";
                string archSpecificPath = System.IO.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                                                       Environment.Is64BitProcess ? "x64" : "x86",
                                                       assemblyName);

                return File.Exists(archSpecificPath)
                           ? Assembly.LoadFile(archSpecificPath)
                           : null;
            }

            string folderPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = System.IO.Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (System.IO.File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);

            folderPath = Interfaces.Extensions.PluginsDirectory;
            assemblyPath = System.IO.Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (System.IO.File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);

            folderPath = Interfaces.Extensions.ProjectsDirectory;
            assemblyPath = System.IO.Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (System.IO.File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);
            return null;
        }
        void nIcon_Click(object sender, EventArgs e)
        {
            //events comes here
            MainWindow.Visibility = Visibility.Visible;
            //MainWindow.WindowState = WindowState.Normal;
            notifyIcon.Visible = false;
            Interfaces.GenericTools.Restore(Interfaces.GenericTools.MainWindow);
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
            OpenRPA.MainWindow.instance.ParseCommandLineArgs(args);
            return true;
        }
    }
}
