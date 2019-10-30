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
        public DispatcherTimer _timer;
        public Workflow()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (sender, args) =>
            {
                NotifyPropertyChanged("State");
                NotifyPropertyChanged("StateImage");
            };
            _timer.Start();
        }
        public string queue { get { return GetProperty<string>(); } set { SetProperty(value); } }        
        public string Xaml { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public List<workflowparameter> Parameters { get { return GetProperty<List<workflowparameter>>(); } set { SetProperty(value); } }
        public bool Serializable { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string Filename { get { return GetProperty<string>(); } set { SetProperty(value); } }
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
        [JsonIgnore]
        public string State
        {
            get
            {
                string state = "unloaded";
                var instace = Instances;
                if (instace.Count() > 0)
                {
                    var running = instace.Where(x => x.isCompleted == false);
                    if (running.Count() > 0)
                    {
                        state = "running";
                    }
                    else
                    {
                        state = instace.First().state;
                    }
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
        [JsonIgnore]
        public List<WorkflowInstance> Instances
        {
            get
            {
                return WorkflowInstance.Instances.Where(x => x.WorkflowId == _id).ToList();
            }
        }
        [JsonIgnore]
        public bool isRunnning
        {
            get
            {
                foreach (var i in WorkflowInstance.Instances)
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
        public void SaveFile()
        {
            if (string.IsNullOrEmpty(name)) return;
            if (string.IsNullOrEmpty(Xaml)) return;
            if (!Project.Workflows.Contains(this)) Project.Workflows.Add(this);

            if (string.IsNullOrEmpty(FilePath))
            {
                Filename = UniqueFilename();
            }
            else
            {
                var guess = name.Replace(" ", "_").Replace(".", "") + ".xaml";
                var newName = UniqueFilename();
                if (guess == newName && Filename != guess)
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(Project.Path, guess), Xaml);
                    System.IO.File.Delete(FilePath);
                    Filename = guess;
                }
            }
            System.IO.File.WriteAllText(FilePath, Xaml);
        }
        public async Task Save()
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
            } else
            {
                var result = await global.webSocketClient.UpdateOne("openrpa", 0, false, this);
                _acl = result._acl;
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
        public async Task Delete()
        {
            if (Project.Workflows.Contains(this)) Project.Workflows.Remove(this);
            if (string.IsNullOrEmpty(FilePath)) return;
            System.IO.File.Delete(FilePath);
            if (!global.isConnected) return;
            if (!string.IsNullOrEmpty(_id))
            {
                var basepath = System.IO.Directory.GetCurrentDirectory();
                var imagepath = System.IO.Path.Combine(basepath, "images");
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
        public async Task RunPendingInstances()
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
                                WorkflowInstance.Instances.Add(i);
                                i.createApp();
                                i.Run();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RunPendingInstances: " + ex.ToString());
                        }
                    }
                }
            }
            var host = Environment.MachineName.ToLower();
            var fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            var results = await global.webSocketClient.Query<WorkflowInstance>("openrpa_instances", "{WorkflowId: '" + _id + "', state: 'idle', fqdn: '" + fqdn + "'}");
            foreach(var i in results)
            {
                try
                {
                    i.Workflow = this;
                    if(!string.IsNullOrEmpty(i.InstanceId) && string.IsNullOrEmpty(i.xml))
                    {
                        Log.Error("Refuse to load instance " + i.InstanceId + " it contains no state!");
                        i.state = "aborted";
                        i.errormessage = "Refuse to load instance " + i.InstanceId + " it contains no state!";
                        i.Save();
                        continue;
                    }
                    //if (idleOrComplete != null) i.OnIdleOrComplete += idleOrComplete;
                    //if (VisualTracking != null) i.OnVisualTracking += VisualTracking;
                    WorkflowInstance.Instances.Add(i);
                    var _ref = (i as IWorkflowInstance);
                    foreach (var runner in Plugins.runPlugins)
                    {
                        if (!runner.onWorkflowStarting(ref _ref, true)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
                    }
                    i.createApp();
                    i.Run();
                }
                catch (Exception ex)
                {
                    i.state = "failed";
                    i.Exception = ex;
                    i.errormessage = ex.Message;
                    i.Save();
                    Log.Error("RunPendingInstances: " + ex.ToString());
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
        [Newtonsoft.Json.JsonIgnore]
        public System.Activities.Activity Activity
        {
            get
            {
                if (string.IsNullOrEmpty(Xaml)) return null;
                var activitySettings = new System.Activities.XamlIntegration.ActivityXamlServicesSettings
                {
                    CompileExpressions = true
                };
                var xamlReaderSettings = new System.Xaml.XamlXmlReaderSettings { LocalAssembly = typeof(Workflow).Assembly };
                var xamlReader = new System.Xaml.XamlXmlReader(new System.IO.StringReader(Xaml), xamlReaderSettings);
                var wf = System.Activities.XamlIntegration.ActivityXamlServices.Load(xamlReader, activitySettings);
                return wf;
            }
        }
        public WorkflowInstance CreateInstance(Dictionary<string, object> Parameters, string queuename, string correlationId, 
            WorkflowInstance.idleOrComplete idleOrComplete, WorkflowInstance.VisualTrackingHandler VisualTracking)
        {
            var instance = WorkflowInstance.Create(this, Parameters);
            instance.queuename = queuename; instance.correlationId = correlationId;
            if (idleOrComplete != null) instance.OnIdleOrComplete += idleOrComplete;
            if (VisualTracking != null) instance.OnVisualTracking += VisualTracking;
            Instances.Add(instance);
            //instance.Run();
            return instance;
        }
    }
    public enum workflowparameterdirection
    {
        @in = 0,
        @out = 1,
        inout = 2,
    }
    public class workflowparameter
    {
        public string name { get; set; }
        public string type { get; set; }
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public workflowparameterdirection direction { get; set; }
    }

}
