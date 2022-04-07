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
        public Project()
        {
            FilteredWorkflowsSource = (System.Windows.Data.ListCollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(Workflows);
            FilteredDetectorsSource = (System.Windows.Data.ListCollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(Detectors);
            FilteredWorkItemQueuesSource = (System.Windows.Data.ListCollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(WorkItemQueues);
        }
        public Dictionary<string, string> dependencies { get; set; }
        public bool disable_local_caching { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string Filename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        // [JsonIgnore, BsonRef("workflows")]
        private System.Collections.ObjectModel.ObservableCollection<IWorkflow> _Workflows = new System.Collections.ObjectModel.ObservableCollection<IWorkflow>();
        [JsonIgnore, BsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<IWorkflow> Workflows
        {
            get
            {
                return _Workflows;
            }
        }
        public void UpdateWorkflowsList()
        {
            var oldcount = _Workflows.Count;
            var list = RobotInstance.instance.Workflows.Find(x => x.projectid == _id && !x.isDeleted).OrderBy(x => x.name).ToList();
            _Workflows.UpdateCollection(list);
            NotifyPropertyChanged("FilteredWorkflows");
        }

        private System.Collections.ObjectModel.ObservableCollection<WorkitemQueue> _WorkItemQueues = new System.Collections.ObjectModel.ObservableCollection<WorkitemQueue>();
        [JsonIgnore, BsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<WorkitemQueue> WorkItemQueues
        {
            get
            {
                return _WorkItemQueues;
            }
        }
        public void UpdateWorkItemQueuesList()
        {
            var oldcount = _WorkItemQueues.Count;
            var list = RobotInstance.instance.WorkItemQueues.Find(x => x.projectid == _id && !x.isDeleted).OrderBy(x => x.name).ToList();
            _WorkItemQueues.UpdateCollection(list);
            NotifyPropertyChanged("FilteredWorkItemQueues");
        }
        public void UpdateFilteredWorkItemQueues()
        {
            var FilterText = Views.OpenProject.Instance.FilterText;
            if (string.IsNullOrEmpty(FilterText))
            {
                FilteredWorkItemQueuesSource.Filter = null;
            }
            FilteredWorkItemQueuesSource.Filter = p => ((WorkitemQueue)p).name.ToLower().Contains(FilterText.ToLower());
        }
        [JsonIgnore, BsonIgnore]
        public System.ComponentModel.ICollectionView FilteredWorkItemQueues
        {
            get
            {
                UpdateFilteredWorkItemQueues();
                return FilteredWorkItemQueuesSource;
            }
            set
            {

            }
        }


        [JsonIgnore, BsonIgnore]
        public System.Windows.Data.ListCollectionView FilteredWorkflowsSource;
        [JsonIgnore, BsonIgnore]
        public System.ComponentModel.ICollectionView FilteredWorkflows
        {
            get
            {
                UpdateFilteredWorkflows();
                return FilteredWorkflowsSource;
            }
        }
        public void UpdateFilteredWorkflows()
        {
            var FilterText = Views.OpenProject.Instance.FilterText;
            if (string.IsNullOrEmpty(FilterText))
            {
                FilteredWorkflowsSource.Filter = null;
            }
            FilteredWorkflowsSource.Filter = p => ((IWorkflow)p).name.ToLower().Contains(FilterText.ToLower());
        }
        private System.Collections.ObjectModel.ObservableCollection<IDetector> _Detectors = new System.Collections.ObjectModel.ObservableCollection<IDetector>();
        [JsonIgnore, BsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<IDetector> Detectors
        {
            get
            {
                return _Detectors;
            }
        }
        public void UpdateDetectorsList()
        {
            var oldcount = _Detectors.Count;
            var list = Plugins.detectorPlugins.Where(x => x.Entity.projectid == _id).OrderBy(x => x.Entity.name).Select(x => x.Entity).ToList();
            Detectors.UpdateCollection(list);
            NotifyPropertyChanged("FilteredDetectors");
        }



        [JsonIgnore, BsonIgnore]
        public System.Windows.Data.ListCollectionView FilteredDetectorsSource;
        [JsonIgnore, BsonIgnore]
        public System.Windows.Data.ListCollectionView FilteredWorkItemQueuesSource;

        [JsonIgnore, BsonIgnore]
        public System.ComponentModel.ICollectionView FilteredDetectors
        {
            get
            {
                UpdateFilteredDetectors();
                return FilteredDetectorsSource;
            }
            set
            {

            }
        }
        public void UpdateFilteredDetectors()
        {
            var FilterText = Views.OpenProject.Instance.FilterText;
            if (string.IsNullOrEmpty(FilterText))
            {
                FilteredDetectorsSource.Filter = null;
            }
            FilteredDetectorsSource.Filter = p => ((IDetector)p).name.ToLower().Contains(FilterText.ToLower());
        }
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
                var exists = RobotInstance.instance.Projects.FindById(project._id);
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
            await project.LoadFilesFromDisk(System.IO.Path.GetDirectoryName(Filepath));
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
                    var wf = RobotInstance.instance.Projects.FindById(_id);
                    if (wf._version == _version)
                    {
                        Log.Verbose("Saving " + this.name + " with version " + this._version);
                        RobotInstance.instance.Projects.Update(this);
                    }
                    else
                    {
                        Log.Verbose("Setting " + this.name + " with version " + this._version);
                        wf.IsExpanded = value;
                    }
                    RobotInstance.instance.Projects.Update(this);
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
                        var wf = RobotInstance.instance.Projects.FindById(_id);
                        if (wf == null) return;
                        if (wf._version == _version)
                        {
                            Log.Verbose("Saving " + this.name + " with version " + this._version);
                            RobotInstance.instance.Projects.Update(this);
                        }
                        else
                        {
                            Log.Verbose("Setting " + this.name + " with version " + this._version);
                            wf.IsSelected = value;
                        }
                        RobotInstance.instance.Projects.Update(this);
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
            Workflows.Add(w);
            return w;
        }
        private async Task LoadFilesFromDisk(string folder)
        {
            if (string.IsNullOrEmpty(folder)) folder = Path;
            var Files = System.IO.Directory.EnumerateFiles(folder, "*.*", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            foreach (string file in Files)
            {
                if (System.IO.Path.GetExtension(file).ToLower() == ".xaml")
                {
                    Workflows.Add(Workflow.FromFile(this, file));
                }
                else if (System.IO.Path.GetExtension(file).ToLower() == ".json")
                {
                    var json = System.IO.File.ReadAllText(file);
                    var o = JObject.Parse(json);
                    var _type = o.Value<string>("_type");
                    if (_type == "workitemqueue")
                    {
                        var _d = JsonConvert.DeserializeObject<WorkitemQueue>(json);
                        _d.isDirty = true;
                        _d.projectid = null;
                        //if (global.webSocketClient != null && global.webSocketClient.user != null && global.webSocketClient.isConnected)
                        //{
                        //    _d = await global.webSocketClient.AddWorkitemQueue(_d);
                        //} else if(string.IsNullOrEmpty(Config.local.wsurl))
                        //{
                        //    if (string.IsNullOrEmpty(_d._id)) _d._id = Guid.NewGuid().ToString();
                        //    await _d.Save();
                        //} else
                        //{
                        //    // System.Windows.MessageBox.Show("Not connected to " + Config.local.wsurl + " so cannot import WorkItemQueue");
                        //    throw new Exception("Not connected to " + Config.local.wsurl + " so cannot import WorkItemQueue");
                        //}
                        WorkItemQueues.Add(_d);
                    }
                    if (_type == "detector")
                    {
                        var _d = JsonConvert.DeserializeObject<Detector>(json);
                        if (!string.IsNullOrEmpty(_d._id))
                        {
                            var exists = RobotInstance.instance.Detectors.FindById(_d._id);
                            if (exists != null) { _d._id = null; } else { _d.isLocalOnly = true; }
                        }
                        if (string.IsNullOrEmpty(_d._id)) _d._id = Guid.NewGuid().ToString();
                        _d.isDirty = true;
                        _d.Start(true);
                        Detectors.Add(_d);
                    }
                }
            }
        }
        public static string UniqueName(string name, string _id = null)
        {
            string Name = name;
            Project exists = null;
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
                exists = RobotInstance.instance.Projects.Find(x => x.name == Name && x._id != _id).FirstOrDefault();
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
                if (filenames.Contains(workflow.Filename))
                {
                    workflow.Filename = workflow.UniqueFilename();
                }
                filenames.Add(workflow.Filename);
                workflow.ExportFile(System.IO.Path.Combine(projectpath, workflow.Filename));
            }
            foreach (var detector in Detectors)
            {
                var _name = detector.name;
                _name = r.Replace(_name, "");
                (detector as Detector).ExportFile(System.IO.Path.Combine(projectpath, _name + ".json"));
            }
            foreach (var wiq in WorkItemQueues)
            {
                var _name = wiq.name;
                _name = r.Replace(_name, "");
                (wiq as WorkitemQueue).ExportFile(System.IO.Path.Combine(projectpath, _name + ".json"));
            }
        }
        public async Task Save()
        {
            await Save<Project>();
            var wfs = Workflows.ToList();
            var dts = Detectors.ToList();
            foreach (var workflow in wfs)
            {
                workflow.projectid = _id;
                await workflow.Save();
            }
            foreach (Detector detector in dts)
            {
                detector.projectid = _id;
                await detector.Save();
            }
            if (WorkItemQueues.Count > 0)
            {
                foreach (WorkitemQueue wiq in WorkItemQueues.ToList())
                {
                    wiq.projectid = _id;
                    if (global.webSocketClient != null && global.webSocketClient.user != null && global.webSocketClient.isConnected)
                    {
                        WorkitemQueue _wiq;
                        if (wiq.isLocalOnly || string.IsNullOrEmpty(wiq._id))
                        {
                            _wiq = await global.webSocketClient.AddWorkitemQueue(wiq);
                            RobotInstance.instance.WorkItemQueues.Insert(_wiq);
                        } else
                        {
                            _wiq = await global.webSocketClient.UpdateWorkitemQueue(wiq, false);
                            RobotInstance.instance.WorkItemQueues.Update(_wiq);
                        }
                        GenericTools.RunUI(() =>
                        {
                            WorkItemQueues.Remove(wiq);
                            WorkItemQueues.Add(_wiq);
                        });
                    }
                    else if (!string.IsNullOrEmpty(Config.local.wsurl))
                    {
                        System.Windows.MessageBox.Show("Not connected to " + Config.local.wsurl + " so cannot validate WorkItemQueue, removing queue from import");
                        GenericTools.RunUI(() =>
                        {
                            WorkItemQueues.Remove(wiq);
                        });
                        RobotInstance.instance.WorkItemQueues.Insert(wiq);
                    }
                    else
                    {
                        await wiq.Save();
                        RobotInstance.instance.WorkItemQueues.Insert(wiq);
                    }                    
                }
                GenericTools.RunUI(() =>
                {
                    RobotInstance.instance.WorkItemQueuesSource.UpdateCollectionById(RobotInstance.instance.WorkItemQueues.FindAll());
                    UpdateFilteredWorkItemQueues();
                    UpdateWorkItemQueuesList();
                });
            }
        }
        public async Task Delete()
        {
            foreach (var wiq in WorkItemQueues.ToList()) {
                if (global.webSocketClient != null && global.webSocketClient.user != null && global.webSocketClient.isConnected)
                {
                    await global.webSocketClient.DeleteWorkitemQueue(wiq, true);
                    RobotInstance.instance.WorkItemQueues.Delete(wiq._id);
                    RobotInstance.instance.WorkItemQueuesSource.UpdateCollectionById(RobotInstance.instance.WorkItemQueues.FindAll());
                    UpdateFilteredWorkItemQueues();
                    UpdateWorkItemQueuesList();
                }
                else if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    System.Windows.MessageBox.Show("Not connected to " + Config.local.wsurl + " so cannot validate deletion of WorkItemQueue");
                } else
                {
                    await wiq.Delete();
                }                
            }
            foreach (var wf in Workflows.ToList()) { await wf.Delete(); }
            foreach (var d in Detectors.ToList()) { 
                var _d = d as Detector;
                _d.Stop();
                await _d.Delete(); 
            }
            
            if (System.IO.Directory.Exists(Path))
            {
                var Files = System.IO.Directory.EnumerateFiles(Path, "*.*", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
                foreach (var f in Files) System.IO.File.Delete(f);
            }
            if (global.isConnected)
            {
                if (!string.IsNullOrEmpty(_id))
                {
                    await global.webSocketClient.DeleteOne("openrpa", this._id);
                }
            }
            try
            {
                if (System.IO.Directory.Exists(Path)) System.IO.Directory.Delete(Path, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            var exists = RobotInstance.instance.Projects.FindById(_id);
            if (exists != null) RobotInstance.instance.Projects.Delete(_id);
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
