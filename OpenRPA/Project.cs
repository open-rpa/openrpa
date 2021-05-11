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
            FilteredSource = ((System.Windows.Data.ListCollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(Workflows));
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
            var list = RobotInstance.instance.Workflows.Find(x => x.projectid == _id && !x.isDeleted).OrderBy(x => x.name).ToList();
            Workflows.UpdateCollection(list);
        }
        [JsonIgnore, BsonIgnore]
        public System.Windows.Data.ListCollectionView FilteredSource;
        [JsonIgnore, BsonIgnore]
        public System.ComponentModel.ICollectionView FilteredWorkflows
        {
            get
            {
                UpdateFilteredWorkflows();
                return FilteredSource;
            }
        }
        public void UpdateFilteredWorkflows()
        {
            var FilterText = Views.OpenProject.Instance.FilterText;
            if (string.IsNullOrEmpty(FilterText))
            {
                FilteredSource.Filter = null;
            }
            FilteredSource.Filter = p => ((IWorkflow)p).name.ToLower().Contains(FilterText.ToLower());

        }

        //public override bool Equals(object obj)
        //{
        //    var p = obj as Project;
        //    if (p == null) return false;
        //    if (p._id != _id) return false;
        //    return true;
        //}
        //public override int GetHashCode()
        //{
        //    int hash = 13;
        //    hash = (hash * 7) + _id.GetHashCode();
        //    return hash;
        //}
        // public List<IWorkflow> Workflows { get; set; }
        // public System.Collections.ObjectModel.ObservableCollection<IWorkflow> Workflows { get; set; }
        [JsonIgnore, BsonIgnore]
        public string Path
        {
            get
            {
                return System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, name);
            }
        }
        // public string Path { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public static async Task<Project[]> LoadProjects(string Path)
        {
            var ProjectFiles = System.IO.Directory.EnumerateFiles(Path, "*.rpaproj", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            var Projects = new List<Project>();
            foreach (string file in ProjectFiles) Projects.Add(await FromFile(file));
            return Projects.ToArray();
        }
        public static async Task<Project> FromFile(string Filepath)
        {
            Project project = JsonConvert.DeserializeObject<Project>(System.IO.File.ReadAllText(Filepath));
            project.Filename = System.IO.Path.GetFileName(Filepath);
            if (string.IsNullOrEmpty(project.name)) { project.name = System.IO.Path.GetFileNameWithoutExtension(Filepath); }
            project._type = "project";
            project.Init();
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
        public void Init()
        {
            var Path = System.IO.Path.GetDirectoryName(System.IO.Path.Combine(this.Path, Filename));
            var ProjectFiles = System.IO.Directory.EnumerateFiles(Path, "*.xaml", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            foreach (string file in ProjectFiles) Workflows.Add(Workflow.FromFile(this, file));
            //return Workflows.ToArray();
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
        }
        public async Task Save()
        {
            await Save<Project>();
            foreach (var workflow in Workflows)
            {
                try
                {
                    await workflow.Save();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error saving " + workflow.name, ex);
                }
            }
        }
        public async Task Delete()
        {
            foreach (var wf in Workflows.ToList()) { await wf.Delete(); }
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
