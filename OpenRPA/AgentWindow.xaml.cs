using Newtonsoft.Json.Linq;
using OpenRPA.Input;
using OpenRPA.Interfaces;
using System;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
                LoadLayout();
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
                    LoadLayout();
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
        public bool isRunningInChildSession()
        {
            if (Config.local.skip_child_session_check) return false;
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
                try
                {
                    return Interfaces.win32.ChildSession.IsChildSessionsEnabled();
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
                return false;
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
                    Log.Error(ex.ToString());
                    MessageBox.Show(ex.Message);
                }
                //Config.local.recording_add_to_designer = value;
                NotifyPropertyChanged("Setting_IsChildSessionsEnabled");
                NotifyPropertyChanged("Setting_ShowChildSessions");
            }
        }
        public ICommand PlayInChildCommand { get { return new RelayCommand<object>(OnPlayInChild, CanPlayInChild); } }
        public ICommand ChildSessionCommand { get { return new RelayCommand<object>(OnChildSessionCommand, CanAllways); } }
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
            SaveLayout();
            foreach(var s in Plugins.Storages)
            {
                s.Dispose();
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
        private bool CanAllways(object _item)
        {
            return true;
        }
        public void OnDetector(IDetectorPlugin plugin, IDetectorEvent detector, EventArgs e)
        {
            var source = new System.Diagnostics.ActivitySource("OpenRPA");
            var activity = source?.StartActivity("Detector " + plugin.Entity.name + " was triggered");
            string traceId = activity?.TraceId.ToString();
            string spanId = activity?.SpanId.ToString();
            try
            {
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
                                if (!string.IsNullOrEmpty(traceId)) wi.TraceId = traceId;
                                if (!string.IsNullOrEmpty(spanId)) wi.SpanId = spanId;
                                wi.ResumeBookmark(b.Key, detector, true);
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
                            await global.webSocketClient.QueueMessage(Entity._id, "", command, null, null, 0, true, traceId, spanId);
                        } 
                        else
                        {
                            await global.webSocketClient.QueueMessage(Entity._id, command, null, null, 0, true, traceId, spanId);
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
                activity?.SetStatus(ActivityStatusCode.Error, ex.ToString());
                Log.Error(ex.ToString());
                MessageBox.Show(ex.Message);
            }
            finally
            {
                activity?.Stop();
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
                    JObject data = null;
                    if (instance.Parameters != null) data = JObject.FromObject(instance.Parameters);
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
                            await global.webSocketClient.QueueMessage(instance.queuename, command, null, instance.correlationId, 0, true, instance.TraceId, instance.SpanId);
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
                    }, 100);
                }

                if (instance.hasError || instance.isCompleted)
                {
                    string message = "";
                    if(instance.Workflow != null)
                    {
                        if (instance.runWatch != null)
                        {
                            message += instance.Workflow.name + " " + instance.state + " in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed);
                        }
                        else
                        {
                            message += instance.Workflow.name + " " + instance.state;
                        }
                    } else
                    {
                        message += "MISSING WORKFLOW!!!! " + instance.state;
                    }
                    if (!string.IsNullOrEmpty(instance.errormessage)) message += Environment.NewLine + "# " + instance.errormessage;
                    if(Thread.CurrentThread.ManagedThreadId > 1) Tracing.InstanceId.Value = instance.InstanceId;
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
                        if (instance.Workflow != null) instance.Workflow.NotifyUIState();
                        var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                        while (sw.Elapsed < TimeSpan.FromSeconds(1))
                        {
                            if (System.Threading.Monitor.TryEnter(WorkflowInstance.Instances, Config.local.thread_lock_timeout_seconds * 1000))
                            {
                                try
                                {
                                    foreach (var wi in WorkflowInstance.Instances.ToList())
                                    {
                                        if (wi.isCompleted) continue;
                                        if (wi.Bookmarks == null) continue;
                                        foreach (var b in wi.Bookmarks)
                                        {
                                            if (b.Key == instance._id)
                                            {
                                                wi.ResumeBookmark(b.Key, instance, true);
                                                return;
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    System.Threading.Monitor.Exit(WorkflowInstance.Instances);
                                }
                            }
                            // else { throw new LockNotReceivedException("Resume bookmark"); }
                        }
                    });
                }
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
            Log.Function("AgentWindow", "OnReload");
            if (!global.isConnected)
            {
                _ = RobotInstance.instance.Connect();
            }
            else
            {
                _ = RobotInstance.instance.LoadServerData(false);
            }
        }
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
                    GenericTools.Minimize();
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
        private void OnChildSessionCommand(object _item)
        {
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
                    instance = workflow.CreateInstance(param, null, null, IdleOrComplete, null, 0);
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
        internal Views.ChildSession childSession;
        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            if (childSession == null)
            {
                childSession = new Views.ChildSession();
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

        private void SaveLayout()
        {
            Log.FunctionIndent("MainWindow", "SaveLayout");
            try
            {
                //Config.local.openworkflows = workflows.ToArray();
                var pos = new System.Drawing.Rectangle((int)Left, (int)Top, (int)Width, (int)Height);
                if (pos.Left > 0 && pos.Top > 0 && pos.Width > 100 && pos.Height > 100)
                {
                    var newpos = pos.ToString();
                    var oldpos = Config.local.mainwindow_position.ToString();
                    if (newpos != oldpos)
                    {
                        Config.local.mainwindow_position = pos;
                        Config.Save();
                    }
                }

                try
                {
                    var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                    using (var stream = new System.IO.StreamWriter(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layoutagent.config")))
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
                    var fi = new System.IO.FileInfo("layoutagent.config");
                    var di = fi.Directory;

                    if (System.IO.File.Exists(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layoutagent.config")))
                    {
                        var ds = DManager.Layout.Descendents();
                        var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                        using (var stream = new System.IO.StreamReader(System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "layoutagent.config")))
                            serializer.Deserialize(stream);
                        ds = DManager.Layout.Descendents();
                    }
                    else if (System.IO.File.Exists("layoutagent.config"))
                    {
                        var ds = DManager.Layout.Descendents();
                        var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                        using (var stream = new System.IO.StreamReader("layoutagent.config"))
                            serializer.Deserialize(stream);
                        ds = DManager.Layout.Descendents();
                    }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(di.Parent.FullName, "layoutagent.config")))
                    {
                        var ds = DManager.Layout.Descendents();
                        var serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(DManager);
                        using (var stream = new System.IO.StreamReader(System.IO.Path.Combine(di.Parent.FullName, "layoutagent.config")))
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
            //Task.Run(() =>
            //{
            //    var sw = new System.Diagnostics.Stopwatch(); sw.Start();
            //    while (true && sw.Elapsed < TimeSpan.FromSeconds(10))
            //    {
            //        System.Threading.Thread.Sleep(200);
            //        if (Views.OpenProject.Instance != null && Views.OpenProject.Instance.Projects.Count > 0) break;
            //    }
            //    foreach (var id in Config.local.openworkflows)
            //    {
            //        var wf = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(id);
            //        if (wf != null) OnOpenWorkflow(wf);
            //    }
            //});
            Log.FunctionOutdent("MainWindow", "LoadLayout");
        }

    }
}
