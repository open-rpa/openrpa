using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using OpenRPA.Net;
using System;
using System.Activities.Core.Presentation;
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
    public partial class MainWindow : Window, INotifyPropertyChanged, iMainWindow
    {
        public static MainWindow instance = null;
        Updates updater = new Updates();
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Project>();
        private bool isRecording = false;
        private bool autoReconnect = true;
        private bool loginInProgress = false;
        // public static Tracing tracing = new Tracing();
        public Tracing tracing { get; set; }  = new Tracing();
        private static object statelock = new object();
        public List<string> ocrlangs { get; set; } = new List<string>() { "afr", "amh", "ara", "asm", "aze", "aze_cyrl", "bel", "ben", "bod", "bos", "bre", "bul", "cat", "ceb", "ces", "chi_sim", "chi_sim_vert", "chi_tra", "chi_tra_vert", "chr", "cos", "cym", "dan", "dan_frak", "deu", "deu_frak", "div", "dzo", "ell", "eng", "enm", "epo", "equ", "est", "eus", "fao", "fas", "fil", "fin", "fra", "frk", "frm", "fry", "gla", "gle", "glg", "grc", "guj", "hat", "heb", "hin", "hrv", "hun", "hye", "iku", "ind", "isl", "ita", "ita_old", "jav", "jpn", "jpn_vert", "kan", "kat", "kat_old", "kaz", "khm", "kir", "kmr", "kor", "kor_vert", "lao", "lat", "lav", "lit", "ltz", "mal", "mar", "mkd", "mlt", "mon", "mri", "msa", "mya", "nep", "nld", "nor", "oci", "ori", "osd", "pan", "pol", "por", "pus", "que", "ron", "rus", "san", "sin", "slk", "slk_frak", "slv", "snd", "spa", "spa_old", "sqi", "srp", "srp_latn", "sun", "swa", "swe", "syr", "tam", "tat", "tel", "tgk", "tgl", "tha", "tir", "ton", "tur", "uig", "ukr", "urd", "uzb", "uzb_cyrl", "vie", "yid", "yor" };
        public string defaultocrlangs {
            get
            {
                return Config.local.ocrlanguage;
            }
            set {
                Config.local.ocrlanguage = value;
                Config.Save();
            }
        }
        public Views.WFToolbox Toolbox { get; set; }
        public Views.Snippets Snippets { get; set; }
       
        public bool allowQuite { get; set; } = true;
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                if (System.IO.File.Exists("Snippets.dll")) System.IO.File.Delete("Snippets.dll");
            }
            catch (Exception)
            {
            }
            AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
            SetStatus("Initializing events");
            instance = this;
            DataContext = this;
            GenericTools.mainWindow = this;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AppDomain currentDomain = AppDomain.CurrentDomain;
            System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.Diagnostics.Trace.Listeners.Add(tracing);
            Console.SetOut(new DebugTextWriter());
            lvDataBinding.ItemsSource = Plugins.recordPlugins;
            cancelkey.Text = Config.local.cancelkey;
            InputDriver.Instance.onCancel += onCancel;
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
            await Plugins.LoadPlugins(Extensions.projectsDirectory);
            //await Task.Run(() =>
            //{
            //    GenericTools.RunUI(() =>
            //    {
            //        Plugins.loadPlugins(Extensions.projectsDirectory);
            //    });                
            //});
            if (string.IsNullOrEmpty(Config.local.wsurl))
            {
                SetStatus("loading detectors");
                var Detectors = Interfaces.entity.Detector.loadDetectors(Extensions.projectsDirectory);
                foreach (var d in Detectors)
                {
                    IDetectorPlugin dp = null;
                    d.Path = Extensions.projectsDirectory;
                    dp = Plugins.AddDetector(d);
                    if (dp != null) dp.OnDetector += OnDetector;
                }
            }
            try
            {
                SetStatus("loading workflow toolbox");
                Toolbox = new Views.WFToolbox();
                Snippets = new Views.Snippets();
                NotifyPropertyChanged("Toolbox");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            await Task.Run(() =>
            {
                ExpressionEditor.EditorUtil.init();
                LoadLayout();
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
                    var _Projects = Project.loadProjects(Extensions.projectsDirectory);
                    Projects = new System.Collections.ObjectModel.ObservableCollection<Project>();
                    foreach (Project p in _Projects)
                    {
                        Projects.Add(p);
                    }
                }
                AutomationHelper.init();
                SetStatus("Reopening workflows");
                onOpen(null);
                AddHotKeys();
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
                        SetStatus("Registering queue for robot");
                        Log.Debug("Registering queue for robot " + global.webSocketClient.user._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        await global.webSocketClient.RegisterQueue(global.webSocketClient.user._id);
                        foreach (var role in global.webSocketClient.user.roles)
                        {
                            SetStatus("Registering queue for robot (" + role.name + ")");
                            Log.Debug("Registering queue for role " + role.name + " " + role._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            await global.webSocketClient.RegisterQueue(role._id);
                        }

                        SetStatus("Loading workflows and state from " + Config.local.wsurl);
                        Log.Debug("Get workflows from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}");
                        Log.Debug("Get projects from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var projects = await global.webSocketClient.Query<Project>("openrpa", "{_type: 'project'}");
                        Log.Debug("Get detectors from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var detectors = await global.webSocketClient.Query<Interfaces.entity.Detector>("openrpa", "{_type: 'detector'}");
                        Log.Debug("Done getting workflows and projects " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        SetStatus("Initialize detecors");
                        foreach (var d in detectors)
                        {
                            IDetectorPlugin dp = null;
                            d.Path = Extensions.projectsDirectory;
                            dp = Plugins.AddDetector(d);
                            if (dp != null) dp.OnDetector += OnDetector;
                            if (dp == null) Log.Error("Detector not loaded!");
                        }
                        var folders = new List<string>();
                        foreach (var p in projects)
                        {
                            p.Path = System.IO.Path.Combine(Extensions.projectsDirectory, p.name);
                            if (folders.Contains(p.Path))
                            {
                                p.Path = System.IO.Path.Combine(Extensions.projectsDirectory, p._id);
                            }
                            folders.Add(p.Path);
                        }
                        SetStatus("Initialize projects and workflows");
                        foreach (var p in projects)
                        {
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
                                if (up == null) up = await Project.Create(Extensions.projectsDirectory, "Unknown", false);
                                up.Workflows.Add(wf);
                            }
                        }
                        if (up != null) Projects.Add(up);
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
                if (Projects.Count > 0)
                {
                    Projects[0].IsExpanded = true;
                    LoadLayout();
                }
                else
                {
                    onOpen(null);
                    string Name = "New Project";
                    try
                    {
                        Project project = await Project.Create(Extensions.projectsDirectory, Name, true);
                        Workflow workflow = project.Workflows.First();
                        workflow.Project = project;
                        Projects.Add(project);
                        onOpenWorkflow(workflow);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
                SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
            }, null);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (allowQuite) return;
            App.notifyIcon.Visible = true;
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if(WindowState== WindowState.Minimized)
            {
                Visibility = Visibility.Hidden;
                App.notifyIcon.Visible = true;
            } else
            {
                Visibility = Visibility.Visible;
                App.notifyIcon.Visible = false;
            }
        }
        private async Task CheckForUpdatesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (updater.UpdaterNeedsUpdate() == true)
                    {
                        updater.UpdateUpdater();
                    }
                    var releasenotes = updater.OpenRPANeedsUpdate();
                    if (!string.IsNullOrEmpty(releasenotes))
                    {
                        var dialogResult = MessageBox.Show(releasenotes, "Update available", MessageBoxButton.YesNo);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            onManagePackages(null);
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
            if(SelectedContent is Views.WFDesigner view) {
                var las = DManager.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
                foreach (var dp in las)
                {
                    //if (dp.Title == "Toolbox")
                    //{
                    //    dp.Content = designer.toolbox;
                    //    if (dp.IsAutoHidden) { dp.ToggleAutoHide(); }
                    //}
                    //if (dp.Title == "Properties")
                    //{
                    //    dp.Content = designer.Properties;
                    //    if (dp.IsAutoHidden) { dp.ToggleAutoHide(); }
                    //}

                }
            }
            else
            {
                var las = DManager.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
                foreach (var dp in las)
                {
                    if (dp.Title == "Toolbox")
                    {
                        // dp.Content = null;
                    }

                }

            }
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
                if (designer != null) _LastDesigner = designer;
                if (SelectedContent is Views.OpenProject) _LastDesigner = null;
                if (SelectedContent is Views.DetectorsView) _LastDesigner = null;
                return _LastDesigner;
            }
        }
        public LayoutDocumentPane mainTabControl
        {
            get
            {
                var documentPane = DManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                return documentPane;
            }
        }
        IDesigner iMainWindow.designer { get => this.designer; }
        public Views.WFDesigner designer
        {
            get
            {
                if(SelectedContent is Views.WFDesigner view)
                {
                    return view;
                }
                return null;
            }
            set
            {

            }
        }
        public Views.WFDesigner[] designers
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
        public bool VisualTracking
        {
            get
            {
                if (designer == null) return false;
                return designer.VisualTracking;
            }
            set
            {
                if (designer == null) return;
                designer.VisualTracking = value;
                NotifyPropertyChanged("VisualTracking");
            }
        }
        public bool SlowMotion
        {
            get
            {
                if (designer == null) return false;
                return designer.SlowMotion;
            }
            set
            {
                if (designer == null) return;
                designer.SlowMotion = value;
                NotifyPropertyChanged("SlowMotion");
            }
        }
        public bool Minimize
        {
            get
            {
                if (designer == null) return false;
                return designer.Minimize;
            }
            set
            {
                if (designer == null) return;
                designer.Minimize = value;
                NotifyPropertyChanged("Minimize");
            }
        }
        public bool usingOpenFlow
        {
            get
            {
                return !string.IsNullOrEmpty(Config.local.wsurl);
            }
        }
        public bool isConnected
        {
            get
            {
                if (!usingOpenFlow) return true; // IF working offline, were allways connected, right ?
                if (global.webSocketClient == null) return false;
                return global.webSocketClient.isConnected;
            }
        }
        public ICommand ExitAppCommand { get { return new RelayCommand<object>(onExitApp, (e)=> true ); } }
        public ICommand SettingsCommand { get { return new RelayCommand<object>(onSettings, canSettings); } }
        public ICommand MinimizeCommand { get { return new RelayCommand<object>(onMinimize, canMinimize); } }
        public ICommand VisualTrackingCommand { get { return new RelayCommand<object>(onVisualTracking, canVisualTracking); } }
        public ICommand SlowMotionCommand { get { return new RelayCommand<object>(onSlowMotion, canSlowMotion); } }
        public ICommand SignoutCommand { get { return new RelayCommand<object>(onSignout, canSignout); } }
        public ICommand OpenCommand { get { return new RelayCommand<object>(onOpen, canOpen); } }
        public ICommand ManagePackagesCommand { get { return new RelayCommand<object>(onManagePackages, canManagePackages); } }        
        public ICommand DetectorsCommand { get { return new RelayCommand<object>(onDetectors, canDetectors); } }
        public ICommand SaveCommand { get { return new RelayCommand<object>(onSave, canSave); } }
        public ICommand NewWorkflowCommand { get { return new RelayCommand<object>(onNewWorkflow, canNewWorkflow); } }
        public ICommand NewProjectCommand { get { return new RelayCommand<object>(onNewProject, canNewProject); } }
        public ICommand CopyCommand { get { return new RelayCommand<object>(onCopy, canCopy); } }
        public ICommand DeleteCommand { get { return new RelayCommand<object>(onDelete, canDelete); } }
        public ICommand PlayCommand { get { return new RelayCommand<object>(onPlay, canPlay); } }
        public ICommand StopCommand { get { return new RelayCommand<object>(onStop, canStop); } }
        public ICommand RecordCommand { get { return new RelayCommand<object>(onRecord, canRecord); } }
        public ICommand ImportCommand { get { return new RelayCommand<object>(onImport, canImport); } }
        public ICommand ExportCommand { get { return new RelayCommand<object>(onExport, canExport); } }
        public ICommand PermissionsCommand { get { return new RelayCommand<object>(onPermissions, canPermissions); } }
        public ICommand linkOpenFlowCommand { get { return new RelayCommand<object>(onlinkOpenFlow, canlinkOpenFlow); } }
        public ICommand linkNodeREDCommand { get { return new RelayCommand<object>(onlinkNodeRED, canlinkNodeRED); } }
        public ICommand OpenChromePageCommand { get { return new RelayCommand<object>(onOpenChromePage, canAllways); } }
        public ICommand OpenFirefoxPageCommand { get { return new RelayCommand<object>(onOpenFirefoxPageCommand, canAllways); } }
        private bool canPermissions(object _item)
        {
            try
            {
                if (!isConnected) return false;
                if (isRecording) return false;
                var view = SelectedContent as Views.OpenProject;
                if (view != null)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    var wf = view.listWorkflows.SelectedValue as Workflow;
                    return true;
                }
                var designer = SelectedContent as Views.WFDesigner;
                if (designer != null)
                {
                    return true;
                }
                var DetectorsView = SelectedContent as Views.DetectorsView;
                if (DetectorsView != null)
                {
                    var detector = DetectorsView.lidtDetectors.SelectedItem as IDetectorPlugin;
                    if (detector == null) return false;
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
        private async void onPermissions(object _item)
        {
            apibase result = null;
            if (!isConnected) return;
            if (isRecording) return;
            var view = SelectedContent as Views.OpenProject;
            if (view != null)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                var wf = val as Workflow;
                var p = val as Project;
                if (wf != null) { result = wf; }
                if (p != null) { result = p; }
            }
            var designer = SelectedContent as Views.WFDesigner;
            if (designer != null)
            {
                result = designer.Workflow;
            }
            var DetectorsView = SelectedContent as Views.DetectorsView;
            if (DetectorsView != null)
            {
                var detector = DetectorsView.lidtDetectors.SelectedItem as IDetectorPlugin;
                if (detector == null) return;
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
                    foreach(var wf in p.Workflows)
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
        private bool canImport(object _item)
        {
            try
            {
            if (!isConnected) return false; return (SelectedContent is Views.WFDesigner || SelectedContent is Views.OpenProject || SelectedContent == null);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void onImport(object _item)
        {
            try
            {
                if (SelectedContent is Views.WFDesigner)
                {
                    var designer = (Views.WFDesigner)SelectedContent;
                    Workflow workflow = Workflow.Create(designer.Project, "New Workflow");
                    onOpenWorkflow(workflow);
                    return;
                }
                else
                {
                    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                        if(result == System.Windows.Forms.DialogResult.OK)
                        {
                            //var _Projects = Project.loadProjects(Extensions.projectsDirectory);
                            //if (_Projects.Count() > 0)
                            //{
                            //    var ProjectFiles = System.IO.Directory.EnumerateFiles(dialog.SelectedPath, "*.rpaproj", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
                            //    foreach(var file in ProjectFiles)
                            //    {
                            //        if()
                            //    }

                            //}

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool canExport(object _item)
        {
            try
            {

            if (!isConnected) return false; return (SelectedContent is Views.WFDesigner || SelectedContent is Views.OpenProject || SelectedContent == null);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void onExport(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
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
                    CommandBindings.Add(new CommandBinding(saveHotkey, onSave));
                    RoutedCommand deleteHotkey = new RoutedCommand();
                    deleteHotkey.InputGestures.Add(new KeyGesture(Key.Delete));
                    CommandBindings.Add(new CommandBinding(deleteHotkey, onDelete));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show(ex.Message);
                }
            }, null);
        }
        private void onExitApp(object _item)
        {
            allowQuite = true;
            Close();
        }
        private void onSave(object sender, ExecutedRoutedEventArgs e)
        {
            SaveCommand.Execute(SelectedContent);
        }
        private void onDelete(object sender, ExecutedRoutedEventArgs e)
        {
            DeleteCommand.Execute(SelectedContent);
        }
        private bool canMinimize(object _item)
        {
            return true;
        }
        private void onMinimize(object _item)
        {
        }
        private bool canVisualTracking(object _item)
        {
            return true;
        }
        private void onVisualTracking(object _item)
        {
            var b = (bool)_item;
            if(SelectedContent is Views.WFDesigner)
            {
                var designer = SelectedContent as Views.WFDesigner;
                designer.VisualTracking = b;
            }
        }
        private bool canSlowMotion(object _item)
        {
            return true;
        }
        private void onSlowMotion(object _item)
        {
            var b = (bool)_item;
            if (SelectedContent is Views.WFDesigner)
            {
                var designer = SelectedContent as Views.WFDesigner;
                designer.SlowMotion = b;
            }
        }
        private bool canSettings(object _item)
        {
            return true;
        }
        private void onSettings(object _item)
        {
            try
            {
                var filename = "settings.json";
                var path = System.IO.Directory.GetCurrentDirectory();
                string settingsFile = System.IO.Path.Combine(path, filename);
                var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = settingsFile
                };
                process.Start();
            }
            catch (Exception ex)
            {
                Log.Error("onSettings: " + ex.Message);
            }
        }
        private bool canSignout(object _item)
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
        private void onSignout(object _item)
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
        private bool canManagePackages(object _item)
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
        private void onManagePackages(object _item)
        {
            var di = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
            if (!System.IO.File.Exists(Environment.CurrentDirectory + @"\OpenRPA.Updater.exe") && !System.IO.File.Exists(di.Parent.FullName + @"\OpenRPA.Updater.exe"))
            {
                MessageBox.Show("OpenRPA.Updater.exe not found");
                return;
            }
            try
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo.FileName = Environment.CurrentDirectory + @"\OpenRPA.Updater";
                p.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
                if (!System.IO.File.Exists(Environment.CurrentDirectory + @"\OpenRPA.Updater.exe"))
                {
                    p.StartInfo.FileName = di.Parent.FullName + @"\OpenRPA.Updater.exe";
                    p.StartInfo.WorkingDirectory = di.Parent.FullName;
                }
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
            //AutomationHelper.syncContext.Post(o =>
            //{
            //    var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
            //    foreach (var document in ld)
            //    {
            //        if (document.Content is Views.ManagePackages op) { document.IsSelected = true; return; }
            //    }
            //    var view = new Views.ManagePackages();
            //    LayoutDocument layoutDocument = new LayoutDocument { Title = "Manage Packages" };
            //    layoutDocument.ContentId = "managepackages";
            //    layoutDocument.CanClose = true;
            //    layoutDocument.Content = view;
            //    mainTabControl.Children.Add(layoutDocument);
            //    layoutDocument.IsSelected = true;
            //    layoutDocument.Closing += LayoutDocument_Closing;
            //}, null);
        }
        private bool canOpen(object _item)
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
        private bool canDetectors(object _item)
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
        private void onOpen(object _item)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.OpenProject op) { document.IsSelected = true; return; }
                }
                var view = new Views.OpenProject(this);
                view.onOpenProject += onOpenProject;
                view.onOpenWorkflow += onOpenWorkflow;

                LayoutDocument layoutDocument = new LayoutDocument { Title = "Open project" };
                layoutDocument.ContentId = "openproject";
                layoutDocument.CanClose = false;
                layoutDocument.Content = view;
                mainTabControl.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
                layoutDocument.Closing += LayoutDocument_Closing;
            }, null);
        }
        private void onDetectors(object _item)
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
                mainTabControl.Children.Add(layoutDocument);
                layoutDocument.IsSelected = true;
            }, null);
        }
        private bool canlinkOpenFlow(object _item)
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
        private void onlinkOpenFlow(object _item)
        {
            if (string.IsNullOrEmpty(Config.local.wsurl)) return;
            if (global.openflowconfig == null) return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(global.openflowconfig.baseurl));
        }
        private bool canlinkNodeRED(object _item)
        {
            try
            {
            if (!isConnected) return false;
            if (string.IsNullOrEmpty(Config.local.wsurl)) return false;
            if (global.openflowconfig == null) return false;
            if(global.openflowconfig.allow_personal_nodered) return true;

            return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void onlinkNodeRED(object _item)
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
            Views.WFDesigner designer = tab.Content as Views.WFDesigner;
            if (designer == null) return;
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
            foreach (var designer in designers)
            {
                workflows.Add(designer.Workflow._id);
            }
            Config.local.openworkflows = workflows.ToArray();

            //var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
            //var sb = new StringBuilder();
            //using (var stream = new System.IO.StringWriter(sb))
            //    serializer.Serialize(stream);
            //Config.local.designerlayout = sb.ToString();
                var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                using (var stream = new System.IO.StreamWriter("layout.config"))
                    serializer.Serialize(stream);
            Config.Save();
        }
        private void LoadLayout()
        {
            //if (!string.IsNullOrEmpty(Config.local.designerlayout))
            //{
            //}
            foreach (var p in Projects)
            {
                foreach (var wf in p.Workflows)
                {
                    
                    if (Config.local.openworkflows.Contains(wf._id) && !string.IsNullOrEmpty(wf._id))
                    {
                        onOpenWorkflow(wf);
                    }
                }
            }
            GenericTools.RunUI(() =>
            {
                //byte[] byteArray = Encoding.Unicode.GetBytes(Config.local.designerlayout);
                //var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                //using (var stream = new System.IO.MemoryStream(byteArray))
                //    serializer.Deserialize(stream);
                if (System.IO.File.Exists("layout.config"))
                {
                    var ds = DManager.Layout.Descendents();
                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                    using (var stream = new System.IO.StreamReader("layout.config"))
                        serializer.Deserialize(stream);
                    ds = DManager.Layout.Descendents();
                }
                else if (System.IO.File.Exists(@"..\layout.config"))
                {
                    var ds = DManager.Layout.Descendents();
                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                    using (var stream = new System.IO.StreamReader(@"..\layout.config"))
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
            });
        }
        public Views.WFDesigner getWorkflowDesignerByFilename(string Filename)
        {
            foreach(var designer in designers)
            {
                if (designer.Workflow.FilePath == Filename) return designer;
            }
            return null;
        }
        public Views.WFDesigner getWorkflowDesignerById(string Id)
        {
            foreach (var designer in designers)
            {
                if (designer.Workflow._id == Id) return designer;
            }
            return null;
        }
        public void onOpenWorkflow(Workflow workflow)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                Views.WFDesigner designer = getWorkflowDesignerByFilename(workflow.FilePath);
                if (designer == null && !string.IsNullOrEmpty(workflow._id)) designer = getWorkflowDesignerById(workflow._id);
                if (designer != null)
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

                    var view = new Views.WFDesigner(layoutDocument, workflow, types.ToArray());
                    view.onChanged = WFDesigneronChanged;
                    layoutDocument.Content = view;
                    mainTabControl.Children.Add(layoutDocument);
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
        private void WFDesigneronChanged(Views.WFDesigner designer)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                designer.tab.Title = (designer.HasChanged ? designer.Workflow.name + "*" : designer.Workflow.name);
                CommandManager.InvalidateRequerySuggested();
            }, null);
            //_syncContext.Post(o => CommandManager.InvalidateRequerySuggested(), null);
        }
        public void onOpenProject(Project project)
        {
            foreach (var wf in project.Workflows)
            {
                onOpenWorkflow(wf);
            }
        }
        private bool canSave(object _item) {
            try
            {

            var wf = SelectedContent as Views.WFDesigner;
            if (wf == null) return false;
            if (wf.isRunnning == true) return false;
            return wf.HasChanged;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void onSave(object _item)
        {
            if (SelectedContent is Views.WFDesigner)
            {
                var designer = (Views.WFDesigner)SelectedContent;
                await designer.Save();
            }
            if (SelectedContent is Views.OpenProject)
            {
                var view = (Views.OpenProject)SelectedContent;
                var Project = view.listWorkflows.SelectedItem as Project;
                if (Project != null)
                {
                    await Project.Save();
                }
            }
        }
        private bool canNewWorkflow(object _item)
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
        private void onNewWorkflow(object _item)
        {
            try
            {
                if (SelectedContent is Views.WFDesigner)
                {
                    var designer = (Views.WFDesigner)SelectedContent;
                    Workflow workflow = Workflow.Create(designer.Project, "New Workflow");
                    onOpenWorkflow(workflow);
                    return;
                }
                var view = SelectedContent as Views.OpenProject;
                if (view == null) return;
                var val = view.listWorkflows.SelectedValue;
                var wf = val as Workflow;
                var p = val as Project;
                if(wf!=null)
                {
                    Workflow workflow = Workflow.Create(wf.Project, "New Workflow");
                    onOpenWorkflow(workflow);
                    return;
                }
                if(p !=null)
                {
                    Workflow workflow = Workflow.Create(p, "New Workflow");
                    onOpenWorkflow(workflow);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool canNewProject(object _item)
        {
            try
            {

            if (!isConnected) return false; return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void onNewProject(object _item)
        {
            try
            {
                string Name = Microsoft.VisualBasic.Interaction.InputBox("Name?", "Name project", "New project");
                if (string.IsNullOrEmpty(Name)) return;
                //string Name = "New project";
                Project project = await Project.Create(Extensions.projectsDirectory, Name, true);
                Workflow workflow = project.Workflows.First();
                workflow.Project = project;
                Projects.Add(project);
                onOpenWorkflow(workflow);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool canCopy(object _item)
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
        private async void onCopy(object _item)
        {
            var designer = (Views.WFDesigner)SelectedContent;
            await designer.Save();
            Workflow workflow = Workflow.Create(designer.Project, "Copy of " + designer.Workflow.name);
            workflow.Xaml = designer.Workflow.Xaml;
            workflow.name = "Copy of " + designer.Workflow.name;
            onOpenWorkflow(workflow);
        }
        private bool canDelete(object _item)
        {
            try
            {
            var view = SelectedContent as Views.OpenProject;
            if (view == null) return false;
            var val = view.listWorkflows.SelectedValue;
            if (val == null) return false;
            if (global.isConnected)
            {
                var wf = val as Workflow;
                var p = val as Project;
                if (wf != null)
                {
                    if (!wf.hasRight(global.webSocketClient.user, ace_right.delete)) return false;
                    return !wf.isRunnning;
                }
                if (p != null)
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
        private async void onDelete(object _item)
        {
            var view = SelectedContent as Views.OpenProject;
            if (view == null) return;
            var val = view.listWorkflows.SelectedValue;
            var wf = val as Workflow;
            var p = val as Project;


            if (wf != null)
            {
                Views.WFDesigner designer = getWorkflowDesignerByFilename(wf.FilePath);
                if (designer == null && !string.IsNullOrEmpty(wf._id)) { designer = getWorkflowDesignerById(wf._id); }
                if (designer != null) { designer.tab.Close(); }

                var messageBoxResult = MessageBox.Show("Delete " + wf.name + " ?", "Delete Confirmation", MessageBoxButton.YesNo);
                if (messageBoxResult != MessageBoxResult.Yes) return;

                await wf.Delete();
            }
            if (p != null)
            {
                if (p.Workflows.Count > 0)
                {
                    var messageBoxResult = MessageBox.Show("Delete project " + p.name + " containing " + p.Workflows.Count() + " workflows", "Delete Confirmation", MessageBoxButton.YesNo);
                    if (messageBoxResult != MessageBoxResult.Yes) return;
                    foreach (var _wf in p.Workflows.ToList())
                    {
                        Views.WFDesigner designer = getWorkflowDesignerByFilename(_wf.FilePath);
                        if (designer == null && !string.IsNullOrEmpty(_wf._id)) { designer = getWorkflowDesignerById(_wf._id); }
                        if (designer != null) { designer.tab.Close(); }
                        await _wf.Delete();
                    }
                }
                await p.Delete();
                Projects.Remove(p);
            }
        }
        private bool canPlay(object _item)
        {
            try
            {
                var view = SelectedContent as Views.OpenProject;
                if (view != null)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    var wf = view.listWorkflows.SelectedValue as Workflow;
                    if(wf == null) return false;
                    if(wf.State == "running") return false;
                    if(global.isConnected)
                    {
                        return wf.hasRight(global.webSocketClient.user, ace_right.invoke);
                    }
                    return true;
                }

                if (!isConnected) return false;
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
        private async void onPlay(object _item)
        {
            var view = SelectedContent as Views.OpenProject;
            if (view != null)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                var workflow = view.listWorkflows.SelectedValue as Workflow;
                if (workflow == null) return;

                var designer = GetDesignerById(workflow._id);
                var param = new Dictionary<string, object>();
                if (designer != null)
                {
                    var instance = workflow.CreateInstance(param, null, null, designer.onIdle, designer.onVisualTracking);
                    designer.Minimize = false;
                    designer.Run(VisualTracking, SlowMotion, instance);
                }
                else
                {
                    var instance = workflow.CreateInstance(param, null, null, idleOrComplete, null);
                    instance.Run();
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
        private bool canStop(object _item)
        {
            try
            {

            var view = SelectedContent as Views.OpenProject;
            if (view != null)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return false;
                var wf = view.listWorkflows.SelectedValue as Workflow;
                if (wf == null) return false;
                if (wf.State == "running") return true;
                return false;
            }
            if (!isConnected) return false;
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
        private void onStop(object _item)
        {
            var view = SelectedContent as Views.OpenProject;
            if (view != null)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                var wf = view.listWorkflows.SelectedValue as Workflow;
                if (wf == null) return;
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
            if (designer.resumeRuntimeFromHost != null) designer.resumeRuntimeFromHost.Set();
            if (isRecording)
            {
                StartDetectorPlugins();
                StopRecordPlugins();
                designer.ReadOnly = false;
                InputDriver.Instance.CallNext = true;
                InputDriver.Instance.OnKeyDown -= OnKeyDown;
                InputDriver.Instance.OnKeyUp -= OnKeyUp;
                GenericTools.restore(GenericTools.mainWindow);
            }
        }
        private bool canRecord(object _item)
        {
            try
            {
            if (!isConnected) return false;
            if (!(SelectedContent is Views.WFDesigner)) return false;
            var designer = (Views.WFDesigner)SelectedContent;
            foreach (var i in designer.Workflow.Instances)
            {
                if (i.isCompleted == false)
                {
                    return false;
                }
            }
            return !isRecording;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private void onCancel()
        {
            if (!isRecording) return;
            StartDetectorPlugins();
            StopRecordPlugins();
            if (SelectedContent is Views.WFDesigner view)
            {
                view.ReadOnly = false;
            }
            InputDriver.Instance.CallNext = true;
            InputDriver.Instance.OnKeyDown -= OnKeyDown;
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
            GenericTools.restore(GenericTools.mainWindow);
        }
        private bool canAllways(object _item)
        {
            return true;
        }
        private void onOpenChromePage(object _item)
        {
            System.Diagnostics.Process.Start("chrome.exe", "https://chrome.google.com/webstore/detail/openrpa/hpnihnhlcnfejboocnckgchjdofeaphe");
        }
        private void onOpenFirefoxPageCommand(object _item)
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
                    if (view.lastinserted != null && view.lastinserted is Activities.TypeText)
                    {
                        Log.Debug("re-use existing TypeText");
                        var item = (Activities.TypeText)view.lastinserted;
                        item.AddKey(new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, false), view.lastinsertedmodel);
                    }
                    else
                    {
                        Log.Debug("Add new TypeText");
                        var rme = new Activities.TypeText();
                        view.lastinsertedmodel = view.addActivity(rme);
                        rme.AddKey(new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, false), view.lastinsertedmodel);
                        view.lastinserted = rme;
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
                    if (view.lastinserted != null && view.lastinserted is Activities.TypeText)
                    {
                        Log.Debug("re-use existing TypeText");
                        view.ReadOnly = false;
                        var item = (Activities.TypeText)view.lastinserted;
                        item.AddKey(new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, true), view.lastinsertedmodel);
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
            foreach(var detector in Plugins.detectorPlugins) detector.Start();
        }
        private void StopDetectorPlugins()
        {
            foreach (var detector in Plugins.detectorPlugins) detector.Stop();
        }
        private void StartRecordPlugins()
        {
            isRecording = true;
            var p = Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            p.OnUserAction += OnUserAction;
            p.Start();
        }
        private void StopRecordPlugins()
        {
            isRecording = false;
            var p = Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
            p.OnUserAction -= OnUserAction;
            p.Stop();
        }
        public void OnUserAction(IPlugin sender, IRecordEvent e)
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
                            if (p.parseUserAction(ref e)) continue;
                        }
                    }
                    if (e.a == null)
                    {
                        StartRecordPlugins();
                        if (e.ClickHandled == false)
                        {
                            InputDriver.Instance.CallNext = true;
                            Log.Debug("MouseMove to " + e.X + "," + e.Y + " and click " + e.Button + " button");
                            //var point = new FlaUI.Core.Shapes.Point(e.X + e.OffsetX, e.Y + e.OffsetY);
                            var point = new FlaUI.Core.Shapes.Point(e.X, e.Y);
                            FlaUI.Core.Input.MouseButton flabuttun = FlaUI.Core.Input.MouseButton.Left;
                            if (e.Button == Input.MouseButton.Middle) flabuttun = FlaUI.Core.Input.MouseButton.Middle;
                            if (e.Button == Input.MouseButton.Right) flabuttun = FlaUI.Core.Input.MouseButton.Right;
                            FlaUI.Core.Input.Mouse.Click(flabuttun, point);
                            // InputDriver.Instance.Click(lastInputEventArgs.Button);
                            //InputDriver.DoMouseClick();
                            Log.Debug("Click done");
                        }
                        return;
                    }
                    InputDriver.Instance.CallNext = true;
                    if (SelectedContent is Views.WFDesigner view)
                    {

                        var VirtualClick = true;
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
                            VirtualClick = VirtualClick
                        }, "item");
                        if (e.SupportInput)
                        {
                            var win = new Views.InsertText();
                            win.Topmost = true;
                            isRecording = false;
                            if (win.ShowDialog() == true)
                            {
                                e.a.AddInput(win.Text, e.Element);
                            }
                            else { e.SupportInput = false; }
                            isRecording = true;
                        }
                        view.ReadOnly = false;
                        view.lastinserted = e.a.Activity;
                        view.lastinsertedmodel = view.addActivity(e.a.Activity);
                        view.ReadOnly = true;
                        if (e.ClickHandled == false && e.SupportInput == false)
                        {
                            InputDriver.Instance.CallNext = true;
                            Log.Debug("MouseMove to " + e.X + "," + e.Y + " and click " + e.Button + " button");
                            //var point = new FlaUI.Core.Shapes.Point(e.X , e.Y);
                            //FlaUI.Core.Input.Mouse.MoveTo(e.X , e.Y);
                            //FlaUI.Core.Input.MouseButton flabuttun = FlaUI.Core.Input.MouseButton.Left;
                            //if (e.Button == Input.MouseButton.Middle) flabuttun = FlaUI.Core.Input.MouseButton.Middle;
                            //if (e.Button == Input.MouseButton.Right) flabuttun = FlaUI.Core.Input.MouseButton.Right;
                            //FlaUI.Core.Input.Mouse.Click(flabuttun, point);

                            InputDriver.Instance.MouseMove(e.X, e.Y);
                            // InputDriver.Instance.Click(lastInputEventArgs.Button);
                            InputDriver.Click(e.Button);
                            Log.Debug("Click done");
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                    InputDriver.Instance.CallNext = false;
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
        private void onRecord(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
            designer.ReadOnly = true;
            designer.lastinserted = null;
            designer.lastinsertedmodel = null;
            StopDetectorPlugins();
            InputDriver.Instance.OnKeyDown += OnKeyDown;
            InputDriver.Instance.OnKeyUp += OnKeyUp;
            StartRecordPlugins();
            InputDriver.Instance.CallNext = false;
            if(this.Minimize) GenericTools.minimize(GenericTools.mainWindow);
        }
        private async void WebSocketClient_OnClose(string reason)
        {
            Log.Information("Disconnected " + reason);
            SetStatus("Disconnected from " + Config.local.wsurl + " reason " + reason);
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
            Log.Information("Detector " + plugin.Entity.name + " was triggered, with id " + plugin.Entity._id);
            foreach (var wi in WorkflowInstance.Instances)
            {
                if (wi.isCompleted) continue;
                if(wi.Bookmarks != null)
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
            RobotCommand command = new RobotCommand();
            detector.user = global.webSocketClient.user;
            var data = JObject.FromObject(detector);
            var Entity = (plugin.Entity as Interfaces.entity.Detector);
            command.command = "detector";
            command.detectorid = Entity._id;
            if (string.IsNullOrEmpty(Entity._id)) return;
            command.data = data;
            _ = global.webSocketClient.QueueMessage(Entity._id, command, null);

        }
        public Workflow GetWorkflowById(string id)
        {
            foreach (var p in Projects)
            {
                foreach (var wf in p.Workflows)
                {
                    if (wf._id == id) return wf;
                }
            }
            return null;
        }
        public Views.WFDesigner GetDesignerById(string workflowid)
        {
            foreach(var designer in designers)
            {
                if (designer.Workflow._id == workflowid) return designer;
            }
            return null;
        }
        private async void WebSocketClient_OnQueueMessage(QueueMessage message, QueueMessageEventArgs e)
        {
            RobotCommand command = null;
            try
            {
                command = Newtonsoft.Json.JsonConvert.DeserializeObject<RobotCommand>(message.data.ToString());
                if (command.data == null)
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
                                    if(!string.IsNullOrEmpty(message.error))
                                    {
                                        wi.Abort(message.error);
                                    } else
                                    {
                                        wi.ResumeBookmark(b.Key, message.data.ToString());
                                    }
                                    
                                }
                            }
                        }
                    }
                }
                JObject data;
                if(command.data != null) { data = JObject.Parse(command.data.ToString()); } else { data = JObject.Parse("{}"); }
                if (command.command == null) return;
                if (command.command == "invoke" && !string.IsNullOrEmpty(command.workflowid))
                {
                    WorkflowInstance instance = null;
                    var workflow = GetWorkflowById(command.workflowid);
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
                            var designer = GetDesignerById(command.workflowid);
                            if (designer != null)
                            {
                                instance = workflow.CreateInstance(param, message.replyto, message.correlationId, designer.onIdle, designer.onVisualTracking);
                                designer.Run(VisualTracking, SlowMotion, instance);
                            }
                            else
                            {
                                instance = workflow.CreateInstance(param, message.replyto, message.correlationId, idleOrComplete, null);
                                instance.Run();
                            }
                        });
                        command.command = "invokesuccess";
                    }
                }
            }
            catch (Exception ex)
            {
                command = new RobotCommand();
                command.command = "error";
                command.data = JObject.FromObject(ex);
            }
            // string data = Newtonsoft.Json.JsonConvert.SerializeObject(command);
            if (!string.IsNullOrEmpty(message.replyto) && message.replyto != message.queuename)
            {
                await global.webSocketClient.QueueMessage(message.replyto, command, message.correlationId);
            }
        }
        public void idleOrComplete(WorkflowInstance instance, EventArgs e)
        {
            if (!string.IsNullOrEmpty(instance.queuename) && !string.IsNullOrEmpty(instance.correlationId))
            {
                RobotCommand command = new RobotCommand();
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
                view = new Views.KeyboardSeqWindow();
                view.oneKeyOnly = true;
                view.Title = "Press New Cancel Key";
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
            string path = System.IO.Path.Combine(Extensions.projectsDirectory, "tessdata");
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

    }
}
