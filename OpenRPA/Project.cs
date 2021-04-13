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
    public class Project : apibase, IProject
    {
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
            var list = RobotInstance.instance.Workflows.Find(x => x.projectid == _id).ToList();
            // Log.Output("Update workflows for " + name);
            Workflows.UpdateCollection(list);
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
        public bool IsExpanded { 
            get { return GetProperty<bool>(); } 
            set {
                if (!_backingFieldValues.ContainsKey("IsExpanded"))
                {
                    SetProperty(value);
                    return;
                }
                var orgvalue = GetProperty<bool>();
                if (Views.OpenProject.isUpdating) return;
                SetProperty(value);
                if (value && orgvalue != value && (_Workflows ==null || _Workflows.Count == 0)) UpdateWorkflowsList();
                if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name) && orgvalue != value) RobotInstance.instance.Projects.Update(this);
            } 
        }
        [JsonIgnore]
        public bool IsSelected { 
            get { return GetProperty<bool>(); }
            set
            {
                if (Views.OpenProject.isUpdating) return;
                SetProperty(value); 
                if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name)) RobotInstance.instance.Projects.Update(this); 
            }
        }
        public static async Task<Project> Create(string Path, string Name)
        {
            var basePath = System.IO.Path.Combine(Path, Name);
            if (System.IO.Directory.Exists(basePath) && System.IO.Directory.GetFiles(basePath).Count() > 0)
            {
                var originalname = Name;
                bool isUnique = false; int counter = 1;
                while (!isUnique)
                {
                    if (counter == 1)
                    {
                        basePath = System.IO.Path.Combine(Path, Name);
                    }
                    else
                    {
                        Name = originalname + counter.ToString();
                        basePath = System.IO.Path.Combine(Path, Name);
                    }
                    if (!System.IO.Directory.Exists(basePath)) isUnique = true;
                    counter++;
                }
            }
            var Filepath = System.IO.Path.Combine(Path, Name, Name.Replace(" ", "_").Replace(".", "") + ".rpaproj");

            if (string.IsNullOrEmpty(Filepath))
            {
                bool isUnique = false; int counter = 1;
                while (!isUnique)
                {
                    if (counter == 1)
                    {
                        Filepath = System.IO.Path.Combine(Path, Name, Name.Replace(" ", "_").Replace(".", "") + ".rpaproj");
                    }
                    else
                    {
                        Filepath = System.IO.Path.Combine(Path, Name, Name.Replace(" ", "_").Replace(".", "") + counter.ToString() +  ".rpaproj");
                    }
                    if (!System.IO.File.Exists(Filepath)) isUnique = true;
                    counter++;
                }
            }
            if (!System.IO.Directory.Exists(basePath)) System.IO.Directory.CreateDirectory(basePath);
            if (System.IO.Directory.GetFiles(basePath).Count() > 0) throw new Exception(basePath + " is not empty");
            Project p = new Project
            {
                _type = "project",
                name = Name,
                Filename = System.IO.Path.GetFileName(Filepath)
            };
            await p.Save(false);
            return p;
        }
        public async Task<IWorkflow> AddDefaultWorkflow()
        {
            var w = Workflow.Create(this, "New Workflow");
            this.Workflows.Add(w);
            await w.Save(false);
            return w;
        }
        public void Init()
        {
            var Path = System.IO.Path.GetDirectoryName(System.IO.Path.Combine(this.Path, Filename));
            var ProjectFiles = System.IO.Directory.EnumerateFiles(Path, "*.xaml", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            foreach (string file in ProjectFiles) Workflows.Add(Workflow.FromFile(this, file));
            //return Workflows.ToArray();
        }
        public void SaveFile(string rootpath = null, bool exportImages = false)
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
                if(filenames.Contains(workflow.Filename))
                {
                    workflow.Filename = workflow.UniqueFilename();
                    _ = workflow.Save(false);
                }
                filenames.Add(workflow.Filename);
                workflow.SaveFile(projectpath, exportImages);
            }
        }
        public async Task Save(bool UpdateImages)
        {
            SaveFile();
            if (global.isConnected)
            {
                try
                {
                    RobotInstance.instance.DisableWatch = true;
                    if (string.IsNullOrEmpty(_id))
                    {
                        var result = await global.webSocketClient.InsertOne("openrpa", 0, false, this);
                        _id = result._id;
                        _acl = result._acl;
                        _modified = result._modified;
                        _modifiedby = result._modifiedby;
                        _modifiedbyid = result._modifiedbyid;
                    }
                    else
                    {
                        var result = await global.webSocketClient.UpdateOne("openrpa", 0, false, this);
                        _acl = result._acl;
                        _modified = result._modified;
                        _modifiedby = result._modifiedby;
                        _modifiedbyid = result._modifiedbyid;
                        _version = result._version;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {
                    RobotInstance.instance.DisableWatch = false;
                }
            }
            foreach (var workflow in Workflows)
            {
                try
                {
                    await workflow.Save(UpdateImages);
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
            if(System.IO.Directory.Exists(Path))
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
            if(exists!=null) RobotInstance.instance.Projects.Delete(_id);
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
