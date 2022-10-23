using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class Project : LocallyCached, IProject
    {
        Project()
        {
            Workflows = new FilteredObservableCollection<IWorkflow>(RobotInstance.instance.Workflows, wffilter);
            WorkItemQueues = new FilteredObservableCollection<IWorkitemQueue>(RobotInstance.instance.WorkItemQueues, wiqfilter);
            Detectors = new FilteredObservableCollection<IDetector>(RobotInstance.instance.Detectors, detectorfilter);
            Children = new CompositionObservableCollection(WorkItemQueues, Detectors, Workflows);
            RobotInstance.instance.PropertyChanged += Instance_PropertyChanged;
        }
        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "FilterText")
            {
                Workflows.Refresh();
                Detectors.Refresh();
                WorkItemQueues.Refresh();
            }
        }
        public Dictionary<string, string> dependencies { get; set; }
        public bool disable_local_caching { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool save_output { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool send_output { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string Filename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore, BsonIgnore]
        public FilteredObservableCollection<IWorkflow> Workflows { get; set; } 
        public bool wffilter(IWorkflow item)
        {
            if (item == null) return false;
            if (string.IsNullOrEmpty(_id)) return false;
            string FilterText = "";
            if (Views.OpenProject.Instance != null) FilterText = RobotInstance.instance.FilterText;
            if (string.IsNullOrEmpty(FilterText)) return item.projectid == _id;
            FilterText = FilterText.ToLower();
            return item.projectid == _id && item.name.ToLower().Contains(FilterText);
        }
        [JsonIgnore, BsonIgnore]
        public FilteredObservableCollection<IWorkitemQueue> WorkItemQueues { get; set; }
        public bool wiqfilter(IWorkitemQueue item)
        {
            if (item == null) return false;
            if (string.IsNullOrEmpty(_id)) return false;
            string FilterText = "";
            if (Views.OpenProject.Instance != null) FilterText = RobotInstance.instance.FilterText;
            if (string.IsNullOrEmpty(FilterText)) return item.projectid == _id;
            FilterText = FilterText.ToLower();
            return item.projectid == _id && item.name.ToLower().Contains(FilterText);
        }
        [JsonIgnore, BsonIgnore]
        public FilteredObservableCollection<IDetector> Detectors { get; set; }
        public bool detectorfilter(IDetector item)
        {
            if (item == null) return false;
            if (string.IsNullOrEmpty(_id)) return false;
            string FilterText = "";
            if (Views.OpenProject.Instance != null) FilterText = RobotInstance.instance.FilterText;
            if (string.IsNullOrEmpty(FilterText)) return item.projectid == _id;
            FilterText = FilterText.ToLower();
            return item.projectid == _id && item.name.ToLower().Contains(FilterText);
        }

        [JsonIgnore, BsonIgnore]
        public CompositionObservableCollection Children { get; set; }

        [JsonIgnore, BsonIgnore]
        public string Path
        {
            get
            {
                return System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, name);
            }
        }
        public static async Task<Project> FromFile(string Filepath)
        {
            Project project = JsonConvert.DeserializeObject<Project>(System.IO.File.ReadAllText(Filepath));
            if(!string.IsNullOrEmpty(project._id))
            {
                var exists = RobotInstance.instance.Projects.Where(x => x._id == project._id).FirstOrDefault() as Project;
                if (exists != null) { project._id = null; } else { project.isLocalOnly = true; }
            }
            if (string.IsNullOrEmpty(project._id))
            {
                project._id = Guid.NewGuid().ToString();
                project.name = UniqueName(project.name, project._id);
            }
            project.isDirty = true;
            project.Filename = System.IO.Path.GetFileName(Filepath);
            if (string.IsNullOrEmpty(project.name)) { project.name = System.IO.Path.GetFileNameWithoutExtension(Filepath); }
            project._type = "project";
            await project.Save();

            await project.LoadFilesFromDisk(System.IO.Path.GetDirectoryName(Filepath));
            await project.Save();
            await project.InstallDependencies(true);
            return project;
        }
        [JsonIgnore]
        public bool IsExpanded
        {
            get { return GetProperty<bool>(); }
            set
            {
                if (value == GetProperty<bool>()) return;
                SetProperty(value);
                if (Views.OpenProject.isUpdating) return;
                if (!_backingFieldValues.ContainsKey("IsExpanded")) return;
                //if (value && orgvalue != value && (_Workflows ==null || _Workflows.Count == 0)) UpdateWorkflowsList();
                //if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name) && orgvalue != value) RobotInstance.instance.Projects.Update(this);
                if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name))
                {
                    var wf = RobotInstance.instance.dbProjects.FindById(_id);
                    if (wf._version == _version)
                    {
                        Log.Verbose("Saving " + this.name + " with version " + this._version);
                        RobotInstance.instance.dbProjects.Update(this);
                    }
                    else
                    {
                        Log.Verbose("Setting " + this.name + " with version " + this._version);
                        wf.IsExpanded = value;
                    }
                    RobotInstance.instance.dbProjects.Update(this);
                }
            }
        }
        [JsonIgnore]
        public bool IsSelected
        {
            get { return GetProperty<bool>(); }
            set
            {
                if (Views.OpenProject.isUpdating) return;
                if (value == GetProperty<bool>()) return;
                SetProperty(value);
                if (!_backingFieldValues.ContainsKey("IsSelected")) return;
                if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name))
                {
                    if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name))
                    {
                        var wf = RobotInstance.instance.dbProjects.FindById(_id);
                        if (wf == null) return;
                        if (wf._version == _version)
                        {
                            Log.Verbose("Saving " + this.name + " with version " + this._version);
                            RobotInstance.instance.dbProjects.Update(this);
                        }
                        else
                        {
                            Log.Verbose("Setting " + this.name + " with version " + this._version);
                            wf.IsSelected = value;
                        }
                        RobotInstance.instance.dbProjects.Update(this);
                    }
                }

            }
        }
        public static async Task<Project> Create(string Path, string Name)
        {
            Name = UniqueName(Name, null);
            var Filename = Name.Replace(" ", "_").Replace(".", "") + ".rpaproj";
            Project p = new Project
            {
                _type = "project",
                name = Name,
                Filename = Filename
            };
            await p.Save();
            return p;
        }
        public async Task<IWorkflow> AddDefaultWorkflow()
        {
            var w = await Workflow.Create(this, "New Workflow");
            // Workflows.Add(w);
            return w;
        }
        public async Task LoadFileFromDisk(string file)
        {
            if (System.IO.Path.GetExtension(file).ToLower() == ".xaml")
            {
                var wf = Workflow.FromFile(this, file);
                wf.projectid = _id;
                wf._acl = _acl;
                await wf.Save();
            }
            else if (System.IO.Path.GetExtension(file).ToLower() == ".json")
            {
                var json = System.IO.File.ReadAllText(file);
                JObject o = null;
                try
                {
                    o = JObject.Parse(json);
                }
                catch (Exception ex)
                {
                    Log.Warning("Error parsing " + file);
                    Log.Error(ex.Message);
                    return;
                }
                if (!o.ContainsKey("_type"))
                {
                    Log.Warning("skipping " + file + " missing _type field");
                    return;
                }
                var _type = o.Value<string>("_type");
                if (_type == "workitemqueue")
                {
                    var _wiq = JsonConvert.DeserializeObject<WorkitemQueue>(json);
                    _wiq.projectid = _id;
                    _wiq._acl = _acl;
                    _wiq.isDirty = true;
                    _wiq.projectid = _id;
                    if (!string.IsNullOrEmpty(_wiq._id))
                    {
                        var exists = RobotInstance.instance.dbWorkItemQueues.FindById(_wiq._id);
                        if (exists == null)
                        {
                            exists = RobotInstance.instance.dbWorkItemQueues.Find(x => x.name.ToLower() == _wiq.name.ToLower()).FirstOrDefault();
                        } else
                        {
                            if (string.IsNullOrEmpty(exists.name)) exists.name = "";
                            if (exists.name.ToLower() != _wiq.name.ToLower())
                            {
                                _wiq._id = null;
                                exists = null;
                            }
                        }
                        if (exists != null) {
                            Log.Warning("Skipping workitem queeu " + exists.name + " it already exists");
                            return;
                        } else { _wiq.isLocalOnly = true; }
                    }
                    _wiq.isDirty = true;
                    if (global.webSocketClient != null && global.webSocketClient.user != null && global.webSocketClient.isConnected)
                    {
                        if (_wiq.isLocalOnly || string.IsNullOrEmpty(_wiq._id))
                        {
                            await _wiq.Save(true);
                        }
                        else
                        {
                            await _wiq.Save();
                        }
                    }
                    else if (!string.IsNullOrEmpty(Config.local.wsurl))
                    {
                        System.Windows.MessageBox.Show("Not connected to " + Config.local.wsurl + " so cannot validate WorkItemQueue, removing queue from import");
                    }
                    else
                    {
                        await _wiq.Save(true);
                    }
                    Log.Output("Adding workitem queue " + _wiq.name);
                }
                if (_type == "detector")
                {
                    var _d = JsonConvert.DeserializeObject<Detector>(json);
                    if (!string.IsNullOrEmpty(_d._id))
                    {
                        var exists = RobotInstance.instance.dbDetectors.FindById(_d._id);
                        if (exists != null) { _d._id = null; } else { _d.isLocalOnly = true; }
                    }
                    if (string.IsNullOrEmpty(_d._id)) _d._id = Guid.NewGuid().ToString();
                    _d.projectid = _id;
                    _d._acl = _acl;
                    _d.isDirty = true;
                    _d.projectid = _id;
                    _d.Start(true);
                    Detectors.Add(_d);
                    Log.Output("Adding detector " + _d.name);
                }
                if (_type == "workflow")
                {
                    var _wf = JsonConvert.DeserializeObject<Workflow>(json);

                    var exists = RobotInstance.instance.dbWorkflows.FindById(_wf._id);
                    _wf.projectid = _id;
                    if (exists == null)
                    {
                        exists = RobotInstance.instance.dbWorkflows.Find(x => x.ProjectAndName.ToLower() == _wf.ProjectAndName.ToLower()).FirstOrDefault();
                    }
                    else
                    {
                        if (exists.ProjectAndName.ToLower() != _wf.ProjectAndName.ToLower())
                        {
                            _wf._id = null;
                            exists = null;
                        }
                    }
                    if (exists != null)
                    {
                        Log.Warning("Skipping workitem queeu " + exists.name + " it already exists");
                        return;
                    }
                    else { _wf.isLocalOnly = true; }
                    //if (!string.IsNullOrEmpty(_wf._id))
                    //{
                    //    var exists = RobotInstance.instance.Workflows.Where(x => x._id == _wf._id).FirstOrDefault();
                    //    if (exists != null) { _wf._id = null; } else { _wf.isLocalOnly = true; }
                    //}
                    if (string.IsNullOrEmpty(_wf._id)) _wf._id = Guid.NewGuid().ToString();
                    _wf.isDirty = true;
                    _wf.projectid = _id;
                    _wf._acl = _acl;
                    await _wf.Save();
                    Log.Output("Adding workflow " + _wf.name);
                }
            }
        }
        private async Task LoadFilesFromDisk(string folder)
        {
            if (string.IsNullOrEmpty(folder)) folder = Path;
            var Files = System.IO.Directory.EnumerateFiles(folder, "*.*", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            foreach (string file in Files)
            {
                try
                {
                    await LoadFileFromDisk(file);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Output(ex.Message);
                }
            }
        }
        public static string UniqueName(string name, string _id = null)
        {
            string Name = name;
            IProject exists = null;
            bool isUnique = false; int counter = 1;
            while (!isUnique)
            {
                if (counter == 1)
                {
                }
                else
                {
                    Name = name + counter.ToString();
                }
                exists = RobotInstance.instance.Projects.Where(x => x.name == Name && x._id != _id).FirstOrDefault();
                isUnique = (exists == null);
                counter++;
            }
            return Name;
        }
        public void ExportProject(string rootpath)
        {
            string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());
            var r = new System.Text.RegularExpressions.Regex(string.Format("[{0}]", System.Text.RegularExpressions.Regex.Escape(regexSearch)));
            name = r.Replace(name, "");
            var projectpath = Path;

            if (!string.IsNullOrEmpty(rootpath)) projectpath = System.IO.Path.Combine(rootpath, name);
            if (!System.IO.Directory.Exists(projectpath)) System.IO.Directory.CreateDirectory(projectpath);

            var projectfilepath = System.IO.Path.Combine(projectpath, Filename);
            System.IO.File.WriteAllText(projectfilepath, JsonConvert.SerializeObject(this));
            var filenames = new List<string>();
            foreach (var workflow in Workflows)
            {
                _ = ((Workflow)workflow).ExportFile(System.IO.Path.Combine(projectpath, workflow._id + ".json"));
            }
            foreach (var detector in Detectors)
            {
                (detector as Detector).ExportFile(System.IO.Path.Combine(projectpath, detector._id + ".json"));
            }
            foreach (var wiq in WorkItemQueues)
            {
                (wiq as WorkitemQueue).ExportFile(System.IO.Path.Combine(projectpath, wiq._id + ".json"));
            }
        }
        public async Task Save(bool skipOnline = false)
        {
            await Save<Project>(skipOnline);
            if(!skipOnline) isDirty = false;
            foreach (var workflow in Workflows.ToList())
            {
                if (workflow.projectid != _id) workflow.projectid = _id;
                await workflow.Save(skipOnline);
            }
            foreach (Detector detector in Detectors.ToList())
            {
                if (detector.projectid != _id) detector.projectid = _id;
                await detector.Save(skipOnline);
            }
            foreach (WorkitemQueue wiq in WorkItemQueues.ToList())
            {
                if (wiq.projectid != _id) wiq.projectid = _id;
                await wiq.Save(skipOnline);
            }
            GenericTools.RunUI(() =>
            {
                if (System.Threading.Monitor.TryEnter(RobotInstance.instance.Projects, Config.local.thread_lock_timeout_seconds * 1000))
                {
                    try
                    {
                        var exists = RobotInstance.instance.Projects.FindById(_id);
                        if (exists == null) RobotInstance.instance.Projects.Add(this);
                        if (exists != null) RobotInstance.instance.Projects.UpdateItem(exists, this);
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(RobotInstance.instance.Projects);
                    }
                }
                else { throw new LockNotReceivedException("Saving Project"); }
            });
        }
        public async Task Update(IProject item, bool skipOnline = false)
        {
            GenericTools.RunUI(() =>
            {
                RobotInstance.instance.Projects.UpdateItem(this, item);
            });
            await Save<Project>(skipOnline);
        }
        public async Task Delete(bool skipOnline = false)
        {
            foreach (var wiq in WorkItemQueues.ToList()) {
                if (global.webSocketClient != null && global.webSocketClient.user != null && global.webSocketClient.isConnected)
                {
                    try
                    {
                        if(!skipOnline) await global.webSocketClient.DeleteWorkitemQueue(wiq, true, "", "");
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("not found") && !ex.Message.Contains("denied")) throw;
                    }
                    GenericTools.RunUI(()=> {
                        RobotInstance.instance.dbWorkItemQueues.Delete(wiq._id);
                        RobotInstance.instance.WorkItemQueues.Remove(wiq);
                    });
                }
                else if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    System.Windows.MessageBox.Show("Not connected to " + Config.local.wsurl + " so cannot validate deletion of WorkItemQueue");
                } else
                {
                    await wiq.Delete(skipOnline);
                }                
            }
            foreach (var wf in Workflows.ToList()) { 
                await wf.Delete(skipOnline); 
            }
            foreach (var d in Detectors.ToList()) { 
                var _d = d as Detector;
                _d.Stop();
                await _d.Delete(skipOnline); 
            }
            
            if (System.IO.Directory.Exists(Path))
            {
                if(!skipOnline)
                {
                    var Files = System.IO.Directory.EnumerateFiles(Path, "*.*", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
                    foreach (var f in Files) System.IO.File.Delete(f);
                }
            }
            if (global.isConnected && !skipOnline)
            {
                if (!string.IsNullOrEmpty(_id))
                {
                    if (!skipOnline) await global.webSocketClient.DeleteOne("openrpa", this._id, "", "");
                }
            }
            try
            {
                if (!skipOnline && System.IO.Directory.Exists(Path)) System.IO.Directory.Delete(Path, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            var exists = RobotInstance.instance.dbProjects.FindById(_id);
            if (exists != null)
            {
                RobotInstance.instance.dbProjects.Delete(_id);
            }
            GenericTools.RunUI(() =>
            {
                if (System.Threading.Monitor.TryEnter(RobotInstance.instance.Projects, Config.local.thread_lock_timeout_seconds * 1000))
                {
                    try
                    {
                        RobotInstance.instance.Projects.Remove(this);
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(RobotInstance.instance.Projects);
                    }
                }
                else { throw new LockNotReceivedException("Delete Project"); }
            });
        }
        public override string ToString()
        {
            return name;
        }
        public async Task InstallDependencies(bool LoadDlls)
        {
            if (dependencies == null) return;
            if (dependencies.Count == 0) return;
            foreach (var jp in dependencies)
            {
                var ver_range = VersionRange.Parse(jp.Value);
                if (ver_range.IsMinInclusive)
                {
                    Log.Information("DownloadAndInstall " + jp.Key);
                    var target_ver = NuGet.Versioning.NuGetVersion.Parse(ver_range.MinVersion.ToString());
                    await NuGetPackageManager.Instance.DownloadAndInstall(this, new NuGet.Packaging.Core.PackageIdentity(jp.Key, target_ver), LoadDlls);
                }
            }
            // Plugins.LoadPlugins(RobotInstance.instance, Interfaces.Extensions.ProjectsDirectory);
            Plugins.LoadPlugins(RobotInstance.instance);
        }
    }
}
