using LiteDB;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.AvalonDock.Layout;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;

namespace OpenRPA
{
    public class RobotInstance : IOpenRPAClient, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public static System.Timers.Timer unsavedTimer = null;
        private readonly System.Timers.Timer reloadTimer = null;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == "Projects")
            {
                Views.OpenProject.UpdateProjectsList(true, false);
            }
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        private RobotInstance()
        {
            reloadTimer = new System.Timers.Timer(Config.local.reloadinterval.TotalMilliseconds);
            reloadTimer.Elapsed += ReloadTimer_Elapsed;
            reloadTimer.Stop();
            unsavedTimer = new System.Timers.Timer(5000);
            unsavedTimer.Elapsed += UnsavedTimer_Elapsed;
            unsavedTimer.Start();
            if (InitializeOTEL())
            {
            }
        }
        // public System.Collections.ObjectModel.ObservableCollection<Project> Projects { get; set; } = new System.Collections.ObjectModel.ObservableCollection<Project>();
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
        public LiteDB.ILiteCollection<WorkitemQueue> WorkItemQueues;        
        public LiteDB.ILiteCollection<WorkflowInstance> dbWorkflowInstances;
        public System.Collections.ObjectModel.ObservableCollection<IWorkitemQueue> WorkItemQueuesSource { get; set; }
        = new System.Collections.ObjectModel.ObservableCollection<IWorkitemQueue>();
        public int ProjectCount
        {
            get
            {
                int result = 0;
                GenericTools.RunUI(() => { result = Projects.Count(); });
                return result;
            }
        }
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

                    var connecttype = "";
                    try
                    {
                        if (Interfaces.win32.ChildSession.IsChildSessionsEnabled())
                        {
                            connecttype = ";connection=shared";
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.Message);
                    }
                    var dbfilename = "offline.db";
                    var logfilename = "offline-log.db";
                    if (!string.IsNullOrEmpty(Config.local.wsurl))
                    {
                        dbfilename = new Uri(Config.local.wsurl).Host + ".db";
                        logfilename = new Uri(Config.local.wsurl).Host + "-log.db";
                    }
                    try
                    {
                        _instance.db = new LiteDatabase("Filename=" + Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename + connecttype);
                    }
                    catch (System.IO.IOException ex)
                    {
                        System.Windows.MessageBox.Show("Cannot start OpenRPA" + Environment.NewLine + ex.Message);
                        System.Windows.Forms.Application.Exit();
                        System.Environment.Exit(1);
                        return _instance;
                    }
                    catch (LiteException)
                    {
                        if (System.IO.File.Exists(Interfaces.Extensions.ProjectsDirectory + @"\" + logfilename))
                        {
                            System.IO.File.Delete(Interfaces.Extensions.ProjectsDirectory + @"\" + logfilename);
                        }
                        if (System.IO.File.Exists(Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename))
                        {
                            var backupfilename = dbfilename + ".bak"; int counter = 0;
                            while (System.IO.File.Exists(Interfaces.Extensions.ProjectsDirectory + @"\" + backupfilename))
                            {
                                counter++;
                                backupfilename = dbfilename + ".bak" + counter;
                            }
                            System.IO.File.Copy(Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename, Interfaces.Extensions.ProjectsDirectory + @"\" + backupfilename);
                            System.IO.File.Delete(Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename);
                            _instance.db = new LiteDatabase("Filename=" + Interfaces.Extensions.ProjectsDirectory + @"\" + dbfilename + connecttype);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Cannot start OpenRPA" + Environment.NewLine + ex.Message);
                        System.Windows.Forms.Application.Exit();
                        System.Environment.Exit(1);
                        return _instance;
                    }

                    _instance.Projects = _instance.db.GetCollection<Project>("projects");
                    _instance.Projects.EnsureIndex(x => x._id, true);

                    _instance.Workflows = _instance.db.GetCollection<Workflow>("workflows");
                    _instance.Workflows.EnsureIndex(x => x._id, true);

                    _instance.Detectors = _instance.db.GetCollection<Detector>("detectors");
                    _instance.Detectors.EnsureIndex(x => x._id, true);

                    _instance.dbWorkflowInstances = _instance.db.GetCollection<WorkflowInstance>("workflowinstances");
                    _instance.dbWorkflowInstances.EnsureIndex(x => x._id, true);

                    _instance.WorkItemQueues = _instance.db.GetCollection<WorkitemQueue>("workitemqueues");
                    _instance.WorkItemQueues.EnsureIndex(x => x._id, true);

                    

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
        public bool loginInProgress = false;
        private bool first_connect = true;
        private int connect_attempts = 0;
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
                if (System.Threading.Monitor.TryEnter(WorkflowInstance.Instances, 1000))
                {
                    try
                    {
                        foreach (var wi in WorkflowInstance.Instances) result.Add(wi);
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(WorkflowInstance.Instances);
                    }
                }
                else
                {
                    throw new Exception("Failed running workflow, due to theading deadlock");
                }
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
        public IDesigner CurrentDesigner
        {
            get
            {
                if (Window == null) return null;
                return Window.Designers.Where(x => x.IsSelected).FirstOrDefault();
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
        private static async void UnsavedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                unsavedTimer.Stop();
                var list = WorkflowInstance.Instances.ToList();
                foreach (var i in list)
                {
                    if (i.isDirty)
                    {
                        await i.Save<WorkflowInstance>();
                        if (i.Workflow != null) i.Workflow.NotifyUIState();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                unsavedTimer.Start();
            }
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
            IWorkflow result = null;
            try
            {
                var filename = IDOrRelativeFilename.ToLower().Replace("\\", "/");
                if (Views.OpenProject.Instance != null && Views.OpenProject.Instance.Projects.Count > 0)
                {
                    foreach (var p in Views.OpenProject.Instance.Projects.ToList())
                    {
                        result = p.Workflows.Where(x => x.RelativeFilename.ToLower() == filename.ToLower() || x._id == IDOrRelativeFilename).FirstOrDefault();
                        if (result != null) return result;
                    }
                }
                if (result == null)
                {
                    result = Workflows.Find(x => x.RelativeFilename.ToLower() == filename.ToLower() || x._id == IDOrRelativeFilename).FirstOrDefault();
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                Log.FunctionOutdent("RobotInstance", "GetWorkflowByIDOrRelativeFilename");
            }
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
            Log.Debug("LoadServerData::begin");
            Window.IsLoading = true;
            try
            {
                Log.FunctionIndent("RobotInstance", "LoadServerData");
                if (!global.isSignedIn)
                {
                    Log.FunctionOutdent("RobotInstance", "LoadServerData", "Not signed in");
                    return;
                }
                Log.Debug("LoadServerData::query project versions");
                SetStatus("query project versions");
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
                            Log.Debug("LoadServerData::Adding project " + p.name);
                            reload_ids.Add(p._id);
                        }
                        if (exists._version > p._version && p.isDirty)
                        {
                            //Log.Debug("LoadServerData::Updating project " + p.name);
                            //await p.Save();
                            Log.Warning("project " + p.name + " has a newer version on the server!");
                            // await wf.Save();
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
                        Log.Debug("LoadServerData::Removing local project " + p.name);
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
                    Log.Debug("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " projects");
                    var q = "{ _type: 'project', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    SetStatus("fetch updated " + reload_ids.Count + "projects");
                    server_projects = await global.webSocketClient.Query<Project>("openrpa", q, orderby: "{\"name\":-1}", top: Config.local.max_projects);
                    foreach (var p in server_projects)
                    {
                        try
                        {
                            var exists = local_projects.Where(x => x._id == p._id).FirstOrDefault();
                            if (exists != null)
                            {
                                Log.Debug("LoadServerData::Updating local project " + p.name);
                                p.IsExpanded = exists.IsExpanded;
                                p.IsSelected = exists.IsSelected;
                                p.isDirty = false;
                                await p.Save();
                                updatePackages.Add(p._id);
                            }
                            else
                            {
                                Log.Debug("LoadServerData::Adding local project " + p.name);
                                p.isDirty = false;
                                await p.Save();
                                updatePackages.Add(p._id);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                }
                local_projects = Projects.FindAll().ToList();
                var local_project_ids = new List<string>();
                for (var i = 0; i < local_projects.Count; i++) local_project_ids.Add("'" + local_projects[i]._id + "'");

                Log.Debug("LoadServerData::query workflow versions");
                var _q = "{ _type: 'workflow', 'projectid': {'$in': [" + string.Join(",", local_project_ids) + "]}}";
                SetStatus("query updated workflows");
                var server_workflows = await global.webSocketClient.Query<Workflow>("openrpa", _q, "{\"_version\": 1}", top: Config.local.max_workflows);
                var local_workflows = Workflows.FindAll().ToList();
                reload_ids = new List<string>();
                Log.Debug("LoadServerData::Loop " + server_workflows.Length + " server workflows");
                foreach (var wf in server_workflows)
                {
                    var exists = local_workflows.Where(x => x._id == wf._id).FirstOrDefault();
                    if (exists != null)
                    {
                        try
                        {
                            if (exists._version < wf._version) reload_ids.Add(wf._id);
                            if (exists._version > wf._version && exists.isDirty) // Do NOT save offline changes. Let user do that using the right click menu
                            {
                                Log.Warning(exists.ProjectAndName + " exists on server with version " + wf._version  + ", but local version is newer " + exists._version  + "! Open and save it, to preserve local changes, or right ciick and select \"get server version\" to fix mitch match");
                                var state = exists.State;
                                exists.SetLastState("warning");
                                await exists.Save(true);
                                // await wf.Save();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                    else
                    {
                        reload_ids.Add(wf._id);
                    }
                }
                Log.Debug("LoadServerData::Loop " + local_workflows.Count + " local workflows");
                foreach (var _wf in local_workflows)
                {
                    try
                    {
                        var wf = _wf;
                        var exists = server_workflows.Where(x => x._id == wf._id).FirstOrDefault();
                        if (exists == null && !wf.isDirty)
                        {
                            Log.Debug("Removing local workflow " + wf.name);
                            Workflows.Delete(wf._id);
                        }
                        else if ((wf.isDirty || wf.isLocalOnly) && exists != null && exists._version >= wf._version) // Do NOT save offline changes. LEt user do that using the right click menu
                        {
                            var _version = wf._version;
                            string name = wf.name;
                            string RelativeFilename = wf.RelativeFilename;
                            if (wf.isDeleted) await wf.Delete();
                            if (!wf.isDeleted && exists._version > wf._version) await wf.Save();
                        }
                        else if (exists == null)
                        {
                            Log.Warning(wf.RelativeFilename + " no longer exists on server, but is marked as dirty! Open and save it, to preserve it, or delete it if no longer needed");
                            if (wf.State != "warning")
                            {
                                wf.SetLastState("warning");
                                await wf.Save(true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
                Log.Debug("LoadServerData::reload_ids " + reload_ids.Count);
                if (reload_ids.Count > 0)
                {
                    for (var i = 0; i < reload_ids.Count; i++) reload_ids[i] = "'" + reload_ids[i] + "'";
                    var q = "{ _type: 'workflow', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    Log.Debug("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " workflows");
                    SetStatus("fetch updated " + reload_ids.Count + " workflows");
                    server_workflows = await global.webSocketClient.Query<Workflow>("openrpa", q, orderby: "{\"name\":-1}", top: Config.local.max_workflows);
                    foreach (var wf in server_workflows)
                    {
                        var exists = local_workflows.Where(x => x._id == wf._id).FirstOrDefault();
                        try
                        {
                            if (exists != null)
                            {
                                Log.Debug("LoadServerData::Updating local workflow " + wf.name);
                                wf.isDirty = false;
                                var isloading = Window.IsLoading;
                                Window.IsLoading = false;
                                UpdateWorkflow(wf, false);
                                Window.IsLoading = isloading;
                            }
                            else
                            {
                                Log.Debug("LoadServerData::Adding local workflow " + wf.name);
                                wf.isDirty = false;
                                await wf.Save(true);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                        }
                    }
                }




                Log.Debug("LoadServerData::query detector versions");
                SetStatus("query updated detectors");
                var server_detectors = await global.webSocketClient.Query<Detector>("openrpa", "{\"_type\": 'detector'}", "{\"_version\": 1}");
                var local_detectors = Detectors.FindAll().ToList();
                reload_ids = new List<string>();
                foreach (var detector in server_detectors)
                {
                    try
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
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                foreach (var detector in local_detectors)
                {
                    try
                    {
                        var exists = server_detectors.Where(x => x._id == detector._id).FirstOrDefault();
                        if (exists == null && !detector.isDirty)
                        {
                            var _id = detector._id;
                            Log.Debug("Removing local detector " + detector.name);
                            var d = Plugins.detectorPlugins.Where(x => x.Entity._id == detector._id).FirstOrDefault();
                            if (d != null) d.Stop();
                            Detectors.Delete(_id);
                        }
                        else if (detector.isDirty)
                        {
                            if (detector.isDeleted) await detector.Delete();
                            if (!detector.isDeleted) await detector.Save();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                if (reload_ids.Count > 0)
                {
                    for (var i = 0; i < reload_ids.Count; i++) reload_ids[i] = "'" + reload_ids[i] + "'";
                    var q = "{ _type: 'detector', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    Log.Debug("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " detectors");
                    SetStatus("Fetch " + reload_ids.Count + "updated detectors");
                    server_detectors = await global.webSocketClient.Query<Detector>("openrpa", q, orderby: "{\"name\":-1}");
                    foreach (var detector in server_detectors)
                    {
                        detector.isDirty = false;
                        try
                        {
                            IDetectorPlugin exists = Plugins.detectorPlugins.Where(x => x.Entity._id == detector._id).FirstOrDefault();
                            if (exists != null && detector._version != exists.Entity._version)
                            {
                                Log.Debug("LoadServerData::Updating detector " + detector.name);
                                detector.UpdateRunning();
                            }
                            else if (exists == null)
                            {
                                Log.Debug("LoadServerData::Adding detector " + detector.name);
                                detector.Start(true);
                            }
                            var dexists = Detectors.FindById(detector._id);
                            if (dexists == null)
                            {
                                Log.Debug("LoadServerData::Adding detector " + detector.name);
                                Detectors.Insert(detector);
                            }
                            if (dexists != null)
                            {
                                Log.Debug("LoadServerData::Updating detector " + detector.name);
                                Detectors.Update(detector);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                }




                Log.Debug("LoadServerData::query workitemqueues versions");
                SetStatus("query updated workitemqueues");
                var server_workitemqueues = await global.webSocketClient.Query<WorkitemQueue>("mq", "{\"_type\": 'workitemqueue'}", "{\"_version\": 1}");
                var local_workitemqueues = WorkItemQueues.FindAll().ToList();
                reload_ids = new List<string>();
                foreach (var WorkItemQueue in server_workitemqueues)
                {
                    try
                    {
                        var exists = local_workitemqueues.Where(x => x._id == WorkItemQueue._id).FirstOrDefault();
                        if (exists != null)
                        {
                            if (exists._version < WorkItemQueue._version) reload_ids.Add(WorkItemQueue._id);
                            if (exists._version > WorkItemQueue._version && WorkItemQueue.isDirty) await WorkItemQueue.Save();
                        }
                        else
                        {
                            reload_ids.Add(WorkItemQueue._id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                foreach (var WorkItemQueue in local_workitemqueues)
                {
                    try
                    {
                        var exists = server_workitemqueues.Where(x => x._id == WorkItemQueue._id).FirstOrDefault();
                        if (exists == null && !WorkItemQueue.isDirty)
                        {
                            var _id = WorkItemQueue._id;
                            Log.Debug("Removing local WorkItemQueue " + WorkItemQueue.name);
                            WorkItemQueues.Delete(_id);
                        }
                        else if (WorkItemQueue.isDirty)
                        {
                            if (WorkItemQueue.isDeleted) await WorkItemQueue.Delete();
                            if (!WorkItemQueue.isDeleted) await WorkItemQueue.Save();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                if (reload_ids.Count > 0)
                {
                    for (var i = 0; i < reload_ids.Count; i++) reload_ids[i] = "'" + reload_ids[i] + "'";
                    var q = "{ _type: 'workitemqueue', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    Log.Debug("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " workitemqueues");
                    SetStatus("Fetch " + reload_ids.Count + "updated workitemqueues");
                    server_workitemqueues = await global.webSocketClient.Query<WorkitemQueue>("mq", q, orderby: "{\"name\":-1}");
                    foreach (var workitemqueue in server_workitemqueues)
                    {
                        workitemqueue.isDirty = false;
                        try
                        {
                            var dexists = WorkItemQueues.FindById(workitemqueue._id);
                            if (dexists == null)
                            {
                                Log.Debug("LoadServerData::Adding WorkItemQueue " + workitemqueue.name);
                                WorkItemQueues.Insert(workitemqueue);
                            }
                            if (dexists != null)
                            {
                                Log.Debug("LoadServerData::Updating WorkItemQueue " + workitemqueue.name);
                                WorkItemQueues.Update(workitemqueue);
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



                if (Projects.Count() == 0)
                {
                    GenericTools.RunUIAsync(async () =>
                    {
                        WorkItemQueuesSource.UpdateCollectionById(WorkItemQueues.FindAll());
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
                    }).Wait();
                } else
                {
                    GenericTools.RunUI(() =>
                    {
                        WorkItemQueuesSource.UpdateCollectionById(WorkItemQueues.FindAll());
                    });
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

                Log.Debug("LoadServerData::query pending workflow instances");
                var host = Environment.MachineName.ToLower();
                var fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
                SetStatus("query idle or running openrpa_instances");
                var runninginstances = await global.webSocketClient.Query<WorkflowInstance>("openrpa_instances", "{'$or':[{state: 'idle'}, {state: 'running'}], fqdn: '" + fqdn + "'}", top: 1000);
                // var runpending = false;
                foreach (var i in runninginstances)
                {
                    var exists = dbWorkflowInstances.Find(x => x._id == i._id).FirstOrDefault();
                    if (exists != null)
                    {
                        if (i._version > exists._version)
                        {
                            i.isDirty = false;
                            await i.Save<WorkflowInstance>();
                        }
                        else if (i._version < exists._version)
                        {
                            await exists.Save<WorkflowInstance>();
                        }
                        else
                        {
                            if (exists.state == "idle" || exists.state == "running")
                            {
                                //var e = WorkflowInstance.Instances.Where(x => x.InstanceId == exists.InstanceId).FirstOrDefault();
                                //if (e == null)
                                //{
                                //    runpending = true;
                                //}
                            }
                        }
                    }
                    else
                    {
                        i.isDirty = false;
                        await i.Save<WorkflowInstance>();
                    }
                }
                var localInstances = dbWorkflowInstances.Find(x => x.isDirty || x.isLocalOnly).ToList();
                foreach (var i in localInstances)
                {
                    await i.Save<WorkflowInstance>();
                }
                _ = Task.Run(async () =>
                  {
                      try
                      {
                          var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                          while (true && sw.Elapsed < TimeSpan.FromSeconds(10))
                          {
                              System.Threading.Thread.Sleep(200);
                              if (Views.OpenProject.Instance != null && Views.OpenProject.Instance.Projects.Count > 0) break;
                          }
                          Log.Debug("RunPendingInstances::begin ");
                          await WorkflowInstance.RunPendingInstances();
                          Log.Debug("RunPendingInstances::end ");
                          if (System.Threading.Monitor.TryEnter(WorkflowInstance.Instances, 1000))
                          {
                              try
                              {
                                  foreach (var i in WorkflowInstance.Instances)
                                  {
                                      if (i.Bookmarks != null && i.Bookmarks.Count > 0)
                                      {
                                          foreach (var b in i.Bookmarks)
                                          {
                                              var instance = dbWorkflowInstances.Find(x => x.correlationId == b.Key || x._id == b.Key).FirstOrDefault();
                                              if (instance != null)
                                              {
                                                  if (instance.isCompleted)
                                                  {
                                                      try
                                                      {
                                                          i.ResumeBookmark(b.Key, instance);
                                                      }
                                                      catch (System.ArgumentException ex)
                                                      {
                                                          if (i.state == "idle" || i.state == "running")
                                                          {
                                                              i.Abort(ex.Message);
                                                          }
                                                      }
                                                      catch (Exception ex)
                                                      {
                                                          Log.Error(ex.ToString());
                                                      }
                                                  }
                                              }
                                          }

                                      }
                                  }
                              }
                              finally
                              {
                                  System.Threading.Monitor.Exit(WorkflowInstance.Instances);
                              }
                          }
                          else
                          {
                              throw new Exception("Failed running workflow, due to theading deadlock");
                          }
                      }
                      catch (Exception ex)
                      {
                          Log.Error(ex.ToString());
                      }
                  });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                if (global.webSocketClient != null && global.webSocketClient.user != null && global.webSocketClient.isConnected)
                {
                    SetStatus("Connected to " + Config.local.wsurl + " as " + global.webSocketClient.user.name);
                }
                else
                {
                    SetStatus("Offline");
                }
                AutoReloading = true;
                Window.IsLoading = false;
                Window.OnOpen(null);
                Log.Debug("LoadServerData::end");
            }
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
        public async Task init()
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
                    global.webSocketClient = Net.WebSocketClient.Get(Config.local.wsurl);
                    global.webSocketClient.OnOpen += RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueClosed += WebSocketClient_OnQueueClosed;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    SetStatus("Connecting to " + Config.local.wsurl);
                    await Connect();
                }
                else
                {
                    SetStatus("loading projects and workflows");
                    System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = true;
                    System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                    System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
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
                    await LoadServerData();
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RobotInstance.CreateMainWindow: " + ex.ToString());
                    }
                }, null);
                // ExpressionEditor.EditorUtil.Init();
                _ = CodeEditor.init.Initialize();
                SetStatus("loading detectors");
                var _detectors = Detectors.FindAll();
                foreach (var d in _detectors)
                {
                    Log.Debug("Loading detector " + d.name);
                    d.Start(false);
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
            Log.FunctionIndent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
            try
            {
                Connected?.Invoke();
                ReconnectDelay = 5000;
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
                Log.Debug("RobotInstance_WebSocketClient_OnOpen::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
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
                            Log.Error("RobotInstance.RobotInstance_WebSocketClient_OnOpen.userlogin: " + ex.Message);
                            errormessage = ex.Message;
                        }
                    }
                    if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                    {
                        try
                        {
                            if (!global.webSocketClient.isConnected) return;
                            SetStatus("Sign in to " + Config.local.wsurl);
                            Log.Debug("Signing in with token " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt));
                            if (user == null)
                            {
                                return;
                            }
                            Config.local.username = user.username;
                            Config.local.password = new byte[] { };
                            // Config.Save();
                            Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RobotInstance.RobotInstance_WebSocketClient_OnOpen.tokenlogin: " + ex.Message);
                            errormessage = ex.Message;
                        }
                    }
                    if (!global.webSocketClient.isConnected) return;
                    if (user == null)
                    {
                        if (loginInProgress == false && (global.webSocketClient.isConnected && !global.webSocketClient.signedin))
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
                                        else if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                                        {
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

                                            if (global.webSocketClient.isConnected && global.webSocketClient.user != null)
                                            {
                                                user = global.webSocketClient.user;
                                                Config.local.username = user.username;
                                            }
                                            else
                                            {
                                                Log.Debug("Call close " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                                Close();
                                            }
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error("RobotInstance.RobotInstance_WebSocketClient_OnOpen.UIlogin: " + ex.Message);
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
                Log.Debug("RobotInstance_WebSocketClient_OnOpen::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));

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
            try
            {
                CreateMainWindow();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
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
                    SetStatus("Connected to " + Config.local.wsurl + " as " + user.name);
                });
            }
            try
            {
                if (global.openflowconfig != null && global.openflowconfig.supports_watch)
                {
                    await global.webSocketClient.Watch("openrpa", "[\"$.[?(@ && @._type == 'workflow')]\", \"$.[?(@ && @._type == 'project')]\", \"$.[?(@ && @._type == 'detector')]\"]", onWatchEvent);
                }
                _ = LoadServerData();
                InitializeOTEL();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            try
            {
                foreach(var d in Plugins.detectorPlugins)
                {
                    var _d = d.Entity as Detector;
                    await _d.RegisterExchange();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
        }
        int ReconnectDelay = 5000;
        private async void WebSocketClient_OnClose(string reason)
        {
            try
            {
                Log.FunctionIndent("RobotInstance", "WebSocketClient_OnClose", reason);
                if (global.webSocketClient.isConnected) Log.Information("Disconnected " + reason);
                SetStatus("Disconnected from " + Config.local.wsurl + " reason " + reason);
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
                if (connect_attempts == 1)
                {
                    try
                    {
                        SetStatus("Run pending workflow instances");
                        await WorkflowInstance.RunPendingInstances();
                        SetStatus("Connecting to " + Config.local.wsurl);
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
            }
            _ = Connect();
            Log.FunctionOutdent("RobotInstance", "WebSocketClient_OnClose");
        }
        async internal Task Connect()
        {
            try
            {
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Connecting) return;
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Open) return;
                await Task.Delay(ReconnectDelay);
                ReconnectDelay += 5000;
                if (ReconnectDelay > 60000 * 2) ReconnectDelay = 60000 * 2;
                connect_attempts++;
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Connecting) return;
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Open) return;
                SetStatus("Connecting to " + Config.local.wsurl);
                await global.webSocketClient.Connect();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                _ = Connect();
            }
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
                                if (command.killexisting && i.WorkflowId == workflow._id && (Config.local.remote_allowed_killing_self || Config.local.remote_allowed_killing_any))
                                {
                                    i.Abort("Killed by nodered rpa node, due to killexisting");
                                }
                                else if (command.killallexisting && (Config.local.remote_allowed_killing_self || Config.local.remote_allowed_killing_any))
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
                        Log.Information("[" + message.correlationId + "] Create instance of " + workflow.name);
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
                                    instance = workflow.CreateInstance(param, message.replyto, message.correlationId, designer.IdleOrComplete, designer.OnVisualTracking);
                                    designer.Run(Window.VisualTracking, Window.SlowMotion, instance);
                                }
                                else
                                {
                                    instance = workflow.CreateInstance(param, message.replyto, message.correlationId, Window.IdleOrComplete, null);
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
        protected internal async void WebSocketClient_OnQueueClosed(IQueueClosedMessage message, QueueMessageEventArgs e)
        {
            await Task.Delay(5000);
            await RegisterQueues();
        }
        async private Task RegisterQueues()
        {
            if (!global.isConnected || !global.webSocketClient.signedin )
            {
                return;
            }
            try
            {
                bool registerqueues = true;
                Interfaces.entity.TokenUser user = global.webSocketClient.user;
                var IsChildSessionsEnabled = false;
                try
                {
                    IsChildSessionsEnabled = Interfaces.win32.ChildSession.IsChildSessionsEnabled();
                }
                catch (Exception)
                {
                }
                if (IsChildSessionsEnabled)
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
                    if(global.webSocketClient.user != null)
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                //_ = Task.Run(async () =>
                //{
                //    await Task.Delay(5000);
                //    await RegisterQueues();
                //});
            }
        }

        //private string last_metric;
        //private System.Diagnostics.PerformanceCounter mem_used_counter;
        // private System.Diagnostics.PerformanceCounter mem_total_counter;
        // private System.Diagnostics.PerformanceCounter mem_free_counter;
        // public Tracer tracer = null;
        // private InstrumentationWithActivitySource Sampler = null;
        private TracerProvider StatsTracerProvider;
        private TracerProvider LocalTracerProvider;
        private Object StatsMeterProvider;
        private Object LocalMeterProvider;
        // private TracerProvider tracerProvider;
        private static readonly Meter OpenRPAMeter = new Meter("OpenRPA");
        private static PerformanceCounter process_cpu = null;
        private static PerformanceCounter total_cpu = null;
        private static PerformanceCounter Working_Set = null;
        public static Counter<long> openrpa_workflow_run_count;
        public static TagList tags;
        private bool InitializeOTEL()
        {
            try
            {
                try
                {
                    if (process_cpu == null) process_cpu = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
                try
                {
                    if (total_cpu == null) total_cpu = new PerformanceCounter("Process", "% Processor Time", "_Total");
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
                try
                {
                    if (Working_Set == null) Working_Set = new PerformanceCounter("Process", "Working Set - Private", Process.GetCurrentProcess().ProcessName);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex.ToString());
                }
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                if (Config.local.enable_analytics && StatsTracerProvider == null)
                {
                    StatsTracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource("OpenRPA")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenRPA"))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri("http://otel.openiap.io");
                        otlpOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                    })
                    .Build();
                }
                if(LocalTracerProvider == null && Config.local != null && !string.IsNullOrEmpty(Config.local.otel_trace_url))
                {
                    LocalTracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource("OpenRPA")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenRPA"))
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(Config.local.otel_trace_url);
                        otlpOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                    })
                    .Build();

                }
                if ( (Config.local.enable_analytics && StatsMeterProvider == null) || (LocalMeterProvider == null && !string.IsNullOrEmpty(Config.local.otel_metric_url)) )
                {
                    // Instance.RootActivity?.SetTag("ofid", Config.local.openflow_uniqueid);
                    try
                    {
                        if (WorkflowTrackingParticipant.hostname == null) WorkflowTrackingParticipant.hostname = System.Net.Dns.GetHostName();
                    }
                    catch (Exception)
                    {
                        WorkflowTrackingParticipant.hostname = "unknown";
                    }
                    var process = Process.GetCurrentProcess();
                    tags = new TagList();
                    tags.Add(new KeyValuePair<string, object>("ofid", Config.local.openflow_uniqueid));
                    tags.Add(new KeyValuePair<string, object>("host", WorkflowTrackingParticipant.hostname));
                    tags.Add(new KeyValuePair<string, object>("username", Environment.UserName));
                    tags.Add(new KeyValuePair<string, object>("version", global.version));
                    OpenRPAMeter.CreateObservableGauge<long>("Process.PrivateMemorySize", () =>
                    {
                        process.Refresh();
                        var username = Environment.UserName;
                        if (global.webSocketClient != null && global.webSocketClient.user != null && !string.IsNullOrEmpty(global.webSocketClient.user.name))
                        {
                            username = global.webSocketClient.user.name;
                        }
                        var _tags = new TagList();
                        _tags.Add(new KeyValuePair<string, object>("ofid", Config.local.openflow_uniqueid));
                        _tags.Add(new KeyValuePair<string, object>("host", WorkflowTrackingParticipant.hostname));
                        _tags.Add(new KeyValuePair<string, object>("username", username));
                        _tags.Add(new KeyValuePair<string, object>("version", global.version));
                        tags = _tags;

                        return new List<Measurement<long>>() { new Measurement<long>(process.PrivateMemorySize64, tags) };
                    });
                    if (Working_Set != null)
                    {
                        OpenRPAMeter.CreateObservableGauge<long>("Process.PrivateWorkingSet", () =>
                        {
                            long value = 0;
                            try
                            {
                                _ = Working_Set.NextValue();
                                value = Working_Set.RawValue;
                            }
                            catch (Exception ex)
                            {
                                Log.Verbose(ex.ToString());
                            }
                            return new List<Measurement<long>>() { new Measurement<long>(value, tags) };
                        });
                    }
                    if (process_cpu != null && total_cpu != null)
                    {
                        OpenRPAMeter.CreateObservableGauge<long>("Process.ProcessorTimePercent", () =>
                        {
                            float t = total_cpu.NextValue();
                            float p = process_cpu.NextValue();
                            long value = 0;
                            try
                            {
                                if (t > 0) value = Convert.ToInt64(p / t * 100);
                            }
                            catch (Exception ex)
                            {
                                Log.Verbose(ex.ToString());
                            }
                            return new List<Measurement<long>>() { new Measurement<long>(value, tags) };
                        });
                        OpenRPAMeter.CreateObservableGauge<long>("Process.ProcessorTime", () =>
                        {
                            long value = 0;
                            try
                            {
                                value = Convert.ToInt64(process_cpu.NextValue());
                            }
                            catch (Exception ex)
                            {
                                Log.Verbose(ex.ToString());
                            }
                            return new List<Measurement<long>>() { new Measurement<long>(value, tags) };
                        });
                        OpenRPAMeter.CreateObservableGauge<long>("Process.ProcessorTimeTotal", () =>
                        {
                            long value = 0;
                            try
                            {
                                value = Convert.ToInt64(total_cpu.NextValue());
                            }
                            catch (Exception ex)
                            {
                                Log.Verbose(ex.ToString());
                            }
                            return new List<Measurement<long>>() { new Measurement<long>(value, tags) };
                        });
                    }
                    OpenRPAMeter.CreateObservableGauge("Process.PagedSystemMemorySize", () => new List<Measurement<long>>() { new Measurement<long>(process.PagedSystemMemorySize64, tags) });
                    OpenRPAMeter.CreateObservableGauge("Process.NonpagedSystemMemorySize", () => new List<Measurement<long>>() { new Measurement<long>(process.NonpagedSystemMemorySize64, tags) });
                    OpenRPAMeter.CreateObservableGauge("Process.PagedMemorySize", () => new List<Measurement<long>>() { new Measurement<long>(process.PagedMemorySize64, tags) });
                    OpenRPAMeter.CreateObservableGauge("Process.WorkingSet", () => new List<Measurement<long>>() { new Measurement<long>(process.WorkingSet64, tags) });
                    OpenRPAMeter.CreateObservableGauge("Process.VirtualMemorySize", () => new List<Measurement<long>>() { new Measurement<long>(process.VirtualMemorySize64, tags) });
                    openrpa_workflow_run_count = OpenRPAMeter.CreateCounter<long>("openrpa.workflow_run_count");
                    OpenRPAMeter.CreateObservableGauge<long>("openrpa.workflow_running_count", () =>
                    {
                        int value = 0;
                        try
                        {
                            value = WorkflowInstance.Instances.Where(x => x.isCompleted == false).Count();
                        }
                        catch (Exception ex)
                        {
                            Log.Verbose(ex.ToString());
                        }
                        return new List<Measurement<long>>() { new Measurement<long>(value, tags) };
                    });

                }
                if (Config.local.enable_analytics && StatsMeterProvider == null)
                {
                    StatsMeterProvider = OpenTelemetry.Sdk.CreateMeterProviderBuilder()
                        .AddMeter(OpenRPAMeter.Name)
                        .AddOtlpExporter(opt =>
                        {
                            // opt.Endpoint = new Uri("https://otel.stats.openiap.io");
                            opt.Endpoint = new Uri("http://otel.openiap.io");
                            opt.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                        })
                        .Build();
                }
                if (LocalMeterProvider == null && !string.IsNullOrEmpty(Config.local.otel_metric_url))
                {
                    LocalMeterProvider = OpenTelemetry.Sdk.CreateMeterProviderBuilder()
                        .AddMeter(OpenRPAMeter.Name)
                        .AddOtlpExporter(opt =>
                        {
                            opt.Endpoint = new Uri(Config.local.otel_metric_url);
                            opt.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
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
        private async void onWatchEvent(string id, Newtonsoft.Json.Linq.JObject data)
        {
            try
            {
                string _type = data["fullDocument"].Value<string>("_type");
                string _id = data["fullDocument"].Value<string>("_id");
                string operationType = data.Value<string>("operationType");
                Log.Debug("WatchEvent: " + _type + " with id " + _id + " operation: " + operationType);
                long _version = data["fullDocument"].Value<long>("_version");
                if (operationType != "replace" && operationType != "insert" && operationType != "update" && operationType != "delete") return;
                if (_type == "workflow")
                {
                    Log.Verbose(operationType + " version " + _version);
                    var workflow = Newtonsoft.Json.JsonConvert.DeserializeObject<Workflow>(data["fullDocument"].ToString());
                    var wfexists = instance.Workflows.FindById(_id);
                    if (operationType == "delete")
                    {
                        if (wfexists == null) return;
                        if (instance.Workflows.Delete(_id))
                        {
                            Views.OpenProject.UpdateProjectsList(true, false);
                        }
                        else
                        {
                            Log.Error("Failed deleting workflow " + wfexists.name + " with id" + _id);
                        }
                        return;
                    }
                    if (wfexists != null && wfexists._version != _version)
                    {
                        UpdateWorkflow(workflow, false);
                        Log.Verbose("Update " + workflow.ProjectAndName + " from version " + wfexists._version + " to " + _version + " " + workflow._id);
                    }
                    else if (wfexists == null)
                    {
                        Log.Verbose("Adding " + workflow.ProjectAndName + " with version " + _version + " " + workflow._id);
                        workflow.isDirty = false;
                        bool skiponline = true;
                        if (string.IsNullOrEmpty(workflow.Filename)) { workflow.Filename = workflow.UniqueFilename(); workflow.isDirty = true; skiponline = false; }
                        _ = workflow.RelativeFilename;
                        await workflow.Save<Workflow>(skiponline);
                        UpdateWorkflow(workflow, false);
                        Log.Verbose("Adding " + workflow.ProjectAndName + " with version " + _version + " " + workflow._id);
                        instance.NotifyPropertyChanged("Projects");
                    }
                }
                if (_type == "project")
                {
                    var project = Newtonsoft.Json.JsonConvert.DeserializeObject<Project>(data["fullDocument"].ToString());
                    project.isDirty = false;
                    Project exists = RobotInstance.instance.Projects.FindById(_id);
                    if (operationType == "delete")
                    {
                        if (exists == null) return;
                        if (instance.Projects.Delete(_id))
                        {
                            Views.OpenProject.UpdateProjectsList(true, false);
                        }
                        else
                        {
                            Log.Error("Failed deleting project " + exists.name + " with id" + _id);
                        }
                        return;
                    }
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
                    d.isDirty = false;
                    GenericTools.RunUI(() =>
                    {
                        try
                        {
                            IDetectorPlugin exists = Plugins.detectorPlugins.Where(x => x.Entity._id == d._id).FirstOrDefault();
                            if (operationType == "delete")
                            {
                                if (exists == null) return;
                                exists.Stop();
                                if (instance.Detectors.Delete(_id))
                                {
                                    Views.OpenProject.UpdateProjectsList(false, true);
                                }
                                else
                                {
                                    if (exists.Entity != null) Log.Error("Failed deleting detector " + exists.Entity.name + " with id" + _id);
                                    if (exists.Entity == null) Log.Error("Failed deleting detector with id" + _id);
                                }
                                return;
                            }
                            if (exists != null && d._version != exists.Entity._version)
                            {
                                d.UpdateRunning();
                            }
                            else if (exists == null)
                            {
                                d.Start(true);
                            }
                            var dexists = Detectors.FindById(d._id);
                            if (dexists == null) Detectors.Insert(d);
                            if (dexists != null) Detectors.Update(d);
                            Views.OpenProject.UpdateProjectsList(false, true);
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
                        var wfo = Workflow as Workflow;
                        var isDirty = wfo.isDirty;
                        wfo.isDirty = false;
                        instance.Workflows.Update(wfo);
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
