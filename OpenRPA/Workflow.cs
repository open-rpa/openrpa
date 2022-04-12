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
                    var wf = RobotInstance.instance.dbWorkflows.FindById(_id);
                    if (wf == null)
                    {
                        RobotInstance.instance.Workflows.Remove(wf);
                        return;
                    }
                    if (wf._version == _version)
                    {
                        Log.Verbose("Saving " + this.name + " with version " + this._version);
                        RobotInstance.instance.dbWorkflows.Update(this);
                    }
                    else
                    {
                        Log.Verbose("Setting " + this.name + " with version " + this._version);
                        wf.IsExpanded = value;
                    }
                    RobotInstance.instance.dbWorkflows.Update(this);
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
                        var wf = RobotInstance.instance.dbWorkflows.FindById(_id);
                        if (wf == null)
                        {
                            RobotInstance.instance.Workflows.Remove(wf);
                            return;
                        }
                        if (wf._version == _version)
                        {
                            Log.Verbose("Saving " + this.name + " with version " + this._version);
                            RobotInstance.instance.dbWorkflows.Update(this);
                        }
                        else
                        {
                            Log.Verbose("Setting " + this.name + " with version " + this._version);
                            wf.IsSelected = value;
                        }
                        RobotInstance.instance.dbWorkflows.Update(this);
                    }
                }
            }
        }
        private string laststate = "unloaded";
        [JsonIgnore]
        public string State
        {
            set
            {
                laststate = value;
            }
            get
            {
                try
                {
                    string state = laststate;
                    var instace = LoadedInstances;
                    if (instace.Count() > 0)
                    {
                        var running = instace.Where(x => x.isCompleted == false).ToList();
                        if (running.Count() > 0)
                        {
                            state = "running";
                        }
                        else
                        {
                            state = instace.OrderByDescending(x => x._modified).First().state;
                        }

                        if (laststate != state)
                        {
                            //laststate = state;
                            //if (State != "idle" && State != "running")
                            //{
                            //    // _ = Save(true);
                            //}
                        }
                    }
                    else if (state == "running" || state == "idle")
                    {
                        if (laststate != state)
                        {
                            state = "unloaded";
                        }
                    }
                    return state;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    return "unloaded";
                }

            }
        }
        private DateTime lastSaveTime = DateTime.Now.AddDays(-1);
        public void SetLastState(string State)
        {
            if (State == "loaded") return;
            if (laststate != State)
            {
                //Task.Run(() =>
                //{
                //    if (State != "idle" && State != "running")
                //    {
                //        if (lastSaveTime.AddSeconds(5) < DateTime.Now || true)
                //        {
                //            Task.Run(async () =>
                //            {
                //                try
                //                {
                //                    // await Save(true);
                //                    await Save<Workflow>(true);
                //                }
                //                catch (Exception ex)
                //                {
                //                    Log.Error(ex.ToString());
                //                }
                //            });
                //            lastSaveTime = DateTime.Now;
                //        }
                //    }
                //    laststate = State;
                //    GenericTools.RunUI(() => NotifyUIState());
                //});
                laststate = State;
                if (State != "idle" && State != "running")
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Save<Workflow>(true);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    });

                }
                GenericTools.RunUI(() => NotifyUIState());
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
                    case "idle": return "/OpenRPA;component/Resources/state/Running_green.png";
                    case "aborted": return "/OpenRPA;component/Resources/state/Abort.png";
                    case "failed": return "/OpenRPA;component/Resources/state/failed.png";
                    case "completed": return "/OpenRPA;component/Resources/state/Completed.png";
                    case "warning": return "/OpenRPA;component/Resources/state/Risk.png";
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
        public List<WorkflowInstance> LoadedInstances
        {
            get
            {
                List<WorkflowInstance> result = null;
                var instances = WorkflowInstance.Instances.ToList();
                result = instances.Where(x => (x.WorkflowId == _id && !string.IsNullOrEmpty(_id)) || (x.RelativeFilename.ToLower() == RelativeFilename.ToLower() && string.IsNullOrEmpty(_id))).ToList();
                return result;
            }
        }
        private WorkflowInstance[] _Instances = new WorkflowInstance[] { };
        [JsonIgnore, BsonIgnore]
        public WorkflowInstance[] Instances
        {
            get
            {
                return RobotInstance.instance.dbWorkflowInstances.Find(x => x.WorkflowId == _id).OrderByDescending(x => x._modified).Take(10).ToArray();
                // return RobotInstance.instance.dbWorkflowInstances.Find(x => x.WorkflowId == _id, 0, 10).ToArray();
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
            result.name = System.IO.Path.GetFileNameWithoutExtension(Filename).Replace("_", " ");
            result.Xaml = System.IO.File.ReadAllText(Filename);
            result.Parameters = new List<workflowparameter>();
            //sresult.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
            result.isDirty = true;
            return result;
        }
        public async static Task<Workflow> Create(IProject Project, string Name)
        {
            Workflow workflow = new Workflow { projectid = Project._id, name = Name, _acl = Project._acl };
            workflow.name = workflow.UniqueName();
            workflow.Filename = workflow.UniqueFilename();
            _ = workflow.RelativeFilename;
            workflow._type = "workflow";
            workflow.Parameters = new List<workflowparameter>();
            //workflow.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
            workflow.projectid = Project._id;
            workflow._id = Guid.NewGuid().ToString();
            workflow.isDirty = true;
            workflow.isLocalOnly = true;
            RobotInstance.instance.dbWorkflows.Insert(workflow);
            // await workflow.Save();
            await workflow.Save();
            return workflow;
        }
        public async Task ExportFile(string filepath)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            var exportedwf = JsonConvert.DeserializeObject<Workflow>(json);
            exportedwf.Xaml = await Views.WFDesigner.LoadImages(Xaml);
            json = Newtonsoft.Json.JsonConvert.SerializeObject(exportedwf);
            System.IO.File.WriteAllText(filepath, json);
        }
        public async Task Save(bool skipOnline = false)
        {
            if (string.IsNullOrEmpty(projectid)) throw new Exception("Cannot save workflow " + name + " with out a project/projectid");
            if (string.IsNullOrEmpty(Filename)) Filename = UniqueFilename();
            if (Project() == null)
            {
                Log.Information("Missing project " + projectid + " while saving workflow " + name);
                var q = "{\"_type\": 'project', '_id': '" + projectid + "'}";
                var server_projects = await global.webSocketClient.Query<Project>("openrpa", q);
                if (server_projects.Length > 0)
                {
                    Log.Information("Adding project " + server_projects[0].name);
                    await server_projects[0].Save();
                }
            }
            await Save<Workflow>(skipOnline);
            if (System.Threading.Monitor.TryEnter(RobotInstance.instance.Workflows, 1000))
            {
                try
                {
                    var exists = RobotInstance.instance.Workflows.FindById(_id);
                    if(exists == null) RobotInstance.instance.Workflows.Add(this);
                    if (exists != null) RobotInstance.instance.Workflows.UpdateItem(exists, this);
                }
                finally
                {
                    System.Threading.Monitor.Exit(RobotInstance.instance.Workflows);
                }
            }
            else { throw new LockNotReceivedException("Failed saving workflow"); }
        }
        public async Task Update(IWorkflow item, bool skipOnline = false)
        {
            RobotInstance.instance.Workflows.UpdateItem(this, item);
            await Save<Workflow>(skipOnline);
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
        public async Task Delete(bool skipOnline = false)
        {
            try
            {
                if(!skipOnline) await Delete<Workflow>();
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
                if (System.Threading.Monitor.TryEnter(RobotInstance.instance.Workflows, 1000))
                {
                    try
                    {
                        RobotInstance.instance.Workflows.Remove(this);
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(RobotInstance.instance.Workflows);
                    }
                }
                else { throw new LockNotReceivedException("Failed deleting workflow"); }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        public string UniqueName()
        {
            string Name = name;
            IWorkflow exists = null;
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
                exists = RobotInstance.instance.Workflows.Where(x => x.name == Name && x.projectid == projectid && x._id != _id).FirstOrDefault();
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
                var exists = RobotInstance.instance.Workflows.Where(x => x.Filename == Filename && x.projectid == projectid && x._id != _id).FirstOrDefault();
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
            LoadedInstances.Add(instance);
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
        public override string ToString()
        {
            return RelativeFilename;
        }
    }

}
