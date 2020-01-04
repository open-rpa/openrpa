using Newtonsoft.Json;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class Project : apibase
    {
        public string Filename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public System.Collections.ObjectModel.ObservableCollection<Workflow> Workflows { get; set; }
        [JsonIgnore]
        public string Path { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public static Project[] LoadProjects(string Path)
        {
            var ProjectFiles = System.IO.Directory.EnumerateFiles(Path, "*.rpaproj", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            var Projects = new List<Project>();
            foreach (string file in ProjectFiles) Projects.Add(FromFile(file));
            return Projects.ToArray();
        }
        public static Project FromFile(string Filepath)
        {
            Project project = JsonConvert.DeserializeObject<Project>(System.IO.File.ReadAllText(Filepath));
            project.Filename = System.IO.Path.GetFileName(Filepath);
            if (string.IsNullOrEmpty(project.name)) { project.name = System.IO.Path.GetFileNameWithoutExtension(Filepath); }
            project.Path = System.IO.Path.GetDirectoryName(Filepath);
            project._type = "project";
            project.Init();
            return project;
        }
        [JsonIgnore]
        public bool IsExpanded { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public bool IsSelected { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public static async Task<Project> Create(string Path, string Name, bool addDefault)
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
                Path = System.IO.Path.GetDirectoryName(Filepath),
                Filename = System.IO.Path.GetFileName(Filepath),
                Workflows = new System.Collections.ObjectModel.ObservableCollection<Workflow>()
            };
            await p.Save(false);
            if(addDefault)
            {
                var w = Workflow.Create(p, "New Workflow");
                p.Workflows.Add(w);
                await w.Save(false);
            }
            return p;
        }
        public void Init()
        {
            var Path = System.IO.Path.GetDirectoryName(System.IO.Path.Combine(this.Path, Filename));
            var ProjectFiles = System.IO.Directory.EnumerateFiles(Path, "*.xaml", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            Workflows = new System.Collections.ObjectModel.ObservableCollection<Workflow>();
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

            if (Workflows == null) Workflows = new System.Collections.ObjectModel.ObservableCollection<Workflow>();

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
            var Files = System.IO.Directory.EnumerateFiles(Path, "*.*", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
            foreach (var f in Files) System.IO.File.Delete(f);
            if (!global.isConnected) return;
            if (!string.IsNullOrEmpty(_id))
            {
                await global.webSocketClient.DeleteOne("openrpa", this._id);
            }
            try
            {
                System.IO.Directory.Delete(Path);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public override string ToString()
        {
            return name;
        }
    }
}
