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
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceHandler;
                var application = new App();
                application.InitializeComponent();
                application.Run();
                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Log.Function("MainWindow", "CurrentDomain_UnhandledException");
            try
            {
                Exception ex = (Exception)args.ExceptionObject;
                Log.Error(ex, "");
                Log.Error("MyHandler caught : " + ex.Message);
                Log.Error("Runtime terminating: {0}", (args.IsTerminating).ToString());
            }
            catch (Exception)
            {
            }
        }
        static void CurrentDomain_FirstChanceHandler(object source, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            try
            {
                Exception ex = e.Exception;
                Log.Verbose("FirstChance: " + ex.ToString());
            }
            catch (Exception)
            {
            }
        }
        public static System.Windows.Forms.NotifyIcon notifyIcon { get; set; }  = new System.Windows.Forms.NotifyIcon();
        public App()
        {
            if (!string.IsNullOrEmpty(Config.local.culture))
            {
                try
                {
                    var cultur = System.Globalization.CultureInfo.GetCultureInfo(Config.local.culture);
                    System.Threading.Thread.CurrentThread.CurrentUICulture = cultur;
                    System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultur;
                    System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultur;
                    ProcessThreadCollection currentThreads = Process.GetCurrentProcess().Threads;
                }
                catch (Exception)
                {
                }
            }
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
            try
            {
                InitializeCefSharp();
                var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/open_rpa.ico")).Stream;
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                notifyIcon.Visible = false;
                //notifyIcon.ShowBalloonTip(5000, "Title", "Text", System.Windows.Forms.ToolTipIcon.Info);
                notifyIcon.Click += nIcon_Click;
                notifyIcon.DoubleClick += nIcon_Click;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitializeCefSharp()
        {
            try
            {
                //Perform dependency check to make sure all relevant resources are in our output directory.
                var settings = new CefSharp.Wpf.CefSettings();
                settings.CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache");
                if(!System.IO.Directory.Exists(settings.CachePath))
                {
                    System.IO.Directory.CreateDirectory(settings.CachePath);
                }
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
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
            string assemblyPath = "";
            if (args != null && !string.IsNullOrEmpty(args.Name)) assemblyPath = args.Name;
            try
            {
                assemblyPath = new AssemblyName(args.Name).Name + ".dll";
            }
            catch (Exception)
            {
            }
            try
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
                assemblyPath = System.IO.Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (System.IO.File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);

                folderPath = Interfaces.Extensions.PluginsDirectory;
                assemblyPath = System.IO.Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (System.IO.File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);

                folderPath = Interfaces.Extensions.ProjectsDirectory;
                assemblyPath = System.IO.Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (System.IO.File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                Log.Error(assemblyPath);
                Log.Error(ex.ToString());
            }
            return null;
        }
        void nIcon_Click(object sender, EventArgs e)
        {
            MainWindow.Visibility = Visibility.Visible;
            // notifyIcon.Visible = false;
            Interfaces.GenericTools.Restore();
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (notifyIcon != null)
            {
                if (notifyIcon.Icon != null) notifyIcon.Icon.Dispose();
                notifyIcon.Dispose();
            }
        }
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            nIcon_Click(null, null);
            RobotInstance.instance.ParseCommandLineArgs(args);
            return true;
        }
        public static Views.SplashScreen splash { get; set; }
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if(Config.local.showloadingscreen)
            {
                splash = new Views.SplashScreen();
                splash.Show();
                splash.BusyContent = "Loading main window";
            }
            AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
            System.Threading.Thread.CurrentThread.Name = "UIThread";
            if (!Config.local.isagent)
            {
                var win = new MainWindow();
                MainWindow = win;
                RobotInstance.instance.Window = win;
                RobotInstance.instance.Window.ReadyForAction += RobotInstance.instance.MainWindowReadyForAction;
                RobotInstance.instance.Window.Status += RobotInstance.instance.MainWindowStatus;
                GenericTools.MainWindow = win;
                if(!Config.local.showloadingscreen) notifyIcon.Visible = true;
            }
            else
            {
                var win = new AgentWindow();
                MainWindow = win;
                RobotInstance.instance.Window = win;
                RobotInstance.instance.Window.ReadyForAction += RobotInstance.instance.MainWindowReadyForAction;
                RobotInstance.instance.Window.Status += RobotInstance.instance.MainWindowStatus;
                GenericTools.MainWindow = win;
                notifyIcon.Visible = true;
            }
            RobotInstance.instance.Status += App_Status;
            Input.InputDriver.Instance.initCancelKey(Config.local.cancelkey);
            await Task.Run(() =>
            {
                try
                {
                    if (Config.local.showloadingscreen) splash.BusyContent = "loading plugins";
                    Plugins.LoadPlugins(RobotInstance.instance, Interfaces.Extensions.PluginsDirectory);
                    if (Config.local.showloadingscreen) splash.BusyContent = "Initialize main window";
                    RobotInstance.instance.init();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }
        private void App_Status(string message)
        {
            try
            {
                Log.Debug(message);
                // notifyIcon.ShowBalloonTip(5000, "Title", message, System.Windows.Forms.ToolTipIcon.Info);
                if (splash!=null) splash.BusyContent = message;
            }
            catch (Exception)
            {
            }
        }
    }
}
