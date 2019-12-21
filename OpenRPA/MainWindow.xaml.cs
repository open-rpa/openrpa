using Microsoft.VisualBasic.Activities;
using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using OpenRPA.Net;
using System;
using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using Xceed.Wpf.AvalonDock.Layout;

namespace OpenRPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IMainWindow, IOpenRPAClient
    {
        public static MainWindow instance = null;
        readonly Updates updater = new Updates();
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public event SignedinEventHandler Signedin;
        public event DisconnectedEventHandler Disconnected;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Project>();
        private bool isRecording = false;
        private bool autoReconnect = true;
        private bool loginInProgress = false;
        // public static Tracing tracing = new Tracing();
        public Tracing Tracing { get; set; } = new Tracing();
        private static readonly object statelock = new object();
        public List<string> OCRlangs { get; set; } = new List<string>() { "afr", "amh", "ara", "asm", "aze", "aze_cyrl", "bel", "ben", "bod", "bos", "bre", "bul", "cat", "ceb", "ces", "chi_sim", "chi_sim_vert", "chi_tra", "chi_tra_vert", "chr", "cos", "cym", "dan", "dan_frak", "deu", "deu_frak", "div", "dzo", "ell", "eng", "enm", "epo", "equ", "est", "eus", "fao", "fas", "fil", "fin", "fra", "frk", "frm", "fry", "gla", "gle", "glg", "grc", "guj", "hat", "heb", "hin", "hrv", "hun", "hye", "iku", "ind", "isl", "ita", "ita_old", "jav", "jpn", "jpn_vert", "kan", "kat", "kat_old", "kaz", "khm", "kir", "kmr", "kor", "kor_vert", "lao", "lat", "lav", "lit", "ltz", "mal", "mar", "mkd", "mlt", "mon", "mri", "msa", "mya", "nep", "nld", "nor", "oci", "ori", "osd", "pan", "pol", "por", "pus", "que", "ron", "rus", "san", "sin", "slk", "slk_frak", "slv", "snd", "spa", "spa_old", "sqi", "srp", "srp_latn", "sun", "swa", "swe", "syr", "tam", "tat", "tel", "tgk", "tgl", "tha", "tir", "ton", "tur", "uig", "ukr", "urd", "uzb", "uzb_cyrl", "vie", "yid", "yor" };
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public string defaultocrlangs
        {
            get
            {
                return Config.local.ocrlanguage;
            }
            set
            {
                Config.local.ocrlanguage = value;
                Config.Save();
            }
        }
        public Views.WFToolbox Toolbox { get; set; }
        public Views.Snippets Snippets { get; set; }
        public bool AllowQuite { get; set; } = true;
        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
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
        public MainWindow()
        {
            if (!string.IsNullOrEmpty(Config.local.culture))
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(Config.local.culture);
            }
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);
            System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = false;
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            InitializeComponent();
            try
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "Snippets.dll"))) System.IO.File.Delete(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "Snippets.dll"));
                if (System.IO.File.Exists("Snippets.dll")) System.IO.File.Delete("Snippets.dll");
            }
            catch (Exception)
            {
            }
            AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
            SetStatus("Initializing events");
            instance = this;
            DataContext = this;
            GenericTools.MainWindow = this;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AppDomain currentDomain = AppDomain.CurrentDomain;
            System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.Diagnostics.Trace.Listeners.Add(Tracing);
            //Console.SetOut(new DebugTextWriter());
            Console.SetOut(new ConsoleDecorator(Console.Out));
            Console.SetError(new ConsoleDecorator(Console.Out, true));
            lvDataBinding.ItemsSource = Plugins.recordPlugins;
            cancelkey.Text = Config.local.cancelkey;
            InputDriver.Instance.onCancel += OnCancel;
            NotifyPropertyChanged("Toolbox");
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetStatus("Checking for updates");
            _ = CheckForUpdatesAsync();
            SetStatus("Registering Designer Metadata");
            new DesignerMetadata().Register();
            SetStatus("init CancelKey and Input Driver");
            OpenRPA.Input.InputDriver.Instance.initCancelKey(cancelkey.Text);
            SetStatus("loading plugins");
            await Plugins.LoadPlugins(this, Interfaces.Extensions.PluginsDirectory);
            //await Task.Run(() =>
            //{
            //    GenericTools.RunUI(() =>
            //    {
            //        Plugins.loadPlugins(Extensions.projectsDirectory);
            //    });                
            //});
            LoadLayout();
            if (string.IsNullOrEmpty(Config.local.wsurl))
            {
                SetStatus("loading detectors");
                var Detectors = Detector.loadDetectors(Interfaces.Extensions.ProjectsDirectory);
                foreach (var d in Detectors)
                {
                    IDetectorPlugin dp = null;
                    d.Path = Interfaces.Extensions.ProjectsDirectory;
                    dp = Plugins.AddDetector(this, d);
                    if (dp != null) dp.OnDetector += OnDetector;
                }
            }
            try
            {
                SetStatus("loading workflow toolbox");
                Toolbox = new Views.WFToolbox();
                NotifyPropertyChanged("Toolbox");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            try
            {
                SetStatus("loading Snippets toolbox");
                Snippets = new Views.Snippets();
                NotifyPropertyChanged("Snippets");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            await Task.Run(() =>
            {
                try
                {
                    // ExpressionEditor.EditorUtil.Init();
                    _ = CodeEditor.init.Initialize();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }                
            });
            await Task.Run(() =>
            {                
                // LoadLayout();
                if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    global.webSocketClient = new WebSocketClient(Config.local.wsurl);
                    global.webSocketClient.OnOpen += WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    SetStatus("Connecting to " + Config.local.wsurl);
                    _ = global.webSocketClient.Connect();
                }
                else
                {
                    SetStatus("loading projects and workflows");
                    var _Projects = Project.LoadProjects(Interfaces.Extensions.ProjectsDirectory);
                    Projects = new System.Collections.ObjectModel.ObservableCollection<Project>();
                    foreach (Project p in _Projects)
                    {
                        Projects.Add(p);
                    }

                    System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = true;
                    System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
                    GenericTools.RunUI(() =>
                    {
                        InputDriver.Instance.Initialize();
                    });
                    SetStatus("Run pending workflow instances");
                    Log.Debug("RunPendingInstances::begin ");
                    foreach (Project p in _Projects)
                    {
                        foreach (var workflow in p.Workflows)
                        {
                            if (workflow.Project != null)
                            {
                                _ = workflow.RunPendingInstances();
                            }

                        }
                    }
                    Log.Debug("RunPendingInstances::end ");
                }
                AutomationHelper.init();
                SetStatus("Reopening workflows");
                OnOpen(null);
                AddHotKeys();
                if (string.IsNullOrEmpty(Config.local.wsurl))
                {
                    LoadLayout();
                }
            });
        }
        private void WebSocketClient_OnOpen()
        {
            AutomationHelper.syncContext.Post(async o =>
            {
                string url = "http";
                var u = new Uri(Config.local.wsurl);
                if (u.Scheme == "wss" || u.Scheme == "https") url = "https";
                url = url + "://" + u.Host;
                if (!u.IsDefaultPort) url = url + ":" + u.Port.ToString();
                // App.notifyIcon.ShowBalloonTip(5000, "tooltiptitle", "tipMessage", System.Windows.Forms.ToolTipIcon.Info);
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Log.Debug("WebSocketClient_OnOpen::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                SetStatus("Connected to " + Config.local.wsurl);
                TokenUser user = null;
                while (user == null)
                {
                    string errormessage = string.Empty;
                    if (!string.IsNullOrEmpty(Config.local.username) && Config.local.password != null && Config.local.password.Length > 0)
                    {
                        try
                        {
                            SetStatus("Connected to " + Config.local.wsurl + " signing in as " + Config.local.username + " ...");
                            Log.Debug("Signing in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.username, Config.local.UnprotectString(Config.local.password));
                            Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                        }
                        catch (Exception ex)
                        {
                            this.Hide();
                            Log.Error(ex, "");
                            errormessage = ex.Message;
                        }
                    }
                    if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                    {
                        try
                        {
                            SetStatus("Connected to " + Config.local.wsurl + " signing ...");
                            Log.Debug("Signing in with token " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt));
                            if (user != null)
                            {
                                Config.local.username = user.username;
                                Config.local.password = new byte[] { };
                                Config.Save();
                                Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Hide();
                            Log.Error(ex, "");
                            errormessage = ex.Message;
                        }
                    }
                    if (user == null)
                    {
                        if (loginInProgress == false)
                        {
                            loginInProgress = true;
                            string jwt = null;
                            try
                            {
                                Hide();
                                var signinWindow = new Views.SigninWindow(url, true);
                                signinWindow.ShowDialog();
                                jwt = signinWindow.jwt;
                                if (!string.IsNullOrEmpty(jwt))
                                {
                                    Config.local.jwt = Config.local.ProtectString(jwt);
                                    user = await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt));
                                    if (user != null)
                                    {
                                        Config.local.username = user.username;
                                        Config.Save();
                                        Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                        SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                                    }
                                }
                                else
                                {
                                    Close();
                                    Application.Current.Shutdown();
                                }

                            }
                            catch (Exception)
                            {
                                throw;
                            }
                            finally
                            {
                                Show();
                                loginInProgress = false;
                            }
                            //SetStatus("Connected to " + Config.local.wsurl);
                            //loginInProgress = true;
                            //var w = new Views.LoginWindow();
                            //w.username = Config.local.username;
                            //w.errormessage = errormessage;
                            //w.fqdn = new Uri(Config.local.wsurl).Host;
                            //this.Hide();
                            //if (w.ShowDialog() != true) { this.Show(); return; }
                            //Config.local.username = w.username; Config.local.password = Config.local.ProtectString(w.password);
                            //Config.Save();
                            //loginInProgress = false;

                        }
                        else
                        {
                            return;
                        }
                    }
                }
                this.Show();
                var test = lvDataBinding.ItemsSource;
                //lvDataBinding.ItemsSource = Plugins.recordPlugins;
                try
                {
                    if (Projects.Count == 0)
                    {

                        SetStatus("Loading workflows and state from " + Config.local.wsurl);
                        Log.Debug("Get workflows from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}", top: 5000);
                        Log.Debug("Get projects from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var projects = await global.webSocketClient.Query<Project>("openrpa", "{_type: 'project'}");
                        Log.Debug("Get detectors from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var detectors = await global.webSocketClient.Query<Detector>("openrpa", "{_type: 'detector'}");
                        Log.Debug("Done getting workflows and projects " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        SetStatus("Initialize detecors");
                        foreach (var d in detectors)
                        {
                            IDetectorPlugin dp = null;
                            d.Path = Interfaces.Extensions.ProjectsDirectory;
                            dp = Plugins.AddDetector(this, d);
                            if (dp != null) dp.OnDetector += OnDetector;
                            if (dp == null) Log.Error("Detector not loaded!");
                        }
                        var folders = new List<string>();
                        foreach (var p in projects)
                        {
                            string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
                            var r = new System.Text.RegularExpressions.Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
                            p.name = r.Replace(p.name, "");

                            p.Path = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, p.name);
                            if (folders.Contains(p.Path))
                            {
                                p.Path = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, p._id);
                            }
                            folders.Add(p.Path);
                        }
                        SetStatus("Initialize projects and workflows");
                        foreach (var p in projects)
                        {
                            p.Path = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, p.name);
                            p.Workflows = new System.Collections.ObjectModel.ObservableCollection<Workflow>();
                            foreach (var workflow in workflows)
                            {
                                if (workflow.projectid == p._id)
                                {
                                    workflow.Project = p;
                                    p.Workflows.Add(workflow);
                                }
                            }
                            Log.Debug("Saving project " + p.name + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            p.SaveFile();
                            Projects.Add(p);
                        }
                        Project up = null;
                        foreach (var wf in workflows)
                        {
                            var hasProject = Projects.Where(x => x._id == wf.projectid && !string.IsNullOrEmpty(wf.projectid)).FirstOrDefault();
                            if (hasProject == null)
                            {
                                if (up == null) up = await Project.Create(Interfaces.Extensions.ProjectsDirectory, "Unknown", false);
                                wf.Project = up;
                                up.Workflows.Add(wf);
                            }
                        }
                        if (up != null) Projects.Add(up);
                        InputDriver.Instance.Initialize();
                        SetStatus("Run pending workflow instances");
                        Log.Debug("RunPendingInstances::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        foreach (var workflow in workflows)
                        {
                            if (workflow.Project != null)
                            {
                                await workflow.RunPendingInstances();
                            }

                        }
                        Log.Debug("RunPendingInstances::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show("WebSocketClient_OnOpen::Sync projects " + ex.Message);
                }
                Log.Debug("WebSocketClient_OnOpen::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                SetStatus("Load layout and reopen workflows");
                if (Projects.Count == 0)
                {
                    OnOpen(null);
                    string Name = "New Project";
                    try
                    {
                        Project project = await Project.Create(Interfaces.Extensions.ProjectsDirectory, Name, true);
                        Workflow workflow = project.Workflows.First();
                        workflow.Project = project;
                        Projects.Add(project);
                        OnOpenWorkflow(workflow);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
                LoadLayout();
                System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = true;
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
                SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                try
                {
                    Signedin?.Invoke(user);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                _ = Task.Run(async () =>
                {
                    try
                    {
                        SetStatus("Registering queue for robot");
                        Log.Debug("Registering queue for robot " + global.webSocketClient.user._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        await global.webSocketClient.RegisterQueue(global.webSocketClient.user._id);

                        foreach (var role in global.webSocketClient.user.roles)
                        {
                            var roles = await global.webSocketClient.Query<apirole>("users", "{_id: '" + role._id + "'}", top: 5000);
                            if (roles.Length == 1 && roles[0].rparole)
                            {
                                SetStatus("Registering queue for robot (" + role.name + ")");
                                Log.Debug("Registering queue for role " + role.name + " " + role._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                await global.webSocketClient.RegisterQueue(role._id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                    finally
                    {
                        SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                    }
                });
            }, null);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AllowQuite) return;
            App.notifyIcon.Visible = true;
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Hidden;
                App.notifyIcon.Visible = true;
            }
            else
            {
                Visibility = Visibility.Visible;
                App.notifyIcon.Visible = false;
            }
        }
        private async Task CheckForUpdatesAsync()
        {
            if (!Config.local.doupdatecheck) return;
            if ((DateTime.Now - Config.local.lastupdatecheck) < Config.local.updatecheckinterval) return;
            await Task.Run(async () =>
            {
                try
                {
                    if (Config.local.autoupdateupdater)
                    {
                        if (await updater.UpdaterNeedsUpdate() == true)
                        {
                            await updater.UpdateUpdater();
                        }
                    }
                    var releasenotes = await updater.OpenRPANeedsUpdate();
                    if (!string.IsNullOrEmpty(releasenotes))
                    {
                        var dialogResult = MessageBox.Show(releasenotes, "Update available", MessageBoxButton.YesNo);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            OnManagePackages(null);
                            Application.Current.Shutdown();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
            });
        }
        private void SetStatus(string message)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                LabelStatusBar.Content = message;
            }, null);
        }
        private void DManager_ActiveContentChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged("VisualTracking");
            NotifyPropertyChanged("SlowMotion");
            NotifyPropertyChanged("Minimize");
            NotifyPropertyChanged("SelectedContent");
            NotifyPropertyChanged("LastDesigner");
        }
        public object SelectedContent
        {
            get
            {
                var b = DManager.ActiveContent;
                return b;
            }
        }
        private Views.WFDesigner _LastDesigner;
        public Views.WFDesigner LastDesigner
        {
            get
            {
                if (Designer != null) _LastDesigner = Designer;
                if (SelectedContent is Views.OpenProject) _LastDesigner = null;
                if (SelectedContent is Views.DetectorsView) _LastDesigner = null;
                return _LastDesigner;
            }
        }
        public LayoutDocumentPane MainTabControl
        {
            get
            {
                var documentPane = DManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                return documentPane;
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        IDesigner IMainWindow.Designer { get => this.Designer; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public Views.WFDesigner Designer
        {
            get
            {
                if (SelectedContent is Views.WFDesigner view)
                {
                    return view;
                }
                return null;
            }
            set
            {

            }
        }
        public Views.WFDesigner[] Designers
        {
            get
            {
                var result = new List<Views.WFDesigner>();
                var las = DManager.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
                foreach (var dp in las)
                {
                    if (dp.Content is Views.WFDesigner view) result.Add(view);

                }
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.WFDesigner view) result.Add(view);
                }
                return result.ToArray();
            }
        }
        public bool Setting_record_overlay
        {
            get
            {
                return Config.local.record_overlay;
            }
            set
            {
                Config.local.record_overlay = value;
                NotifyPropertyChanged("record_overlay");
            }
        }
        public bool VisualTracking
        {
            get
            {
                if (Designer == null) return false;
                return Designer.VisualTracking;
            }
            set
            {
                if (Designer == null) return;
                Designer.VisualTracking = value;
                NotifyPropertyChanged("VisualTracking");
            }
        }
        public bool SlowMotion
        {
            get
            {
                if (Designer == null) return false;
                return Designer.SlowMotion;
            }
            set
            {
                if (Designer == null) return;
                Designer.SlowMotion = value;
                NotifyPropertyChanged("SlowMotion");
            }
        }
        public bool Minimize
        {
            get
            {
                return Config.local.minimize;
            }
            set
            {
                Config.local.minimize = value;
                NotifyPropertyChanged("Minimize");
            }
        }
        public bool UsingOpenFlow
        {
            get
            {
                return !string.IsNullOrEmpty(Config.local.wsurl);
            }
        }
        public bool IsConnected
        {
            get
            {
                if (!UsingOpenFlow) return true; // IF working offline, were allways connected, right ?
                if (global.webSocketClient == null) return false;
                return global.webSocketClient.isConnected;
            }
        }
        public bool Setting_log_debug
        {
            get
            {
                return Config.local.log_debug;
            }
            set
            {
                Config.local.log_debug = value;
                NotifyPropertyChanged("log_debug");
            }
        }
        public bool Setting_log_warning
        {
            get
            {
                return Config.local.log_warning;
            }
            set
            {
                Config.local.log_warning = value;
                NotifyPropertyChanged("log_warning");
            }
        }
        public bool Setting_log_verbose
        {
            get
            {
                return Config.local.log_verbose;
            }
            set
            {
                Config.local.log_verbose = value;
                NotifyPropertyChanged("log_verbose");
            }
        }
        public bool Setting_log_selector
        {
            get
            {
                return Config.local.log_selector;
            }
            set
            {
                Config.local.log_selector = value;
                NotifyPropertyChanged("log_selector");
            }
        }
        public bool Setting_log_selector_verbose
        {
            get
            {
                return Config.local.log_selector_verbose;
            }
            set
            {
                Config.local.log_selector_verbose = value;
                NotifyPropertyChanged("log_selector_verbose");
            }
        }
        public bool Setting_use_sendkeys
        {
            get
            {
                return Config.local.use_sendkeys;
            }
            set
            {
                Config.local.use_sendkeys = value;
                NotifyPropertyChanged("use_sendkeys");
            }
        }
        public bool Setting_use_virtual_click
        {
            get
            {
                return Config.local.use_virtual_click;
            }
            set
            {
                Config.local.use_virtual_click = value;
                NotifyPropertyChanged("use_virtual_click");
            }
        }
        public bool Setting_use_animate_mouse
        {
            get
            {
                return Config.local.use_animate_mouse;
            }
            set
            {
                Config.local.use_animate_mouse = value;
                NotifyPropertyChanged("use_animate_mouse");
            }
        }
        public string Setting_use_postwait
        {
            get
            {
                return Config.local.use_postwait.ToString();
            }
            set
            {
                if (TimeSpan.TryParse(value, out TimeSpan ts))
                {
                    Config.local.use_postwait = ts;
                    NotifyPropertyChanged("use_postwait");
                }
            }
        }
        public bool Setting_recording_add_to_designer
        {
            get
            {
                return Config.local.recording_add_to_designer;
            }
            set
            {
                Config.local.recording_add_to_designer = value;
                NotifyPropertyChanged("recording_add_to_designer");
            }
        }
        
        public ICommand LoggingOptionCommand { get { return new RelayCommand<object>(OnLoggingOptionCommand, CanAllways); } }
        public ICommand ExitAppCommand { get { return new RelayCommand<object>(OnExitApp, (e) => true); } }
        public ICommand SettingsCommand { get { return new RelayCommand<object>(OnSettings, CanSettings); } }
        public ICommand MinimizeCommand { get { return new RelayCommand<object>(OnMinimize, CanMinimize); } }
        public ICommand VisualTrackingCommand { get { return new RelayCommand<object>(OnVisualTracking, CanVisualTracking); } }
        public ICommand SlowMotionCommand { get { return new RelayCommand<object>(OnSlowMotion, CanSlowMotion); } }
        public ICommand SignoutCommand { get { return new RelayCommand<object>(OnSignout, CanSignout); } }
        public ICommand OpenCommand { get { return new RelayCommand<object>(OnOpen, CanOpen); } }
        public ICommand ManagePackagesCommand { get { return new RelayCommand<object>(OnManagePackages, CanManagePackages); } }
        public ICommand DetectorsCommand { get { return new RelayCommand<object>(OnDetectors, CanDetectors); } }
        public ICommand RunPluginsCommand { get { return new RelayCommand<object>(OnRunPlugins, CanRunPlugins); } }
        public ICommand RecorderPluginsCommand { get { return new RelayCommand<object>(OnRecorderPluginsCommand, CanRecorderPluginsCommand); } }
        public ICommand SaveCommand { get { return new RelayCommand<object>(OnSave, CanSave); } }
        public ICommand NewWorkflowCommand { get { return new RelayCommand<object>(OnNewWorkflow, CanNewWorkflow); } }
        public ICommand NewProjectCommand { get { return new RelayCommand<object>(OnNewProject, CanNewProject); } }
        public ICommand CopyCommand { get { return new RelayCommand<object>(OnCopy, CanCopy); } }
        public ICommand DeleteCommand { get { return new RelayCommand<object>(OnDelete, CanDelete); } }
        public ICommand PlayCommand { get { return new RelayCommand<object>(OnPlay, CanPlay); } }
        public ICommand StopCommand { get { return new RelayCommand<object>(OnStop, CanStop); } }
        public ICommand RecordCommand { get { return new RelayCommand<object>(OnRecord, CanRecord); } }
        public ICommand ImportCommand { get { return new RelayCommand<object>(OnImport, CanImport); } }
        public ICommand ExportCommand { get { return new RelayCommand<object>(OnExport, CanExport); } }
        public ICommand PermissionsCommand { get { return new RelayCommand<object>(OnPermissions, CanPermissions); } }
        public ICommand LinkOpenFlowCommand { get { return new RelayCommand<object>(OnlinkOpenFlow, CanlinkOpenFlow); } }
        public ICommand LinkNodeREDCommand { get { return new RelayCommand<object>(OnlinkNodeRED, CanlinkNodeRED); } }
        public ICommand OpenChromePageCommand { get { return new RelayCommand<object>(OnOpenChromePage, CanAllways); } }
        public ICommand OpenFirefoxPageCommand { get { return new RelayCommand<object>(OnOpenFirefoxPageCommand, CanAllways); } }
        public ICommand SwapSendKeysCommand { get { return new RelayCommand<object>(OnSwapSendKeys, CanSwapSendKeys); } }
        private bool CanSwapSendKeys(object _item)
        {
            try
            {
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                if (designer.SelectedActivity == null) return false;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void SwapSendKeys(Views.WFDesigner designer, System.Activities.Presentation.Model.ModelItem model)
        {
            if (model.ItemType == typeof(System.Activities.Statements.Assign<string>))
            {

                var To = model.GetValue<string>("To");
                // var Value = model.GetValue<string>("Value");
                if (!string.IsNullOrEmpty(To) && To.ToLower() == "item.value")
                {
                    var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                    using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                    {
                        model.Properties["To"].ComputedValue = new OutArgument<string>(new VisualBasicReference<string>("item.SendKeys"));
                        editingScope.Complete();
                    }
                }
                else if (!string.IsNullOrEmpty(To) && To.ToLower() == "item.sendkeys")
                {
                    var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                    using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                    {
                        model.Properties["To"].ComputedValue = new OutArgument<string>(new VisualBasicReference<string>("item.Value"));
                        editingScope.Complete();
                    }
                }
            }
            System.Activities.Presentation.Model.ModelItemCollection Activities = null;
            if (model.Attributes[typeof(System.Windows.Markup.ContentPropertyAttribute)] != null)
            {
                var a = model.Attributes[typeof(System.Windows.Markup.ContentPropertyAttribute)] as System.Windows.Markup.ContentPropertyAttribute;
                if (model.Properties[a.Name] != null)
                {
                    if (model.Properties[a.Name].Collection != null)
                    {
                        Activities = model.Properties[a.Name].Collection;
                    }
                    else if (model.Properties[a.Name].Value != null)
                    {
                        if (model.Properties[a.Name].Value is System.Activities.Presentation.Model.ModelItem _a) SwapSendKeys(designer, _a);
                    }

                }

            }
            //if (model.Properties["Activities"] != null)
            //{
            //    Activities = model.Properties["Activities"].Collection;
            //}
            //else if (model.Properties["Nodes"] != null)
            //{
            //    Activities = model.Properties["Nodes"].Collection;
            //}
            if (Activities != null)
            {
                foreach (var a in Activities)
                {
                    SwapSendKeys(designer, a);
                }
            }

        }
        private void OnSwapSendKeys(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
            if (designer.SelectedActivity == null) return;
            SwapSendKeys(designer, designer.SelectedActivity);


        }
        public ICommand SwapVirtualClickCommand { get { return new RelayCommand<object>(OnSwapVirtualClick, CanSwapVirtualClick); } }
        private bool CanSwapVirtualClick(object _item)
        {
            try
            {
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                if (designer.SelectedActivity == null) return false;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void SwapVirtualClick(Views.WFDesigner designer, System.Activities.Presentation.Model.ModelItem model)
        {
            if (model.ItemType == typeof(Activities.ClickElement))
            {

                var VirtualClick = model.GetValue<bool>("VirtualClick");
                if(VirtualClick)
                {
                    var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                    using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                    {
                        model.Properties["VirtualClick"].ComputedValue = new InArgument<bool>(){Expression = new VisualBasicValue<bool>("false")};
                        editingScope.Complete();
                    }
                }
                else
                {
                    var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                    using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                    {
                        model.Properties["VirtualClick"].ComputedValue = new InArgument<bool>() { Expression = new VisualBasicValue<bool>("true") };
                        editingScope.Complete();
                    }

                }
            }
            System.Activities.Presentation.Model.ModelItemCollection Activities = null;
            if (model.Attributes[typeof(System.Windows.Markup.ContentPropertyAttribute)] != null)
            {
                var a = model.Attributes[typeof(System.Windows.Markup.ContentPropertyAttribute)] as System.Windows.Markup.ContentPropertyAttribute;
                if (model.Properties[a.Name] != null)
                {
                    if (model.Properties[a.Name].Collection != null)
                    {
                        Activities = model.Properties[a.Name].Collection;
                    }
                    else if (model.Properties[a.Name].Value != null)
                    {
                        if (model.Properties[a.Name].Value is System.Activities.Presentation.Model.ModelItem _a) SwapVirtualClick(designer, _a);
                    }

                }

            }
            //if (model.Properties["Activities"] != null)
            //{
            //    Activities = model.Properties["Activities"].Collection;
            //}
            //else if (model.Properties["Nodes"] != null)
            //{
            //    Activities = model.Properties["Nodes"].Collection;
            //}
            if (Activities != null)
            {
                foreach (var a in Activities)
                {
                    SwapVirtualClick(designer, a);
                }
            }

        }
        private void OnSwapVirtualClick(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
            if (designer.SelectedActivity == null) return;
            SwapVirtualClick(designer, designer.SelectedActivity);


        }
        private void OnLoggingOptionCommand(object _item)
        {
            try
            {
                Config.Save();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanPermissions(object _item)
        {
            try
            {
                if (!IsConnected) return false;
                if (isRecording) return false;
                if (SelectedContent as Views.OpenProject != null)
                {
                    var val = (SelectedContent as Views.OpenProject).listWorkflows.SelectedValue;
                    if (val == null) return false;
                    var wf = (SelectedContent as Views.OpenProject).listWorkflows.SelectedValue as Workflow;
                    return true;
                }
                if (SelectedContent is Views.WFDesigner designer)
                {
                    return true;
                }
                var DetectorsView = SelectedContent as Views.DetectorsView;
                if (DetectorsView != null)
                {
                    if (!(DetectorsView.lidtDetectors.SelectedItem is IDetectorPlugin detector)) return false;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnPermissions(object _item)
        {
            apibase result = null;
            if (!IsConnected) return;
            if (isRecording) return;
            if (SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (val is Workflow wf) { result = wf; }
                if (val is Project p) { result = p; }
            }
            if (SelectedContent is Views.WFDesigner designer)
            {
                result = designer.Workflow;
            }
            var DetectorsView = SelectedContent as Views.DetectorsView;
            if (DetectorsView != null)
            {
                if (!(DetectorsView.lidtDetectors.SelectedItem is IDetectorPlugin detector)) return;
                result = detector.Entity;
            }
            List<ace> orgAcl = new List<ace>();
            try
            {
                result._acl.ForEach((a) => { orgAcl.Add(new ace(a)); });

                var pw = new Views.PermissionsWindow(result);
                Hide();
                pw.ShowDialog();
                if (result is Project)
                {
                    var p = result as Project;
                    foreach (var wf in p.Workflows)
                    {
                        wf._acl = p._acl;
                    }
                    await ((Project)result).Save();
                }
                if (result is Workflow) await ((Workflow)result).Save();
                if (result is Detector)
                {
                    var _result = await global.webSocketClient.UpdateOne("openrpa", 0, false, result);
                    result._acl = _result._acl;
                }
                // result.Save();
            }
            catch (Exception ex)
            {
                result._acl = orgAcl.ToArray();
                Log.Error(ex.ToString());
                System.Windows.MessageBox.Show("CmdTest: " + ex.Message);
            }
            finally
            {
                Show();
            }
        }
        private bool CanImport(object _item)
        {
            return true;
            //try
            //{
            //if (!isConnected) return false; return (SelectedContent is Views.WFDesigner || SelectedContent is Views.OpenProject || SelectedContent == null);
            //}
            //catch (Exception ex)
            //{
            //    Log.Error(ex.ToString());
            //    return false;
            //}
        }
        private async void OnImport(object _item)
        {
            try
            {
                Views.WFDesigner designer = SelectedContent as Views.WFDesigner;
                Views.OpenProject op = SelectedContent as Views.OpenProject;
                Workflow wf = null;
                Project p = null;
                string filename = null;
                if (SelectedContent is Views.OpenProject)
                {
                    wf = op.listWorkflows.SelectedItem as Workflow;
                    p = op.listWorkflows.SelectedItem as Project;
                    if (wf != null) p = wf.Project;
                }
                else if (SelectedContent is Views.WFDesigner)
                {
                    wf = designer.Workflow;
                    p = wf.Project;
                }
                var dialogOpen = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open Workflow",
                    Filter = "OpenRPA Project (.rpaproj)|*.rpaproj"
                };
                if (wf != null || p != null) dialogOpen.Filter = "Workflows (.xaml)|*.xaml|OpenRPA Project (.rpaproj)|*.rpaproj";
                if (dialogOpen.ShowDialog() == true) filename = dialogOpen.FileName;
                if (string.IsNullOrEmpty(filename)) return;
                if (System.IO.Path.GetExtension(filename) == ".xaml")
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(dialogOpen.FileName);
                    Workflow workflow = Workflow.Create(p, name);
                    workflow.Xaml = System.IO.File.ReadAllText(dialogOpen.FileName);
                    _onOpenWorkflow(workflow, true);
                    return;
                }
                if (System.IO.Path.GetExtension(filename) == ".rpaproj")
                {
                    Project project = Newtonsoft.Json.JsonConvert.DeserializeObject<Project>(System.IO.File.ReadAllText(filename));
                    var sourcepath = System.IO.Path.GetDirectoryName(filename);
                    var projectpath = Interfaces.Extensions.ProjectsDirectory + "\\" + project.name;
                    int index = 1;
                    string name = project.name;
                    while (System.IO.Directory.Exists(projectpath))
                    {
                        index++;
                        name = project.name + index.ToString();
                        projectpath = Interfaces.Extensions.ProjectsDirectory + "\\" + name;
                    }
                    System.IO.Directory.CreateDirectory(projectpath);
                    System.IO.File.Copy(filename, System.IO.Path.Combine(projectpath, name + ".rpaproj"));
                    var ProjectFiles = System.IO.Directory.EnumerateFiles(sourcepath, "*.xaml", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
                    foreach (string file in ProjectFiles) System.IO.File.Copy(file, System.IO.Path.Combine(projectpath, System.IO.Path.GetFileName(file)));
                    if (ProjectFiles.Length == 0)
                    {
                        Log.Information("Loading empty projects are not supported");
                        return;
                    }
                    project = Project.FromFile(System.IO.Path.Combine(projectpath, name + ".rpaproj"));
                    Projects.Add(project);
                    project.name = name;
                    project._id = null;
                    await project.Save();
                    Workflow workflow = project.Workflows.First();
                    workflow.Project = project;
                    OnOpenWorkflow(workflow);
                    return;
                }


                //if (SelectedContent is Views.WFDesigner)
                //{
                //    var designer = (Views.WFDesigner)SelectedContent;
                //    Workflow workflow = Workflow.Create(designer.Project, "New Workflow");
                //    onOpenWorkflow(workflow);
                //    return;
                //}
                //else
                //{
                //    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                //    {
                //        System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                //        if(result == System.Windows.Forms.DialogResult.OK)
                //        {
                //            //var _Projects = Project.loadProjects(Extensions.projectsDirectory);
                //            //if (_Projects.Count() > 0)
                //            //{
                //            //    var ProjectFiles = System.IO.Directory.EnumerateFiles(dialog.SelectedPath, "*.rpaproj", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
                //            //    foreach(var file in ProjectFiles)
                //            //    {
                //            //        if()
                //            //    }

                //            //}

                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanExport(object _item)
        {
            try
            {
                if (!IsConnected) return false;
                return (SelectedContent is Views.WFDesigner || SelectedContent is Views.OpenProject || SelectedContent == null);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnExport(object _item)
        {
            if (SelectedContent is Views.WFDesigner designer)
            {
                designer.WorkflowDesigner.Flush();
                string beforexaml = designer.WorkflowDesigner.Text;
                string xaml = await Views.WFDesigner.LoadImages(beforexaml);
                var dialogSave = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Workflow",
                    Filter = "Workflows (.xaml)|*.xaml",
                    FileName = designer.Workflow.name + ".xaml"
                };
                if (dialogSave.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialogSave.FileName, xaml);
                }
                return;
            }
            if (SelectedContent is Views.OpenProject op)
            {
                if (op.listWorkflows.SelectedItem is Project p)
                {
                    using (var openFileDialog1 = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                        var path = openFileDialog1.SelectedPath;
                        p.SaveFile(path, true);
                    }
                }
                if (op.listWorkflows.SelectedItem is Workflow wf)
                {
                    string beforexaml = wf.Xaml;
                    string xaml = await Views.WFDesigner.LoadImages(beforexaml);
                    var dialogSave = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = "Save Workflow",
                        Filter = "Workflows (.xaml)|*.xaml",
                        FileName = wf.name + ".xaml"
                    };
                    if (dialogSave.ShowDialog() == true)
                    {
                        System.IO.File.WriteAllText(dialogSave.FileName, xaml);
                    }
                }
                return;
            }
        }
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log.Error(e.Exception, "");
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = (Exception)args.ExceptionObject;
            Log.Error(ex, "");
            Log.Error("MyHandler caught : " + ex.Message);
            Log.Error("Runtime terminating: {0}", (args.IsTerminating).ToString());
        }
        private void AddHotKeys()
        {
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    RoutedCommand saveHotkey = new RoutedCommand();
                    saveHotkey.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
                    CommandBindings.Add(new CommandBinding(saveHotkey, OnSave));
                    RoutedCommand deleteHotkey = new RoutedCommand();
                    deleteHotkey.InputGestures.Add(new KeyGesture(Key.Delete));
                    CommandBindings.Add(new CommandBinding(deleteHotkey, OnDelete));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show(ex.Message);
                }
            }, null);
        }
        private void OnExitApp(object _item)
        {
            AllowQuite = true;
            Close();
        }
        private void OnSave(object sender, ExecutedRoutedEventArgs e)
        {
            SaveCommand.Execute(SelectedContent);
        }
        private void OnDelete(object sender, ExecutedRoutedEventArgs e)
        {
            DeleteCommand.Execute(SelectedContent);
        }
        private bool CanMinimize(object _item)
        {
            return true;
        }
        private void OnMinimize(object _item)
        {
        }
        private bool CanVisualTracking(object _item)
        {
            return true;
        }
        private void OnVisualTracking(object _item)
        {
            var b = (bool)_item;
            if (SelectedContent is Views.WFDesigner)
            {
                var designer = SelectedContent as Views.WFDesigner;
                designer.VisualTracking = b;
            }
        }
        private bool CanSlowMotion(object _item)
        {
            return true;
        }
        private void OnSlowMotion(object _item)
        {
            var b = (bool)_item;
            if (SelectedContent is Views.WFDesigner)
            {
                var designer = SelectedContent as Views.WFDesigner;
                designer.SlowMotion = b;
            }
        }
        private bool CanSettings(object _item)
        {
            return true;
        }
        private void OnSettings(object _item)
        {
            try
            {
                var filename = "settings.json";
                var path = Interfaces.Extensions.ProjectsDirectory;
                string settingsFile = System.IO.Path.Combine(path, filename);
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        UseShellExecute = true,
                        FileName = settingsFile
                    }
                };
                process.Start();
            }
            catch (Exception ex)
            {
                Log.Error("onSettings: " + ex.Message);
            }
        }
        private bool CanSignout(object _item)
        {
            try
            {

                if (!global.isConnected) return false;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

        }
        private void OnSignout(object _item)
        {
            try
            {
                autoReconnect = true;
                Projects.Clear();
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.WFDesigner view) document.Close();
                }

                Config.Reload();
                Config.local.password = new byte[] { };
                Config.local.jwt = new byte[] { };
                global.webSocketClient.url = Config.local.wsurl;
                _ = global.webSocketClient.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanManagePackages(object _item)
        {
            try
            {

                var hits = System.Diagnostics.Process.GetProcessesByName("OpenRPA.Updater");
                return hits.Count() == 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
            //var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
            //foreach (var document in ld)
            //{
            //    if (document.Content is Views.ManagePackages op) return false;
            //}
            //return true;
        }
        private void OnManagePackages(object _item)
        {
            var di = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
            var path = "";
            var filename = "";
            if (System.IO.File.Exists(System.IO.Path.Combine(di.FullName, "Updater", "OpenRPA.Updater.exe")))
            {
                path = System.IO.Path.Combine(di.FullName, "Updater");
                filename = System.IO.Path.Combine(path, "OpenRPA.Updater.exe");
            }
            else if (System.IO.File.Exists(System.IO.Path.Combine(di.Parent.FullName, "OpenRPA.Updater.exe")))
            {
                path = di.Parent.FullName;
                filename = System.IO.Path.Combine(path, "OpenRPA.Updater.exe");
            }
            else if (System.IO.File.Exists(System.IO.Path.Combine(di.FullName, "OpenRPA.Updater.exe")))
            {
                path = di.FullName;
                filename = System.IO.Path.Combine(path, "OpenRPA.Updater.exe");
            }
            if (string.IsNullOrEmpty(filename))
            {
                MessageBox.Show("OpenRPA.Updater.exe not found");
                return;
            }
            try
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo.FileName = filename;
                p.StartInfo.WorkingDirectory = path;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Verb = "runas";
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanOpen(object _item)
        {
            try
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.OpenProject op) return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private bool CanDetectors(object _item)
        {
            try
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.DetectorsView op) return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private bool CanRunPlugins(object _item)
        {
            try
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.RunPlugins op) return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private bool CanRecorderPluginsCommand(object _item)
        {
            try
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.RecorderPlugins op) return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void OnOpen(object _item)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                    foreach (var document in ld)
                    {
                        if (document.Content is Views.OpenProject op) { document.IsSelected = true; return; }
                    }
                    var view = new Views.OpenProject(this);
                    view.onOpenProject += OnOpenProject;
                    view.onOpenWorkflow += OnOpenWorkflow;

                    LayoutDocument layoutDocument = new LayoutDocument { Title = "Open project" };
                    layoutDocument.ContentId = "openproject";
                    layoutDocument.CanClose = false;
                    layoutDocument.Content = view;
                    MainTabControl.Children.Add(layoutDocument);
                    layoutDocument.IsSelected = true;
                    layoutDocument.Closing += LayoutDocument_Closing;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show(ex.Message);
                }
            }, null);
        }
        private void OnDetectors(object _item)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.DetectorsView op) { document.IsSelected = true; return; }
                }
                var view = new Views.DetectorsView(this);
                LayoutDocument layoutDocument = new LayoutDocument { Title = "Detectors" };
                layoutDocument.ContentId = "detectors";
                layoutDocument.Content = view;
                MainTabControl.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
            }, null);
        }
        private void OnRunPlugins(object _item)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.RunPlugins op) { document.IsSelected = true; return; }
                }
                var view = new Views.RunPlugins();
                LayoutDocument layoutDocument = new LayoutDocument { Title = "Run Plugins" };
                layoutDocument.ContentId = "detectors";
                layoutDocument.Content = view;
                MainTabControl.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
            }, null);
        }
        private void OnRecorderPluginsCommand(object _item)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.RecorderPlugins op) { document.IsSelected = true; return; }
                }
                var view = new Views.RecorderPlugins();
                LayoutDocument layoutDocument = new LayoutDocument { Title = "Recorder Plugins" };
                layoutDocument.ContentId = "detectors";
                layoutDocument.Content = view;
                MainTabControl.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
            }, null);
        }
        private bool CanlinkOpenFlow(object _item)
        {
            try
            {

                if (string.IsNullOrEmpty(Config.local.wsurl)) return false;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void OnlinkOpenFlow(object _item)
        {
            if (string.IsNullOrEmpty(Config.local.wsurl)) return;
            if (global.openflowconfig == null) return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(global.openflowconfig.baseurl));
        }
        private bool CanlinkNodeRED(object _item)
        {
            try
            {
                if (!IsConnected) return false;
                if (string.IsNullOrEmpty(Config.local.wsurl)) return false;
                if (global.openflowconfig == null) return false;
                if (global.openflowconfig.allow_personal_nodered) return true;

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void OnlinkNodeRED(object _item)
        {
            if (global.openflowconfig == null) return;
            var baseurl = new Uri(Config.local.wsurl);
            var username = global.webSocketClient.user.username.Replace("@", "").Replace(".", "");
            var url = global.openflowconfig.nodered_domain_schema.Replace("$nodered_id$", username);
            if (baseurl.Scheme == "wss") { url = "https://" + url; } else { url = "http://" + url; }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url));
        }
        private void LayoutDocument_Closing(object sender, CancelEventArgs e)
        {
            var tab = sender as LayoutDocument;
            if (!(tab.Content is Views.WFDesigner designer)) return;
            if (!designer.HasChanged) return;

            if (designer.HasChanged && (global.isConnected ? designer.Workflow.hasRight(global.webSocketClient.user, ace_right.update) : true))
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Save " + designer.Workflow.name + " ?", "Workflow unsaved", MessageBoxButton.YesNoCancel);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    _ = designer.Save();
                }
                else if (messageBoxResult != MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
        private void SaveLayout()
        {
            var workflows = new List<string>();
            foreach (var designer in Designers)
            {
                if (string.IsNullOrEmpty(designer.Workflow._id) && !string.IsNullOrEmpty(designer.Workflow.Filename))
                {
                    workflows.Add(designer.Workflow.RelativeFilename);
                }
                else if (!string.IsNullOrEmpty(designer.Workflow._id))
                {
                    workflows.Add(designer.Workflow._id);

                }
            }
            Config.local.openworkflows = workflows.ToArray();
            try
            {
                var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                using (var stream = new System.IO.StreamWriter(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layout.config")))
                    serializer.Serialize(stream);
            }
            catch (Exception)
            {

                throw;
            }
            Config.Save();
        }
        private void LoadLayout()
        {
            GenericTools.RunUI(() =>
            {
                try
                {
                    var fi = new System.IO.FileInfo("layout.config");
                    var di = fi.Directory;

                    if (System.IO.File.Exists(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layout.config")))
                    {
                        var ds = DManager.Layout.Descendents();
                        var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                        using (var stream = new System.IO.StreamReader(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layout.config")))
                            serializer.Deserialize(stream);
                        ds = DManager.Layout.Descendents();
                    }
                    else if (System.IO.File.Exists("layout.config"))
                    {
                        var ds = DManager.Layout.Descendents();
                        var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                        using (var stream = new System.IO.StreamReader("layout.config"))
                            serializer.Deserialize(stream);
                        ds = DManager.Layout.Descendents();
                    }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(di.Parent.FullName, "layout.config")))
                    {
                        var ds = DManager.Layout.Descendents();
                        var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                        using (var stream = new System.IO.StreamReader(System.IO.Path.Combine(di.Parent.FullName, "layout.config")))
                            serializer.Deserialize(stream);
                        ds = DManager.Layout.Descendents();
                    }
                    else
                    {
                        var las = DManager.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
                        foreach (var dp in las)
                        {
                            if (dp.Title == "Toolbox")
                            {
                                if (dp.IsAutoHidden) { dp.ToggleAutoHide(); }
                            }
                            if (dp.Title == "Properties")
                            {
                                if (dp.IsAutoHidden) { dp.ToggleAutoHide(); }
                            }
                            if (dp.Title == "Snippets")
                            {
                                if (dp.IsAutoHidden) { dp.ToggleAutoHide(); }
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            try
            {
                foreach (var p in Projects)
                {
                    foreach (var wf in p.Workflows)
                    {

                        if (Config.local.openworkflows.Contains(wf._id) && !string.IsNullOrEmpty(wf._id))
                        {
                            OnOpenWorkflow(wf);
                        }
                        else if (Config.local.openworkflows.Contains(wf.RelativeFilename) && !string.IsNullOrEmpty(wf.RelativeFilename))
                        {
                            OnOpenWorkflow(wf);
                        }
                    }
                }
                if (Snippets != null) Snippets.Reload();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public void _onOpenWorkflow(Workflow workflow, bool HasChanged = false)
        {
            if (GetWorkflowDesignerByIDOrRelativeFilename(workflow.IDOrRelativeFilename) is Views.WFDesigner designer)
            {
                designer.tab.IsSelected = true;
                return;
            }
            try
            {
                var types = new List<Type>();
                foreach (var p in Plugins.recordPlugins) { types.Add(p.GetType()); }
                LayoutDocument layoutDocument = new LayoutDocument { Title = workflow.name };
                layoutDocument.ContentId = workflow._id;
                Views.WFDesigner view = new Views.WFDesigner(layoutDocument, workflow, types.ToArray())
                {
                    OnChanged = WFDesigneronChanged
                };
                layoutDocument.Content = view;
                MainTabControl.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
                layoutDocument.Closing += LayoutDocument_Closing;
                if (HasChanged) view.SetHasChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        public void OnOpenWorkflow(Workflow workflow)
        {
            GenericTools.RunUI(() =>
            {
                if (workflow.Project != null)
                {
                    workflow.Project.IsExpanded = true;
                }
                _onOpenWorkflow(workflow);
            });
        }
        private void WFDesigneronChanged(Views.WFDesigner designer)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                designer.tab.Title = (designer.HasChanged ? designer.Workflow.name + "*" : designer.Workflow.name);
                CommandManager.InvalidateRequerySuggested();
            }, null);
            //_syncContext.Post(o => CommandManager.InvalidateRequerySuggested(), null);
        }
        public void OnOpenProject(Project project)
        {
            foreach (var wf in project.Workflows)
            {
                OnOpenWorkflow(wf);
            }
        }
        private bool CanSave(object _item)
        {
            try
            {
                if (!(SelectedContent is Views.WFDesigner wf)) return false;
                if (wf.IsRunnning == true) return false;
                return wf.HasChanged;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnSave(object _item)
        {
            try
            {
                if (SelectedContent is Views.WFDesigner designer)
                {
                    await designer.Save();
                }
                if (SelectedContent is Views.OpenProject view)
                {
                    var Project = view.listWorkflows.SelectedItem as Project;
                    if (Project != null)
                    {
                        await Project.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanNewWorkflow(object _item)
        {
            try
            {
                if (SelectedContent is Views.WFDesigner) return true;
                if (SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    var wf = val as Workflow;
                    var p = val as Project;
                    if (wf != null || p != null) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void OnNewWorkflow(object _item)
        {
            try
            {
                if (SelectedContent is Views.WFDesigner designer)
                {
                    Workflow workflow = Workflow.Create(designer.Project, "New Workflow");
                    OnOpenWorkflow(workflow);
                    return;
                }
                if (!(SelectedContent is Views.OpenProject view)) return;
                var val = view.listWorkflows.SelectedValue;
                if (val is Workflow wf)
                {
                    Workflow workflow = Workflow.Create(wf.Project, "New Workflow");
                    OnOpenWorkflow(workflow);
                    return;
                }
                if (val is Project p)
                {
                    Workflow workflow = Workflow.Create(p, "New Workflow");
                    OnOpenWorkflow(workflow);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanNewProject(object _item)
        {
            try
            {

                if (!IsConnected) return false; return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnNewProject(object _item)
        {
            try
            {
                string Name = Microsoft.VisualBasic.Interaction.InputBox("Name?", "Name project", "New project");
                if (string.IsNullOrEmpty(Name)) return;
                //string Name = "New project";
                Project project = await Project.Create(Interfaces.Extensions.ProjectsDirectory, Name, true);
                Workflow workflow = project.Workflows.First();
                workflow.Project = project;
                Projects.Add(project);
                OnOpenWorkflow(workflow);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanCopy(object _item)
        {
            try
            {
                return (SelectedContent is Views.WFDesigner);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnCopy(object _item)
        {
            try
            {
                var designer = (Views.WFDesigner)SelectedContent;
                await designer.Save();
                Workflow workflow = Workflow.Create(designer.Project, "Copy of " + designer.Workflow.name);
                var xaml = designer.Workflow.Xaml;
                xaml = Views.WFDesigner.SetWorkflowName(xaml, workflow.name);
                workflow.Xaml = xaml;
                workflow.name = "Copy of " + designer.Workflow.name;
                _onOpenWorkflow(workflow, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanDelete(object _item)
        {
            try
            {
                if (!(SelectedContent is Views.OpenProject view)) return false;
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return false;
                if (global.isConnected)
                {
                    if (val is Workflow wf)
                    {
                        if (!wf.hasRight(global.webSocketClient.user, ace_right.delete)) return false;
                        return !wf.isRunnning;
                    }
                    if (val is Project p)
                    {
                        return p.hasRight(global.webSocketClient.user, ace_right.delete);
                    }
                }
                // don't know what your deleteing, lets just assume yes then
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnDelete(object _item)
        {
            try
            {
                if (!(SelectedContent is Views.OpenProject view)) return;
                var val = view.listWorkflows.SelectedValue;
                if (val is Workflow wf)
                {
                    if (GetWorkflowDesignerByIDOrRelativeFilename(wf.IDOrRelativeFilename) is Views.WFDesigner designer) { designer.tab.Close(); }
                    var messageBoxResult = MessageBox.Show("Delete " + wf.name + " ?", "Delete Confirmation", MessageBoxButton.YesNo);
                    if (messageBoxResult != MessageBoxResult.Yes) return;
                    await wf.Delete();
                }
                if (val is Project p)
                {
                    if (p.Workflows.Count > 0)
                    {
                        var messageBoxResult = MessageBox.Show("Delete project " + p.name + " containing " + p.Workflows.Count() + " workflows", "Delete Confirmation", MessageBoxButton.YesNo);
                        if (messageBoxResult != MessageBoxResult.Yes) return;
                        foreach (var _wf in p.Workflows.ToList())
                        {
                            var designer = GetWorkflowDesignerByIDOrRelativeFilename(_wf.IDOrRelativeFilename) as Views.WFDesigner;
                            if (designer == null && !string.IsNullOrEmpty(_wf._id)) { }
                            if (designer != null) { designer.tab.Close(); }
                            await _wf.Delete();
                        }
                    }
                    await p.Delete();
                    Projects.Remove(p);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanPlay(object _item)
        {
            try
            {
                if (SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (!(view.listWorkflows.SelectedValue is Workflow wf)) return false;
                    if (wf.State == "running") return false;
                    if (global.isConnected)
                    {
                        return wf.hasRight(global.webSocketClient.user, ace_right.invoke);
                    }
                    return true;
                }

                if (!IsConnected) return false;
                if (isRecording) return false;
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                if (designer.BreakPointhit) return true;
                foreach (var i in designer.Workflow.Instances)
                {
                    if (i.isCompleted == false)
                    {
                        return false;
                    }
                }
                if (global.webSocketClient == null) return true;
                return designer.Workflow.hasRight(global.webSocketClient.user, ace_right.invoke);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnPlay(object _item)
        {
            if (SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (!(view.listWorkflows.SelectedValue is Workflow workflow)) return;
                try
                {
                    if (this.Minimize) GenericTools.Minimize(GenericTools.MainWindow);
                    IWorkflowInstance instance;
                    var param = new Dictionary<string, object>();
                    if (GetWorkflowDesignerByIDOrRelativeFilename(workflow.IDOrRelativeFilename) is Views.WFDesigner designer)
                    {
                        designer.BreakpointLocations = null;
                        //if (Config.local.minimize)
                        //{
                        //    instance = workflow.CreateInstance(param, null, null, new idleOrComplete(designer.OnIdle), null);
                        //}
                        //else
                        //{
                        //    instance = workflow.CreateInstance(param, null, null, new idleOrComplete(designer.OnIdle), designer.OnVisualTracking);
                        //}
                        instance = workflow.CreateInstance(param, null, null, new idleOrComplete(designer.OnIdle), designer.OnVisualTracking);
                        designer.Run(VisualTracking, SlowMotion, instance);
                    }
                    else
                    {
                        instance = workflow.CreateInstance(param, null, null, IdleOrComplete, null);
                        instance.Run();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return;
            }
            try
            {
                if (!(SelectedContent is Views.WFDesigner)) return;
                var designer = (Views.WFDesigner)SelectedContent;
                if (designer.HasChanged) { await designer.Save(); }
                designer.Run(VisualTracking, SlowMotion, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("onPlay " + ex.Message);
            }
        }
        private bool CanStop(object _item)
        {
            try
            {
                if (SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (!(view.listWorkflows.SelectedValue is Workflow wf)) return false;
                    if (wf.State == "running") return true;
                    return false;
                }
                if (!IsConnected) return false;
                if (isRecording) return true;
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                foreach (var i in designer.Workflow.Instances)
                {
                    if (i.isCompleted != true && i.state != "loaded")
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }

        }
        private void OnStop(object _item)
        {
            try
            {
                if (SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return;
                    if (!(view.listWorkflows.SelectedValue is Workflow wf)) return;
                    foreach (var i in wf.Instances)
                    {
                        if (i.isCompleted == false)
                        {
                            i.Abort("User clicked stop");
                        }
                    }
                    return;
                }

                if (!(SelectedContent is Views.WFDesigner)) return;
                var designer = (Views.WFDesigner)SelectedContent;
                foreach (var i in designer.Workflow.Instances)
                {
                    if (i.isCompleted == false)
                    {
                        i.Abort("User clicked stop");
                    }
                }
                if (designer.ResumeRuntimeFromHost != null) designer.ResumeRuntimeFromHost.Set();
                if (isRecording)
                {
                    StartDetectorPlugins();
                    StopRecordPlugins();
                    designer.ReadOnly = false;
                    InputDriver.Instance.CallNext = true;
                    InputDriver.Instance.OnKeyDown -= OnKeyDown;
                    InputDriver.Instance.OnKeyUp -= OnKeyUp;
                    GenericTools.Restore(GenericTools.MainWindow);
                    designer.EndRecording();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool CanRecord(object _item)
        {
            try
            {
                if (!IsConnected) return false;
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                foreach (var i in designer.Workflow.Instances)
                {
                    if (i.isCompleted == false)
                    {
                        return false;
                    }
                }
                if(designer.IsSequenceSelected) return !isRecording;
                return false;

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void OnCancel()
        {
            if (!isRecording) return;
            StartDetectorPlugins();
            StopRecordPlugins();
            if (SelectedContent is Views.WFDesigner view)
            {
                view.ReadOnly = false;
                view.EndRecording();
            }
            InputDriver.Instance.CallNext = true;
            InputDriver.Instance.OnKeyDown -= OnKeyDown;
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
            GenericTools.Restore(GenericTools.MainWindow);
            CommandManager.InvalidateRequerySuggested();
        }
        private bool CanAllways(object _item)
        {
            return true;
        }
        private void OnOpenChromePage(object _item)
        {
            System.Diagnostics.Process.Start("chrome.exe", "https://chrome.google.com/webstore/detail/openrpa/hpnihnhlcnfejboocnckgchjdofeaphe");
        }
        private void OnOpenFirefoxPageCommand(object _item)
        {
            System.Diagnostics.Process.Start("firefox.exe", "https://addons.mozilla.org/en-US/firefox/addon/openrpa/");
        }
        private void OnKeyDown(Input.InputEventArgs e)
        {
            if (!isRecording) return;
            // if (e.Key == KeyboardKey. 255) return;
            try
            {
                var cancelkey = InputDriver.Instance.cancelKeys.Where(x => x.KeyValue == e.KeyValue).ToList();
                if (cancelkey.Count > 0) return;
                if (SelectedContent is Views.WFDesigner view)
                {
                    view.ReadOnly = false;
                    if (view.Lastinserted != null && view.Lastinserted is Activities.TypeText)
                    {
                        Log.Debug("re-use existing TypeText");
                        var item = (Activities.TypeText)view.Lastinserted;
                        item.AddKey(new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, false), view.Lastinsertedmodel);
                    }
                    else
                    {
                        Log.Debug("Add new TypeText");
                        var rme = new Activities.TypeText();
                        view.Lastinsertedmodel = view.AddRecordingActivity(rme);
                        rme.AddKey(new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, false), view.Lastinsertedmodel);
                        view.Lastinserted = rme;
                    }
                    view.ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

        }
        private void OnKeyUp(Input.InputEventArgs e)
        {
            if (!isRecording) return;
            // if (e.KeyValue == 255) return;
            try
            {
                if (SelectedContent is Views.WFDesigner view)
                {
                    if (view.Lastinserted != null && view.Lastinserted is Activities.TypeText)
                    {
                        Log.Debug("re-use existing TypeText");
                        view.ReadOnly = false;
                        var item = (Activities.TypeText)view.Lastinserted;
                        item.AddKey(new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, true), view.Lastinsertedmodel);
                        view.ReadOnly = true;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void StartDetectorPlugins()
        {
            foreach (var detector in Plugins.detectorPlugins) detector.Start();
        }
        private void StopDetectorPlugins()
        {
            foreach (var detector in Plugins.detectorPlugins) detector.Stop();
        }
        Interfaces.Overlay.OverlayWindow _overlayWindow = null;
        private void StartRecordPlugins()
        {
            isRecording = true;
            var p = Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            p.OnUserAction += OnUserAction;
            if (Config.local.record_overlay) p.OnMouseMove += OnMouseMove;
            p.Start();
            if (_overlayWindow == null && Config.local.record_overlay)
            {
                _overlayWindow = new Interfaces.Overlay.OverlayWindow(true)
                {
                    BackColor = System.Drawing.Color.PaleGreen,
                    Visible = true,
                    TopMost = true
                };
            }
        }
        private void StopRecordPlugins()
        {
            isRecording = false;
            var p = Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            p.OnUserAction -= OnUserAction;
            if (Config.local.record_overlay) p.OnMouseMove -= OnMouseMove;
            p.Stop();
            if (_overlayWindow != null)
            {
                GenericTools.RunUI(_overlayWindow, () =>
                {
                    _overlayWindow.Visible = true;
                    _overlayWindow.Dispose();
                });
            }
            _overlayWindow = null;
        }
        public void OnMouseMove(IRecordPlugin sender, IRecordEvent e)
        {
            if (!Config.local.record_overlay) return;
            foreach (var p in Plugins.recordPlugins)
            {
                if (p.Name != sender.Name)
                {
                    if (p.ParseMouseMoveAction(ref e)) continue;
                }
            }

            // e.Element.Highlight(false, System.Drawing.Color.PaleGreen, TimeSpan.FromSeconds(1));
            if (e.Element != null && _overlayWindow != null)
            {

                GenericTools.RunUI(_overlayWindow, () =>
                {
                    try
                    {
                        _overlayWindow.Visible = true;
                        _overlayWindow.Bounds = e.Element.Rectangle;
                    }
                    catch (Exception)
                    {
                    }
                });
            }
            else if (_overlayWindow != null)
            {
                GenericTools.RunUI(_overlayWindow, () =>
                {
                    try
                    {
                        _overlayWindow.Visible = false;
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }
        public void OnUserAction(IRecordPlugin sender, IRecordEvent e)
        {
            StopRecordPlugins();
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    // TODO: Add priotrity, we could create an ordered list in config ?
                    foreach (var p in Plugins.recordPlugins)
                    {
                        if (p.Name != sender.Name)
                        {
                            try
                            {
                                if (p.ParseUserAction(ref e)) continue;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        }
                    }
                    if (e.a == null)
                    {
                        StartRecordPlugins();
                        if (e.ClickHandled == false)
                        {
                            NativeMethods.SetCursorPos(e.X, e.Y);
                            InputDriver.Click(e.Button);
                        }
                        return;
                    }
                    if (SelectedContent is Views.WFDesigner view)
                    {

                        var VirtualClick = Config.local.use_virtual_click;
                        if (!e.SupportVirtualClick) VirtualClick = false;
                        e.a.AddActivity(new Activities.ClickElement
                        {
                            Element = new System.Activities.InArgument<IElement>()
                            {
                                Expression = new Microsoft.VisualBasic.Activities.VisualBasicValue<IElement>("item")
                            },
                            OffsetX = e.OffsetX,
                            OffsetY = e.OffsetY,
                            Button = (int)e.Button,
                            VirtualClick = VirtualClick,
                            AnimateMouse = Config.local.use_animate_mouse
                        }, "item");
                        if (e.SupportInput)
                        {
                            var win = new Views.InsertText
                            {
                                Topmost = true
                            };
                            isRecording = false;
                            if (win.ShowDialog() == true)
                            {
                                e.a.AddInput(win.Text, e.Element);
                            }
                            else { e.SupportInput = false; }
                            isRecording = true;
                        } 
                        else if (e.SupportSelect)
                        {
                            var win = new Views.InsertSelect(e.Element)
                            {
                                Topmost = true
                            };
                            isRecording = false;
                            InputDriver.Instance.CallNext = true;
                            if (win.ShowDialog() == true)
                            {
                                e.ClickHandled = true;
                                e.a.AddInput(win.SelectedItem.Name, e.Element);
                            }
                            else { 
                                e.SupportSelect = false; 
                            }
                            InputDriver.Instance.CallNext = false;
                            isRecording = true;
                        }
                        view.ReadOnly = false;
                        view.Lastinserted = e.a.Activity;
                        view.Lastinsertedmodel = view.AddRecordingActivity(e.a.Activity);
                        view.ReadOnly = true;
                        if (e.ClickHandled == false && e.SupportInput == false)
                        {
                            NativeMethods.SetCursorPos(e.X, e.Y);
                            InputDriver.Click(e.Button);
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                    StartRecordPlugins();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    this.Show();
                    Log.Error(ex.ToString());
                }
            }, null);
        }
        private void OnRecord(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
            InputDriver.Instance.Initialize();
            designer.ReadOnly = true;
            designer.Lastinserted = null;
            designer.Lastinsertedmodel = null;
            StopDetectorPlugins();
            InputDriver.Instance.OnKeyDown += OnKeyDown;
            InputDriver.Instance.OnKeyUp += OnKeyUp;
            StartRecordPlugins();
            InputDriver.Instance.CallNext = false;
            if (this.Minimize) GenericTools.Minimize(GenericTools.MainWindow);
        }
        private async void WebSocketClient_OnClose(string reason)
        {
            Log.Information("Disconnected " + reason);
            SetStatus("Disconnected from " + Config.local.wsurl + " reason " + reason);
            try
            {
                Disconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            await Task.Delay(1000);
            if (autoReconnect)
            {
                autoReconnect = false;
                global.webSocketClient.OnOpen -= WebSocketClient_OnOpen;
                global.webSocketClient.OnClose -= WebSocketClient_OnClose;
                global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
                global.webSocketClient = null;

                global.webSocketClient = new WebSocketClient(Config.local.wsurl);
                global.webSocketClient.OnOpen += WebSocketClient_OnOpen;
                global.webSocketClient.OnClose += WebSocketClient_OnClose;
                global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                SetStatus("Connecting to " + Config.local.wsurl);

                await global.webSocketClient.Connect();
                autoReconnect = true;
            }
        }
        internal void OnDetector(IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e)
        {
            try
            {
                Log.Information("Detector " + plugin.Entity.name + " was triggered, with id " + plugin.Entity._id);
                foreach (var wi in WorkflowInstance.Instances)
                {
                    if (wi.isCompleted) continue;
                    if (wi.Bookmarks != null)
                    {
                        foreach (var b in wi.Bookmarks)
                        {
                            var _id = (plugin.Entity as Detector)._id;
                            Log.Debug(b.Key + " -> " + "detector_" + _id);
                            if (b.Key == "detector_" + _id)
                            {
                                wi.ResumeBookmark(b.Key, detector);
                            }
                        }
                    }
                }
                if (!global.isConnected) return;
                Interfaces.mq.RobotCommand command = new Interfaces.mq.RobotCommand();
                detector.user = global.webSocketClient.user;
                var data = JObject.FromObject(detector);
                var Entity = (plugin.Entity as Detector);
                command.command = "detector";
                command.detectorid = Entity._id;
                if (string.IsNullOrEmpty(Entity._id)) return;
                command.data = data;
                _ = global.webSocketClient.QueueMessage(Entity._id, command, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private async void WebSocketClient_OnQueueMessage(IQueueMessage message, QueueMessageEventArgs e)
        {
            Interfaces.mq.RobotCommand command = null;
            try
            {
                command = Newtonsoft.Json.JsonConvert.DeserializeObject<Interfaces.mq.RobotCommand>(message.data.ToString());
                if (command.command == "invokecompleted" || command.command == "invokefailed" || command.command == "invokeaborted" || command.command == "error" || command.command == null)
                {
                    if (!string.IsNullOrEmpty(message.correlationId))
                    {
                        foreach (var wi in WorkflowInstance.Instances)
                        {
                            if (wi.isCompleted) continue;
                            if (wi.Bookmarks == null) continue;
                            foreach (var b in wi.Bookmarks)
                            {
                                if (b.Key == message.correlationId)
                                {
                                    if (!string.IsNullOrEmpty(message.error))
                                    {
                                        wi.Abort(message.error);
                                    }
                                    else
                                    {
                                        wi.ResumeBookmark(b.Key, message.data.ToString());
                                    }

                                }
                            }
                        }
                    }
                }
                JObject data;
                if (command.data != null) { data = JObject.Parse(command.data.ToString()); } else { data = JObject.Parse("{}"); }
                if (command.command == null) return;
                if (command.command == "invoke" && !string.IsNullOrEmpty(command.workflowid))
                {
                    IWorkflowInstance instance = null;
                    var workflow = GetWorkflowByIDOrRelativeFilename(command.workflowid);
                    if (workflow == null) throw new ArgumentException("Unknown workflow " + command.workflowid);
                    lock (statelock)
                    {
                        foreach (var i in WorkflowInstance.Instances)
                        {
                            if (i.state == "running" || (!string.IsNullOrEmpty(i.correlationId) && !i.isCompleted))
                            {
                                Log.Warning("Cannot invoke " + workflow.name + ", I'm busy.");
                                e.isBusy = true; return;
                            }
                        }
                        // e.sendReply = true;
                        var param = new Dictionary<string, object>();
                        foreach (var k in data)
                        {
                            switch (k.Value.Type)
                            {
                                case JTokenType.Integer: param.Add(k.Key, k.Value.Value<long>()); break;
                                case JTokenType.Float: param.Add(k.Key, k.Value.Value<float>()); break;
                                case JTokenType.Boolean: param.Add(k.Key, k.Value.Value<bool>()); break;
                                case JTokenType.Date: param.Add(k.Key, k.Value.Value<DateTime>()); break;
                                case JTokenType.TimeSpan: param.Add(k.Key, k.Value.Value<TimeSpan>()); break;
                                default:
                                    try
                                    {
                                        param.Add(k.Key, k.Value.Value<string>());
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Debug("WebSocketClient_OnQueueMessage: " + ex.Message);
                                    }
                                    break;

                                    // default: param.Add(k.Key, k.Value.Value<string>()); break;
                            }
                        }
                        Log.Information("Create instance of " + workflow.name);
                        GenericTools.RunUI(() =>
                        {
                            if (GetWorkflowDesignerByIDOrRelativeFilename(command.workflowid) is Views.WFDesigner designer)
                            {
                                designer.BreakpointLocations = null;
                                instance = workflow.CreateInstance(param, message.replyto, message.correlationId, designer.OnIdle, designer.OnVisualTracking);
                                designer.Run(VisualTracking, SlowMotion, instance);
                            }
                            else
                            {
                                instance = workflow.CreateInstance(param, message.replyto, message.correlationId, IdleOrComplete, null);
                                instance.Run();
                            }
                        });
                        command.command = "invokesuccess";
                    }
                }
            }
            catch (Exception ex)
            {
                command = new Interfaces.mq.RobotCommand
                {
                    command = "error",
                    data = JObject.FromObject(ex)
                };
            }
            // string data = Newtonsoft.Json.JsonConvert.SerializeObject(command);
            if (command.command == "error" || (command.command == "invoke" && !string.IsNullOrEmpty(command.workflowid)))
            {
                if (!string.IsNullOrEmpty(message.replyto) && message.replyto != message.queuename)
                {
                    await global.webSocketClient.QueueMessage(message.replyto, command, message.correlationId);
                }
            }
        }
        public void IdleOrComplete(IWorkflowInstance instance, EventArgs e)
        {
            GenericTools.RunUI(() =>
            {
                CommandManager.InvalidateRequerySuggested();
                if (string.IsNullOrEmpty(instance.queuename) && string.IsNullOrEmpty(instance.correlationId) && string.IsNullOrEmpty(instance.caller) && instance.isCompleted)
                {
                    if (this.Minimize) GenericTools.Restore(GenericTools.MainWindow);

                }
            });
            if (!string.IsNullOrEmpty(instance.queuename) && !string.IsNullOrEmpty(instance.correlationId))
            {
                Interfaces.mq.RobotCommand command = new Interfaces.mq.RobotCommand();
                var data = JObject.FromObject(instance.Parameters);
                command.command = "invoke" + instance.state;
                command.workflowid = instance.WorkflowId;
                command.data = data;
                if ((instance.state == "failed" || instance.state == "aborted") && instance.Exception != null)
                {
                    command.data = JObject.FromObject(instance.Exception);
                }
                _ = global.webSocketClient.QueueMessage(instance.queuename, command, instance.correlationId);
            }
            if (instance.hasError || instance.isCompleted)
            {
                string message = "";
                if (instance.runWatch != null)
                {
                    message += (instance.Workflow.name + " " + instance.state + " in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed));
                }
                else
                {
                    message += (instance.Workflow.name + " " + instance.state);
                }
                if (!string.IsNullOrEmpty(instance.errormessage)) message += (Environment.NewLine + "# " + instance.errormessage);
                Log.Information(message);
                System.Threading.Thread.Sleep(200);
                foreach (var wi in WorkflowInstance.Instances)
                {
                    if (wi.isCompleted) continue;
                    if (wi.Bookmarks == null) continue;
                    foreach (var b in wi.Bookmarks)
                    {
                        if (b.Key == instance._id)
                        {
                            wi.ResumeBookmark(b.Key, instance);
                        }
                    }
                }
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            InputDriver.Instance.CallNext = true;
            InputDriver.Instance.OnKeyDown -= OnKeyDown;
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
            InputDriver.Instance.Dispose();
            SaveLayout();
            // automation threads will not allways abort, and mousemove hook will "hang" the application for several seconds
            Environment.Exit(Environment.ExitCode);

        }
        private Views.KeyboardSeqWindow view = null;
        private void Cancelkey_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (view != null) return;
            try
            {
                view = new Views.KeyboardSeqWindow
                {
                    oneKeyOnly = true,
                    Title = "Press New Cancel Key"
                };
                Hide();
                if (view.ShowDialog() == true)
                {
                    cancelkey.Text = view.Text;
                    Config.local.cancelkey = view.Text;
                    Config.Save();
                    OpenRPA.Input.InputDriver.Instance.initCancelKey(cancelkey.Text);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                MessageBox.Show("cancelkey_GotKeyboardFocus: " + ex.ToString());
            }
            finally
            {
                Show();
                Keyboard.ClearFocus();
                view = null;
            }

        }
        private void TesseractLang_Click(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "tessdata");
            TesseractDownloadLangFile(path, Config.local.ocrlanguage);
            System.Windows.MessageBox.Show("Download complete");
        }
        private void TesseractDownloadLangFile(string folder, string lang)
        {
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            string dest = System.IO.Path.Combine(folder, string.Format("{0}.traineddata", lang));
            if (!System.IO.File.Exists(dest))
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {
                    // string source = string.Format("https://github.com/tesseract-ocr/tessdata/blob/4592b8d453889181e01982d22328b5846765eaad/{0}.traineddata?raw=true", lang);
                    string source = string.Format("https://github.com/tesseract-ocr/tessdata/blob/master/{0}.traineddata?raw=true", lang);
                    Log.Information(String.Format("Downloading file from '{0}' to '{1}'", source, dest));
                    webclient.DownloadFile(source, dest);
                    Log.Information(String.Format("Download completed"));
                }
        }
        public IDesigner GetWorkflowDesignerByIDOrRelativeFilename(string IDOrRelativeFilename)
        {
            foreach (var designer in Designers)
            {
                if (designer.Workflow.IDOrRelativeFilename == IDOrRelativeFilename) return designer;
            }
            return null;
        }
        public IWorkflow GetWorkflowByIDOrRelativeFilename(string IDOrRelativeFilename)
        {
            foreach (var p in Projects)
            {
                foreach (var wf in p.Workflows)
                {
                    if (wf.IDOrRelativeFilename == IDOrRelativeFilename) return wf;
                }
            }
            return null;
        }
        public IWorkflowInstance GetWorkflowInstanceByInstanceId(string InstanceId)
        {
            var result = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId).FirstOrDefault();
            return result;
        }
    }
}
