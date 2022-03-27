using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.AvalonDock.Layout;

namespace OpenRPA
{
    public partial class AgentWindow : Window, INotifyPropertyChanged, IMainWindow
    {
        public bool VisualTracking { get; set; } = false;
        public bool SlowMotion { get; set; } = false;
        public static AgentWindow Instance { get; set; }
        public AgentWindow()
        {
            Instance = this;
            if (!string.IsNullOrEmpty(Config.local.culture))
            {
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(Config.local.culture);
                }
                catch (Exception)
                {
                }
            }
            System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = false;
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            InitializeComponent();
            DataContext = this;

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
            NotifyPropertyChanged("Toolbox");
            lblVersion.Text = global.version;
            WindowState = WindowState.Minimized;
        }
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log.Error(e.Exception, "");
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = (Exception)args.ExceptionObject;
            Log.Error(ex.ToString());
            Log.Error("MyHandler caught : " + ex.Message);
            Log.Error("Runtime terminating: {0}", (args.IsTerminating).ToString());
        }
        public event ReadyForActionEventHandler ReadyForAction;
        public event StatusEventHandler Status;
        bool AllowQuite = false;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                OnOpen(null);
                //if(RobotInstance.instance.ProjectCount>0)
                //{
                //    RobotInstance.instance.Projects.First().IsExpanded = true;
                //}
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
                // WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        private bool _IsLoading = false;
        public bool IsLoading { get { return _IsLoading; } set { _IsLoading = value; NotifyPropertyChanged("IsLoading"); } }
        private bool first_connect = true;
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
        public void MainWindow_WebSocketClient_OnOpen()
        {
            Log.FunctionIndent("AgentWindow", "MainWindow_WebSocketClient_OnOpen");
            if (RobotInstance.instance.Projects.Count() == 0 && first_connect)
            {
            }
            if (first_connect)
            {
                try
                {
                    ReadyForAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                first_connect = false;
            }
            Log.FunctionOutdent("AgentWindow", "MainWindow_WebSocketClient_OnOpen");
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public Tracing Tracing { get; set; } = new Tracing();
        public ICommand SignoutCommand { get { return new RelayCommand<object>(OnSignout, CanSignout); } }
        public ICommand PlayCommand { get { return new RelayCommand<object>(OnPlay, CanPlay); } }
        public ICommand StopCommand { get { return new RelayCommand<object>(OnStop, CanStop); } }
        public ICommand ExitAppCommand { get { return new RelayCommand<object>(OnExitApp, (e) => true); } }
        public ICommand ReloadCommand { get { return new RelayCommand<object>(OnReload, (e) => true); } }
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
        public object SelectedContent
        {
            get
            {
                if (DManager == null) return null;
                var b = DManager.ActiveContent;
                return b;
            }
        }
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                RobotInstance.instance.AutoReloading = false;
                Log.Information("AllowQuite: " + AllowQuite);
                if (AllowQuite && e.Cancel == false)
                {
                    foreach (var d in Plugins.detectorPlugins) d.Stop();
                    InputDriver.Instance.Dispose();
                    return;
                }
                if (AllowQuite)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                    Close();
                }
                else
                {
                    RobotInstance.instance.AutoReloading = true;
                    Visibility = Visibility.Hidden;
                    App.notifyIcon.Visible = true;
                }
                e.Cancel = !AllowQuite;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            InputDriver.Instance.CallNext = true;
            InputDriver.Instance.Dispose();
            if (RobotInstance.instance.db != null)
            {
                try
                {
                    RobotInstance.instance.db.Dispose();
                    RobotInstance.instance.db = null;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            // automation threads will not allways abort, and mousemove hook will "hang" the application for several seconds
            Application.Current.Shutdown();
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
                // App.notifyIcon.Visible = false;
            }
        }
        public void SetStatus(string message)
        {
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
        public IDesigner Designer => null;
        public IDesigner LastDesigner => null;
        public void OnDetector(IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e)
        {
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
                            var _id = plugin.Entity._id;
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
                // detector.user = global.webSocketClient.user;
                var data = JObject.FromObject(detector);
                var Entity = plugin.Entity;
                command.command = "detector";
                command.detectorid = Entity._id;
                if (string.IsNullOrEmpty(Entity._id)) return;
                command.data = data;
                Task.Run(async () =>
                {
                    try
                    {
                        if(plugin.Entity.detectortype == "exchange")
                        {
                            await global.webSocketClient.QueueMessage(Entity._id, "", command, null, null, 0);
                        } 
                        else
                        {
                            await global.webSocketClient.QueueMessage(Entity._id, command, null, null, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        public void IdleOrComplete(IWorkflowInstance instance, EventArgs e)
        {
            if (instance == null) return;
            Log.FunctionIndent("MainWindow", "IdleOrComplete");
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
                    Task.Run(async () =>
                    {
                        try
                        {
                            await global.webSocketClient.QueueMessage(instance.queuename, command, null, instance.correlationId, 0);
                        }
                        catch (Exception ex)
                        {
                            Log.Debug(ex.Message);
                        }
                    });

                }

                if (instance.state != "idle")
                {
                    GenericTools.RunUI(() =>
                    {
                        CommandManager.InvalidateRequerySuggested();
                    });
                }

                if (instance.hasError || instance.isCompleted)
                {
                    string message = "";
                    if (instance.runWatch != null)
                    {
                        message += instance.Workflow.name + " " + instance.state + " in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed);
                    }
                    else
                    {
                        message += instance.Workflow.name + " " + instance.state;
                    }
                    if (!string.IsNullOrEmpty(instance.errormessage)) message += Environment.NewLine + "# " + instance.errormessage;
                    Log.Information(message);
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
                        if (instance.Workflow != null) instance.Workflow.NotifyUIState();
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
                RobotInstance.instance.NotifyPropertyChanged("Projects");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
            }
        }
        private void OnExitApp(object _item)
        {
            AllowQuite = true;
            Close();
        }
        private void OnReload(object _item)
        {
            Log.Function("MainWindow", "OnReload");
            if (!global.isConnected)
            {
                _ = RobotInstance.instance.Connect();
            }
            else
            {
                _ = RobotInstance.instance.LoadServerData();
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
                var ld = DManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
                foreach (var document in ld)
                {
                    if (document.Content is Views.WFDesigner view) document.Close();
                }
                Config.Reload();
                Config.local.password = new byte[] { };
                Config.local.jwt = new byte[] { };
                global.webSocketClient.url = Config.local.wsurl;
                global.webSocketClient.user = null;
                _ = global.webSocketClient.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        public void OnOpen(object _item)
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
                    view.onOpenWorkflow += OnOpenWorkflow;

                    LayoutDocument layoutDocument = new LayoutDocument { Title = "Open project" };
                    layoutDocument.ContentId = "openproject";
                    layoutDocument.CanClose = false;
                    layoutDocument.Content = view;
                    MainTabControl.Children.Add(layoutDocument);
                    layoutDocument.IsSelected = true;
                    // layoutDocument.Closing += LayoutDocument_Closing;
                    RobotInstance.instance.NotifyPropertyChanged("Projects");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    MessageBox.Show(ex.Message);
                }
            }, null);
        }
        public void OnOpenWorkflow(IWorkflow obj)
        {
            OnPlay(obj);
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
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                foreach (var i in designer.Workflow.LoadedInstances)
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
                    foreach (var i in wf.LoadedInstances)
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
                foreach (var i in designer.Workflow.LoadedInstances)
                {
                    if (i.isCompleted == false)
                    {
                        i.Abort("User clicked stop");
                    }
                }
                if (designer.ResumeRuntimeFromHost != null) designer.ResumeRuntimeFromHost.Set();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                MessageBox.Show(ex.Message);
            }
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
                    if (wf.State == "running") return false;
                    if (global.isConnected)
                    {
                        return wf.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.invoke);
                    }
                    return true;
                }

                if (!IsConnected) return false;
                if (!(SelectedContent is Views.WFDesigner)) return false;
                var designer = (Views.WFDesigner)SelectedContent;
                if (designer.BreakPointhit) return true;
                foreach (var i in designer.Workflow.LoadedInstances)
                {
                    if (i.isCompleted == false)
                    {
                        return false;
                    }
                }
                if (global.webSocketClient == null) return true;
                return designer.Workflow.hasRight(global.webSocketClient.user, Interfaces.entity.ace_right.invoke);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }
        internal void OnPlay(object _item)
        {
            Workflow workflow = null;
            if (_item != null && _item is Workflow) workflow = _item as Workflow;
            if (SelectedContent is Views.OpenProject view)
            {
                if (view != null)
                {
                    var val = view.listWorkflows.SelectedValue;
                    if (val == null) return;
                    if (!(view.listWorkflows.SelectedValue is Workflow wf)) return;
                    workflow = wf;
                }
            }
            if (workflow != null)
            {
                string errormessage = "";
                try
                {
                    GenericTools.Minimize();
                    IWorkflowInstance instance;
                    var param = new Dictionary<string, object>();
                    instance = workflow.CreateInstance(param, null, null, IdleOrComplete, null);
                    instance.Run();
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
                    MessageBox.Show("onPlay " + errormessage);
                }
                return;
            }
        }

    }
}
