using Newtonsoft.Json;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OpenRPA
{
    public class Workflow : apibase, IWorkflow
    {
        [JsonIgnore]
        private long _current_version = 0;
        public long current_version { get {
                if (_version > _current_version) return _version;
                return _current_version;
                        } 
            set {
                _current_version = value;
            } 
        }
        public Workflow()
        {
            Serializable = true;
        }
        public string queue { get { return GetProperty<string>(); } set { SetProperty(value); } }        
        public string Xaml { get { return GetProperty<string>(); } set { _activity = null; SetProperty(value); } }
        public List<workflowparameter> Parameters { get { return GetProperty<List<workflowparameter>>(); } set { SetProperty(value); } }
        public bool Serializable { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string Filename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public string RelativeFilename
        {
            get
            {
                if (Project == null) return Filename;
                if (string.IsNullOrEmpty(Project.Path)) return Filename;
                string lastFolderName = System.IO.Path.GetFileName(Project.Path);
                return System.IO.Path.Combine(lastFolderName, Filename);
            }
        }
        [JsonIgnore]
        public string IDOrRelativeFilename
        {
            get
            {
                if (string.IsNullOrEmpty(_id)) return RelativeFilename;
                return _id;
            }
        }
        private string _ProjectAndName;
        [JsonProperty("projectandname")]
        public string ProjectAndName
        {
            get
            {
                if (Project == null)
                {
                    if (!string.IsNullOrEmpty(_ProjectAndName)) return _ProjectAndName;
                    return name;
                }
                return Project.name + "/" + name;
            }
            set
            {
                _ProjectAndName = value;
            }
        }
        public string FilePath
        {
            get
            {
                return System.IO.Path.Combine(Project.Path, Filename);
            }
        }   
        public string projectid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public bool IsExpanded { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public bool IsSelected { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        private string laststate = "unloaded";
        [JsonIgnore]
        public string State
        {
            get
            {
                string state = laststate;
                var instace = Instances;
                if (instace.Count() > 0)
                {
                    var running = instace.Where(x => x.isCompleted == false).ToList();
                    if (running.Count() > 0)
                    {
                        state = "running";
                    }
                    else
                    {
                        laststate = "running";
                        state = instace.First().state;
                    }
                    laststate = state;
                }
                return state;
            }
        }
        [JsonIgnore]
        public string StateImage
        {
            get
            {
                switch (State)
                {
                    case "unloaded": return "/OpenRPA;component/Resources/state/unloaded.png";
                    case "running": return "/OpenRPA;component/Resources/state/Running_green.png";
                    case "aborted": return "/OpenRPA;component/Resources/state/Abort.png";
                    case "failed": return "/OpenRPA;component/Resources/state/failed.png";
                    case "completed": return "/OpenRPA;component/Resources/state/Completed.png";
                    default: return "/OpenRPA;component/Resources/state/unloaded.png";
                }
            }
        }
        public void NotifyUIState()
        {
            NotifyPropertyChanged("State");
            NotifyPropertyChanged("StateImage");
        }
        [JsonIgnore]
        public List<WorkflowInstance> Instances
        {
            get
            {
                return WorkflowInstance.Instances.Where(x => (x.WorkflowId == _id && !string.IsNullOrEmpty(_id)) || (x.RelativeFilename == RelativeFilename && string.IsNullOrEmpty(_id))).ToList();
            }
        }
        [JsonIgnore]
        public bool isRunnning
        {
            get
            {
                foreach (var i in WorkflowInstance.Instances.ToList())
                {
                    if (!string.IsNullOrEmpty(_id) && i.WorkflowId == _id)
                    {
                        if (i.state != "completed" && i.state != "aborted" && i.state != "failed")
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        [JsonIgnore]
        public Project Project { get; set; }
        public static Workflow FromFile(Project project, string Filename)
        {
            var result = new Workflow();
            result._type = "workflow";
            result.Project = project;
            result.Filename = System.IO.Path.GetFileName(Filename);
            result.name = System.IO.Path.GetFileNameWithoutExtension(Filename);
            result.Xaml = System.IO.File.ReadAllText(Filename);
            result.Parameters = new List<workflowparameter>();
            //sresult.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
            return result;
        }
        public static Workflow Create(Project Project, string Name)
        {
            Workflow workflow = new Workflow { Project = Project, name = Name, _acl = Project._acl };
            bool isUnique = false; int counter = 1;
            while (!isUnique)
            {
                if (counter == 1)
                {
                    // workflow.FilePath = System.IO.Path.Combine(Project.Path, Name.Replace(" ", "_").Replace(".", "") + ".xaml");
                    workflow.Filename = Name.Replace(" ", "_").Replace(".", "") + ".xaml";
                }
                else
                {
                    workflow.name = Name + counter.ToString();
                    //workflow.FilePath = System.IO.Path.Combine(Project.Path, Name.Replace(" ", "_").Replace(".", "") + counter.ToString() + ".xaml");
                    workflow.Filename = Name.Replace(" ", "_").Replace(".", "") + counter.ToString() + ".xaml";
                }
                if (!System.IO.File.Exists(workflow.FilePath)) isUnique = true;
                counter++;
            }
            workflow._type = "workflow";
            workflow.Parameters = new List<workflowparameter>();
            //workflow.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
            workflow.projectid = Project._id;
            return workflow;
        }
        public void SaveFile(string overridepath = null, bool exportImages = false)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (string.IsNullOrEmpty(Xaml)) return;
            if (!Project.Workflows.Contains(this)) Project.Workflows.Add(this);

            var workflowpath = Project.Path;
            if (!string.IsNullOrEmpty(overridepath)) workflowpath = overridepath;
            var workflowfilepath = System.IO.Path.Combine(workflowpath, Filename);
            if (string.IsNullOrEmpty(workflowfilepath))
            {
                Filename = UniqueFilename();
            }
            else
            {
                var guess = name.Replace(" ", "_").Replace(".", "") + ".xaml";
                var newName = UniqueFilename();
                if (guess == newName && Filename != guess)
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(workflowpath, guess), Xaml);
                    System.IO.File.Delete(workflowfilepath);
                    Filename = guess;
                    return;
                }
            }
            if(exportImages)
            {
                GenericTools.RunUI(async () => {
                    string beforexaml = Xaml;
                    string xaml = await Views.WFDesigner.LoadImages(beforexaml);
                    //string xaml = Task.Run(() =>
                    //{
                    //    return Views.WFDesigner.LoadImages(beforexaml);
                    //}).Result;
                    System.IO.File.WriteAllText(workflowfilepath, xaml);
                });
                return;
            }
            if(Project.disable_local_caching)
            {
                if (System.IO.File.Exists(workflowfilepath)) System.IO.File.Delete(workflowfilepath);
                return;
            }
            System.IO.File.WriteAllText(workflowfilepath, Xaml);
        }
        public async Task Save(bool UpdateImages)
        {
            try
            {
                SaveFile();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            projectid = Project._id;
            if (!global.isConnected) return;
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
                if (UpdateImages)
                {
                    var files = await global.webSocketClient.Query<metadataitem>("files", "{\"metadata.workflow\": \"" + _id + "\"}");
                    foreach (var f in files)
                    {
                        bool equal = f.metadata._acl.SequenceEqual(_acl);
                        if (!equal)
                        {
                            f.metadata._acl = _acl;
                            await global.webSocketClient.UpdateOne("files", 0, false, f);
                        }
                    }
                }
            }
        }
        public async Task Delete()
        {
            if (Project.Workflows.Contains(this)) Project.Workflows.Remove(this);
            if (string.IsNullOrEmpty(FilePath)) return;
            System.IO.File.Delete(FilePath);
            if (!global.isConnected) return;
            if (!string.IsNullOrEmpty(_id))
            {
                var imagepath = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "images");
                if (!System.IO.Directory.Exists(imagepath)) System.IO.Directory.CreateDirectory(imagepath);
                var files = await global.webSocketClient.Query<metadataitem>("files", "{\"metadata.workflow\": \"" + _id + "\"}");
                foreach (var f in files)
                {
                    await global.webSocketClient.DeleteOne("files", f._id);
                    var imagefilepath = System.IO.Path.Combine(imagepath, f._id + ".png");
                    if (System.IO.File.Exists(imagefilepath)) System.IO.File.Delete(imagefilepath);
                }
                await global.webSocketClient.DeleteOne("openrpa", this._id);
            }
        }
        public void RunPendingInstances()
        {
            var statepath = System.IO.Path.Combine(Project.Path, "state");
            if(System.IO.Directory.Exists(statepath))
            {
                var ProjectFiles = System.IO.Directory.EnumerateFiles(statepath, "*.json", System.IO.SearchOption.AllDirectories).OrderBy((x) => x).ToArray();
                if (!global.isConnected)
                {
                    foreach (var f in ProjectFiles)
                    {
                        try
                        {
                            var i = JsonConvert.DeserializeObject<WorkflowInstance>(System.IO.File.ReadAllText(f));
                            i.Workflow = this;
                            i.Path = Project.Path;
                            //if (idleOrComplete != null) i.OnIdleOrComplete += idleOrComplete;
                            //if (VisualTracking != null) i.OnVisualTracking += VisualTracking;
                            var exists = WorkflowInstance.Instances.Where(x => x.InstanceId == i.InstanceId).FirstOrDefault();
                            if (exists != null) continue;
                            if (i.state != "failed" && i.state != "aborted" && i.state != "completed")
                            {
                                var _ref = (i as IWorkflowInstance);
                                foreach (var runner in Plugins.runPlugins)
                                {
                                    if (!runner.onWorkflowStarting(ref _ref, true)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
                                }
                                lock(WorkflowInstance.Instances) WorkflowInstance.Instances.Add(i);
                                i.createApp(Activity);
                                i.Run();
                            }
                        }
                        catch (System.Runtime.DurableInstancing.InstancePersistenceException ex)
                        {
                            Log.Error("RunPendingInstances: " + ex.ToString());
                            try
                            {
                                System.IO.File.Delete(f);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RunPendingInstances: " + ex.ToString());
                        }
                    }
                }
            }
        }
        public string UniqueFilename()
        {
            string Filename = ""; string FilePath = "";
            bool isUnique = false; int counter = 1;
            while (!isUnique)
            {
                if (counter == 1)
                {
                    Filename = System.Text.RegularExpressions.Regex.Replace(name, @"[^0-9a-zA-Z]+", "") + ".xaml";
                    FilePath = System.IO.Path.Combine(Project.Path, Filename);
                }
                else
                {
                    Filename = name.Replace(" ", "_").Replace(".", "") + counter.ToString() + ".xaml";
                    FilePath = System.IO.Path.Combine(Project.Path, Filename);
                }
                if (!System.IO.File.Exists(FilePath)) isUnique = true;
                counter++;
            }
            return Filename;
        }
        private System.Activities.Activity _activity = null;
        [Newtonsoft.Json.JsonIgnore]
        public System.Activities.Activity Activity
        {
            get
            {
                if (string.IsNullOrEmpty(Xaml)) return null;
                if (_activity != null) return _activity;
                var activitySettings = new System.Activities.XamlIntegration.ActivityXamlServicesSettings
                {
                    CompileExpressions = true
                };
                var xamlReaderSettings = new System.Xaml.XamlXmlReaderSettings { LocalAssembly = typeof(Workflow).Assembly };
                var xamlReader = new System.Xaml.XamlXmlReader(new System.IO.StringReader(Xaml), xamlReaderSettings);
                _activity = System.Activities.XamlIntegration.ActivityXamlServices.Load(xamlReader, activitySettings);
                return _activity;
            }
        }
        public IWorkflowInstance CreateInstance(Dictionary<string, object> Parameters, string queuename, string correlationId,
            OpenRPA.Interfaces.idleOrComplete idleOrComplete, OpenRPA.Interfaces.VisualTrackingHandler VisualTracking)
        {
            if (this.Parameters == null) this.Parameters = new List<workflowparameter>();
            if (this.Parameters.Count == 0)
            {
                ParseParameters();
            }
            var instance = WorkflowInstance.Create(this, Parameters);
            instance.queuename = queuename; instance.correlationId = correlationId;
            if (idleOrComplete != null) instance.OnIdleOrComplete += idleOrComplete;
            if (VisualTracking != null) instance.OnVisualTracking += VisualTracking;
            Instances.Add(instance);
            //instance.Run();
            return instance;
        }
        public System.Collections.ObjectModel.KeyedCollection<string, System.Activities.DynamicActivityProperty> GetParameters()
        {
            System.Activities.ActivityBuilder ab2;
            using (var stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(Xaml)))
            {
                ab2 = System.Xaml.XamlServices.Load(
                    System.Activities.XamlIntegration.ActivityXamlServices.CreateBuilderReader(
                    new System.Xaml.XamlXmlReader(stream))) as System.Activities.ActivityBuilder;
            }
            return ab2.Properties;
        }
        public void ParseParameters()
        {
            Parameters.Clear();
            if (!string.IsNullOrEmpty(Xaml))
            {
                var parameters = GetParameters();
                foreach (var prop in parameters)
                {
                    var par = new workflowparameter() { name = prop.Name };
                    par.type = prop.Type.GenericTypeArguments[0].FullName;
                    string baseTypeName = prop.Type.BaseType.FullName;
                    if (baseTypeName == "System.Activities.InArgument")
                    {
                        par.direction = workflowparameterdirection.@in;
                    }
                    if (baseTypeName == "System.Activities.InOutArgument")
                    {
                        par.direction = workflowparameterdirection.inout;
                    }
                    if (baseTypeName == "System.Activities.OutArgument")
                    {
                        par.direction = workflowparameterdirection.@out;
                    }
                    Parameters.Add(par);
                }
            }
        }
    }

}
