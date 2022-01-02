using Microsoft.VisualBasic.Activities;
using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using OpenRPA.Net;
using OpenRPA.Views;
using System;
using System.Activities;
using System.Activities.Core.Presentation;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class MainWindow : Window, INotifyPropertyChanged, IMainWindow
    {
        System.Timers.Timer statetimer = null;
        public MainWindow()
        {
            statetimer = new System.Timers.Timer(200);
            statetimer.Elapsed += Statetimer_Elapsed;
            statetimer.Start();

            Log.FunctionIndent("MainWindow", "MainWindow");
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
            SetStatus("Initializing events");
            DataContext = this;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            AppDomain currentDomain = AppDomain.CurrentDomain;
            System.Windows.Forms.Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            System.Diagnostics.Trace.Listeners.Add(Tracing);
            //Console.SetOut(new DebugTextWriter());
            Console.SetOut(new ConsoleDecorator(Console.Out));
            Console.SetError(new ConsoleDecorator(Console.Out, true));
            lvDataBinding.ItemsSource = Plugins.recordPlugins;
            cancelkey.Text = Config.local.cancelkey;
            InputDriver.Instance.onCancel += OnCancel;
            NotifyPropertyChanged("Toolbox");
            lblVersion.Text = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
            Log.FunctionOutdent("MainWindow", "MainWindow");
            instance = this;
        }
        private void Statetimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            NotifyPropertyChanged("wsstate");
            NotifyPropertyChanged("wsmsgqueue");
        }
        public bool IsOnScreen(System.Drawing.Point pos)
        {
            var screens = System.Windows.Forms.Screen.AllScreens;
            foreach (var screen in screens)
            {
                if (screen.WorkingArea.Contains(pos))
                {
                    return true;
                }
            }
            return false;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Log.FunctionIndent("MainWindow", "Window_Loaded");
            try
            {
                SetStatus("Registering Designer Metadata");
                new DesignerMetadata().Register();
                var pos = Config.local.mainwindow_position;
                if (pos.Left > 0 && pos.Top > 0 && pos.Width > 100 && pos.Height > 100)
                {
                    if (IsOnScreen(new System.Drawing.Point(pos.X, pos.Y)))
                    {
                        Left = pos.Left;
                        Top = pos.Top;
                        Width = pos.Width;
                        Height = pos.Height;
                    }
                }
                SetStatus("loading workflow toolbox");
                Toolbox = new Views.WFToolbox();
                NotifyPropertyChanged("Toolbox");
                SetStatus("loading Snippets toolbox");
                Snippets = new Views.Snippets();
                NotifyPropertyChanged("Snippets");
                // OnOpen(null);
                AddHotKeys();
                LoadLayout();
                if (string.IsNullOrEmpty(Config.local.wsurl))
                {
                    try
                    {
                        ReadyForAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            Log.FunctionOutdent("MainWindow", "Window_Loaded");
        }
        internal static MainWindow instance;
        private bool first_connect = true;
        public void MainWindow_WebSocketClient_OnOpen()
        {
            Log.FunctionIndent("MainWindow", "MainWindow_WebSocketClient_OnOpen");
            if (first_connect)
            {
                GenericTools.RunUI(() =>
                {

                    {
                        try
                        {
                            SetStatus("Load layout and reopen workflows");
                            first_connect = false;
                            LoadLayout();

                            if (Config.local.show_getting_started)
                            {
                                var url = Config.local.getting_started_url;
                                if (string.IsNullOrEmpty(url)) url = "https://skadefro.github.io/openrpa.dk/gettingstarted.html";
                                if (global.openflowconfig != null && !string.IsNullOrEmpty(global.openflowconfig.getting_started_url)) url = global.openflowconfig.getting_started_url;
                                LayoutDocument layoutDocument = new LayoutDocument { Title = "Getting started" };
                                layoutDocument.ContentId = "GettingStarted";
                                // Views.GettingStarted view = new Views.GettingStarted(url + "://" + u.Host + "/gettingstarted.html");
                                Views.GettingStarted view = new Views.GettingStarted(url);
                                layoutDocument.Content = view;
                                MainTabControl.Children.Add(layoutDocument);
                                layoutDocument.IsSelected = true;
                                layoutDocument.Closing += LayoutDocument_Closing;
                            }

                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                    try
                    {
                        ReadyForAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
            Log.FunctionOutdent("MainWindow", "MainWindow_WebSocketClient_OnOpen");
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public event StatusEventHandler Status;
        public event ReadyForActionEventHandler ReadyForAction;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        private bool _IsLoading = false;
        public bool IsLoading { get { return _IsLoading; } set { _IsLoading = value; NotifyPropertyChanged("IsLoading"); } }

        private bool isRecording = false;
        public Tracing Tracing { get; set; } = new Tracing();
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
        public class uilocal
        {
            public uilocal(string Name, string Value)
            {
                this.Name = Name;
                this.Value = Value;
            }
            public string Name { get; set; }
            public string Value { get; set; }
        }
        // private ObservableCollection<string> _uilocals = null;
        private readonly ObservableCollection<uilocal> _uilocals = new ObservableCollection<uilocal>();
        public ObservableCollection<uilocal> uilocals
        {
            get
            {
                if (_uilocals.Count == 0)
                {
                    var cultures = Interfaces.Extensions.GetAvailableCultures(typeof(OpenRPA.Resources.strings));
                    _uilocals.Add(new uilocal("English (English [en])", "en"));
                    foreach (System.Globalization.CultureInfo culture in cultures)
                        _uilocals.Add(new uilocal(culture.NativeName + " (" + culture.EnglishName + " [" + culture.TwoLetterISOLanguageName + "])", culture.TwoLetterISOLanguageName));
                }
                return _uilocals;
            }
            set { }
        }
        private bool SkipLayoutSaving = false;
        public IDesigner[] Designers
        {
            get
            {
                if (DManager == null) return new Views.WFDesigner[] { };
                var result = new List<Views.WFDesigner>();
                try
                {
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
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return result.ToArray();
            }
        }
        public Views.WFDesigner Designer
        {
            get
            {
                if (SelectedContent is Views.WFDesigner view)
                {
                    _LastDesigner = view;
                    return view;
                }
                return null;
            }
            set
            {

            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        IDesigner IMainWindow.Designer { get => Designer; }
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
        IDesigner IMainWindow.LastDesigner { get => LastDesigner; }
        public uilocal defaultuilocal
        {
            get
            {
                var item = uilocals.Where(x => x.Value == Config.local.culture).FirstOrDefault();

                var current = System.Globalization.CultureInfo.CurrentCulture;
                if (item == null) item = uilocals.Where(x => x.Value == current.TwoLetterISOLanguageName).FirstOrDefault();
                if (item == null) item = uilocals.Where(x => x.Value == "en").FirstOrDefault();
                return item;
            }
            set
            {
                var current = System.Globalization.CultureInfo.CurrentCulture;
                if (string.IsNullOrEmpty(Config.local.culture)) Config.local.culture = current.TwoLetterISOLanguageName;
                if (value != null && !string.IsNullOrEmpty(value.Value) && value.Value != Config.local.culture)
                {
                    Config.local.culture = value.Value;
                    Config.Save();
                    try
                    {
                        if (System.IO.File.Exists(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layout.config")))
                        {
                            System.IO.File.Delete(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layout.config"));
                        }
                        SkipLayoutSaving = true;
                        //System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(Config.local.culture);
                        //InitializeComponent();
                        MessageBox.Show("Please restart the robot for the change to take fully effect");
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
        public Views.WFToolbox Toolbox { get; set; }
        public Views.Snippets Snippets { get; set; }
        private async void LayoutDocument_Closing(object sender, CancelEventArgs e)
        {
            Log.FunctionIndent("MainWindow", "LayoutDocument_Closing");
            try
            {
                var tab = sender as LayoutDocument;
                if (!(tab.Content is Views.WFDesigner designer))
                {
                    Log.FunctionOutdent("MainWindow", "not Views.WFDesigner");
                    return;
                }
                if (!designer.HasChanged)
                {
                    Log.FunctionOutdent("MainWindow", "designer.HasChanged is false");
                    return;
                }

                if (designer.HasChanged && (global.isConnected ? designer.Workflow.hasRight(global.webSocketClient.user, ace_right.update) : true))
                {
                    e.Cancel = true;
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Save " + designer.Workflow.name + " ?", "Workflow unsaved", MessageBoxButton.YesNoCancel);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        designer.Workflow.current_version = designer.Workflow._version;
                        var res = await designer.SaveAsync();
                        if (res)
                        {
                            var doc = sender as LayoutDocument;
                            doc.Close();
                        }
                    }
                    else if (messageBoxResult == MessageBoxResult.No)
                    {
                        e.Cancel = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "LayoutDocument_Closing");
        }
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.FunctionIndent("MainWindow", "Window_Closing");
            try
            {
                RobotInstance.instance.AutoReloading = false;
                bool AllowQuite = true;
                foreach (var designer in RobotInstance.instance.Designers)
                {
                    if (designer.HasChanged)
                    {
                        e.Cancel = true;
                        MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Save " + designer.Workflow.name + " ?", "Workflow unsaved", MessageBoxButton.YesNoCancel);
                        if (messageBoxResult == MessageBoxResult.Yes)
                        {
                            designer.Workflow.current_version = designer.Workflow._version;
                            var res = await designer.SaveAsync();
                            if (!res)
                            {
                                AllowQuite = false;
                            }
                        }
                        else if (messageBoxResult != MessageBoxResult.No)
                        {
                            AllowQuite = false;
                        }
                        else
                        {
                            var d = designer as WFDesigner;
                            designer.forceHasChanged(false);
                            d.tab.Close();
                        }
                    }
                }
                Log.Information("AllowQuite: " + AllowQuite);
                if (AllowQuite && e.Cancel == false)
                {
                    foreach (var d in Plugins.detectorPlugins) d.Stop();
                    InputDriver.Instance.Dispose();
                    Log.FunctionOutdent("MainWindow", "AllowQuite true");
                    return;
                }
                if (AllowQuite)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                    this.Close();
                }
                else
                {
                    RobotInstance.instance.AutoReloading = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "Window_Closing");
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Log.FunctionIndent("MainWindow", "Window_Closed");
            InputDriver.Instance.CallNext = true;
            InputDriver.Instance.OnKeyDown -= OnKeyDown;
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
            InputDriver.Instance.Dispose();
            StopDetectorPlugins();
            SaveLayout();
            if (RobotInstance.instance.db != null)
            {
                RobotInstance.instance.db.Dispose();
                RobotInstance.instance.db = null;
            }
            // automation threads will not allways abort, and mousemove hook will "hang" the application for several seconds
            Application.Current.Shutdown();
            Log.FunctionOutdent("MainWindow", "Window_Closed");
        }
        private void Window_StateChanged(object sender, EventArgs e)
        {
            Log.FunctionIndent("MainWindow", "Window_StateChanged");
            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Hidden;
                App.notifyIcon.Visible = true;
            }
            else
            {
                Visibility = Visibility.Visible;
                // App.notifyIcon.Visible = false;
            }
            Log.FunctionOutdent("MainWindow", "Window_StateChanged");
        }
        public void SetStatus(string message)
        {
            Log.FunctionIndent("MainWindow", "SetStatus");
            try
            {
                Status?.Invoke(message);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            try
            {
                AutomationHelper.syncContext.Post(o =>
                {
                    try
                    {
                        LabelStatusBar.Content = message;
                    }
                    catch (Exception)
                    {
                    }
                }, null);
            }
            catch (Exception)
            {
            }
            Log.FunctionOutdent("MainWindow", "SetStatus");
        }
        private void DManager_ActiveContentChanged(object sender, EventArgs e)
        {
            NotifyPropertyChanged("VisualTracking");
            NotifyPropertyChanged("SlowMotion");
            NotifyPropertyChanged("Minimize");
            NotifyPropertyChanged("SelectedContent");
            NotifyPropertyChanged("CurrentWorkflow");
            NotifyPropertyChanged("LastDesigner");
        }
        public object SelectedContent
        {
            get
            {
                if (DManager == null) return null;
                var b = DManager.ActiveContent;
                return b;
            }
        }
        public Workflow CurrentWorkflow
        {
            get
            {
                Workflow workflow = null;
                if (SelectedContent is Views.WFDesigner wfview)
                {
                    workflow = wfview.Workflow;
                }
                if (SelectedContent is Views.OpenProject opview)
                {
                    var wf = opview.listWorkflows.SelectedValue as Workflow;
                    workflow = wf;
                }
                return workflow;
            }
            set
            {

            }
        }
        public LayoutDocumentPane MainTabControl
        {
            get
            {
                try
                {
                    if (DManager == null) return null;
                    if (DManager.Layout == null) return null;
                    var documentPane = DManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                    return documentPane;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return null;
                }
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
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
                NotifyPropertyChanged("Setting_log_debug");
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
                NotifyPropertyChanged("Setting_log_warning");
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
                NotifyPropertyChanged("Setting_log_verbose");
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
                NotifyPropertyChanged("Setting_log_selector");
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
                NotifyPropertyChanged("Setting_log_selector_verbose");
            }
        }
        public bool Setting_log_network
        {
            get
            {
                return Config.local.log_network;
            }
            set
            {
                Config.local.log_network = value;
                NotifyPropertyChanged("Setting_log_network");
            }
        }
        public bool Setting_log_activity
        {
            get
            {
                return Config.local.log_activity;
            }
            set
            {
                Config.local.log_activity = value;
                NotifyPropertyChanged("Setting_log_activity");
            }
        }
        public bool Setting_log_output
        {
            get
            {
                return Config.local.log_output;
            }
            set
            {
                Config.local.log_output = value;
                NotifyPropertyChanged("Setting_log_output");
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
        public bool isRunningInChildSession()
        {
            var CurrentP = System.Diagnostics.Process.GetCurrentProcess();
            var mywinstation = UserLogins.QuerySessionInformation(CurrentP.SessionId, UserLogins.WTS_INFO_CLASS.WTSWinStationName);
            if (string.IsNullOrEmpty(mywinstation)) mywinstation = "";
            mywinstation = mywinstation.ToLower();
            if (!mywinstation.Contains("rdp") && mywinstation != "console" && !mywinstation.Contains("#0"))
            {
                return true;
            }
            return false;

        }
        public bool Setting_ShowChildSessions
        {
            get
            {
                if (isRunningInChildSession())
                {
                    return false;
                }
                return Setting_IsChildSessionsEnabled;
            }
            set
            {

            }
        }
        public bool Setting_IsChildSessionsEnabled
        {
            get
            {
                return Interfaces.win32.ChildSession.IsChildSessionsEnabled();
            }
            set
            {
                var state = (bool)value;
                try
                {
                    if (Interfaces.win32.ChildSession.IsChildSessionsEnabled())
                    {
                        if (state == false) Interfaces.win32.ChildSession.DisableChildSessions();
                    }
                    else
                    {
                        if (state == true)
                        {
                            var messageBoxResult = MessageBox.Show("Enable ChildSessions ?\nYou will be prompted for username and password until you have restarted your computer\nYou can only enabled child session if you ran the robot with administrator rights!", "Enable ChildSessions", MessageBoxButton.YesNo);
                            if (messageBoxResult == MessageBoxResult.Yes)
                            {
                                Interfaces.win32.ChildSession.EnableChildSessions();
                                MessageBox.Show("Child sessions enabled, you may need to reboot for this to work.\nIf you do not reboot now, you may be prompted for username and password\n the first time you start the child session");
                            }
                        }
                    }
                    Config.Save();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show(ex.Message);
                }
                //Config.local.recording_add_to_designer = value;
                NotifyPropertyChanged("Setting_IsChildSessionsEnabled");
                NotifyPropertyChanged("Setting_ShowChildSessions");
            }
        }
        public string wsstate
        {
            get
            {
                if (string.IsNullOrEmpty(Config.local.wsurl)) return "";
                if (global.webSocketClient == null) return "null";
                if (global.webSocketClient.ws == null) return "ws null";
                return global.webSocketClient.ws.State.ToString();
            }
        }
        public string wsmsgqueue
        {
            get
            {
                if (string.IsNullOrEmpty(Config.local.wsurl)) return "";
                if (global.webSocketClient == null) return "";
                if (global.webSocketClient.ws == null) return "";
                if (global.webSocketClient.MessageQueueSize == 0) return "";
                return "(" + global.webSocketClient.MessageQueueSize.ToString() + ")";
            }
        }
        public ICommand PlayInChildCommand { get { return new RelayCommand<object>(OnPlayInChild, CanPlayInChild); } }
        public ICommand LoggingOptionCommand { get { return new RelayCommand<object>(OnLoggingOptionCommand, CanAllways); } }
        public ICommand ChildSessionCommand { get { return new RelayCommand<object>(OnChildSessionCommand, CanAllways); } }
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
        public ICommand ReloadCommand { get { return new RelayCommand<object>(OnReload, CanReload); } }
        public ICommand LinkOpenFlowCommand { get { return new RelayCommand<object>(OnlinkOpenFlow, CanlinkOpenFlow); } }
        public ICommand LinkNodeREDCommand { get { return new RelayCommand<object>(OnlinkNodeRED, CanlinkNodeRED); } }
        public ICommand OpenChromePageCommand { get { return new RelayCommand<object>(OnOpenChromePage, CanAllways); } }
        public ICommand OpenFirefoxPageCommand { get { return new RelayCommand<object>(OnOpenFirefoxPageCommand, CanAllways); } }
        public ICommand OpenEdgePageCommand { get { return new RelayCommand<object>(OnOpenEdgePageCommand, CanAllways); } }
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
            Log.FunctionIndent("MainWindow", "SwapSendKeys");
            try
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "SwapSendKeys");
        }
        private void OnSwapSendKeys(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
            if (designer.SelectedActivity == null) return;
            SwapSendKeys(designer, designer.SelectedActivity);


        }
        public ICommand SwapVirtualClickCommand { get { return new RelayCommand<object>(OnSwapVirtualClick, CanSwapVirtualClick); } }
        public ICommand SwapAnimateCommand { get { return new RelayCommand<object>(OnSwapAnimate, CanSwapAnimate); } }
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
            Log.FunctionIndent("MainWindow", "SwapVirtualClick");
            try
            {
                if (model.ItemType == typeof(Activities.ClickElement))
                {

                    var VirtualClick = model.GetValue<bool>("VirtualClick");
                    if (VirtualClick)
                    {
                        var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                        using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                        {
                            model.Properties["VirtualClick"].ComputedValue = new InArgument<bool>() { Expression = new VisualBasicValue<bool>("false") };
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "SwapVirtualClick");
        }
        private void OnSwapVirtualClick(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
            if (designer.SelectedActivity == null) return;
            SwapVirtualClick(designer, designer.SelectedActivity);


        }
        private bool CanSwapAnimate(object _item)
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
        private void OnSwapAnimate(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)SelectedContent;
            if (designer.SelectedActivity == null) return;
            SwapAnimate(designer, designer.SelectedActivity);
        }
        private void SwapAnimate(Views.WFDesigner designer, System.Activities.Presentation.Model.ModelItem model)
        {
            Log.FunctionIndent("MainWindow", "SwapAnimate");
            try
            {
                if (model.ItemType == typeof(Activities.ClickElement))
                {
                    var AnimateMouse = model.GetValue<bool>("AnimateMouse");
                    if (AnimateMouse)
                    {
                        var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                        using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                        {
                            model.Properties["AnimateMouse"].ComputedValue = new InArgument<bool>() { Expression = new VisualBasicValue<bool>("false") };
                            editingScope.Complete();
                        }
                    }
                    else
                    {
                        var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                        using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                        {
                            model.Properties["AnimateMouse"].ComputedValue = new InArgument<bool>() { Expression = new VisualBasicValue<bool>("true") };
                            editingScope.Complete();
                        }

                    }
                }
                if (model.ItemType == typeof(Activities.OpenApplication))
                {
                    var AnimateMove = model.GetValue<bool>("AnimateMove");
                    if (AnimateMove)
                    {
                        var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                        using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                        {
                            model.Properties["AnimateMove"].ComputedValue = new InArgument<bool>() { Expression = new VisualBasicValue<bool>("false") };
                            editingScope.Complete();
                        }
                    }
                    else
                    {
                        var modelService = designer.WorkflowDesigner.Context.Services.GetService<System.Activities.Presentation.Services.ModelService>();
                        using (var editingScope = modelService.Root.BeginEdit("Implementation"))
                        {
                            model.Properties["AnimateMove"].ComputedValue = new InArgument<bool>() { Expression = new VisualBasicValue<bool>("true") };
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
                            if (model.Properties[a.Name].Value is System.Activities.Presentation.Model.ModelItem _a) SwapAnimate(designer, _a);
                        }

                    }

                }
                if (Activities != null)
                {
                    foreach (var a in Activities)
                    {
                        SwapAnimate(designer, a);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "SwapAnimate");
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
        private void OnChildSessionCommand(object _item)
        {
        }
        private bool CanPermissions(object _item)
        {
            try
            {
                // if (!IsConnected) return false;
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
                if (val is Workflow wf) { result = wf.Project() as Project; } // Avoid confusion, disallow setting permission on workflows
                if (val is Project p) { result = p; }
                if (val is Detector d) { result = RobotInstance.instance.Projects.FindById(d.projectid); }
            }
            Log.FunctionIndent("MainWindow", "OnPermissions");
            if (SelectedContent is Views.WFDesigner designer)
            {
                result = designer.Workflow;
            }
            var DetectorsView = SelectedContent as Views.DetectorsView;
            if (DetectorsView != null)
            {
                if (!(DetectorsView.lidtDetectors.SelectedItem is IDetectorPlugin detector)) return;
                result = detector.Entity as apibase;
            }
            if (result == null)
            {
                Log.FunctionOutdent("MainWindow", "OnPermissions");
                return;
            }
            List<ace> orgAcl = new List<ace>();
            try
            {
                result._acl.ForEach((a) => { if (a != null) orgAcl.Add(new ace(a)); });
                Log.Function("MainWindow", "OnPermissions", "Create and show Views.PermissionsWindow");
                var pw = new Views.PermissionsWindow(result);
                Hide();
                pw.Owner = GenericTools.MainWindow;
                pw.ShowDialog();
                if (result is Project p)
                {
                    p.isDirty = true;
                    Log.Function("MainWindow", "OnPermissions", "Update permissions on each workflow in project");
                    if (p.Workflows.Count == 0) p.UpdateWorkflowsList();
                    foreach (Workflow _wf in p.Workflows)
                    {
                        _wf._acl = p._acl;
                        _wf.isDirty = true;
                        await ((Workflow)_wf).UpdateImagePermissions();
                    }
                    if (p.Detectors.Count == 0) p.UpdateDetectorsList();
                    foreach (Detector _wf in p.Detectors)
                    {
                        _wf._acl = p._acl;
                        _wf.isDirty = true;
                    }
                    await p.Save();
                }
                Log.Function("MainWindow", "OnPermissions", "save Entity");
                if (result is Workflow wf)
                {
                    wf.isDirty = true;
                    await wf.Save();
                    await wf.UpdateImagePermissions();
                }
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
            Log.FunctionOutdent("MainWindow", "OnPermissions");
        }
        private bool CanReload(object _item)
        {
            return true;
        }
        private void OnReload(object _item)
        {
            Log.Function("MainWindow", "OnReload");
            if (!global.isConnected)
            {
                _ = global.webSocketClient.Connect();
            }
            else
            {
                _ = RobotInstance.instance.LoadServerData();
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
            Log.FunctionIndent("MainWindow", "OnImport");
            try
            {
                Views.WFDesigner designer = SelectedContent as Views.WFDesigner;
                Views.OpenProject op = SelectedContent as Views.OpenProject;
                Workflow wf = null;
                Project p = null;
                Detector d = null;
                string filename = null;
                if (SelectedContent is Views.OpenProject)
                {
                    wf = op.listWorkflows.SelectedItem as Workflow;
                    p = op.listWorkflows.SelectedItem as Project;
                    d = op.listWorkflows.SelectedItem as Detector;
                    if (wf != null) p = wf.Project() as Project;
                    if (d != null)
                    {
                        p = RobotInstance.instance.Projects.FindById(d.projectid);
                    }
                }
                else if (SelectedContent is Views.WFDesigner)
                {
                    wf = designer.Workflow;
                    p = wf.Project() as Project;
                }
                var dialogOpen = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open Workflow",
                    Filter = "OpenRPA Project (.rpaproj)|*.rpaproj"
                };
                if (wf != null || p != null) dialogOpen.Filter = "Workflows (.xaml)|*.xaml|Detector (.json)|*.json|OpenRPA Project (.rpaproj)|*.rpaproj";
                if (dialogOpen.ShowDialog() == true) filename = dialogOpen.FileName;
                if (string.IsNullOrEmpty(filename)) return;
                if (System.IO.Path.GetExtension(filename) == ".xaml")
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(dialogOpen.FileName);
                    Workflow workflow = await Workflow.Create(p, name);
                    workflow._acl = p._acl;
                    workflow.Xaml = System.IO.File.ReadAllText(dialogOpen.FileName);
                    workflow.isDirty = true;
                    _onOpenWorkflow(workflow, true);
                    return;
                }
                if (System.IO.Path.GetExtension(filename) == ".json")
                {
                    var json = System.IO.File.ReadAllText(dialogOpen.FileName);
                    Detector _d = Newtonsoft.Json.JsonConvert.DeserializeObject<Detector>(json);
                    _d._acl = p._acl;
                    var exists = RobotInstance.instance.Detectors.FindById(_d._id);
                    if (exists != null) { _d._id = null; } else { _d.isLocalOnly = true; }
                    _d.isDirty = true;
                    _d.projectid = p._id;
                    await _d.Save();
                    _d.Start();
                    OpenProject.UpdateProjectsList(false, true);
                    return;
                }
                if (System.IO.Path.GetExtension(filename) == ".rpaproj")
                {
                    var project = await Project.FromFile(filename);
                    project.isDirty = true;
                    project._id = null;
                    await project.Save();
                    IWorkflow workflow = project.Workflows.First();
                    workflow.projectid = project._id;
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                    OnOpenWorkflow(workflow);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
            Log.FunctionOutdent("MainWindow", "OnImport");
        }
        internal bool CanExport(object _item)
        {
            try
            {
                // if (!IsConnected) return false;
                if (SelectedContent is Views.WFDesigner designer)
                {
                    return true;
                }
                if (SelectedContent is Views.OpenProject open)
                {
                    var val = open.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (open.listWorkflows.SelectedValue is Workflow wf)
                    {
                        //if (wf.Project == null) return true;
                        //return !wf.Project.disable_local_caching;
                        return true;
                    }
                    if (open.listWorkflows.SelectedValue is Project p)
                    {
                        return !p.disable_local_caching;
                    }
                    if (open.listWorkflows.SelectedValue is Detector d)
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
        internal async void OnExport(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnExport");
            try
            {
                if (SelectedContent is Views.WFDesigner designer)
                {
                    designer.WorkflowDesigner.Flush();
                    designer.Workflow.Xaml = designer.WorkflowDesigner.Text;
                    var dialogSave = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = "Save Workflow",
                        Filter = "Workflows (.xaml)|*.xaml",
                        FileName = designer.Workflow.name + ".xaml"
                    };
                    if (dialogSave.ShowDialog() == true)
                    {
                        await designer.Workflow.ExportFile(dialogSave.FileName);
                    }
                    Log.FunctionOutdent("MainWindow", "OnExport");
                    return;
                }
                if (SelectedContent is Views.OpenProject op)
                {
                    if (op.listWorkflows.SelectedItem is Project p)
                    {
                        using (var openFileDialog1 = new System.Windows.Forms.FolderBrowserDialog())
                        {
                            if (openFileDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                            {
                                Log.FunctionOutdent("MainWindow", "OnExport", "User canceled");
                                return;
                            }
                            var path = openFileDialog1.SelectedPath;
                            p.ExportProject(path);
                        }
                    }
                    if (op.listWorkflows.SelectedItem is Workflow wf)
                    {
                        var dialogSave = new Microsoft.Win32.SaveFileDialog
                        {
                            Title = "Save Workflow",
                            Filter = "Workflows (.xaml)|*.xaml",
                            FileName = wf.name + ".xaml"
                        };
                        if (dialogSave.ShowDialog() == true)
                        {
                            await wf.ExportFile(dialogSave.FileName);
                        }
                    }
                    if (op.listWorkflows.SelectedItem is Detector d)
                    {
                        var dialogSave = new Microsoft.Win32.SaveFileDialog
                        {
                            Title = "Save Detector",
                            Filter = "Detector (.json)|*.json",
                            FileName = d.name + ".json"
                        };
                        if (dialogSave.ShowDialog() == true)
                        {
                            d.ExportFile(dialogSave.FileName);
                        }
                    }
                    Log.FunctionOutdent("MainWindow", "OnExport");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "OnExport", "error?");
        }
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log.Function("MainWindow", "Application_ThreadException");
            try
            {
                Log.Error(e.Exception, "");
            }
            catch (Exception)
            {
            }
        }
        private void AddHotKeys()
        {
            Log.FunctionIndent("MainWindow", "AddHotKeys");
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
            Log.FunctionOutdent("MainWindow", "AddHotKeys");
        }
        private void OnExitApp(object _item)
        {
            Close();
        }
        private void OnSave(object sender, ExecutedRoutedEventArgs e)
        {
            SaveCommand.Execute(SelectedContent);
        }
        internal void OnDelete(object sender, ExecutedRoutedEventArgs e)
        {
            DeleteCommand.Execute(SelectedContent);
        }
        internal void OnDelete2(object sender)
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
                RobotInstance.instance.autoReconnect = true;
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

                //var hits = System.Diagnostics.Process.GetProcessesByName("OpenRPA.Updater");
                //return hits.Count() == 0;
                return true;
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
            Log.FunctionIndent("MainWindow", "OnManagePackages");
            try
            {
                var di = new System.IO.DirectoryInfo(global.CurrentDirectory);
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
                    Log.FunctionOutdent("MainWindow", "OpenRPA.Updater.exe not found");
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
            catch (Exception)
            {

                throw;
            }
            Log.FunctionOutdent("MainWindow", "OnManagePackages");
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
        public void OnOpen(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnOpen");
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                    foreach (var document in ld)
                    {
                        if (document.Content is Views.OpenProject op)
                        {
                            // document.IsSelected = true;
                            Log.FunctionOutdent("MainWindow", "OnOpen", "allready open");
                            return;
                        }
                    }
                    var view = new Views.OpenProject(this);
                    // view.onOpenProject += OnOpenProject;
                    view.onOpenWorkflow += OnOpenWorkflow;
                    view.onSelectedItemChanged += View_onSelectedItemChanged;

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
            Log.FunctionOutdent("MainWindow", "OnOpen");
        }
        private void View_onSelectedItemChanged()
        {
            NotifyPropertyChanged("CurrentWorkflow");
        }
        public async Task<Views.DetectorsView> OpenDetectors()
        {
            await _OnDetectors();
            var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
            foreach (var document in ld)
            {
                if (document.Content is Views.DetectorsView op)
                {
                    document.IsSelected = true;
                    Log.FunctionOutdent("MainWindow", "OnDetectors", "allready open");
                    return op;
                }
            }
            return null;
        }
        private async Task _OnDetectors()
        {
            bool result = false;
            Log.FunctionIndent("MainWindow", "OnDetectors");
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                    foreach (var document in ld)
                    {
                        if (document.Content is Views.DetectorsView op)
                        {
                            result = true;
                            document.IsSelected = true;
                            Log.FunctionOutdent("MainWindow", "OnDetectors", "allready open");
                            return;
                        }
                    }
                    var view = new Views.DetectorsView(this);
                    LayoutDocument layoutDocument = new LayoutDocument { Title = "Detectors" };
                    layoutDocument.ContentId = "detectors";
                    layoutDocument.Content = view;
                    MainTabControl.Children.Add(layoutDocument);
                    layoutDocument.IsSelected = true;
                    layoutDocument.IsSelectedChanged += view.LayoutDocument_IsSelectedChanged;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }, null);
            Log.FunctionOutdent("MainWindow", "OnDetectors");
            if (!result) await Task.Delay(500);
        }
        private void OnDetectors(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnDetectors");
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                    foreach (var document in ld)
                    {
                        if (document.Content is Views.DetectorsView op)
                        {
                            document.IsSelected = true;
                            Log.FunctionOutdent("MainWindow", "OnDetectors", "allready open");
                            return;
                        }
                    }
                    var view = new Views.DetectorsView(this);
                    LayoutDocument layoutDocument = new LayoutDocument { Title = "Detectors" };
                    layoutDocument.ContentId = "detectors";
                    layoutDocument.Content = view;
                    MainTabControl.Children.Add(layoutDocument);
                    layoutDocument.IsSelected = true;
                    layoutDocument.IsSelectedChanged += view.LayoutDocument_IsSelectedChanged;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }, null);
            Log.FunctionOutdent("MainWindow", "OnDetectors");
        }
        private void OnRunPlugins(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnRunPlugins");
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                    foreach (var document in ld)
                    {
                        if (document.Content is Views.RunPlugins op)
                        {
                            document.IsSelected = true;
                            Log.FunctionOutdent("MainWindow", "OnRunPlugins", "allready open");
                            return;
                        }
                    }
                    var view = new Views.RunPlugins();
                    LayoutDocument layoutDocument = new LayoutDocument { Title = "Run Plugins" };
                    layoutDocument.ContentId = "detectors";
                    layoutDocument.Content = view;
                    MainTabControl.Children.Add(layoutDocument);
                    layoutDocument.IsSelected = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }, null);
            Log.FunctionOutdent("MainWindow", "OnRunPlugins");
        }
        private void OnRecorderPluginsCommand(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnRecorderPluginsCommand");
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                    foreach (var document in ld)
                    {
                        if (document.Content is Views.RecorderPlugins op)
                        {
                            document.IsSelected = true;
                            Log.FunctionOutdent("MainWindow", "OnRecorderPluginsCommand", "allready open");
                            return;
                        }
                    }
                    var view = new Views.RecorderPlugins();
                    LayoutDocument layoutDocument = new LayoutDocument { Title = "Recorder Plugins" };
                    layoutDocument.ContentId = "detectors";
                    layoutDocument.Content = view;
                    MainTabControl.Children.Add(layoutDocument);
                    layoutDocument.IsSelected = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }, null);
            Log.FunctionOutdent("MainWindow", "OnRecorderPluginsCommand");
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
        private void SaveLayout()
        {
            Log.FunctionIndent("MainWindow", "SaveLayout");
            try
            {
                var workflows = new List<string>();
                foreach (var designer in RobotInstance.instance.Designers)
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
                var pos = new System.Drawing.Rectangle((int)Left, (int)Top, (int)Width, (int)Height);
                if (pos.Left > 0 && pos.Top > 0 && pos.Width > 100 && pos.Height > 100)
                {
                    Config.local.mainwindow_position = pos;
                }
                Config.Save();
                if (SkipLayoutSaving)
                {
                    Log.FunctionOutdent("MainWindow", "SaveLayout", "SkipLayoutSaving");
                    return;
                }
                try
                {
                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                    using (var stream = new System.IO.StreamWriter(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layout.config")))
                        serializer.Serialize(stream);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            catch (Exception)
            {

                throw;
            }
            Log.FunctionOutdent("MainWindow", "SaveLayout");
        }
        private void LoadLayout()
        {
            Log.FunctionIndent("MainWindow", "LoadLayout");
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
            Task.Run(() =>
            {
                var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                while (true && sw.Elapsed < TimeSpan.FromSeconds(10))
                {
                    System.Threading.Thread.Sleep(200);
                    if (Views.OpenProject.Instance != null && Views.OpenProject.Instance.Projects.Count > 0) break;
                }
                foreach (var id in Config.local.openworkflows)
                {
                    var wf = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(id);
                    if (wf != null) OnOpenWorkflow(wf);
                }
            });
            Log.FunctionOutdent("MainWindow", "LoadLayout");
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "IDE1006")]
        public void _onOpenWorkflow(IWorkflow workflow, bool HasChanged = false)
        {
            Log.FunctionIndent("MainWindow", "_onOpenWorkflow");
            if (RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(workflow.RelativeFilename) is Views.WFDesigner designer)
            {
                designer.tab.IsSelected = true;
                Log.FunctionOutdent("MainWindow", "_onOpenWorkflow", "Already open");
                return;
            }
            try
            {
                var types = new List<Type>();
                LayoutDocument layoutDocument = new LayoutDocument { Title = workflow.name };
                layoutDocument.ContentId = workflow._id;
                if (isRunningInChildSession())
                {
                    Log.Warning("Refuse loading workflow designer in ChildSession");
                    Log.FunctionOutdent("MainWindow", "_onOpenWorkflow");
                    return;
                }
                Views.WFDesigner view = new Views.WFDesigner(layoutDocument, workflow as Workflow, types.ToArray())
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
            Log.FunctionOutdent("MainWindow", "_onOpenWorkflow");
        }
        public void OnOpenWorkflow(IWorkflow workflow)
        {
            GenericTools.RunUI(() =>
            {
                try
                {
                    _onOpenWorkflow(workflow);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }
        private void WFDesigneronChanged(Views.WFDesigner designer)
        {
            Log.FunctionIndent("MainWindow", "WFDesigneronChanged");
            AutomationHelper.syncContext.Post(o =>
            {
                try
                {
                    designer.tab.Title = designer.HasChanged ? designer.Workflow.name + "*" : designer.Workflow.name;
                    CommandManager.InvalidateRequerySuggested();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }, null);
            Log.FunctionOutdent("MainWindow", "WFDesigneronChanged");
        }
        //public void OnOpenProject(Project project)
        //{
        //    foreach (var wf in project.Workflows)
        //    {
        //        OnOpenWorkflow(wf);
        //    }
        //}
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
            Log.FunctionIndent("MainWindow", "OnSave");
            try
            {
                if (SelectedContent is Views.WFDesigner designer)
                {
                    await designer.SaveAsync();
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
            Log.FunctionOutdent("MainWindow", "OnSave");
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
                    var d = val as Detector;
                    if (wf != null || p != null || d != null) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnNewWorkflow(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnNewWorkflow");
            try
            {
                if (SelectedContent is Views.WFDesigner designer)
                {
                    Workflow workflow = await Workflow.Create(designer.Workflow.Project(), "New Workflow");
                    OnOpenWorkflow(workflow);
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                    Log.FunctionOutdent("MainWindow", "OnNewWorkflow", "Designer selected");
                    return;
                }
                if (!(SelectedContent is Views.OpenProject view)) return;
                var val = view.listWorkflows.SelectedValue;
                if (val is Detector d)
                {
                    var project = RobotInstance.instance.Projects.FindById(d.projectid);
                    Workflow workflow = await Workflow.Create(project, "New Workflow");
                    OnOpenWorkflow(workflow);
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                    Log.FunctionOutdent("MainWindow", "OnNewWorkflow", "Workflow selected");
                    return;
                }
                if (val is Workflow wf)
                {
                    Workflow workflow = await Workflow.Create(wf.Project(), "New Workflow");
                    OnOpenWorkflow(workflow);
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                    Log.FunctionOutdent("MainWindow", "OnNewWorkflow", "Workflow selected");
                    return;
                }
                if (val is Project p)
                {
                    Workflow workflow = await Workflow.Create(p, "New Workflow");
                    OnOpenWorkflow(workflow);
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                    Log.FunctionOutdent("MainWindow", "OnNewWorkflow", "Project selected");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
            Log.FunctionOutdent("MainWindow", "OnNewWorkflow");
        }
        private bool CanNewProject(object _item)
        {
            try
            {

                // if (!IsConnected) return false; 
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        private async void OnNewProject(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnNewProject");
            try
            {

                string Name = Project.UniqueName("New project", null);
                Name = Microsoft.VisualBasic.Interaction.InputBox("Name?", "Name project", Name);
                if (string.IsNullOrEmpty(Name))
                {
                    Log.FunctionOutdent("MainWindow", "OnNewProject", "Name is null");
                    return;
                }
                Project project = await Project.Create(Interfaces.Extensions.ProjectsDirectory, Name);
                IWorkflow workflow = await project.AddDefaultWorkflow();
                RobotInstance.instance.NotifyPropertyChanged("Projects");
                OnOpenWorkflow(workflow);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
            Log.FunctionOutdent("MainWindow", "OnNewProject");
        }
        internal bool CanCopy(object _item)
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
        internal async void OnCopy(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnCopy");
            try
            {
                var designer = (Views.WFDesigner)SelectedContent;
                await designer.SaveAsync();
                Workflow workflow = await Workflow.Create(designer.Workflow.Project(), "Copy of " + designer.Workflow.name);
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
            Log.FunctionOutdent("MainWindow", "OnCopy");
        }
        internal bool CanDelete(object _item)
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
                    if (val is Detector d)
                    {
                        if (!d.hasRight(global.webSocketClient.user, ace_right.delete)) return false;
                        return true;
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
            Log.FunctionIndent("MainWindow", "OnDelete");
            try
            {
                if (!(SelectedContent is Views.OpenProject view))
                {
                    Log.FunctionOutdent("MainWindow", "OnDelete", "OpenProject not selected");
                    return;
                }
                var val = view.listWorkflows.SelectedValue;
                if (val is Workflow wf)
                {
                    if (RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(wf.IDOrRelativeFilename) is Views.WFDesigner designer) { designer.tab.Close(); }
                    var messageBoxResult = MessageBox.Show("Delete " + wf.name + " ?", "Delete Confirmation", MessageBoxButton.YesNo);
                    if (messageBoxResult != MessageBoxResult.Yes)
                    {
                        Log.FunctionOutdent("MainWindow", "OnDelete", "User canceled");
                        return;
                    }
                    await wf.Delete();
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                }
                if (val is Detector d)
                {
                    var messageBoxResult = MessageBox.Show("Delete " + d.name + " ?", "Delete Confirmation", MessageBoxButton.YesNo);
                    if (messageBoxResult != MessageBoxResult.Yes)
                    {
                        Log.FunctionOutdent("MainWindow", "OnDelete", "User canceled");
                        return;
                    }
                    d.Stop();
                    await d.Delete();
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                }
                if (val is Project p)
                {
                    if (p.Workflows.Count > 0)
                    {
                        var messageBoxResult = MessageBox.Show("Delete project " + p.name + " containing " + p.Workflows.Count() + " workflows", "Delete Confirmation", MessageBoxButton.YesNo);
                        if (messageBoxResult != MessageBoxResult.Yes)
                        {
                            Log.FunctionOutdent("MainWindow", "OnDelete", "User canceled");
                            return;
                        }
                        foreach (var _wf in p.Workflows.ToList())
                        {
                            var designer = RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(_wf.RelativeFilename) as Views.WFDesigner;
                            if (designer == null && !string.IsNullOrEmpty(_wf._id)) { }
                            if (designer != null) { designer.tab.Close(); }
                            await _wf.Delete();
                        }
                    }
                    await p.Delete();
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
            Log.FunctionOutdent("MainWindow", "OnDelete");
        }
        internal bool CanPlay(object _item)
        {
            try
            {
                if (SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (!(view.listWorkflows.SelectedValue is Workflow wf)) return false;
                    if (wf.State == "running" || wf.State == "idle") return false;
                    if (global.isConnected)
                    {
                        return wf.hasRight(global.webSocketClient.user, ace_right.invoke);
                    }
                    return true;
                }

                //if (!IsConnected) return false;
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
        internal async void OnPlay(object _item)
        {
            Log.FunctionIndent("MainWindow", "OnPlay");
            string errormessage = "";
            if (SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null)
                {
                    Log.FunctionOutdent("MainWindow", "OnPlay", "SelectedValue is null");
                    return;
                }
                if (!(view.listWorkflows.SelectedValue is Workflow workflow))
                {
                    Log.FunctionOutdent("MainWindow", "OnPlay", "SelectedValue is not workflow");
                    return;
                }
                try
                {
                    if (this.Minimize) GenericTools.Minimize();
                    IWorkflowInstance instance;
                    var param = new Dictionary<string, object>();
                    if (RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(workflow.IDOrRelativeFilename) is Views.WFDesigner designer)
                    {
                        designer.BreakpointLocations = null;
                        instance = workflow.CreateInstance(param, null, null, new idleOrComplete(designer.IdleOrComplete), designer.OnVisualTracking);
                        designer.SetDebugLocation(null);
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
                    errormessage = ex.Message;
                    Log.Error(ex.ToString());
                }
                if (Config.local.notify_on_workflow_end && !string.IsNullOrEmpty(errormessage))
                {
                    App.notifyIcon.ShowBalloonTip(1000, "", errormessage, System.Windows.Forms.ToolTipIcon.Error);
                    GenericTools.Restore();
                }
                else if (!string.IsNullOrEmpty(errormessage))
                {
                    GenericTools.Restore();
                    // MessageBox.Show("onPlay " + errormessage);
                }
                Log.FunctionOutdent("MainWindow", "OnPlay");
                return;
            }
            try
            {
                if (!(SelectedContent is Views.WFDesigner))
                {
                    Log.FunctionOutdent("MainWindow", "OnPlay", "Selected content is not WFDesigner");
                    return;
                }
                var designer = (Views.WFDesigner)SelectedContent;
                if (designer.HasChanged) { await designer.SaveAsync(); }
                designer.Run(VisualTracking, SlowMotion, null);
            }
            catch (Exception ex)
            {
                errormessage = ex.Message;
            }
            if (Config.local.notify_on_workflow_end && !string.IsNullOrEmpty(errormessage))
            {
                App.notifyIcon.ShowBalloonTip(1000, "", errormessage, System.Windows.Forms.ToolTipIcon.Error);
                GenericTools.Restore();
            }
            else if (!string.IsNullOrEmpty(errormessage))
            {
                GenericTools.Restore();
                MessageBox.Show("onPlay " + errormessage);
            }
            Log.FunctionOutdent("MainWindow", "OnPlay");
        }
        internal bool CanRename(object _item)
        {
            try
            {
                if (SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (!(view.listWorkflows.SelectedValue is Workflow wf)) return false;
                    if (global.isConnected)
                    {
                        return wf.hasRight(global.webSocketClient.user, ace_right.invoke);
                    }
                    return true;
                }
                // if (!IsConnected) return false;
                if (isRecording) return false;
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                if (global.webSocketClient == null) return true;
                return designer.Workflow.hasRight(global.webSocketClient.user, ace_right.invoke);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal async void OnRename(object _item)
        {
            if (SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (view.listWorkflows.SelectedValue is Project project)
                {
                    string Name = Microsoft.VisualBasic.Interaction.InputBox("Name?", "New name", project.name);
                    if (string.IsNullOrEmpty(Name) || project.name == Name) return;
                    project.name = Name;
                    await project.Save();
                }
                if (!(view.listWorkflows.SelectedValue is Workflow workflow)) return;
                try
                {
                    if (RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(workflow.IDOrRelativeFilename) is Views.WFDesigner designer)
                    {
                        string Name = Microsoft.VisualBasic.Interaction.InputBox("Name?", "New name", designer.Workflow.name);
                        if (string.IsNullOrEmpty(Name) || designer.Workflow.name == Name) return;
                        designer.RenameWorkflow(Name);
                    }
                    else
                    {
                        string Name = Microsoft.VisualBasic.Interaction.InputBox("Name?", "New name", workflow.name);
                        if (string.IsNullOrEmpty(Name) || workflow.name == Name) return;
                        workflow.Xaml = Views.WFDesigner.SetWorkflowName(workflow.Xaml, Name);
                        workflow.name = Name;
                        await workflow.Save();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                return;
            }
        }
        internal bool CanCopyID(object _item)
        {
            try
            {
                if (SelectedContent is Views.OpenProject view)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return false;
                    if (!(view.listWorkflows.SelectedValue is Workflow wf)) return false;
                    if (global.isConnected)
                    {
                        return wf.hasRight(global.webSocketClient.user, ace_right.invoke);
                    }
                    return true;
                }
                // if (!IsConnected) return false;
                if (isRecording) return false;
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                if (global.webSocketClient == null) return true;
                return designer.Workflow.hasRight(global.webSocketClient.user, ace_right.invoke);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal void OnCopyID(object _item)
        {
            if (SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (!(view.listWorkflows.SelectedValue is Workflow workflow)) return;
                try
                {
                    Clipboard.SetText(workflow._id);
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
                Clipboard.SetText(designer.Workflow._id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnCopyID " + ex.Message);
            }
        }
        internal void OnCopyRelativeFilename(object _item)
        {
            if (SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                if (!(view.listWorkflows.SelectedValue is Workflow workflow)) return;
                try
                {
                    Clipboard.SetText(workflow.RelativeFilename);
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
                Clipboard.SetText(designer.Workflow.RelativeFilename);
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnCopyID " + ex.Message);
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
                    if (wf.State == "running" || wf.State == "idle") return true;
                    return false;
                }
                // if (!IsConnected) return false;
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
                    wf.SetLastState("aborted");
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
                    StopRecordPlugins(true);
                    designer.ReadOnly = false;
                    InputDriver.Instance.CallNext = true;
                    InputDriver.Instance.OnKeyDown -= OnKeyDown;
                    InputDriver.Instance.OnKeyUp -= OnKeyUp;
                    GenericTools.Restore();
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
                // if (!IsConnected) return false;
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                foreach (var i in designer.Workflow.Instances)
                {
                    if (i.isCompleted == false)
                    {
                        return false;
                    }
                }
                if (designer.IsSequenceSelected) return !isRecording;
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
            StopRecordPlugins(true);
            if (SelectedContent is Views.WFDesigner view)
            {
                view.ReadOnly = false;
                view.EndRecording();
            }
            InputDriver.Instance.CallNext = true;
            InputDriver.Instance.OnKeyDown -= OnKeyDown;
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
            GenericTools.Restore();
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
        private void OnOpenEdgePageCommand(object _item)
        {
            System.Diagnostics.Process.Start("msedge.exe", "https://chrome.google.com/webstore/detail/openrpa/hpnihnhlcnfejboocnckgchjdofeaphe");
        }

        private int lastsapprocessid = -1;
        private void OnKeyDown(Input.InputEventArgs e)
        {
            if (!isRecording) return;
            // if (e.Key == KeyboardKey. 255) return;
            try
            {
                var element = AutomationUtil.getAutomation().FocusedElement();
                if (element != null && element.Properties.ProcessId.IsSupported)
                {
                    if (element.Properties.ProcessId == lastsapprocessid) return;
                    var p = System.Diagnostics.Process.GetProcessById(element.Properties.ProcessId);
                    if (p.ProcessName.ToLower() == "saplogon")
                    {
                        lastsapprocessid = element.Properties.ProcessId;
                        return;
                    }
                }
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
                        view.Lastinsertedmodel = view.AddRecordingActivity(rme, null);
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
                var element = AutomationUtil.getAutomation().FocusedElement();
                if (element != null && element.Properties.ProcessId.IsSupported)
                {
                    if (element.Properties.ProcessId == lastsapprocessid) return;
                    var p = System.Diagnostics.Process.GetProcessById(element.Properties.ProcessId);
                    if (p.ProcessName.ToLower() == "saplogon")
                    {
                        lastsapprocessid = element.Properties.ProcessId;
                        return;
                    }
                }

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
            Log.FunctionIndent("MainWindow", "StartDetectorPlugins");
            try
            {
                foreach (var detector in Plugins.detectorPlugins) detector.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "StartDetectorPlugins");
        }
        private void StopDetectorPlugins()
        {
            Log.FunctionIndent("MainWindow", "StopDetectorPlugins");
            try
            {
                foreach (var detector in Plugins.detectorPlugins) detector.Stop();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "StopDetectorPlugins");
        }
        Interfaces.Overlay.OverlayWindow _overlayWindow = null;
        private void StartRecordPlugins(bool all)
        {
            Log.FunctionIndent("MainWindow", "StartRecordPlugins");
            try
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

                p = Plugins.recordPlugins.Where(x => x.Name == "SAP").FirstOrDefault();
                if (p != null && (all == true || all == false))
                {
                    p.OnUserAction += OnUserAction;
                    if (Config.local.record_overlay) p.OnMouseMove += OnMouseMove;
                    p.Start();
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "StartRecordPlugins");
        }
        private void StopRecordPlugins(bool all)
        {
            Log.FunctionIndent("MainWindow", "StopRecordPlugins");
            try
            {
                isRecording = false;
                var p = Plugins.recordPlugins.Where(x => x.Name == "Windows").First();
                p.OnUserAction -= OnUserAction;
                if (Config.local.record_overlay) p.OnMouseMove -= OnMouseMove;
                p.Stop();

                p = Plugins.recordPlugins.Where(x => x.Name == "SAP").FirstOrDefault();
                if (p != null && (all == true || all == false))
                {
                    p.OnUserAction -= OnUserAction;
                    p.Stop();
                }

                if (_overlayWindow != null)
                {
                    GenericTools.RunUI(_overlayWindow, () =>
                    {
                        try
                        {
                            _overlayWindow.Visible = true;
                            _overlayWindow.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    });
                }
                _overlayWindow = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "StopRecordPlugins");
        }
        public void OnMouseMove(IRecordPlugin sender, IRecordEvent e)
        {
            if (!Config.local.record_overlay) return;
            foreach (var p in Plugins.recordPlugins.OrderBy(x => x.Priority))
            {
                if (p.Name != sender.Name)
                {
                    if (p.ParseMouseMoveAction(ref e)) break;
                }
            }
            if (e.Element != null && _overlayWindow != null)
            {

                GenericTools.RunUI(_overlayWindow, () =>
                {
                    try
                    {
                        if (_overlayWindow != null)
                        {
                            _overlayWindow.Visible = true;
                            _overlayWindow.Bounds = e.Element.Rectangle;
                        }
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
            Log.FunctionIndent("MainWindow", "OnUserAction");
            if (sender.Name == "Windows") StopRecordPlugins(false);
            AutomationHelper.syncContext.Post(o =>
            {
                IPlugin plugin = sender;
                try
                {
                    if (sender.Name == "Windows")
                    {
                        foreach (var p in Plugins.recordPlugins.OrderBy(x => x.Priority))
                        {
                            if (p.Name != sender.Name)
                            {
                                try
                                {
                                    if (p.ParseUserAction(ref e))
                                    {
                                        plugin = p;
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.ToString());
                                }
                            }
                        }
                    }
                    if (e.a == null)
                    {
                        if (sender.Name == "Windows") StartRecordPlugins(false);
                        if (e.ClickHandled == false)
                        {
                            NativeMethods.SetCursorPos(e.X, e.Y);
                            InputDriver.Click(e.Button);
                        }
                        Log.Function("MainWindow", "OnUserAction", "Action is null");
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
                        if (e.SupportSelect)
                        {
                            var win = new Views.InsertSelect(e.Element)
                            {
                                Topmost = true
                            };
                            isRecording = false;
                            InputDriver.Instance.CallNext = true;
                            win.Owner = this;
                            if (win.ShowDialog() == true)
                            {
                                e.ClickHandled = true;
                                if (!string.IsNullOrEmpty(win.SelectedItem.Value))
                                {
                                    e.a.AddInput(win.SelectedItem.Value, e.Element);
                                }
                                else
                                {
                                    e.a.AddInput(win.SelectedItem.Name, e.Element);
                                }

                            }
                            else
                            {
                                e.SupportSelect = false;
                            }
                            InputDriver.Instance.CallNext = false;
                            isRecording = true;
                        }
                        else if (e.SupportInput)
                        {
                            var win = new Views.InsertText
                            {
                                Topmost = true
                            };
                            isRecording = false;
                            win.Owner = this;
                            if (win.ShowDialog() == true)
                            {
                                e.a.AddInput(win.Text, e.Element);
                            }
                            else { e.SupportInput = false; }
                            isRecording = true;
                        }

                        view.ReadOnly = false;
                        view.Lastinserted = e.a.Activity;
                        view.Lastinsertedmodel = view.AddRecordingActivity(e.a.Activity, plugin);
                        view.ReadOnly = true;
                        if (e.ClickHandled == false && e.SupportInput == false)
                        {
                            NativeMethods.SetCursorPos(e.X, e.Y);
                            InputDriver.Click(e.Button);
                        }
                        System.Threading.Thread.Sleep(500);
                    }
                    if (sender.Name == "Windows") StartRecordPlugins(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Show();
                    Log.Error(ex.ToString());
                }
            }, null);
            Log.FunctionOutdent("MainWindow", "OnUserAction");
        }
        internal void OnRecord(object _item)
        {
            if (!(SelectedContent is Views.WFDesigner)) return;
            Log.FunctionIndent("MainWindow", "OnRecord");
            try
            {
                var designer = (Views.WFDesigner)SelectedContent;
                designer.ReadOnly = true;
                designer.Lastinserted = null;
                designer.Lastinsertedmodel = null;
                StopDetectorPlugins();
                InputDriver.Instance.OnKeyDown += OnKeyDown;
                InputDriver.Instance.OnKeyUp += OnKeyUp;
                StartRecordPlugins(true);
                InputDriver.Instance.CallNext = false;
                if (this.Minimize) GenericTools.Minimize();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "OnRecord");
        }
        public void OnDetector(IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e)
        {
            Log.FunctionIndent("MainWindow", "OnDetector");
            try
            {
                Log.Information("Detector " + plugin.Entity.name + " was triggered, with id " + plugin.Entity._id);
                foreach (var wi in WorkflowInstance.Instances.ToList())
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
                if (!global.isConnected)
                {
                    Log.FunctionOutdent("MainWindow", "OnDetector", "isConnected is false");
                    return;
                }
                Interfaces.mq.RobotCommand command = new Interfaces.mq.RobotCommand();
                // detector.user = global.webSocketClient.user;
                var data = JObject.FromObject(detector);
                var Entity = (plugin.Entity as Detector);
                command.command = "detector";
                command.detectorid = Entity._id;
                if (string.IsNullOrEmpty(Entity._id))
                {
                    Log.FunctionOutdent("MainWindow", "OnDetector", "Entity._id is null");
                    return;
                }
                command.data = data;
                Task.Run(async () =>
                {
                    try
                    {
                        await global.webSocketClient.QueueMessage(Entity._id, command, null, null, 0);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
            Log.FunctionOutdent("MainWindow", "OnDetector");
        }
        public async void IdleOrComplete(IWorkflowInstance instance, EventArgs e)
        {
            if (instance == null) return;
            Log.FunctionIndent("MainWindow", "IdleOrComplete");
            try
            {
                if (string.IsNullOrEmpty(instance.queuename) && string.IsNullOrEmpty(instance.correlationId) && string.IsNullOrEmpty(instance.caller) && instance.isCompleted)
                {
                    if (this.Minimize) GenericTools.Restore();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            if (instance.state != "idle")
            {
                GenericTools.RunUI(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
            }

            try
            {
                bool isRemote = false;
                if (!string.IsNullOrEmpty(instance.queuename) && !string.IsNullOrEmpty(instance.correlationId))
                {
                    isRemote = true;
                    Interfaces.mq.RobotCommand command = new Interfaces.mq.RobotCommand();
                    var data = JObject.FromObject(instance.Parameters);
                    command.command = "invoke" + instance.state;
                    command.workflowid = instance.WorkflowId;
                    command.data = data;
                    if ((instance.state == "failed" || instance.state == "aborted") && instance.Exception != null)
                    {
                        command.data = JObject.FromObject(instance.Exception);
                    }
                    // Log.Output("Send Instance state " + instance.state + " to " + instance.queuename);
                    try
                    {
                        await global.webSocketClient.QueueMessage(instance.queuename, command, null, instance.correlationId, 0);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                    }
                }
                else
                {
                    Log.Verbose("No queue name " + instance.queuename + " on instance, to notify about state " + instance.state);
                }
                if (instance.hasError || instance.isCompleted)
                {
                    string message = (instance.Workflow.name + " " + instance.state);
                    if (!string.IsNullOrEmpty(instance.errorsource))
                    {
                        message += " at " + instance.errorsource;
                    }
                    if (instance.runWatch != null)
                    {
                        message += (" in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed));
                    }
                    if (!string.IsNullOrEmpty(instance.errormessage)) message += (Environment.NewLine + "# " + instance.errormessage);
                    Log.Output(message);
                    if ((Config.local.notify_on_workflow_end && !isRemote) || (Config.local.notify_on_workflow_remote_end && isRemote))
                    {
                        if (instance.state == "completed")
                        {
                            // App.notifyIcon.ShowBalloonTip(1000, instance.Workflow.name + " " + instance.state, message, System.Windows.Forms.ToolTipIcon.Info);
                            App.notifyIcon.ShowBalloonTip(1000, "", message, System.Windows.Forms.ToolTipIcon.Info);
                        }
                        else
                        {
                            // App.notifyIcon.ShowBalloonTip(1000, instance.Workflow.name + " " + instance.state, message, System.Windows.Forms.ToolTipIcon.Error);
                            App.notifyIcon.ShowBalloonTip(1000, "", message, System.Windows.Forms.ToolTipIcon.Error);
                        }
                    }
                    _ = Task.Run(() =>
                    {
                        var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                        while (sw.Elapsed < TimeSpan.FromSeconds(1))
                        {
                            lock (WorkflowInstance.Instances)
                            {
                                foreach (var wi in WorkflowInstance.Instances.ToList())
                                {
                                    if (wi.isCompleted) continue;
                                    if (wi.Bookmarks == null) continue;
                                    foreach (var b in wi.Bookmarks)
                                    {
                                        if (b.Key == instance._id)
                                        {
                                            wi.ResumeBookmark(b.Key, instance);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
                // RobotInstance.instance.NotifyPropertyChanged("Projects");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
            }
            Log.FunctionOutdent("MainWindow", "IdleOrComplete");
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
                view.Owner = GenericTools.MainWindow;
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
            Log.FunctionIndent("MainWindow", "TesseractLang_Click");
            string path = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "tessdata");
            TesseractDownloadLangFile(path, Config.local.ocrlanguage);
            System.Windows.MessageBox.Show("Download complete");
            Log.FunctionOutdent("MainWindow", "TesseractLang_Click");
        }
        private void TesseractDownloadLangFile(string folder, string lang)
        {
            Log.FunctionIndent("MainWindow", "TesseractLang_Click", lang + " " + folder);
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
            Log.FunctionOutdent("MainWindow", "TesseractDownloadLangFile");
        }
        private void SearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.FunctionIndent("MainWindow", "SearchBox_SelectionChanged");
            QuickLaunchItem item = null;
            try
            {
                if (SearchBox.SelectedItem != null && SearchBox.SelectedItem is QuickLaunchItem)
                {
                    item = SearchBox.SelectedItem as QuickLaunchItem;
                }
                if (item == null)
                {
                    Log.FunctionOutdent("MainWindow", "SearchBox_SelectionChanged", "item is null");
                    return;
                }
                if (item.designer == null)
                {
                    Log.FunctionOutdent("MainWindow", "SearchBox_SelectionChanged", "item.designer is null");
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            GenericTools.RunUI(() =>
            {
                try
                {
                    item.designer.SetDebugLocation(null);
                    item.designer.IsSelected = true;
                    if (item.item != null && item.item != item.originalitem)
                    {
                        item.designer.NavigateTo(item.item);
                    }
                    if (item.originalitem != null)
                    {
                        item.designer.NavigateTo(item.originalitem);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            Log.FunctionOutdent("MainWindow", "SearchBox_SelectionChanged");
        }
        private void SearchBox_Populating(object sender, PopulatingEventArgs e)
        {
            Log.FunctionIndent("MainWindow", "SearchBox_Populating");
            try
            {
                var text = SearchBox.Text.ToLower();
                var options = new List<QuickLaunchItem>();
                foreach (var designer in RobotInstance.instance.Designers)
                {
                    var suboptions = new List<QuickLaunchItem>();
                    foreach (var arg in designer.GetParameters())
                    {
                        if (arg.Name.ToLower().Contains(text))
                        {

                            AddOption(designer, arg, suboptions);
                        }
                    }
                    foreach (System.Activities.Presentation.Model.ModelItem item in designer.GetWorkflowActivities())
                    {
                        bool wasadded = false;
                        string displayname = item.ToString();
                        System.Activities.Presentation.Model.ModelProperty property = item.Properties["ExpressionText"];
                        if ((property != null) && (property.Value != null))
                        {
                            string input = item.Properties["ExpressionText"].Value.ToString();
                            if (input.ToLower().Contains(text))
                            {
                                wasadded = true;
                                AddOption(designer, item, suboptions);
                            }
                        }
                        property = item.Properties["Variables"];
                        if ((property != null) && (property.Value != null))
                        {
                            foreach (var v in property.Collection)
                            {
                                var nameproperty = v.Properties["Name"];
                                if (nameproperty.Value.ToString().ToLower().Contains(text))
                                {
                                    wasadded = true;
                                    AddOption(designer, v, suboptions);
                                }

                            }
                        }
                        if (!wasadded && displayname.ToLower().Contains(text))
                        {
                            AddOption(designer, item, suboptions);
                        }
                    }
                    if (suboptions.Count > 0)
                    {
                        options.Add(new QuickLaunchItem() { Header = designer.Workflow.name });
                        options.AddRange(suboptions);
                    }
                }
                SearchBox.ItemsSource = options;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "SearchBox_Populating");
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                tabGeneral.IsSelected = true;
                //searchTab.Focus();
                SearchBox.Focus();
            }
        }
        private void AddOption(IDesigner designer, System.Activities.Presentation.Model.ModelItem item, List<QuickLaunchItem> options)
        {
            Log.FunctionIndent("MainWindow", "AddOption");
            try
            {
                var ImageSource = new BitmapImage(new Uri("/Resources/icons/activity.png", UriKind.Relative));
                var _item = GetActivity(item);
                if (!item.ItemType.ToString().Contains("System.Activities.Variable"))
                {
                    var exists = options.Where(x => x.item == _item).FirstOrDefault();
                    if (exists != null) return;
                }
                if (item.ItemType.ToString().Contains("System.Activities.Statements.Flow") ||
                    item.ItemType.ToString().Contains("System.Activities.Statements.Flow"))
                {
                    ImageSource = new BitmapImage(new Uri("/Resources/icons/flowchart.png", UriKind.Relative));
                }
                var displayname = _item.ToString();
                if (_item != item)
                {
                    if (item.ItemType.ToString().Contains("System.Activities.Variable"))
                    {
                        ImageSource = new BitmapImage(new Uri("/Resources/icons/variable.png", UriKind.Relative));
                        displayname = "Variable of " + _item.ToString();
                        var p = item.Properties["Name"];
                        if (p != null && p.Value != null)
                        {
                            displayname = "Variable " + p.Value + " of " + _item.ToString();
                        }
                    }
                    else
                    {
                        ImageSource = new BitmapImage(new Uri("/Resources/icons/property.png", UriKind.Relative));
                        displayname = "Property of " + _item.ToString();
                        foreach (var p in _item.Properties)
                        {
                            if (p.Value == item)
                            {
                                displayname = "Property " + p.Name + " of " + _item.ToString();
                            }
                            else if (p.Value == item.Parent)
                            {
                                displayname = "Property " + p.Name + " of " + _item.ToString();
                            }
                        }
                    }
                }
                options.Add(new QuickLaunchItem()
                {
                    Text = displayname,
                    designer = designer as Views.WFDesigner,
                    originalitem = item,
                    item = _item,
                    ImageSource = ImageSource
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "AddOption");
        }
        private void AddOption(IDesigner designer, DynamicActivityProperty arg, List<QuickLaunchItem> options)
        {
            Log.FunctionIndent("MainWindow", "AddOption");
            try
            {
                var ImageSource = new BitmapImage(new Uri("/Resources/icons/openin.png", UriKind.Relative));
                var displayname = "Argument " + arg.Name;
                options.Add(new QuickLaunchItem()
                {
                    Text = displayname,
                    designer = designer as WFDesigner,
                    argument = arg,
                    ImageSource = ImageSource
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "AddOption");
        }
        private System.Activities.Presentation.Model.ModelItem GetActivity(System.Activities.Presentation.Model.ModelItem item)
        {
            Log.FunctionIndent("MainWindow", "GetActivity");
            try
            {
                var result = item;
                while (result != null)
                {
                    if (result.ItemType.ToString().Contains("System.Activities.InArgument") ||
                        result.ItemType.ToString().Contains("System.Activities.OutArgument") ||
                        result.ItemType.ToString().Contains("System.Activities.InOutArgument") ||
                        result.ItemType.ToString().Contains("VisualBasic.Activities.VisualBasicValue") ||
                        result.ItemType.ToString().Contains("VisualBasic.Activities.VisualBasicReference") ||
                        result.ItemType.ToString().Contains("System.Activities.Variable") ||
                        result.ItemType.ToString().Contains("System.Activities.Expressions"))
                    {
                        result = result.Parent;
                        continue;
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("MainWindow", "GetActivity");
            return null;
        }
        private void SearchBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (SearchBox.IsDropDownOpen)
            {
                e.Handled = true;
            }
        }
        private void SearchBox_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (SearchBox.IsDropDownOpen) e.Handled = true;
        }
        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (SelectedContent is Views.OpenProject op)
            {
            }
            Views.OpenProject.Instance.FilterText = SearchBox.Text;
        }
        private void clearTraceMessages_Click(object sender, RoutedEventArgs e)
        {
            Tracing.TraceMessages = "";
        }
        private void clearOutputMessages_Click(object sender, RoutedEventArgs e)
        {
            Tracing.OutputMessages = "";
        }
        internal Views.ChildSession childSession;
        private bool CanPlayInChild(object _item)
        {
            if (Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance == null) return false;
            if (!Interfaces.IPCService.OpenRPAServiceUtil._ChildSession) return false;
            try
            {
                Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.Ping();
                return CanPlay(_item);
            }
            catch (Exception)
            {
            }
            return false;
        }
        private async void OnPlayInChild(object item)
        {
            Log.FunctionIndent("MainWindow", "OnPlayInChild");
            string errormessage = "";
            if (SelectedContent is Views.OpenProject view)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null)
                {
                    Log.FunctionOutdent("MainWindow", "OnPlayInChild", "SelectedValue is null");
                    return;
                }
                if (!(view.listWorkflows.SelectedValue is Workflow workflow))
                {
                    Log.FunctionOutdent("MainWindow", "OnPlayInChild", "SelectedValue is not workflow");
                    return;
                }
                try
                {
                    if (this.Minimize) GenericTools.Minimize();
                    var param = new Dictionary<string, object>();
                    await Task.Run(() => Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.RunWorkflowByIDOrRelativeFilename(workflow.IDOrRelativeFilename, true, param));
                }
                catch (Exception ex)
                {
                    errormessage = ex.Message;
                    Log.Error(ex.ToString());
                }
                if (Config.local.notify_on_workflow_end && !string.IsNullOrEmpty(errormessage))
                {
                    App.notifyIcon.ShowBalloonTip(1000, "", errormessage, System.Windows.Forms.ToolTipIcon.Error);
                    GenericTools.Restore();
                }
                else if (!string.IsNullOrEmpty(errormessage))
                {
                    GenericTools.Restore();
                    MessageBox.Show("OnPlayInChild " + errormessage);
                }
                Log.FunctionOutdent("MainWindow", "OnPlayInChild");
                return;
            }
            try
            {
                if (!(SelectedContent is Views.WFDesigner))
                {
                    Log.FunctionOutdent("MainWindow", "OnPlayInChild", "Selected content is not WFDesigner");
                    return;
                }
                var designer = (Views.WFDesigner)SelectedContent;
                if (designer.HasChanged) { await designer.SaveAsync(); }
                var param = new Dictionary<string, object>();
                await Task.Run(() => Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.RunWorkflowByIDOrRelativeFilename(designer.Workflow.IDOrRelativeFilename, true, param));
            }
            catch (Exception ex)
            {
                errormessage = ex.Message;
            }
            if (Config.local.notify_on_workflow_end && !string.IsNullOrEmpty(errormessage))
            {
                App.notifyIcon.ShowBalloonTip(1000, "", errormessage, System.Windows.Forms.ToolTipIcon.Error);
                GenericTools.Restore();
            }
            else if (!string.IsNullOrEmpty(errormessage))
            {
                GenericTools.Restore();
                MessageBox.Show("onPlay " + errormessage);
            }
            Log.FunctionOutdent("MainWindow", "OnPlay");

        }
        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            if (childSession == null)
            {
                childSession = new ChildSession();
            }
            uint SessionId = Interfaces.win32.ChildSession.GetChildSessionId();
            if (SessionId > 0)
            {
                var winstation = UserLogins.QuerySessionInformation((int)SessionId, UserLogins.WTS_INFO_CLASS.WTSWinStationName);
                if (!string.IsNullOrEmpty(winstation))
                {
                    // Interfaces.win32.ChildSession.LogOffChildSession();
                    Interfaces.win32.ChildSession.DisconnectChildSession();
                }
            }
            childSession.Show();
        }
        private void StatusTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var txt = sender as TextBlock;
            var plugin = txt.DataContext as IRecordPlugin;
            plugin.StatusTextMouseUp();
        }
    }
    public class QuickLaunchItem
    {
        public System.Windows.Media.ImageSource ImageSource { get; set; }
        public string Text { get; set; }
        public System.Activities.Presentation.Model.ModelItem item { get; set; }
        public System.Activities.Presentation.Model.ModelItem originalitem { get; set; }
        public Views.WFDesigner designer { get; set; }
        public string Header { get; set; }
        public DynamicActivityProperty argument { get; set; }
    }
}
