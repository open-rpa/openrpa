using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.AvalonDock.Layout;

namespace OpenRPA
{
    public class RobotInstance : IOpenRPAClient
    {
        private RobotInstance()
        {
            reloadTimer = new System.Timers.Timer(Config.local.reloadinterval.TotalMilliseconds);
            reloadTimer.Elapsed += ReloadTimer_Elapsed;
            if(InitializeOTEL())
            {
                //metricTime = new System.Timers.Timer(5000);
                //metricTime.Elapsed += metricTime_Elapsed;
                //metricTime.Start();
            }
        }
        //public static Prometheus.Client.Collectors.CollectorRegistry registry = new Prometheus.Client.Collectors.CollectorRegistry();
        //public static Prometheus.Client.MetricFactory factory = new Prometheus.Client.MetricFactory(registry);
        //public static Prometheus.Client.Abstractions.IMetricFamily<Prometheus.Client.Abstractions.ICounter, (string, string, string)> activity_counter = 
        //    factory.CreateCounter("openrpa_activity_counter", "Total number of acitivity activations", labelNames: ("activity", "type", "workflow"));
        //public static Prometheus.Client.Abstractions.IMetricFamily<Prometheus.Client.Abstractions.IHistogram, (string, string, string)> activity_duration = 
        //    factory.CreateHistogram("openrpa_activity_duration", "Duration of each acitivity activation",
        //        buckets: new[] { 0.1, 0.3, 0.5, 0.7, 1, 3, 5, 7, 10 },
        //        labelNames: ("activity", "type", "workflow"));
        //public static Prometheus.Client.Abstractions.IGauge mem_used = factory.CreateGauge("openrpa_memory_size_used_bytes", "Amount of heap memory usage for OpenRPA client");
        //public static Prometheus.Client.Abstractions.IGauge mem_total = factory.CreateGauge("openrpa_memory_size_total_bytes", "Amount of heap memory usage for OpenRPA client");
        public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Project>();
        public int ProjectCount
        {
            get
            {
                int result = 0;
                GenericTools.RunUI(()=> { result = Projects.Count; });
                return result;
            }
        }
        private readonly System.Timers.Timer reloadTimer = null;
        //private readonly System.Timers.Timer metricTime= null;
        public bool isReadyForAction { get; set; } = false;
        public event StatusEventHandler Status;
        public event SignedinEventHandler Signedin;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event ReadyForActionEventHandler ReadyForAction;
        private static RobotInstance _instance = null;
        // readonly Updates updater = new Updates();
        public static RobotInstance instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RobotInstance();
                    global.OpenRPAClient = _instance;
                    Interfaces.IPCService.OpenRPAServiceUtil.InitializeService();
                }
                return _instance;
            }
        }
        public string robotqueue = "";
        public bool autoReconnect = true;
        public bool loginInProgress = false;
        private bool first_connect = true;
        private bool? _isRunningInChildSession = null;
        public bool isRunningInChildSession
        {
            get {
                if (_isRunningInChildSession != null) return _isRunningInChildSession.Value;
                try
                {
                    var CurrentP = System.Diagnostics.Process.GetCurrentProcess();
                    var mywinstation = UserLogins.QuerySessionInformation(CurrentP.SessionId, UserLogins.WTS_INFO_CLASS.WTSWinStationName);
                    if (string.IsNullOrEmpty(mywinstation)) mywinstation = "";
                    mywinstation = mywinstation.ToLower();
                    if (!mywinstation.Contains("rdp") && mywinstation != "console")
                    {
                        _isRunningInChildSession = true;
                        return true;
                    }
                    _isRunningInChildSession = false;
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return false;
                }
            }
        }
        private static readonly object statelock = new object();
        // public MainWindow MainWindow { get; set; }
        public IMainWindow Window { get; set; }
        // public AgentWindow AgentWindow { get; set; }
        public List<IWorkflowInstance> WorkflowInstances
        {
            get
            {
                var result = new List<IWorkflowInstance>();
                foreach (var wi in WorkflowInstance.Instances) result.Add(wi);
                return result;
            }
        }
        public IDesigner[] Designers
        {
            get
            {
                if (Window == null) return new Views.WFDesigner[] { };
                return Window.Designers;
            }
        }
        public bool AutoReloading
        {
            get
            {
                return reloadTimer.Enabled;
            }
            set
            {
                if (reloadTimer.Enabled = value)
                {
                    reloadTimer.Stop();
                    reloadTimer.Start();
                    return;
                }
                if (value == true) reloadTimer.Start();
                if (value == false) reloadTimer.Stop();
            }
        }
        public void MainWindowReadyForAction()
        {
            Log.FunctionIndent("RobotInstance", "MainWindowReadyForAction");
            GenericTools.RunUI(() =>
            {
                try
                {
                    if (App.splash != null)
                    {
                        App.splash.Close();
                        App.splash = null;
                    }
                    if(!Config.local.isagent) Show();
                    ReadyForAction?.Invoke();
                    Input.InputDriver.Instance.Initialize();

                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            Log.FunctionOutdent("RobotInstance", "MainWindowReadyForAction");
        }
        private void ReloadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            reloadTimer.Stop();
            _ = LoadServerData();
        }
        public IDesigner GetWorkflowDesignerByIDOrRelativeFilename(string IDOrRelativeFilename)
        {
            Log.FunctionIndent("RobotInstance", "GetWorkflowDesignerByIDOrRelativeFilename");
            foreach (var designer in Designers)
            {
                if (designer.Workflow._id == IDOrRelativeFilename) return designer;
                if (designer.Workflow.RelativeFilename.ToLower().Replace("\\", "/") == IDOrRelativeFilename.ToLower().Replace("\\", "/")) return designer;
            }
            Log.FunctionOutdent("RobotInstance", "GetWorkflowDesignerByIDOrRelativeFilename");
            return null;
        }
        public IWorkflow GetWorkflowByIDOrRelativeFilename(string IDOrRelativeFilename)
        {
            Log.FunctionIndent("RobotInstance", "GetWorkflowByIDOrRelativeFilename");
            foreach (var p in Projects)
            {
                foreach (var wf in p.Workflows)
                {
                    if (wf._id == IDOrRelativeFilename) return wf;
                    if (wf.RelativeFilename.ToLower().Replace("\\", "/") == IDOrRelativeFilename.ToLower().Replace("\\", "/")) return wf;
                }
            }
            Log.FunctionOutdent("RobotInstance", "GetWorkflowByIDOrRelativeFilename");
            return null;
        }
        public IWorkflowInstance GetWorkflowInstanceByInstanceId(string InstanceId)
        {
            Log.FunctionIndent("RobotInstance", "GetWorkflowInstanceByInstanceId");
            var result = WorkflowInstance.Instances.Where(x => x.InstanceId == InstanceId).FirstOrDefault();
            Log.FunctionOutdent("RobotInstance", "GetWorkflowInstanceByInstanceId");
            return result;
        }
        private async Task CheckForUpdatesAsync()
        {
            Log.Function("RobotInstance", "CheckForUpdatesAsync");
            if (!Config.local.doupdatecheck) return;
            if ((DateTime.Now - Config.local.lastupdatecheck) < Config.local.updatecheckinterval) return;
            await Task.Run(() =>
            {
                Log.FunctionIndent("RobotInstance", "CheckForUpdatesAsync");
                try
                {
                    //if (Config.local.autoupdateupdater)
                    //{
                    //    if (await updater.UpdaterNeedsUpdate() == true)
                    //    {
                    //        await updater.UpdateUpdater();
                    //    }
                    //}
                    //var newversion = await updater.OpenRPANeedsUpdate();
                    //if (!string.IsNullOrEmpty(newversion))
                    //{
                    //    if (newversion.EndsWith(".0")) newversion = newversion.Substring(0, newversion.Length - 2);
                    //    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    //    var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                    //    string version = fileVersionInfo.ProductVersion;
                    //    if (version.EndsWith(".0")) version = version.Substring(0, version.Length - 2);
                    //    var dialogResult = System.Windows.MessageBox.Show("A new version " + newversion + " is ready for download, current version is " + version, "Update available", System.Windows.MessageBoxButton.YesNo);
                    //    if (dialogResult == System.Windows.MessageBoxResult.Yes)
                    //    {
                    //        //OnManagePackages(null);
                    //        // System.Diagnostics.Process.Start("https://github.com/open-rpa/openrpa/releases/download/" + newversion + "/OpenRPA.exe");
                    //        System.Diagnostics.Process.Start("https://github.com/open-rpa/openrpa/releases/download/" + newversion + "/OpenRPA.msi");
                    //        System.Windows.Application.Current.Shutdown();
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
                Log.FunctionOutdent("RobotInstance", "CheckForUpdatesAsync");
            });
        }
        internal void MainWindowStatus(string message)
        {
            try
            {
                Log.Function("RobotInstance", "MainWindowStatus", message);
                Status?.Invoke(message);
            }
            catch (Exception)
            {
            }
        }
        public async Task LoadServerData()
        {
            Log.FunctionIndent("RobotInstance", "LoadServerData");
            if (!global.isSignedIn)
            {
                Log.FunctionOutdent("RobotInstance", "LoadServerData", "Not signed in");
                return;
            }
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            try
            {
                if (ProjectCount == 0)
                {
                    SetStatus("Loading workflows and state");
                    Log.Debug("Get workflows from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    var workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}", orderby: "{\"projectid\":-1,\"name\":-1}", top: 5000);
                    workflows = workflows.OrderBy(x => x.name).ToArray();
                    Log.Debug("Get projects from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    var projects = await global.webSocketClient.Query<Project>("openrpa", "{_type: 'project'}", orderby: "{\"name\":-1}");
                    projects = projects.OrderBy(x => x.name).ToArray();
                    Log.Debug("Get detectors from server " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    var detectors = await global.webSocketClient.Query<Interfaces.entity.Detector>("openrpa", "{_type: 'detector'}");
                    Log.Debug("Done getting workflows and projects " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    CreateMainWindow();
                    SetStatus("Initialize detecors");
                    foreach (var d in detectors)
                    {
                        IDetectorPlugin dp = null;
                        d.Path = Interfaces.Extensions.ProjectsDirectory;
                        dp = Plugins.AddDetector(this, d);
                        if (dp != null) dp.OnDetector += Window.OnDetector;
                        if (dp == null) Log.Error("Detector " + d.name + " not loaded! (plugin: " + d.Plugin + ")");
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
                    SetStatus("Initialize projects and workflows ");
                    foreach (var p in projects)
                    {
                        SetStatus("Install project dependencies");
                        try
                        {
                            await p.InstallDependencies(true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                        SetStatus("Initialize " + p.name);
                        p.Path = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, p.name);
                        p.Workflows = new System.Collections.ObjectModel.ObservableCollection<IWorkflow>();
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
                        GenericTools.RunUI(() => Projects.Add(p));
                    }
                    Project up = null;
                    foreach (var wf in workflows)
                    {
                        var hasProject = RobotInstance.instance.Projects.Where(x => x._id == wf.projectid && !string.IsNullOrEmpty(wf.projectid)).FirstOrDefault();
                        if (hasProject == null)
                        {
                            if (up == null) up = await Project.Create(Interfaces.Extensions.ProjectsDirectory, "Unknown", false);
                            wf.Project = up;
                            up.Workflows.Add(wf);
                        }
                    }
                    if (up != null) GenericTools.RunUI(() => Projects.Add(up));
                }
                else
                {
                    Log.Debug("Reloading server data");
                    SetStatus("Fetching projects");
                    var projects = await global.webSocketClient.Query<Project>("openrpa", "{_type: 'project'}", top: 5000);
                    SetStatus("Fetching workflows");
                    var workflows = await global.webSocketClient.Query<Workflow>("openrpa", "{_type: 'workflow'}", orderby: "{\"projectid\":-1,\"name\":-1}", top: 5000);
                    SetStatus("Fetching detectors");
                    var detectors = await global.webSocketClient.Query<Interfaces.entity.Detector>("openrpa", "{_type: 'detector'}");
                    GenericTools.RunUI(async () =>
                    {
                        foreach (var project in projects)
                        {
                            project.Path = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, project.name);
                            Project exists = RobotInstance.instance.Projects.Where(x => x._id == project._id).FirstOrDefault();
                            if (exists != null && exists._version != project._version)
                            {
                                int index = -1;
                                try
                                {
                                    Log.Information("Updating project " + project.name);
                                    index = RobotInstance.instance.Projects.IndexOf(exists);
                                    project.SaveFile();
                                    RobotInstance.instance.Projects.Remove(exists);
                                    RobotInstance.instance.Projects.Insert(index, project);
                                    SetStatus("Install project dependencies");
                                    try
                                    {
                                        await project.InstallDependencies(true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex.ToString());
                                    }                                    
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("project1, index: " + index.ToString());
                                    Log.Error(ex.ToString());
                                }
                            }
                            else if (exists == null)
                            {
                                project.SaveFile();
                                RobotInstance.instance.Projects.Add(project);

                            }
                        }
                        foreach (var workflow in workflows)
                        {
                            IWorkflow exists = null;
                            Project project = RobotInstance.instance.Projects.Where(x => x._id == workflow.projectid).FirstOrDefault();
                            workflow.Project = project;

                            RobotInstance.instance.Projects.ForEach(p =>
                            {
                                try
                                {
                                    if (exists == null)
                                    {
                                        if (p.Workflows == null) p.Workflows = new System.Collections.ObjectModel.ObservableCollection<IWorkflow>();
                                        var temp = p.Workflows.Where(x => x.IDOrRelativeFilename == workflow.IDOrRelativeFilename).FirstOrDefault();
                                        if (temp != null)
                                        {
                                            exists = temp;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.ToString());
                                }
                            });
                            if (exists != null && exists.current_version != workflow._version)
                            {
                                if (!(RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(workflow.IDOrRelativeFilename) is Views.WFDesigner designer))
                                {
                                    int index = -1;
                                    try
                                    {
                                        if (project.Workflows == null) project.Workflows = new System.Collections.ObjectModel.ObservableCollection<IWorkflow>();
                                        index = project.Workflows.IndexOf(exists);
                                        project.Workflows.Remove(exists);
                                        project.Workflows.Insert(index, workflow);
                                        workflow.SaveFile();
                                        project.NotifyPropertyChanged("Workflows");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error("project2, index: " + index.ToString());
                                        Log.Error(ex.ToString());
                                    }
                                }
                                else
                                {
                                    //var messageBoxResult = MessageBox.Show(workflow.name + " has been updated by " + workflow._modifiedby + ", reload workflow ?", "Workflow has been updated", 
                                    //    MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly);
                                    var messageBoxResult = System.Windows.MessageBox.Show(workflow.name + " has been updated by " + workflow._modifiedby + ", reload workflow ?", "Workflow has been updated",
                                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.None, System.Windows.MessageBoxResult.Yes);
                                    if (messageBoxResult == System.Windows.MessageBoxResult.Yes)
                                    {
                                        int index = -1;
                                        designer.forceHasChanged(false);
                                        designer.tab.Close();
                                        index = project.Workflows.IndexOf(exists);
                                        project.Workflows.Remove(exists);
                                        project.Workflows.Insert(index, workflow);
                                        workflow.SaveFile();
                                        project.NotifyPropertyChanged("Workflows");
                                        Window.OnOpenWorkflow(workflow);
                                    }
                                    else
                                    {
                                        designer.Workflow.current_version = workflow._version;
                                    }
                                }
                            }
                            else if (exists == null)
                            {
                                project = RobotInstance.instance.Projects.Where(p => p._id == workflow.projectid).FirstOrDefault();
                                if (project != null)
                                {
                                    Log.Information("Adding " + workflow.name + " to project " + project.name);
                                    workflow.Project = project;
                                    if (project.Workflows == null) project.Workflows = new System.Collections.ObjectModel.ObservableCollection<IWorkflow>();
                                    project.Workflows.Add(workflow);
                                    workflow.SaveFile();
                                    project.NotifyPropertyChanged("Workflows");
                                }
                                else
                                {
                                    Log.Information("No project found, so disposing workflow " + workflow.name);
                                }
                            }
                            else
                            {
                                // workflow not new and not updated, so dispose
                            }
                        }
                        Log.Debug("Done getting workflows and projects " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        SetStatus("Initialize detecors");
                        foreach (var d in detectors)
                        {
                            IDetectorPlugin exists = Plugins.detectorPlugins.Where(x => x.Entity._id == d._id).FirstOrDefault();
                            if (exists != null && d._version != exists.Entity._version)
                            {
                                exists.Stop();
                                exists.OnDetector -= Window.OnDetector;
                                Plugins.detectorPlugins.Remove(exists);
                                exists = Plugins.AddDetector(RobotInstance.instance, d);
                                exists.OnDetector += Window.OnDetector;
                            }
                            else if (exists == null)
                            {
                                exists = Plugins.AddDetector(RobotInstance.instance, d);
                                if (exists != null)
                                {
                                    exists.OnDetector += Window.OnDetector;
                                }
                                else { Log.Information("Failed loading detector " + d.name); }

                            }
                        }
                        foreach (var d in Plugins.detectorPlugins.ToList())
                        {
                            var exists = detectors.Where(x => x._id == d.Entity._id).FirstOrDefault();
                            if (exists == null)
                            {
                                d.Stop();
                                d.OnDetector -= Window.OnDetector;
                                Plugins.detectorPlugins.Remove(d);
                            }
                        }

                        RobotInstance.instance.Projects.ToList().ForEach(p =>
                        {
                            try
                            {
                                Workflow wfexists = null;
                                if (p.Workflows == null) p.Workflows = new System.Collections.ObjectModel.ObservableCollection<IWorkflow>();
                                foreach (var workflow in p.Workflows.ToList())
                                {
                                    wfexists = workflows.Where(x => x.IDOrRelativeFilename == workflow.IDOrRelativeFilename).FirstOrDefault();
                                    if (wfexists == null)
                                    {
                                        var designer = RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(workflow.IDOrRelativeFilename);
                                        if (designer == null)
                                        {
                                            p.Workflows.Remove(workflow);
                                            try
                                            {
                                                System.IO.File.Delete(workflow.FilePath);
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Error(ex.ToString());
                                            }
                                        }
                                    }
                                }
                                Project projexists = null;
                                projexists = projects.Where(x => x._id == p._id).FirstOrDefault();
                                if (wfexists == null)
                                {
                                    if (p.Workflows.Count == 0)
                                    {
                                        RobotInstance.instance.Projects.Remove(p);
                                        try
                                        {
                                            var projectfilepath = System.IO.Path.Combine(p.Path, p.Filename);
                                            System.IO.File.Delete(projectfilepath);
                                            System.IO.Directory.Delete(p.Path);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex.ToString());
                                        }

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.ToString());
                            }
                        });
                    });

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
            finally
            {
                if(global.webSocketClient.user != null)
                {
                    SetStatus("Connected to " + Config.local.wsurl + " as " + global.webSocketClient.user.name);
                }
                AutoReloading = true;
            }
            Log.FunctionOutdent("RobotInstance", "LoadServerData");
        }
        private void SetStatus(string message)
        {
            Log.FunctionIndent("RobotInstance", "SetStatus", "Status?.Invoke");
            try
            {
                Status?.Invoke(message);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.Function("RobotInstance", "SetStatus", "Window.SetStatus");
            try
            {
                if(Window!=null) Window.SetStatus(message);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("RobotInstance", "SetStatus");
        }
        public void ParseCommandLineArgs(IList<string> args)
        {
            Log.FunctionIndent("RobotInstance", "ParseCommandLineArgs");
            try
            {
                CommandLineParser parser = new CommandLineParser();
                // parser.Parse(string.Join(" ", args), true);
                var options = parser.Parse(args, true);
                if (options.ContainsKey("workflowid"))
                {
                    Interfaces.IPCService.OpenRPAServiceUtil.RemoteInstance.RunWorkflowByIDOrRelativeFilename(options["workflowid"].ToString(), false, options);
                }
            }
            catch (Exception ex)
            {
                App.notifyIcon.ShowBalloonTip(1000, "", ex.Message, System.Windows.Forms.ToolTipIcon.Error);
            }
            Log.FunctionOutdent("RobotInstance", "ParseCommandLineArgs");
        }
        public void ParseCommandLineArgs()
        {
            ParseCommandLineArgs(Environment.GetCommandLineArgs());
        }
        public async Task init()
        {
            Log.FunctionIndent("RobotInstance", "init");
            SetStatus("Checking for updates");
            Config.Save();
            SetStatus("Checking for updates");
            _ = CheckForUpdatesAsync();
            try
            {

                if (string.IsNullOrEmpty(Config.local.wsurl))
                {
                    CreateMainWindow();
                    SetStatus("loading detectors");
                    var Detectors = Interfaces.entity.Detector.loadDetectors(Interfaces.Extensions.ProjectsDirectory);
                    foreach (var d in Detectors)
                    {
                        IDetectorPlugin dp = null;
                        d.Path = Interfaces.Extensions.ProjectsDirectory;
                        dp = Plugins.AddDetector(RobotInstance.instance, d);
                        if (dp != null) dp.OnDetector += Window.OnDetector;
                    }
                }
                try
                {
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            try
            {
                if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    global.webSocketClient = new Net.WebSocketClient(Config.local.wsurl);
                    global.webSocketClient.OnOpen += RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    SetStatus("Connecting to " + Config.local.wsurl);
                    _ = global.webSocketClient.Connect();
                }
                else
                {
                    SetStatus("loading projects and workflows");
                    var _Projects = await Project.LoadProjects(Interfaces.Extensions.ProjectsDirectory);
                    RobotInstance.instance.Projects = new System.Collections.ObjectModel.ObservableCollection<Project>();
                    foreach (Project p in _Projects)
                    {
                        RobotInstance.instance.Projects.Add(p);
                    }

                    System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = true;
                    System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
                    if (first_connect)
                    {
                        SetStatus("Run pending workflow instances");
                        Log.Debug("RunPendingInstances::begin ");
                        foreach (Project p in _Projects)
                        {
                            foreach (var workflow in p.Workflows)
                            {
                                if (workflow.Project != null)
                                {
                                    workflow.RunPendingInstances();
                                }

                            }
                        }
                        Log.Debug("RunPendingInstances::end ");
                    }
                    GenericTools.RunUI(() =>
                    {
                        if (App.splash != null)
                        {
                            App.splash.Close();
                            App.splash = null;
                        }
                        if(!Config.local.isagent) Show();
                        ReadyForAction?.Invoke();
                    });
                    if(!isReadyForAction)
                    {
                        ParseCommandLineArgs();
                        isReadyForAction = true;
                    }
                }
                AutomationHelper.init();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("RobotInstance", "init");
        }
        private void Hide()
        {
            Log.FunctionIndent("RobotInstance", "Hide");
            GenericTools.RunUI(() => {
                if (App.splash != null) App.splash.Hide();
                if (Window != null) Window.Hide();
            });
            Log.FunctionOutdent("RobotInstance", "Hide");
        }
        private void CreateMainWindow()
        {
            if (Window == null)
            {
                var isagent = Config.local.isagent;
                AutomationHelper.syncContext.Send(o =>
                {
                    if(!Config.local.isagent && global.webSocketClient != null)
                    {
                        if(global.webSocketClient.user != null)
                        {
                            if(global.webSocketClient.user.hasRole("robot agent users"))
                            {
                                isagent = true;
                            }
                        }
                    }
                    SetStatus("Creating main window");
                    if (!isagent)
                    {
                        var win = new MainWindow();
                        App.Current.MainWindow = win;
                        Window = win;
                        Window.ReadyForAction += MainWindowReadyForAction;
                        Window.Status += MainWindowStatus;
                        GenericTools.MainWindow = win;
                    }
                    else
                    {
                        var win = new AgentWindow();
                        App.Current.MainWindow = win;
                        Window = win;
                        Window.ReadyForAction += MainWindowReadyForAction;
                        Window.Status += MainWindowStatus;
                        GenericTools.MainWindow = win;
                    }
                    // ExpressionEditor.EditorUtil.Init();
                    _ = CodeEditor.init.Initialize();
                }, null);

            }

        }
        private void Show()
        {
            Log.FunctionIndent("RobotInstance", "Show");
            GenericTools.RunUI(() => {
                if (App.splash != null)
                {
                    App.splash.Show();
                } else
                {
                    if (Window != null) Window.Show();
                }
            });
            Log.FunctionOutdent("RobotInstance", "Show");
        }
        private void Close()
        {
            Log.FunctionIndent("RobotInstance", "Close");
            GenericTools.RunUI(() => {
                if (App.splash != null) App.splash.Close();
                if (Window != null) Window.Close();
                System.Windows.Application.Current.Shutdown();
            });
            Log.FunctionOutdent("RobotInstance", "Close");
        }
        private async void RobotInstance_WebSocketClient_OnOpen()
        {
            Log.FunctionIndent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
            try
            {
                Connected?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Interfaces.entity.TokenUser user = null;
            try
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
                            Log.Error(ex, "");
                            errormessage = ex.Message;
                        }
                    }
                    if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                    {
                        try
                        {
                            SetStatus("Sign in to " + Config.local.wsurl);
                            Log.Debug("Signing in with token " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt));
                            if (user != null)
                            {
                                Config.local.username = user.username;
                                Config.local.password = new byte[] { };
                                // Config.Save();
                                Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                            }
                        }
                        catch (Exception ex)
                        {
                            Hide();
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
                                GenericTools.RunUI(async () =>
                                {
                                    try
                                    {
                                        Log.Debug("Create SigninWindow " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                        var signinWindow = new Views.SigninWindow(url, true);
                                        Log.Debug("ShowDialog " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
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
                                            Log.Debug("Call close " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                            Close();
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex.ToString());
                                    }
                                });


                            }
                            catch (Exception)
                            {
                                throw;
                            }
                            finally
                            {
                                loginInProgress = false;
                            }

                        }
                        else
                        {
                            return;
                        }
                    }
                }
                InitializeOTEL();
                await LoadServerData();
                try
                {
                    SetStatus("Run pending workflow instances");
                    Log.Debug("RunPendingInstances::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    await WorkflowInstance.RunPendingInstances();
                    Log.Debug("RunPendingInstances::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                Log.Debug("WebSocketClient_OnOpen::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));

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
                try
                {

                    bool registerqueues = true;
                    if(Interfaces.win32.ChildSession.IsChildSessionsEnabled())
                    {
                        var CurrentP = System.Diagnostics.Process.GetCurrentProcess();
                        var myusername = UserLogins.QuerySessionInformation(CurrentP.SessionId, UserLogins.WTS_INFO_CLASS.WTSUserName);
                        var mydomain = UserLogins.QuerySessionInformation(CurrentP.SessionId, UserLogins.WTS_INFO_CLASS.WTSDomainName);
                        var mywinstation = UserLogins.QuerySessionInformation(CurrentP.SessionId, UserLogins.WTS_INFO_CLASS.WTSWinStationName);

                        if (string.IsNullOrEmpty(mywinstation)) mywinstation = "";
                        mywinstation = mywinstation.ToLower();
                        if (!mywinstation.Contains("rdp") && mywinstation != "console")
                        {
                            Log.Debug("my WTSUserName: " + myusername);
                            Log.Debug("my WTSDomainName: " + mydomain);
                            Log.Debug("my WTSWinStationName: " + mywinstation);
                            registerqueues = false;
                            Log.Warning("mywinstation is empty or does not contain RDP, skip registering queues");
                        }
                        else
                        {
                            var processes = System.Diagnostics.Process.GetProcessesByName("explorer");
                            foreach (var ps in processes)
                            {
                                var username = UserLogins.QuerySessionInformation(ps.SessionId, UserLogins.WTS_INFO_CLASS.WTSUserName);
                                var domain = UserLogins.QuerySessionInformation(ps.SessionId, UserLogins.WTS_INFO_CLASS.WTSDomainName);
                                var winstation = UserLogins.QuerySessionInformation(ps.SessionId, UserLogins.WTS_INFO_CLASS.WTSWinStationName);
                                Log.Debug("WTSUserName: " + username);
                                Log.Debug("WTSDomainName: " + domain);
                                Log.Debug("WTSWinStationName: " + winstation);
                            }
                        }
                        //int ConsoleSession = NativeMethods.WTSGetActiveConsoleSessionId();
                        ////uint SessionId = Interfaces.win32.ChildSession.GetChildSessionId();
                        //var p = System.Diagnostics.Process.GetCurrentProcess();
                        //if (ConsoleSession != p.SessionId)
                        //{
                        //    Log.Warning("Child sessions enabled and not running as console, skip registering queues");
                        //    registerqueues = false;
                        //}
                    }
                    if(registerqueues)
                    {
                        SetStatus("Registering queues");
                        Log.Debug("Registering queue for robot " + user._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        robotqueue = await global.webSocketClient.RegisterQueue(user._id);

                        foreach (var role in global.webSocketClient.user.roles)
                        {
                            var roles = await global.webSocketClient.Query<Interfaces.entity.apirole>("users", "{_id: '" + role._id + "'}", top: 5000);
                            if (roles.Length == 1 && roles[0].rparole)
                            {
                                SetStatus("Add queue " + role.name);
                                Log.Debug("Registering queue for role " + role.name + " " + role._id + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                await global.webSocketClient.RegisterQueue(role._id);
                            }
                        }
                    }

                    if (!isReadyForAction)
                    {
                        ParseCommandLineArgs();
                        isReadyForAction = true;
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
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            if (Window != null)
            {
                Window.MainWindow_WebSocketClient_OnOpen();
            }
            try
            {
                SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            if(first_connect)
            {
                first_connect = false;
                GenericTools.RunUI(() =>
                {
                    try
                    {
                        if (App.splash != null)
                        {
                            App.splash.Close();
                            App.splash = null;
                        }
                        if(!Config.local.isagent) Show();
                        ReadyForAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
            Log.FunctionOutdent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
        }
        private async void WebSocketClient_OnClose(string reason)
        {
            Log.FunctionIndent("RobotInstance", "WebSocketClient_OnClose", reason);
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
            await Task.Delay(5000);
            if (autoReconnect)
            {
                try
                {
                    autoReconnect = false;
                    global.webSocketClient.OnOpen -= RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose -= WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
                    global.webSocketClient = null;

                    global.webSocketClient = new Net.WebSocketClient(Config.local.wsurl);
                    global.webSocketClient.OnOpen += RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    SetStatus("Connecting to " + Config.local.wsurl);

                    await global.webSocketClient.Connect();
                    autoReconnect = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            Log.FunctionOutdent("RobotInstance", "WebSocketClient_OnClose");
        }
        private async void WebSocketClient_OnQueueMessage(IQueueMessage message, QueueMessageEventArgs e)
        {
            Log.FunctionIndent("RobotInstance", "WebSocketClient_OnQueueMessage");
            Interfaces.mq.RobotCommand command = null;
            try
            {
                command = Newtonsoft.Json.JsonConvert.DeserializeObject<Interfaces.mq.RobotCommand>(message.data.ToString());
                if (command.command == "invokecompleted" || command.command == "invokefailed" || command.command == "invokeaborted" || command.command == "error" || command.command == null
                    || command.command == "timeout")
                {
                    if (!string.IsNullOrEmpty(message.correlationId))
                    {
                        Log.Function("RobotInstance", "WebSocketClient_OnQueueMessage", "loop instances");
                        foreach (var wi in WorkflowInstance.Instances.ToList())
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
                if (data != null && data.ContainsKey("payload"))
                {
                    data = data.Value<JObject>("payload");
                }
                if (command.command == null) return;
                if (command.command == "invoke" && !string.IsNullOrEmpty(command.workflowid))
                {
                    Log.Function("RobotInstance", "WebSocketClient_OnQueueMessage", "Prepare workflow invoke");
                    IWorkflowInstance instance = null;
                    var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(command.workflowid);
                    if (workflow == null) throw new ArgumentException("Unknown workflow " + command.workflowid);
                    lock (statelock)
                    {
                        if (!Config.local.remote_allowed)
                        {
                            // Don't fail, just say busy and let the message expire
                            // so if this was send to a robot in a role, another robot can pick this up.
                            e.isBusy = true; return;
                        }
                        int RunningCount = 0;
                        int RemoteRunningCount = 0;
                        foreach (var i in WorkflowInstance.Instances.ToList())
                        {
                            if (i.isCompleted) lock (WorkflowInstance.Instances) WorkflowInstance.Instances.Remove(i);
                        }
                        foreach (var i in WorkflowInstance.Instances.ToList())
                        {
                            if (!string.IsNullOrEmpty(i.correlationId) && !i.isCompleted)
                            {
                                RemoteRunningCount++;
                                RunningCount++;
                            }
                            else if (i.state == "running")
                            {
                                RunningCount++;
                            }
                            if (!Config.local.remote_allow_multiple_running && RunningCount > 0)
                            {
                                if (i.Workflow != null)
                                {
                                    if(Config.local.log_busy_warning) Log.Warning("Cannot invoke " + workflow.name + ", I'm busy. (running " + i.Workflow.ProjectAndName + ")");
                                }
                                else
                                {
                                    if (Config.local.log_busy_warning) Log.Warning("Cannot invoke " + workflow.name + ", I'm busy.");
                                }
                                e.isBusy = true; return;
                            }
                            else if (Config.local.remote_allow_multiple_running && RemoteRunningCount > Config.local.remote_allow_multiple_running_max)
                            {
                                if (i.Workflow != null)
                                {
                                    if (Config.local.log_busy_warning) Log.Warning("Cannot invoke " + workflow.name + ", I'm busy. (running " + i.Workflow.ProjectAndName + ")");
                                }
                                else
                                {
                                    if (Config.local.log_busy_warning) Log.Warning("Cannot invoke " + workflow.name + ", I'm busy.");
                                }
                                e.isBusy = true; return;
                            }
                        }
                        // e.sendReply = true;
                        var param = new Dictionary<string, object>();
                        foreach (var k in data)
                        {
                            var p = workflow.Parameters.Where(x => x.name == k.Key).FirstOrDefault();
                            if (p == null) continue;
                            switch (k.Value.Type)
                            {
                                case JTokenType.Integer: param.Add(k.Key, k.Value.Value<long>()); break;
                                case JTokenType.Float: param.Add(k.Key, k.Value.Value<float>()); break;
                                case JTokenType.Boolean: param.Add(k.Key, k.Value.Value<bool>()); break;
                                case JTokenType.Date: param.Add(k.Key, k.Value.Value<DateTime>()); break;
                                case JTokenType.TimeSpan: param.Add(k.Key, k.Value.Value<TimeSpan>()); break;
                                case JTokenType.Array: param.Add(k.Key, k.Value.Value<JArray>()); break;
                                default:
                                    try
                                    {
                                        
                                        // param.Add(k.Key, k.Value.Value<string>());
                                        var v = k.Value.ToObject(Type.GetType(p.type));
                                        param.Add(k.Key, v);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Debug("WebSocketClient_OnQueueMessage: " + ex.Message);
                                    }
                                    break;

                                    // default: param.Add(k.Key, k.Value.Value<string>()); break;
                            }
                        }
                        foreach(var p in workflow.Parameters)
                        {
                            if(param.ContainsKey(p.name))
                            {
                                var value = param[p.name];
                                if (p.type == "System.Data.DataTable" && value != null)
                                {
                                    if(value is JArray)
                                    {
                                        param[p.name] = ((JArray)value).ToDataTable();
                                    }

                                } 
                                else if(p.type.EndsWith("[]"))
                                {
                                    param[p.name] = ((JArray)value).ToObject(Type.GetType(p.type));
                                }
                            }
                        }
                        Log.Information("Create instance of " + workflow.name);
                        Log.Function("RobotInstance", "WebSocketClient_OnQueueMessage", "Create instance and run workflow");
                        GenericTools.RunUI(() =>
                        {
                            command.command = "invokesuccess";
                            string errormessage = "";
                            try
                            {
                                if (RobotInstance.instance.GetWorkflowDesignerByIDOrRelativeFilename(command.workflowid) is Views.WFDesigner designer)
                                {
                                    designer.BreakpointLocations = null;
                                    instance = workflow.CreateInstance(param, message.replyto, message.correlationId, designer.IdleOrComplete, designer.OnVisualTracking, null, null);
                                    designer.Run(Window.VisualTracking, Window.SlowMotion, instance);
                                }
                                else
                                {
                                    instance = workflow.CreateInstance(param, message.replyto, message.correlationId, Window.IdleOrComplete, null, null, null);
                                    instance.Run();
                                }
                                if (Config.local.notify_on_workflow_remote_start)
                                {
                                    App.notifyIcon.ShowBalloonTip(1000, "", workflow.name + " remotly started", System.Windows.Forms.ToolTipIcon.Info);
                                }
                            }
                            catch (Exception ex)
                            {
                                command.command = "error";
                                command.data = data = JObject.FromObject(ex);
                                errormessage = ex.Message;
                                Log.Error(ex.ToString());
                            }
                        });
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
            if (command.command == "error" || ((command.command == "invoke" || command.command == "invokesuccess") && !string.IsNullOrEmpty(command.workflowid)))
            {
                if (!string.IsNullOrEmpty(message.replyto) && message.replyto != message.queuename)
                {
                    try
                    {
                        await global.webSocketClient.QueueMessage(message.replyto, command, null, message.correlationId, 0);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                    }
                }
            }
            Log.FunctionOutdent("RobotInstance", "WebSocketClient_OnQueueMessage");
        }
        //private string last_metric;
        //private System.Diagnostics.PerformanceCounter mem_used_counter;
        // private System.Diagnostics.PerformanceCounter mem_total_counter;
        // private System.Diagnostics.PerformanceCounter mem_free_counter;
        private TracerProvider StatsTracerProvider;
        private TracerProvider tracerProvider;
        // public Tracer tracer = null;
        // private InstrumentationWithActivitySource Sampler = null;
        private bool InitializeOTEL()
        {
            try
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                if(Config.local.enable_analytics && StatsTracerProvider == null)
                {
                    StatsTracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource("OpenRPA")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenRPA"))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri("https://otel.stats.openiap.io");
                    })
                    .Build();
                }
                if (!string.IsNullOrEmpty(Config.local.otel_trace_url) && tracerProvider == null)
                {
                    tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource("OpenRPA")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenRPA"))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        if(Config.local.otel_trace_url.Contains("http://") && Config.local.otel_trace_url.Contains(":80"))
                        {
                            Config.local.otel_trace_url = Config.local.otel_trace_url.Replace("http://", "https://").Replace(":80", "");
                        }
                        otlpOptions.Endpoint = new Uri(Config.local.otel_trace_url);
                    })
                    .Build();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
        //private async void metricTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        ////public static Prometheus.Client.Abstractions.IGauge mem_used = factory.CreateGauge("openrpa_memory_size_used_bytes", "Amount of heap memory usage for OpenRPA client");
        ////public static Prometheus.Client.Abstractions.IGauge mem_total = factory.CreateGauge("openrpa_memory_size_total_bytes", "Amount of heap memory usage for OpenRPA client");
        //metricTime.Stop();
        //    try
        //    {
        //        if (global.webSocketClient != null && global.webSocketClient.user != null)
        //        {
        //            //mem_used.Set(mem_used_counter.NextValue());
        //            //// mem_total.Set(mem_total_counter.NextValue());
        //            //using (var memoryStream = await Prometheus.Client.ScrapeHandler.ProcessAsync(registry))
        //            //{
        //            //    var result = System.Text.Encoding.ASCII.GetString(memoryStream.ToArray());
        //            //    if (last_metric != result)
        //            //    {
        //            //        await global.webSocketClient.PushMetrics(result);
        //            //        last_metric = result;
        //            //    }
        //            //}
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if(ex.Message == "server error: Unknown command error")
        //        {
        //            return;
        //        }
        //        Log.Error(ex.ToString());
        //    }
        //    metricTime.Start();
        //}
    }
}
