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
using System.Threading;
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
                // AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceHandler;
                try
                {
                    var args = Environment.GetCommandLineArgs();
                    CommandLineParser parser = new CommandLineParser();
                    // parser.Parse(string.Join(" ", args), true);
                    var options = parser.Parse(args, true);
                    if (options.ContainsKey("workingdir"))
                    {
                        var filepath = options["workingdir"].ToString();
                        if (System.IO.Directory.Exists(filepath))
                        {
                            Log.ResetLogPath(filepath);
                        }
                        else
                        {
                            MessageBox.Show("Path not found " + filepath);
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                }
                var application = new App();
                application.InitializeComponent();
                application.Run();
                // application.Run();
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
                Log.Error(ex.ToString());
                Log.Error("MyHandler caught : " + ex.Message);
                Log.Error("Runtime terminating: {0}", (args.IsTerminating).ToString());
            }
            catch (Exception)
            {
            }
        }
        public static System.Windows.Forms.NotifyIcon notifyIcon { get; set; } = new System.Windows.Forms.NotifyIcon();
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
                    foreach (object obj in currentThreads)
                    {
                        try
                        {
                            Thread t = obj as Thread;
                            if (t != null)
                            {
                                t.CurrentUICulture = cultur;
                                t.CurrentCulture = cultur;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }


                }
                catch (Exception)
                {
                }
            }
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
            try
            {
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

                folderPath = Path.Combine(Interfaces.Extensions.ProjectsDirectory, "extensions");
                assemblyPath = System.IO.Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
                if (System.IO.File.Exists(assemblyPath)) return Assembly.LoadFrom(assemblyPath);

                folderPath = System.IO.Path.GetTempPath();
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
            GenericTools.Restore();
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
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
                System.Threading.Thread.CurrentThread.Name = "UIThread";
                if (!Config.local.isagent)
                {
                    StartupUri = new Uri("/OpenRPA;component/MainWindow.xaml", UriKind.Relative);
                    notifyIcon.Visible = false;
                }
                else
                {
                    StartupUri = new Uri("/OpenRPA;component/AgentWindow.xaml", UriKind.Relative);
                    notifyIcon.Visible = true;
                }
                if (Config.local.files_pending_deletion.Length > 0)
                {
                    bool sucess = true;
                    foreach (var f in Config.local.files_pending_deletion)
                    {
                        try
                        {
                            if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
                        }
                        catch (Exception ex)
                        {
                            sucess = false;
                            Log.Error(ex.ToString());
                        }
                    }
                    if (sucess)
                    {
                        Config.local.files_pending_deletion = new string[] { };
                        Config.Save();
                    }
                }

                if (Config.local.restoreDependenciesOnStartup)
                {
                    Log.Debug("Package restore on startup enabled -> cleaning existing extensions.");
                    var extensionsPath = Path.Combine(Interfaces.Extensions.ProjectsDirectory, "extensions");
                    if (Directory.Exists(extensionsPath))
                    {
                        foreach (var file in Directory.GetFiles(extensionsPath))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Could not clean extension: " + ex.ToString());
                            }
                        }
                    }
                }

                RobotInstance.instance.Status += App_Status;
                Input.InputDriver.Instance.initCancelKey(Config.local.cancelkey);
                Plugins.LoadPlugins(RobotInstance.instance, Interfaces.Extensions.PluginsDirectory, false);
                RobotInstance.instance.Initialize();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }

            await Task.Run(async () =>
            {
                try
                {
                    // if (Config.local.showloadingscreen) splash.BusyContent = "loading plugins";
                    // Plugins.LoadPlugins(RobotInstance.instance, Interfaces.Extensions.ProjectsDirectory);
                    // if (Config.local.showloadingscreen) splash.BusyContent = "Initialize main window";
                    await RobotInstance.instance.init();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    Console.WriteLine(ex.ToString());
                    MessageBox.Show(ex.Message);
                }
            });
        }
        private void App_Status(string message)
        {
            try
            {
                Log.Debug(message);
                // notifyIcon.ShowBalloonTip(5000, "Title", message, System.Windows.Forms.ToolTipIcon.Info);
                // if (splash != null) splash.BusyContent = message;
            }
            catch (Exception)
            {
            }
        }
    }
}
