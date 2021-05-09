using LiteDB;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.AvalonDock.Layout;

namespace OpenRPA
{
    public class RobotInstance : IOpenRPAClient, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == "Projects")
            {
                Views.OpenProject.UpdateProjectsList();
            }
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public System.Diagnostics.ActivitySource source = new System.Diagnostics.ActivitySource("OpenRPA.RobotInstance");
        private RobotInstance()
        {
            reloadTimer = new System.Timers.Timer(Config.local.reloadinterval.TotalMilliseconds);
            reloadTimer.Elapsed += ReloadTimer_Elapsed;
            reloadTimer.Stop();
            if (InitializeOTEL())
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
        // public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Project>();
        public LiteDatabase db;
        public LiteDB.ILiteCollection<Project> Projects;
        public LiteDB.ILiteCollection<Workflow> Workflows;
        public LiteDB.ILiteCollection<Detector> Detectors;
        public LiteDB.ILiteCollection<WorkflowInstance> dbWorkflowInstances;
        public int ProjectCount
        {
            get
            {
                int result = 0;
                GenericTools.RunUI(() => { result = Projects.Count(); });
                return result;
            }
        }
        private readonly System.Timers.Timer reloadTimer = null;
        public bool isReadyForAction { get; set; } = false;
        public event StatusEventHandler Status;
        public event SignedinEventHandler Signedin;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event ReadyForActionEventHandler ReadyForAction;
        private static RobotInstance _instance = null;
        public static RobotInstance instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RobotInstance();
                    global.OpenRPAClient = _instance;
                    Interfaces.IPCService.OpenRPAServiceUtil.InitializeService();
                    BsonMapper.Global.MaxDepth = 50;
                    BsonMapper.Global.TypeDescriptor = "__type";

                    BsonMapper.Global.RegisterType<Uri>
                    (
                        serialize: (uri) => uri.AbsoluteUri,
                        deserialize: (bson) => new Uri(bson.AsString)
                    );
                    BsonMapper.Global.RegisterType<JToken>
                    (
                        serialize: (o) => o.ToString(),
                        deserialize: (bson) => JToken.Parse(bson.ToString())
                    );
                    var dbfilename = "offline.db";
                    if (!string.IsNullOrEmpty(Config.local.wsurl))
                    {
                        dbfilename = new Uri(Config.local.wsurl).Host + ".db";
                    }
                    _instance.db = new LiteDatabase(Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename);
                    _instance.Projects = _instance.db.GetCollection<Project>("projects");
                    _instance.Projects.EnsureIndex(x => x._id, true);

                    _instance.Workflows = _instance.db.GetCollection<Workflow>("workflows");
                    _instance.Workflows.EnsureIndex(x => x._id, true);

                    _instance.Detectors = _instance.db.GetCollection<Detector>("detectors");
                    _instance.Detectors.EnsureIndex(x => x._id, true);

                    _instance.dbWorkflowInstances = _instance.db.GetCollection<WorkflowInstance>("workflowinstances");
                    _instance.dbWorkflowInstances.EnsureIndex(x => x._id, true);

                    // BsonMapper.Global.Entity<Project>().DbRef(x => x.Workflows, "workflows");
                    AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
                    {
                        try
                        {
                            if (instance.db != null) instance.db.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                    };

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
            get
            {
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
        public IMainWindow Window { get; set; }
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
                if (global.openflowconfig != null && !global.openflowconfig.supports_watch)
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
                else
                {
                    reloadTimer.Stop();
                }
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
                    if (!Config.local.isagent) Show();
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
            if (!string.IsNullOrEmpty(IDOrRelativeFilename))
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
            var filename = IDOrRelativeFilename.ToLower().Replace("\\", "/");
            var result = Workflows.Find(x => x.RelativeFilename.ToLower() == filename.ToLower() || x._id == IDOrRelativeFilename).FirstOrDefault();
            Log.FunctionOutdent("RobotInstance", "GetWorkflowByIDOrRelativeFilename");
            return result;
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
            Log.Information("LoadServerData::begin");
            DisableWatch = true;
            Window.IsLoading = true;
            var span = source.StartActivity("LoadServerData", ActivityKind.Consumer);
            try
            {
                Log.FunctionIndent("RobotInstance", "LoadServerData");
                if (!global.isSignedIn)
                {
                    Log.FunctionOutdent("RobotInstance", "LoadServerData", "Not signed in");
                    return;
                }
                span?.AddEvent(new ActivityEvent("query project versions"));
                Log.Information("LoadServerData::query project versions");
                var server_projects = await global.webSocketClient.Query<Project>("openrpa", "{\"_type\": 'project'}", "{\"_version\": 1}", top: Config.local.max_projects);
                var local_projects = Projects.FindAll().ToList();
                var reload_ids = new List<string>();
                var updatePackages = new List<string>();
                foreach (var p in server_projects)
                {
                    var exists = local_projects.Where(x => x._id == p._id).FirstOrDefault();
                    if (exists != null)
                    {
                        if (exists._version < p._version)
                        {
                            Log.Information("LoadServerData::Adding project " + p.name);
                            reload_ids.Add(p._id);
                        }
                        if (exists._version > p._version && p.isDirty)
                        {
                            Log.Information("LoadServerData::Updating project " + p.name);
                            await p.Save();
                        }
                    }
                    else
                    {
                        reload_ids.Add(p._id);
                    }
                }
                foreach (var p in local_projects)
                {
                    var exists = server_projects.Where(x => x._id == p._id).FirstOrDefault();
                    if (exists == null && !p.isDirty)
                    {
                        span?.AddEvent(new ActivityEvent("Removing local project " + p.name));
                        Log.Information("LoadServerData::Removing local project " + p.name);
                        Projects.Delete(p._id);
                    }
                    else if (p.isDirty)
                    {
                        if (p.isDeleted) await p.Delete();
                        if (!p.isDeleted) await p.Save();
                    }
                }
                if (reload_ids.Count > 0)
                {
                    for (var i = 0; i < reload_ids.Count; i++) reload_ids[i] = "'" + reload_ids[i] + "'";
                    Log.Information("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " projects");
                    span?.AddEvent(new ActivityEvent("Featching fresh version of ´" + reload_ids.Count + " projects"));
                    var q = "{ _type: 'project', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    server_projects = await global.webSocketClient.Query<Project>("openrpa", q, orderby: "{\"name\":-1}", top: Config.local.max_projects);
                    foreach (var p in server_projects)
                    {
                        var exists = local_projects.Where(x => x._id == p._id).FirstOrDefault();
                        if (exists != null)
                        {
                            span?.AddEvent(new ActivityEvent("Updating local project " + p.name));
                            Log.Information("LoadServerData::Updating local project " + p.name);
                            p.IsExpanded = exists.IsExpanded;
                            p.IsSelected = exists.IsSelected;
                            p.isDirty = false;
                            await p.Save();
                            updatePackages.Add(p._id);
                        }
                        else
                        {
                            span?.AddEvent(new ActivityEvent("Adding local project " + p.name));
                            Log.Information("LoadServerData::Adding local project " + p.name);
                            p.isDirty = false;
                            await p.Save();
                            updatePackages.Add(p._id);
                        }
                    }
                }
                local_projects = Projects.FindAll().ToList();
                var local_project_ids = new List<string>();
                for (var i = 0; i < local_projects.Count; i++) local_project_ids.Add("'" + local_projects[i]._id + "'");

                Log.Information("LoadServerData::query workflow versions");
                span?.AddEvent(new ActivityEvent("query workflow versions"));
                var _q = "{ _type: 'workflow', 'projectid': {'$in': [" + string.Join(",", local_project_ids) + "]}}";
                var server_workflows = await global.webSocketClient.Query<Workflow>("openrpa", _q, "{\"_version\": 1}", top: Config.local.max_workflows);
                var local_workflows = Workflows.FindAll().ToList();
                reload_ids = new List<string>();
                foreach (var wf in server_workflows)
                {
                    var exists = local_workflows.Where(x => x._id == wf._id).FirstOrDefault();
                    if (exists != null)
                    {
                        if (exists._version < wf._version) reload_ids.Add(wf._id);
                        if (exists._version > wf._version && wf.isDirty) await wf.Save();
                    }
                    else
                    {
                        reload_ids.Add(wf._id);
                    }
                }
                foreach (var wf in local_workflows)
                {
                    var exists = server_workflows.Where(x => x._id == wf._id).FirstOrDefault();
                    if (exists == null && !wf.isDirty)
                    {
                        span?.AddEvent(new ActivityEvent("Removing local workflow " + wf.name));
                        Log.Debug("Removing local workflow " + wf.name);
                        Workflows.Delete(wf._id);
                    }
                    else if (wf.isDirty)
                    {
                        if (wf.isDeleted) await wf.Delete();
                        if (!wf.isDeleted) await wf.Save();
                    }
                }
                if (reload_ids.Count > 0)
                {
                    for (var i = 0; i < reload_ids.Count; i++) reload_ids[i] = "'" + reload_ids[i] + "'";
                    var q = "{ _type: 'workflow', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    Log.Information("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " workflows");
                    server_workflows = await global.webSocketClient.Query<Workflow>("openrpa", q, orderby: "{\"name\":-1}", top: Config.local.max_workflows);
                    foreach (var wf in server_workflows)
                    {
                        var exists = local_workflows.Where(x => x._id == wf._id).FirstOrDefault();
                        try
                        {
                            if (exists != null)
                            {
                                span?.AddEvent(new ActivityEvent("Updating local workflow " + wf.name));
                                Log.Information("LoadServerData::Updating local workflow " + wf.name);
                                wf.isDirty = false;
                                UpdateWorkflow(wf, false);
                            }
                            else
                            {
                                span?.AddEvent(new ActivityEvent("Adding local workflow " + wf.name));
                                Log.Information("LoadServerData::Adding local workflow " + wf.name);
                                wf.isDirty = false;
                                await wf.Save();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                        }
                    }
                }




                Log.Information("LoadServerData::query detector versions");
                span?.AddEvent(new ActivityEvent("query detector versions"));
                var server_detectors = await global.webSocketClient.Query<Detector>("openrpa", "{\"_type\": 'detector'}", "{\"_version\": 1}");
                var local_detectors = Detectors.FindAll().ToList();
                reload_ids = new List<string>();
                foreach (var detector in server_detectors)
                {
                    var exists = local_detectors.Where(x => x._id == detector._id).FirstOrDefault();
                    if (exists != null)
                    {
                        if (exists._version < detector._version) reload_ids.Add(detector._id);
                        if (exists._version > detector._version && detector.isDirty) await detector.Save();
                    }
                    else
                    {
                        reload_ids.Add(detector._id);
                    }
                }
                foreach (var detector in local_detectors)
                {
                    var exists = server_detectors.Where(x => x._id == detector._id).FirstOrDefault();
                    if (exists == null && !detector.isDirty)
                    {
                        span?.AddEvent(new ActivityEvent("Removing local detector " + detector.name));
                        Log.Debug("Removing local detector " + detector.name);
                        var d = Plugins.detectorPlugins.Where(x => x.Entity._id == detector._id).FirstOrDefault();
                        if (d != null)
                        {
                            d.OnDetector -= Window.OnDetector;
                            d.Stop();
                            Plugins.detectorPlugins.Remove(d);
                        }
                        Detectors.Delete(detector._id);
                    }
                    else if (detector.isDirty)
                    {
                        if (detector.isDeleted) await detector.Delete();
                        if (!detector.isDeleted) await detector.Save();

                    }
                }
                if (reload_ids.Count > 0)
                {
                    for (var i = 0; i < reload_ids.Count; i++) reload_ids[i] = "'" + reload_ids[i] + "'";
                    var q = "{ _type: 'detector', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    Log.Information("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " detectors");
                    server_detectors = await global.webSocketClient.Query<Detector>("openrpa", q, orderby: "{\"name\":-1}");
                    foreach (var detector in server_detectors)
                    {
                        detector.isDirty = false;
                        try
                        {
                            IDetectorPlugin exists = Plugins.detectorPlugins.Where(x => x.Entity._id == detector._id).FirstOrDefault();
                            if (exists != null && detector._version != exists.Entity._version)
                            {
                                Log.Information("LoadServerData::Updating detector " + detector.name);
                                exists.Stop();
                                exists.OnDetector -= Window.OnDetector;
                                exists = Plugins.UpdateDetector(this, detector);
                                if (exists != null) exists.OnDetector += Window.OnDetector;
                            }
                            else if (exists == null)
                            {
                                Log.Information("LoadServerData::Adding detector " + detector.name);
                                exists = Plugins.AddDetector(this, detector);
                                if (exists != null)
                                {
                                    exists.OnDetector += Window.OnDetector;
                                }
                                else { Log.Information("Failed loading detector " + detector.name); }
                            }
                            var dexists = Detectors.FindById(detector._id);
                            if (dexists == null)
                            {
                                Log.Information("LoadServerData::Adding detector " + detector.name);
                                Detectors.Insert(detector);
                            }
                            if (dexists != null)
                            {
                                Log.Information("LoadServerData::Updating detector " + detector.name);
                                Detectors.Update(detector);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                }
                var LocalOnlyProjects = _instance.Projects.Find(x => x.isLocalOnly);
                foreach (var i in LocalOnlyProjects) await i.Save<Project>();
                var LocalOnlyWorkflws = _instance.Workflows.Find(x => x.isLocalOnly);
                foreach (var i in LocalOnlyWorkflws) await i.Save<Workflow>();
                //_instance.dbWorkflowInstances = _instance.db.GetCollection<WorkflowInstance>("workflowinstances");
                //_instance.dbWorkflowInstances.EnsureIndex(x => x._id, true);


                if (Projects.Count() == 0 && first_connect)
                {
                    string Name = "New Project";
                    try
                    {
                        Project project = await Project.Create(Interfaces.Extensions.ProjectsDirectory, Name);

                        IWorkflow workflow = await project.AddDefaultWorkflow();
                        Window.OnOpenWorkflow(workflow);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
                NotifyPropertyChanged("Projects");

                foreach (var _id in updatePackages)
                {
                    try
                    {
                        var p = Projects.FindById(_id);
                        await p.InstallDependencies(true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                span?.RecordException(ex);
                Log.Error(ex, "");
            }
            finally
            {
                if (global.webSocketClient.user != null && global.webSocketClient.isConnected)
                {
                    SetStatus("Connected to " + Config.local.wsurl + " as " + global.webSocketClient.user.name);
                }
                else
                {
                    SetStatus("Offline");
                }
                AutoReloading = true;
                DisableWatch = false;
                span?.Dispose();
                Window.IsLoading = false;
                Window.OnOpen(null);
                Log.Information("LoadServerData::end");
            }
        }
        private string openrpa_watchid = "";
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
                if (Window != null) Window.SetStatus(message);
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
        public void init()
        {
            Log.FunctionIndent("RobotInstance", "init");
            SetStatus("Checking for updates");
            Config.Save();
            SetStatus("Checking for updates");
            _ = CheckForUpdatesAsync();
            try
            {
                if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    global.webSocketClient = new Net.WebSocketClient(Config.local.wsurl);
                    global.webSocketClient.OnOpen += RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    global.webSocketClient.OnQueueClosed += WebSocketClient_OnQueueClosed;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    SetStatus("Connecting to " + Config.local.wsurl);
                    _ = global.webSocketClient.Connect();
                }
                else
                {
                    SetStatus("loading projects and workflows");
                    System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = true;
                    System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
                    if (first_connect)
                    {
                        SetStatus("Run pending workflow instances");
                        Log.Debug("RunPendingInstances::begin ");
                        var wfs = Workflows.FindAll();
                        foreach (var workflow in wfs)
                        {
                            workflow.RunPendingInstances();
                        }
                        Log.Debug("RunPendingInstances::end ");
                        // CreateMainWindow();
                    }
                    GenericTools.RunUI(() =>
                    {
                        if (App.splash != null)
                        {
                            App.splash.Close();
                            App.splash = null;
                        }
                        if (!Config.local.isagent) Show();
                        ReadyForAction?.Invoke();
                    });
                    if (!isReadyForAction)
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
            GenericTools.RunUI(() =>
            {
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
                    try
                    {
                        if (!Config.local.isagent && global.webSocketClient != null)
                        {
                            if (global.webSocketClient.user != null)
                            {
                                if (global.webSocketClient.user.hasRole("robot agent users"))
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RobotInstance.CreateMainWindow: " + ex.ToString());
                    }
                }, null);
                SetStatus("loading detectors");
                var _detectors = Detectors.FindAll();
                foreach (var d in _detectors)
                {
                    Log.Information("Loading detector " + d.name);
                    IDetectorPlugin dp = null;
                    dp = Plugins.AddDetector(this, d);
                    if (dp != null) dp.OnDetector += Window.OnDetector;
                }

            }

        }
        private void Show()
        {
            Log.FunctionIndent("RobotInstance", "Show");
            GenericTools.RunUI(() =>
            {
                if (App.splash != null)
                {
                    App.splash.Show();
                }
                else
                {
                    if (Window != null) Window.Show();
                }
            });
            Log.FunctionOutdent("RobotInstance", "Show");
        }
        private void Close()
        {
            Log.FunctionIndent("RobotInstance", "Close");
            GenericTools.RunUI(() =>
            {
                if (App.splash != null) App.splash.Close();
                if (Window != null) Window.Close();
                System.Windows.Application.Current.Shutdown();
            });
            Log.FunctionOutdent("RobotInstance", "Close");
        }
        private async void RobotInstance_WebSocketClient_OnOpen()
        {
            var span = source.StartActivity("SocketOpen", ActivityKind.Internal);
            Log.FunctionIndent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
            try
            {
                Connected?.Invoke();
                ReconnectDelay = 5000;
            }
            catch (Exception ex)
            {
                span?.RecordException(ex);
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
                    var loginspan = source.StartActivity("Signin", ActivityKind.Internal);
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
                    loginspan?.Dispose();
                }
                InitializeOTEL();
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
                    await RegisterQueues();
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
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            CreateMainWindow();
            GenericTools.RunUI(() =>
            {
                if (App.splash != null)
                {
                    App.splash.Close();
                    App.splash = null;
                }
                if (!Config.local.isagent) Show();
                ReadyForAction?.Invoke();
            });
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
            if (first_connect)
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
                        if (!Config.local.isagent) Show();
                        ReadyForAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
            }
            try
            {
                if (global.openflowconfig != null && global.openflowconfig.supports_watch)
                {
                    if (string.IsNullOrEmpty(openrpa_watchid))
                    {
                        openrpa_watchid = await global.webSocketClient.Watch("openrpa", "[{ '$match': { 'fullDocument._type': {'$exists': true} } }]", onWatchEvent);
                    }
                }
                _ = LoadServerData();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            span?.Dispose();
            Log.FunctionOutdent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
        }
        int ReconnectDelay = 5000;
        private async void WebSocketClient_OnClose(string reason)
        {
            Log.FunctionIndent("RobotInstance", "WebSocketClient_OnClose", reason);
            if (global.webSocketClient.isConnected) Log.Information("Disconnected " + reason);
            SetStatus("Disconnected from " + Config.local.wsurl + " reason " + reason);
            openrpa_watchid = null;
            try
            {
                Disconnected?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            if (!isReadyForAction)
            {
                ParseCommandLineArgs();
                isReadyForAction = true;
            }
            if (Window != null)
            {
                Window.MainWindow_WebSocketClient_OnOpen();
            }
            CreateMainWindow();
            GenericTools.RunUI(() =>
            {
                if (App.splash != null)
                {
                    App.splash.Close();
                    App.splash = null;
                }
                if (!Config.local.isagent) Show();
                ReadyForAction?.Invoke();
            });

            await Task.Delay(ReconnectDelay);
            ReconnectDelay += 5000;
            if (ReconnectDelay > 60000 * 2) ReconnectDelay = 60000 * 2;
            if (autoReconnect)
            {
                try
                {
                    autoReconnect = false;
                    global.webSocketClient.OnOpen -= RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose -= WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
                    global.webSocketClient.OnQueueClosed -= WebSocketClient_OnQueueClosed;
                    global.webSocketClient.OnQueueMessage -= WebSocketClient_OnQueueMessage;
                    global.webSocketClient = null;

                    global.webSocketClient = new Net.WebSocketClient(Config.local.wsurl);
                    global.webSocketClient.OnOpen += RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    global.webSocketClient.OnQueueClosed += WebSocketClient_OnQueueClosed;
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
                if (command.command == "killallworkflows")
                {
                    if (Config.local.remote_allowed_killing_any)
                    {
                        command.command = "killallworkflowssuccess";
                        foreach (var i in WorkflowInstance.Instances.ToList())
                        {
                            if (!i.isCompleted)
                            {
                                i.Abort("Killed remotly by killallworkflows command");
                            }
                        }
                    }
                    else
                    {
                        command.command = "error";
                        command.data = JObject.FromObject(new Exception("kill all not allowed for " + global.webSocketClient.user + " running on " + System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower()));
                    }
                    if (data != null) command.data = JObject.FromObject(data);
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
                        WorkflowInstance.CleanUp();
                        foreach (var i in WorkflowInstance.Instances.ToList())
                        {
                            if (command.killallexisting && Config.local.remote_allowed_killing_any && !i.isCompleted)
                            {
                                i.Abort("Killed by nodered rpa node, due to killallexisting");
                            }
                            else if (!string.IsNullOrEmpty(i.correlationId) && !i.isCompleted)
                            {
                                if (command.killexisting && i.WorkflowId == workflow._id && (Config.local.remote_allowed_killing_self || Config.local.remote_allowed_killing_any))
                                {
                                    i.Abort("Killed by nodered rpa node, due to killexisting");
                                }
                                else
                                {
                                    RemoteRunningCount++;
                                    RunningCount++;
                                }
                            }
                            else if (!i.isCompleted)
                            {
                                if (command.killexisting && i.WorkflowId == workflow._id && Config.local.remote_allowed_killing_any)
                                {
                                    i.Abort("Killed by nodered rpa node, due to killexisting");
                                }
                                else
                                {
                                    RunningCount++;
                                }
                            }
                            if (!Config.local.remote_allow_multiple_running && RunningCount > 0)
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
                        foreach (var p in workflow.Parameters)
                        {
                            if (param.ContainsKey(p.name))
                            {
                                var value = param[p.name];
                                if (p.type == "System.Data.DataTable" && value != null)
                                {
                                    if (value is JArray)
                                    {
                                        param[p.name] = ((JArray)value).ToDataTable();
                                    }

                                }
                                else if (p.type.EndsWith("[]"))
                                {
                                    param[p.name] = ((JArray)value).ToObject(Type.GetType(p.type));
                                }
                            }
                        }
                        Log.Information("Create instance of " + workflow.name);
                        Log.Function("RobotInstance", "WebSocketClient_OnQueueMessage", "Create instance and run workflow");
                        if (Window == null) { e.isBusy = true; return; }
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
            if (command.command == "error" || command.command == "killallworkflowssuccess" || ((command.command == "invoke" || command.command == "invokesuccess") && !string.IsNullOrEmpty(command.workflowid)))
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
        private async void WebSocketClient_OnQueueClosed(IQueueClosedMessage message, QueueMessageEventArgs e)
        {
            await Task.Delay(5000);
            await RegisterQueues();
        }
        async private Task RegisterQueues()
        {
            if (!global.isConnected)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await RegisterQueues();
                });
                return;
            }
            try
            {
                bool registerqueues = true;
                Interfaces.entity.TokenUser user = global.webSocketClient.user;
                if (Interfaces.win32.ChildSession.IsChildSessionsEnabled())
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
                if (registerqueues)
                {
                    SetStatus("Registering queues");
                    Log.Debug("Registering queue for robot " + user._id);
                    robotqueue = await global.webSocketClient.RegisterQueue(user._id);

                    foreach (var role in global.webSocketClient.user.roles)
                    {
                        var roles = await global.webSocketClient.Query<Interfaces.entity.apirole>("users", "{_id: '" + role._id + "'}", top: 5000);
                        if (roles.Length == 1 && roles[0].rparole)
                        {
                            SetStatus("Add queue " + role.name);
                            Log.Debug("Registering queue for role " + role.name + " " + role._id + " ");
                            await global.webSocketClient.RegisterQueue(role._id);
                        }
                    }
                }
            }
            catch (Exception)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await RegisterQueues();
                });
            }
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

                if (Config.local.enable_analytics && StatsTracerProvider == null)
                {
                    StatsTracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource("OpenRPA").AddSource("OpenRPA.RobotInstance").AddSource("OpenRPA.Net")
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
                    .AddSource("OpenRPA").AddSource("OpenRPA.RobotInstance").AddSource("OpenRPA.Net")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenRPA"))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        if (Config.local.otel_trace_url.Contains("http://") && Config.local.otel_trace_url.Contains(":80"))
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
        public bool DisableWatch = false;
        private async void onWatchEvent(string id, Newtonsoft.Json.Linq.JObject data)
        {
            try
            {
                if (DisableWatch) return;
                string _type = data["fullDocument"].Value<string>("_type");
                string _id = data["fullDocument"].Value<string>("_id");
                long _version = data["fullDocument"].Value<long>("_version");
                string operationType = data.Value<string>("operationType");
                if (operationType != "replace" && operationType != "insert" && operationType != "update") return; // we don't support delete right now
                if (_type == "workflow")
                {
                    Log.Verbose(operationType + " version " + _version);
                    var workflow = Newtonsoft.Json.JsonConvert.DeserializeObject<Workflow>(data["fullDocument"].ToString());
                    var wfexists = instance.Workflows.FindById(_id);
                    if (wfexists != null && wfexists._version != _version)
                    {
                        UpdateWorkflow(workflow, false);
                    }
                    else if (wfexists == null)
                    {
                        workflow.isDirty = false;
                        await workflow.Save();
                        instance.NotifyPropertyChanged("Projects");
                    }
                }
                if (_type == "project")
                {
                    var project = Newtonsoft.Json.JsonConvert.DeserializeObject<Project>(data["fullDocument"].ToString());
                    Project exists = RobotInstance.instance.Projects.FindById(_id);
                    if (exists != null && _version != exists._version)
                    {
                        await UpdateProject(project);
                    }
                    else if (exists == null)
                    {
                        await UpdateProject(project);
                    }

                }
                if (_type == "detector")
                {
                    var d = Newtonsoft.Json.JsonConvert.DeserializeObject<Detector>(data["fullDocument"].ToString());
                    GenericTools.RunUI(() =>
                    {
                        try
                        {
                            IDetectorPlugin exists = Plugins.detectorPlugins.Where(x => x.Entity._id == d._id).FirstOrDefault();
                            if (exists != null && d._version != exists.Entity._version)
                            {
                                exists.Stop();
                                exists.OnDetector -= Window.OnDetector;
                                exists = Plugins.UpdateDetector(this, d);
                                if (exists != null) exists.OnDetector += Window.OnDetector;
                            }
                            else if (exists == null)
                            {
                                exists = Plugins.AddDetector(this, d);
                                if (exists != null)
                                {
                                    exists.OnDetector += Window.OnDetector;
                                }
                                else { Log.Information("Failed loading detector " + d.name); }
                            }
                            var dexists = Detectors.FindById(d._id);
                            if (dexists == null) Detectors.Insert(d);
                            if (dexists != null) Detectors.Update(d);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void UpdateWorkflow(IWorkflow Workflow, bool forceSave)
        {
            if (Window.IsLoading) return;
            GenericTools.RunUI(() =>
            {
                try
                {
                    if (!(instance.GetWorkflowDesignerByIDOrRelativeFilename(Workflow.RelativeFilename) is Views.WFDesigner designer))
                    {
                        instance.Workflows.Update(Workflow as Workflow);
                    }
                    else
                    {
                        if (designer.HasChanged)
                        {
                            if (forceSave)
                            {
                                instance.Workflows.Update(Workflow as Workflow);
                            }
                            else
                            {
                                var messageBoxResult = System.Windows.MessageBox.Show(Workflow.name + " has been updated by " + Workflow._modifiedby + ", reload workflow ?", "Workflow has been updated",
                                        System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.None, System.Windows.MessageBoxResult.Yes);
                                if (messageBoxResult == System.Windows.MessageBoxResult.Yes)
                                {
                                    instance.Workflows.Update(Workflow as Workflow);
                                    designer.forceHasChanged(false);
                                    designer.tab.Close();
                                    Window.OnOpenWorkflow(Workflow);
                                }
                                else
                                {
                                    designer.Workflow.current_version = Workflow._version;
                                }
                            }
                        }
                        else
                        {
                            if (designer.Workflow._version != Workflow._version)
                            {
                                designer.forceHasChanged(false);
                                designer.tab.Close();
                                instance.Workflows.Update(Workflow as Workflow);
                                Window.OnOpenWorkflow(Workflow);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
            instance.NotifyPropertyChanged("Projects");
        }
        public async Task UpdateProject(IProject project)
        {
            await project.Save();
            instance.NotifyPropertyChanged("Projects");
            try
            {
                await project.InstallDependencies(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
