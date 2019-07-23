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

namespace OpenRPA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static MainWindow instance = null;
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Project>();
        private bool isRecording = false;
        private bool autoReconnect = true;
        private bool loginInProgress = false;
        public static Tracing tracing = new Tracing();
        private static object statelock = new object();
        public List<string> ocrlangs { get; set; }  = new List<string>() { "afr", "amh", "ara", "asm", "aze", "aze_cyrl", "bel", "ben", "bod", "bos", "bre", "bul", "cat", "ceb", "ces", "chi_sim", "chi_sim_vert", "chi_tra", "chi_tra_vert", "chr", "cos", "cym", "dan", "dan_frak", "deu", "deu_frak", "div", "dzo", "ell", "eng", "enm", "epo", "equ", "est", "eus", "fao", "fas", "fil", "fin", "fra", "frk", "frm", "fry", "gla", "gle", "glg", "grc", "guj", "hat", "heb", "hin", "hrv", "hun", "hye", "iku", "ind", "isl", "ita", "ita_old", "jav", "jpn", "jpn_vert", "kan", "kat", "kat_old", "kaz", "khm", "kir", "kmr", "kor", "kor_vert", "lao", "lat", "lav", "lit", "ltz", "mal", "mar", "mkd", "mlt", "mon", "mri", "msa", "mya", "nep", "nld", "nor", "oci", "ori", "osd", "pan", "pol", "por", "pus", "que", "ron", "rus", "san", "sin", "slk", "slk_frak", "slv", "snd", "spa", "spa_old", "sqi", "srp", "srp_latn", "sun", "swa", "swe", "syr", "tam", "tat", "tel", "tgk", "tgl", "tha", "tir", "ton", "tur", "uig", "ukr", "urd", "uzb", "uzb_cyrl", "vie", "yid", "yor" };
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
        public MainWindow()
        {
            InitializeComponent();
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
            OpenRPA.Input.InputDriver.Instance.initCancelKey(cancelkey.Text);
            InputDriver.Instance.onCancel += onCancel;
        }
        public Views.WFDesigner designer
        {
            get
            {
                foreach (TabItem tab in mainTabControl.Items)
                {
                    if (tab.Content is Views.WFDesigner)
                    {
                        Views.WFDesigner _designer = tab.Content as Views.WFDesigner;
                        if (tab.IsSelected) return _designer;
                    }
                }
                return null;
            }
        }
        public bool VisualTracking
        {
            get
            {
                if (designer == null) return false;
                Log.Debug(designer.Workflow.name);
                return designer.VisualTracking;
            }
            set
            {
                if (designer == null) return;
                Log.Debug(designer.Workflow.name);
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
        public ICommand SettingsCommand { get { return new RelayCommand<object>(onSettings, canSettings); } }
        public ICommand MinimizeCommand { get { return new RelayCommand<object>(onMinimize, canMinimize); } }
        public ICommand VisualTrackingCommand { get { return new RelayCommand<object>(onVisualTracking, canVisualTracking); } }
        public ICommand SlowMotionCommand { get { return new RelayCommand<object>(onSlowMotion, canSlowMotion); } }
        public ICommand SignoutCommand { get { return new RelayCommand<object>(onSignout, canSignout); } }
        public ICommand OpenCommand { get { return new RelayCommand<object>(onOpen, canOpen); } }
        public ICommand DetectorsCommand { get { return new RelayCommand<object>(onDetectors, canDetectors); } }
        public ICommand SaveCommand { get { return new RelayCommand<object>(onSave, canSave); } }
        public ICommand NewCommand { get { return new RelayCommand<object>(onNew, canNew); } }
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
        private bool canPermissions(object item)
        {
            if (!isConnected) return false;
            if (isRecording) return false;
            var view = item as Views.OpenProject;
            if (view != null)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return false;
                var wf = view.listWorkflows.SelectedValue as Workflow;
                return true;
            }
            var designer = item as Views.WFDesigner;
            if (designer != null)
            {
                return true;
            }
            var DetectorsView = item as Views.DetectorsView;
            if (DetectorsView != null)
            {
                var detector = DetectorsView.lidtDetectors.SelectedItem as IDetectorPlugin;
                if (detector == null) return false;
                return true;
            }
            return false;
        }
        private async void onPermissions(object item)
        {
            apibase result = null;
            if (!isConnected) return;
            if (isRecording) return;
            var view = item as Views.OpenProject;
            if (view != null)
            {
                var val = view.listWorkflows.SelectedValue;
                if (val == null) return;
                var wf = val as Workflow;
                var p = val as Project;
                if (wf != null) { result = wf; }
                if (p != null) { result = p; }
            }
            var designer = item as Views.WFDesigner;
            if (designer != null)
            {
                result = designer.Workflow;
            }
            var DetectorsView = item as Views.DetectorsView;
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
                if (result is Project) await ((Project)result).Save();
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
        private bool canImport(object item) { if (!isConnected) return false; return (item is Views.WFDesigner || item is Views.OpenProject || item == null); }
        private void onImport(object item)
        {
            try
            {
                if (item is Views.WFDesigner)
                {
                    var designer = (Views.WFDesigner)item;
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
        private bool canExport(object item) { if (!isConnected) return false; return (item is Views.WFDesigner || item is Views.OpenProject || item == null); }
        private void onExport(object item)
        {
            if (!(item is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)item;
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
        private void onSave(object sender, ExecutedRoutedEventArgs e)
        {
            SaveCommand.Execute(mainTabControl.SelectedContent);
        }
        private void onDelete(object sender, ExecutedRoutedEventArgs e)
        {
            DeleteCommand.Execute(mainTabControl.SelectedContent);
        }
        private bool canMinimize(object item)
        {
            return true;
        }
        private void onMinimize(object item)
        {
        }
        private bool canVisualTracking(object item)
        {
            return true;
        }
        private void onVisualTracking(object item)
        {
            //var b = (bool)item;
            //model.VisualTracking = b;
            //foreach (TabItem tab in mainTabControl.Items)
            //{
            //    if (tab.Content is Views.WFDesigner && tab.IsSelected)
            //    {
            //        var designer = tab.Content as Views.WFDesigner;
            //        designer.VisualTracking = b;
            //    }
            //}
        }
        private bool canSlowMotion(object item)
        {
            return true;
        }
        private void onSlowMotion(object item)
        {
            //var b = (bool)item;
            //model.SlowMotion = b;
            //foreach (TabItem tab in mainTabControl.Items)
            //{
            //    if (tab.Content is Views.WFDesigner && tab.IsSelected)
            //    {
            //        var designer = tab.Content as Views.WFDesigner;
            //        designer.SlowMotion = b;
            //    }
            //}
        }
        private bool canSettings(object item)
        {
            return true;
        }
        private void onSettings(object item)
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
        private bool canSignout(object item)
        {
            if (!global.isConnected) return false;
            return true;
        }
        private void onSignout(object item)
        {
            autoReconnect = true;
            Projects.Clear();
            Config.local.password = Config.local.ProtectString("BadPassword");
            Config.Reload();
            _ = global.webSocketClient.Close();
        }
        private bool canOpen(object item)
        {
            if (!isConnected) return false;
            foreach (TabItem tab in mainTabControl.Items)
            {
                if (tab.Content is Views.OpenProject)
                {
                    return false;
                }
            }
            return true;
        }
        private bool canDetectors(object item)
        {
            if (!isConnected) return false;
            foreach (TabItem tab in mainTabControl.Items)
            {
                if (tab.Content is Views.DetectorsView)
                {
                    return false;
                }
            }
            return true;
        }
        private void onOpen(object item)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                foreach (TabItem tab in mainTabControl.Items)
                {
                    if (tab.Content is Views.OpenProject)
                    {
                        tab.IsSelected = true;
                        return;
                    }
                }
                var view = new Views.OpenProject(this);
                view.onOpenProject += onOpenProject;
                view.onOpenWorkflow += onOpenWorkflow;
                Views.ClosableTab newTabItem = new Views.ClosableTab
                {
                    Title = "Open project",
                    Name = "openproject",
                    Content = view
                };
                newTabItem.OnClose += NewTabItem_OnClose;
                mainTabControl.Items.Add(newTabItem);
                newTabItem.IsSelected = true;
            }, null);
        }
        private void onDetectors(object item)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                foreach (TabItem tab in mainTabControl.Items)
                {
                    if (tab.Content is Views.DetectorsView)
                    {
                        tab.IsSelected = true;
                        return;
                    }
                }
                var view = new Views.DetectorsView(this);
                Views.ClosableTab newTabItem = new Views.ClosableTab
                {
                    Title = "Detectors",
                    Name = "detectors",
                    Content = view
                };
                newTabItem.OnClose += NewTabItem_OnClose;
                mainTabControl.Items.Add(newTabItem);
                newTabItem.IsSelected = true;
            }, null);
        }
        private bool canlinkOpenFlow(object item)
        {
            if (string.IsNullOrEmpty(Config.local.wsurl)) return false;
            return true;
        }
        private void onlinkOpenFlow(object item)
        {
            if (string.IsNullOrEmpty(Config.local.wsurl)) return;
            if (global.openflowconfig == null) return;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(global.openflowconfig.baseurl));
        }
        private bool canlinkNodeRED(object item)
        {
            if (!isConnected) return false;
            if (string.IsNullOrEmpty(Config.local.wsurl)) return false;
            if (global.openflowconfig == null) return false;
            if(global.openflowconfig.allow_personal_nodered) return true;

            return true;
        }
        private void onlinkNodeRED(object item)
        {
            if (global.openflowconfig == null) return;
            var baseurl = new Uri(Config.local.wsurl);
            var username = global.webSocketClient.user.username.Replace("@", "").Replace(".", "");
            var url = global.openflowconfig.nodered_domain_schema.Replace("$nodered_id$", username);
            if (baseurl.Scheme == "wss") { url = "https://" + url; } else { url = "http://" + url; }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url));
        }


        private async void NewTabItem_OnClose(object sender, RoutedEventArgs e)
        {
            Views.ClosableTab tab = sender as Views.ClosableTab;
            Views.WFDesigner designer = tab.Content as Views.WFDesigner;
            if (designer == null) return;
            if (!designer.HasChanged) return;
            if (designer.HasChanged && (global.isConnected ? global.webSocketClient.user.hasRole("robot admins") : true))
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Save " + designer.Workflow.name + " ?", "Workflow unsaved", MessageBoxButton.YesNoCancel);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    await designer.Save();
                }
                else if (messageBoxResult != MessageBoxResult.No)
                {
                    e.Handled = true;
                }
            }
        }
        public Views.WFDesigner getWorkflowDesignerByFilename(string Filename)
        {
            Views.WFDesigner designer = null;
            foreach (TabItem tab in mainTabControl.Items)
            {
                if (tab.Content is Views.WFDesigner)
                {
                    designer = (Views.WFDesigner)tab.Content;
                    if (designer.Workflow.FilePath == Filename)
                    {
                        return designer;
                    }
                }
            }
            return null;
        }
        public Views.WFDesigner getWorkflowDesignerById(string Id)
        {
            Views.WFDesigner designer = null;
            foreach (TabItem tab in mainTabControl.Items)
            {
                if (tab.Content is Views.WFDesigner)
                {
                    designer = (Views.WFDesigner)tab.Content;
                    if (designer.Workflow._id == Id)
                    {
                        return designer;
                    }
                }
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
                    Views.ClosableTab newTabItem = new Views.ClosableTab
                    {
                        Title = "Open project",
                        Name = "openproject"
                    };
                    newTabItem.OnClose += NewTabItem_OnClose;
                    var types = new List<Type>();
                    foreach (var p in Plugins.recordPlugins) { types.Add(p.GetType()); }
                    var view = new Views.WFDesigner((Views.ClosableTab)newTabItem, workflow, types.ToArray());
                    view.onChanged += WFDesigneronChanged;
                    newTabItem.Content = view;
                    mainTabControl.Items.Add(newTabItem);
                    newTabItem.IsSelected = true;

                    var workflows = new List<string>();
                    foreach (TabItem tab in mainTabControl.Items)
                    {
                        if (tab.Content is Views.WFDesigner)
                        {
                            designer = (Views.WFDesigner)tab.Content;
                            workflows.Add(designer.Workflow._id);
                        }
                    }
                    Config.local.openworkflows = workflows.ToArray();
                    Config.Save();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show(ex.Message);
                }
            }, null);
        }
        private void WFDesigneronChanged(Views.WFDesigner view)
        {
            AutomationHelper.syncContext.Post(o =>
            {
                Views.WFDesigner designer = null;
                foreach (TabItem tab in mainTabControl.Items)
                {
                    if (tab.Content is Views.WFDesigner)
                    {
                        designer = (Views.WFDesigner)tab.Content;
                        if (designer.Workflow.FilePath == view.Workflow.FilePath)
                        {
                            var t = (Views.ClosableTab)tab;
                            t.Title = (designer.HasChanged ? designer.Workflow.name + "*" : designer.Workflow.name);
                            CommandManager.InvalidateRequerySuggested();
                            return;
                        }
                    }
                }
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
        private bool canSave(object item) {
            var wf = item as Views.WFDesigner;
            if (wf == null) return false;
            return !wf.isRunnning;
        }
        private async void onSave(object item)
        {
            if (item is Views.WFDesigner)
            {
                var designer = (Views.WFDesigner)item;
                await designer.Save();
            }
            if (item is Views.OpenProject)
            {
                var view = (Views.OpenProject)item;
                var Project = view.listWorkflows.SelectedItem as Project;
                if (Project != null)
                {
                    await Project.Save();
                }
            }
        }
        private bool canNew(object item) { if (!isConnected) return false; return (item is Views.WFDesigner || item is Views.OpenProject || item == null); }
        private async void onNew(object item)
        {
            try
            {
                if (item is Views.WFDesigner)
                {
                    var designer = (Views.WFDesigner)item;
                    Workflow workflow = Workflow.Create(designer.Project, "New Workflow");
                    onOpenWorkflow(workflow);
                    return;
                }
                else
                {
                    string Name = Microsoft.VisualBasic.Interaction.InputBox("Name?", "Name project", "New project");
                    if (string.IsNullOrEmpty(Name)) return;
                    Project project = await Project.Create(Extensions.projectsDirectory, Name);
                    Workflow workflow = project.Workflows.First();
                    workflow.Project = project;
                    Projects.Add(project);
                    onOpenWorkflow(workflow);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                MessageBox.Show(ex.Message);
            }
        }
        private bool canCopy(object item)
        {
            return (item is Views.WFDesigner);
        }
        private async void onCopy(object item)
        {
            var designer = (Views.WFDesigner)item;
            await designer.Save();
            Workflow workflow = Workflow.Create(designer.Project, "Copy of " + designer.Workflow.name);
            workflow.Xaml = designer.Workflow.Xaml;
            workflow.name = "Copy of " + designer.Workflow.name;
            onOpenWorkflow(workflow);
        }
        private bool canDelete(object item)
        {
            var view = item as Views.OpenProject;
            if (view == null) return false;
            var val = view.listWorkflows.SelectedValue;
            if (val == null) return false;
            if (global.isConnected)
            {
                var wf = val as Workflow;
                var p = val as Project;
                if (wf != null)
                {
                    return wf.hasRight(global.webSocketClient.user, ace_right.delete);
                }
                if (p != null)
                {
                    return p.hasRight(global.webSocketClient.user, ace_right.delete);
                }
            }
            // don't know what your deleteing, lets just assume yes then
            return true;
        }
        private async void onDelete(object item)
        {
            var view = item as Views.OpenProject;
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
        private bool canPlay(object item)
        {
            var view = item as Views.OpenProject;
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
            if (!(item is Views.WFDesigner)) return false;
            var designer = (Views.WFDesigner)item;
            if (designer.BreakPointhit) return true;
            foreach (var i in designer.Workflow.Instances)
            {
                if (i.isCompleted == false)
                {
                    return false;
                }
            }
            return designer.Workflow.hasRight(global.webSocketClient.user, ace_right.invoke);
            // return true;
        }
        private async void onPlay(object item)
        {
            var view = item as Views.OpenProject;
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
                if (!(item is Views.WFDesigner)) return;
                var designer = (Views.WFDesigner)item;
                if (designer.HasChanged) { await designer.Save(); }
                designer.Run(VisualTracking, SlowMotion, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("onPlay " + ex.Message);
            }
        }
        private bool canStop(object item)
        {
            var view = item as Views.OpenProject;
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
            if (!(item is Views.WFDesigner)) return false;
            var designer = (Views.WFDesigner)item;
            foreach (var i in designer.Workflow.Instances)
            {
                if (i.isCompleted != true && i.state != "loaded")
                {
                    return true;
                }
            }
            return false;
        }
        private void onStop(object item)
        {
            var view = item as Views.OpenProject;
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

            if (!(item is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)item;
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
                InputDriver.Instance.CallNext = true;
                InputDriver.Instance.OnKeyDown -= OnKeyDown;
                InputDriver.Instance.OnKeyUp -= OnKeyUp;
                GenericTools.restore(GenericTools.mainWindow);
            }
        }
        private bool canRecord(object item)
        {
            if (!isConnected) return false;
            if (!(item is Views.WFDesigner)) return false;
            var designer = (Views.WFDesigner)item;
            foreach (var i in designer.Workflow.Instances)
            {
                if (i.isCompleted == false)
                {
                    return false;
                }
            }
            return !isRecording;
        }
        private void onCancel()
        {
            if (!isRecording) return;
            StartDetectorPlugins();
            StopRecordPlugins();

            
            InputDriver.Instance.CallNext = true;
            InputDriver.Instance.OnKeyDown -= OnKeyDown;
            InputDriver.Instance.OnKeyUp -= OnKeyUp;
            GenericTools.restore(GenericTools.mainWindow);
        }
        private void OnKeyDown(Input.InputEventArgs e)
        {
            if (!isRecording) return;
            // if (e.Key == KeyboardKey. 255) return;
            try
            {
                var cancelkey = InputDriver.Instance.cancelKeys.Where(x => x.KeyValue == e.KeyValue).ToList();
                if (cancelkey.Count > 0) return;
                if (mainTabControl.SelectedContent is Views.WFDesigner view)
                {
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
                if (mainTabControl.SelectedContent is Views.WFDesigner view)
                {
                    if (view.lastinserted != null && view.lastinserted is Activities.TypeText)
                    {
                        Log.Debug("re-use existing TypeText");
                        var item = (Activities.TypeText)view.lastinserted;
                        item.AddKey(new Interfaces.Input.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, true), view.lastinsertedmodel);
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
                if (mainTabControl.SelectedContent is Views.WFDesigner view)
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
                    view.lastinserted = e.a.Activity;
                    view.lastinsertedmodel = view.addActivity(e.a.Activity);
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
            }, null);
        }
        private void onRecord(object item)
        {
            if (!(item is Views.WFDesigner)) return;
            var designer = (Views.WFDesigner)item;
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
            AutomationHelper.syncContext.Post(o =>
            {
                LabelStatusBar.Content = "Disconnected from " + Config.local.wsurl + " reason " + reason;
            }, null);
            await Task.Delay(1000);
            if (autoReconnect) _ = global.webSocketClient.Connect();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
            if (!string.IsNullOrEmpty(Config.local.wsurl))
            {
                LabelStatusBar.Content = "Connecting to " + Config.local.wsurl;
            }
            Plugins.loadPlugins(Extensions.projectsDirectory);



            if (string.IsNullOrEmpty(Config.local.wsurl))
            {
                var Detectors = Interfaces.entity.Detector.loadDetectors(Extensions.projectsDirectory);
                foreach (var d in Detectors)
                {
                    IDetectorPlugin dp = null;
                    d.Path = Extensions.projectsDirectory;
                    dp = Plugins.AddDetector(d);
                    if (dp != null) dp.OnDetector += OnDetector;
                }
            }
            Task.Run(() =>
            {
                ExpressionEditor.EditorUtil.init();

                if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    global.webSocketClient = new WebSocketClient(Config.local.wsurl);
                    global.webSocketClient.OnOpen += WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;

                    _ = global.webSocketClient.Connect();
                }
                else
                {
                    var _Projects = Project.loadProjects(Extensions.projectsDirectory);
                    Projects = new System.Collections.ObjectModel.ObservableCollection<Project>();
                    foreach (Project p in _Projects)
                    {
                        Projects.Add(p);
                    }
                }
                AutomationHelper.init();
                new DesignerMetadata().Register();
                onOpen(null);
                foreach(var p in Projects)
                {
                    foreach(var wf in p.Workflows)
                    {
                        if(Config.local.openworkflows.Contains(wf._id))
                        {
                            onOpenWorkflow(wf);
                        }
                    }
                }

                AddHotKeys();
            });
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
            foreach (TabItem tab in mainTabControl.Items)
            {
                if (tab.Content is Views.WFDesigner)
                {
                    Views.WFDesigner _designer = tab.Content as Views.WFDesigner;
                    if (_designer.Workflow._id == workflowid) return _designer;
                }
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
                                case JTokenType.Integer: param.Add(k.Key, k.Value.Value<int>()); break;
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
            // automation threads will not allways abort, and mousemove hook will "hang" the application for several seconds
            Environment.Exit(Environment.ExitCode);

        }
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainTabControl.SelectedContent == null) return;
            NotifyPropertyChanged("VisualTracking");
            NotifyPropertyChanged("SlowMotion");
            NotifyPropertyChanged("Minimize");
        }
        private void WebSocketClient_OnOpen()
        {
            AutomationHelper.syncContext.Post(async o =>
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Log.Debug("WebSocketClient_OnOpen::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                LabelStatusBar.Content = "Connected to " + Config.local.wsurl;
                TokenUser user = null;
                while (user == null)
                {
                    string errormessage = string.Empty;
                    if (!string.IsNullOrEmpty(Config.local.username))
                    {
                        try
                        {
                            LabelStatusBar.Content = "Connected to " + Config.local.wsurl + " signing in as " + Config.local.username + " ...";
                            Log.Debug("Signing in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.username, Config.local.UnprotectString(Config.local.password));
                            Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            LabelStatusBar.Content = "Connected to " + Config.local.wsurl + " as " + user.name;
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
                            var w = new Views.LoginWindow();
                            w.username = Config.local.username;
                            w.errormessage = errormessage;
                            w.fqdn = new Uri(Config.local.wsurl).Host;
                            this.Hide();
                            if (w.ShowDialog() != true) { this.Show(); return; }
                            Config.local.username = w.username; Config.local.password = Config.local.ProtectString(w.password);
                            Config.Save();
                            loginInProgress = false;
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

                        Log.Debug("Get workflows from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}");
                        Log.Debug("Get projects from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var projects = await global.webSocketClient.Query<Project>("openrpa", "{_type: 'project'}");
                        Log.Debug("Get detectors from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        var detectors = await global.webSocketClient.Query<Interfaces.entity.Detector>("openrpa", "{_type: 'detector'}");
                        Log.Debug("Done getting workflows and projects " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        foreach (var d in detectors)
                        {
                            IDetectorPlugin dp = null;
                            d.Path = Extensions.projectsDirectory;
                            dp = Plugins.AddDetector(d);
                            dp.OnDetector += OnDetector;
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

                    try
                    {
                        Log.Debug("Registering queue for robot " + global.webSocketClient.user._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        await global.webSocketClient.RegisterQueue(global.webSocketClient.user._id);
                        foreach(var role in global.webSocketClient.user.roles)
                        {
                            Log.Debug("Registering queue for role " + role.name + " " + role._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            await global.webSocketClient.RegisterQueue(role._id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error RegisterQueue" + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                    MessageBox.Show("WebSocketClient_OnOpen::Sync projects " + ex.Message);
                }
                Log.Debug("WebSocketClient_OnOpen::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                LabelStatusBar.Content = "Connected to " + Config.local.wsurl + " as " + user.name;
                if (Projects.Count > 0)
                {
                    Projects[0].IsExpanded = true;
                    // foreach (TabItem tab in mainTabControl.Items)
                    //    var tab = mainTabControl.SelectedContent as Views.OpenProject;
                    //if(tab != null)
                    //{
                    //    if(tab.listWorkflows.Items.Count > 0)
                    //    {
                    //        Console.WriteLine(tab.listWorkflows.SelectedValuePath);
                    //        // = 
                    //        //foreach (var childItem in tab.listWorkflows.Items.OfType<TreeViewItem>())
                    //        //{
                    //        //    childItem.IsExpanded = true;
                    //        //}
                    //        //var stack = new Stack<TreeViewItem>(tab.listWorkflows.Items.Cast<TreeViewItem>());
                    //        //TreeViewItem item = stack.Pop();
                    //        //item.IsExpanded = true;
                    //    }
                    //}

                    foreach (var p in Projects)
                    {
                        foreach (var wf in p.Workflows)
                        {
                            if (Config.local.openworkflows.Contains(wf._id))
                            {
                                onOpenWorkflow(wf);
                            }
                        }
                    }

                    //Log.Debug("Opening first project");
                    ////onOpenProject(Projects[0]);
                    //var wf = Projects[0].Workflows.FirstOrDefault();
                    //if(wf!=null)
                    //{
                    //    onOpenWorkflow(wf);
                    //}                    
                }
            }, null);
        }

        private Views.KeyboardSeqWindow view = null;
        private void Cancelkey_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (view != null) return;
            try
            {
                view = new Views.KeyboardSeqWindow();
                //Hide();
                if (view.ShowDialog() == true)
                {
                    cancelkey.Text = view.Text;
                    Config.local.cancelkey = view.Text;
                    Config.Save();
                    OpenRPA.Input.InputDriver.Instance.initCancelKey(cancelkey.Text);
                } else
                {
                    Console.WriteLine("done");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                MessageBox.Show("cancelkey_GotKeyboardFocus: " + ex.ToString());
            }
            finally
            {
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
                    System.Diagnostics.Trace.WriteLine(String.Format("Downloading file from '{0}' to '{1}'", source, dest));
                    webclient.DownloadFile(source, dest);
                    System.Diagnostics.Trace.WriteLine(String.Format("Download completed"));
                }
        }

    }
}
