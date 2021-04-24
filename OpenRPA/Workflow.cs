using LiteDB;
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
    public class Workflow : LocallyCached, IWorkflow
    {
        [JsonIgnore, BsonIgnore]
        private long _current_version = 0;
        public long current_version
        {
            get
            {
                if (_version > _current_version) return _version;
                return _current_version;
            }
            set
            {
                _current_version = value;
            }
        }
        public Workflow()
        {
            Serializable = true;
            IsVisible = true;
        }
        public string queue { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string Xaml { get { return GetProperty<string>(); } set { _activity = null; SetProperty(value); } }
        public List<workflowparameter> Parameters { get { return GetProperty<List<workflowparameter>>(); } set { SetProperty(value); } }
        public bool Serializable { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string Filename
        {
            get
            {
                var value = GetProperty<string>();
                if (string.IsNullOrEmpty(value)) value = "";
                //if(string.IsNullOrEmpty(value))
                //{
                //    _backingFieldValues["Filename"] = UniqueFilename();
                //    _backingFieldValues["isDirty"] = true;
                //    return _backingFieldValues["Filename"] as string;
                //}
                return value;
            }
            set { SetProperty(value); }
        }
        private string _RelativeFilename;
        [JsonProperty("projectandfilename"), BsonField("projectandfilename")]
        public string RelativeFilename
        {
            get
            {
                if (string.IsNullOrEmpty(Filename)) return "";
                if (!string.IsNullOrEmpty(_RelativeFilename)) { return _RelativeFilename; }
                if (Project() == null) { return Filename; }
                if (string.IsNullOrEmpty(Project().Path)) { return Filename; }
                string lastFolderName = System.IO.Path.GetFileName(Project().Path);
                _RelativeFilename = System.IO.Path.Combine(lastFolderName, Filename).Replace("\\", "/");
                return _RelativeFilename;
            }
            set
            {
                _RelativeFilename = value;
            }
        }
        [JsonIgnore, BsonIgnore]
        public string IDOrRelativeFilename
        {
            get
            {
                if (string.IsNullOrEmpty(RelativeFilename)) return name;
                if (RelativeFilename.Contains("\\")) return RelativeFilename;
                if (Project() != null) return Project().name + "\\" + Filename;
                if (!string.IsNullOrEmpty(_ProjectAndName) && _ProjectAndName.Contains("/"))
                {
                    return _ProjectAndName.Substring(0, _ProjectAndName.IndexOf("/") + 1) + RelativeFilename;
                }
                return _id;
                //if (string.IsNullOrEmpty(_id)) return RelativeFilename;
                //return _id;
            }
        }
        private string _ProjectAndName;
        [JsonProperty("projectandname"), BsonField("projectandname")]
        public string ProjectAndName
        {
            get
            {
                if (Project() == null)
                {
                    if (!string.IsNullOrEmpty(_ProjectAndName)) return _ProjectAndName;
                    return name;
                }
                return Project().name + "/" + name;
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
                if (Project() == null) return Filename;
                return System.IO.Path.Combine(Project().Path, Filename);
            }
        }
        public string projectid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public bool IsExpanded
        {
            get { return GetProperty<bool>(); }
            set
            {
                if (Views.OpenProject.isUpdating) return;
                if (value == GetProperty<bool>()) return;
                SetProperty(value);
                if (!_backingFieldValues.ContainsKey("IsExpanded")) return;
                if (!string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(name))
                {
                    var wf = RobotInstance.instance.Workflows.FindById(_id);
                    if (wf._version == _version)
                    {
                        Log.Verbose("Saving " + this.name + " with version " + this._version);
                        RobotInstance.instance.Workflows.Update(this);
                    }
                    else
                    {
                        Log.Verbose("Setting " + this.name + " with version " + this._version);
                        wf.IsExpanded = value;
                    }
                    RobotInstance.instance.Workflows.Update(this);
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
                        var wf = RobotInstance.instance.Workflows.FindById(_id);
                        if (wf._version == _version)
                        {
                            Log.Verbose("Saving " + this.name + " with version " + this._version);
                            RobotInstance.instance.Workflows.Update(this);
                        }
                        else
                        {
                            Log.Verbose("Setting " + this.name + " with version " + this._version);
                            wf.IsSelected = value;
                        }
                        RobotInstance.instance.Workflows.Update(this);
                    }
                }
            }
        }
        private string laststate = "unloaded";
        [JsonIgnore, BsonIgnore]
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
                        state = instace.OrderByDescending(x => x._modified).First().state;
                    }
                    laststate = state;
                }
                return state;
            }
        }
        [JsonIgnore, BsonIgnore]
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
        [JsonIgnore, BsonIgnore]
        public List<WorkflowInstance> Instances
        {
            get
            {
                return WorkflowInstance.Instances.Where(x => (x.WorkflowId == _id && !string.IsNullOrEmpty(_id)) || (x.RelativeFilename.ToLower() == RelativeFilename.ToLower() && string.IsNullOrEmpty(_id))).ToList();
            }
        }
        [JsonIgnore, BsonIgnore]
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
        //[JsonIgnore, BsonIgnore]
        //public IProject Project { 
        //    get
        //    {
        //        return RobotInstance.instance.Projects.FindById(projectid);
        //    }
        //}
        public IProject Project()
        {
            return RobotInstance.instance.Projects.FindById(projectid);
        }
        public static Workflow FromFile(IProject project, string Filename)
        {
            var result = new Workflow();
            result._type = "workflow";
            result.projectid = project._id;
            result.Filename = System.IO.Path.GetFileName(Filename);
            result.name = System.IO.Path.GetFileNameWithoutExtension(Filename);
            result.Xaml = System.IO.File.ReadAllText(Filename);
            result.Parameters = new List<workflowparameter>();
            //sresult.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
            return result;
        }
        public async static Task<Workflow> Create(IProject Project, string Name)
        {
            Workflow workflow = new Workflow { projectid = Project._id, name = Name, _acl = Project._acl };
            var exists = RobotInstance.instance.Workflows.Find(x => x.name == workflow.name && x.projectid == workflow.projectid).FirstOrDefault();
            workflow.name = workflow.UniqueName();
            workflow._type = "workflow";
            workflow.Parameters = new List<workflowparameter>();
            //workflow.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
            workflow.projectid = Project._id;
            workflow._id = Guid.NewGuid().ToString();
            workflow.isDirty = true;
            workflow.isLocalOnly = true;
            RobotInstance.instance.Workflows.Insert(workflow);
            // await workflow.Save();
            await workflow.Save<Workflow>();
            return workflow;
        }
        public async Task ExportFile(string filepath)
        {
            string xaml = await Views.WFDesigner.LoadImages(Xaml);
            System.IO.File.WriteAllText(filepath, xaml);
        }
        public async Task Save()
        {
            if (projectid == null && string.IsNullOrEmpty(projectid)) throw new Exception("Cannot save workflow with out a project/projectid");
            if (string.IsNullOrEmpty(Filename)) Filename = UniqueFilename();
            if (string.IsNullOrEmpty(projectid)) projectid = Project()._id;
            await Save<Workflow>();
            RobotInstance.instance.UpdateWorkflow(this, false);
        }
        public async Task UpdateImagePermissions()
        {
            if (!global.isConnected) return;
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
        public async Task Delete()
        {
            try
            {
                await Delete<Workflow>();
                if (!string.IsNullOrEmpty(_id) && global.isConnected)
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        public void RunPendingInstances()
        {
            var statepath = System.IO.Path.Combine(Project().Path, "state");
            if (System.IO.Directory.Exists(statepath))
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
                            i.Path = Project().Path;
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
                                lock (WorkflowInstance.Instances) WorkflowInstance.Instances.Add(i);
                                i.createApp(Activity());
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
        public string UniqueName()
        {
            string Name = name;
            Workflow exists = null;
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
                exists = RobotInstance.instance.Workflows.Find(x => x.name == Name && x.projectid == projectid && x._id != _id).FirstOrDefault();
                isUnique = (exists == null);
                counter++;
            }
            return Name;
        }
        public string UniqueFilename()
        {
            string Filename = "";
            var isUnique = false;
            int counter = 1;
            while (!isUnique)
            {
                if (counter == 1)
                {
                    Filename = System.Text.RegularExpressions.Regex.Replace(name, @"[^0-9a-zA-Z]+", "") + ".xaml";
                }
                else
                {
                    Filename = name.Replace(" ", "_").Replace(".", "") + counter.ToString() + ".xaml";
                }
                var exists = RobotInstance.instance.Workflows.Find(x => x.Filename == Filename && x.projectid == projectid && x._id != _id).FirstOrDefault();
                isUnique = (exists == null);
                counter++;
            }
            return Filename;
        }
        private System.Activities.Activity _activity = null;
        public System.Activities.Activity Activity()
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
        public IWorkflowInstance CreateInstance(Dictionary<string, object> Parameters, string queuename, string correlationId,
            OpenRPA.Interfaces.idleOrComplete idleOrComplete, OpenRPA.Interfaces.VisualTrackingHandler VisualTracking, string SpanId, string ParentSpanId)
        {
            if (this.Parameters == null) this.Parameters = new List<workflowparameter>();
            if (this.Parameters.Count == 0)
            {
                ParseParameters();
            }
            var instance = WorkflowInstance.Create(this, Parameters);
            instance.SpanId = SpanId;
            instance.ParentSpanId = ParentSpanId;
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
        [JsonIgnore, BsonIgnore]
        public bool IsVisible { get { return GetProperty<bool>(); } set { SetProperty(value); } }
    }

}
