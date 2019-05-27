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
    public class Workflow : apibase
    {
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
        public System.Collections.ObjectModel.ObservableCollection<WorkflowInstance> Instances {
            get { if (_Instances == null) _Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>(); return _Instances; }
            set { _Instances = value; }
        }
        //[JsonIgnore]
        // public Action<Workflow, WorkflowInstance> idleOrComplete { get; set; }
        public event WorkflowInstance.idleOrComplete OnIdleOrComplete;
        [JsonIgnore]
        public Project Project { get; set; }
        private System.Collections.ObjectModel.ObservableCollection<WorkflowInstance> _Instances;
        public static Workflow FromFile(Project project, string Filename)
        {
            var result = new Workflow();
            result._type = "workflow";
            result.Project = project;
            result.Filename = System.IO.Path.GetFileName(Filename);
            result.name = System.IO.Path.GetFileNameWithoutExtension(Filename);
            result.Xaml = System.IO.File.ReadAllText(Filename);
            result.Parameters = new List<workflowparameter>();
            result.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
            return result;
        }
        public static Workflow Create(Project Project, string Name)
        {
            Workflow workflow = new Workflow { Project = Project, name = Name };
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
            workflow.Instances = new System.Collections.ObjectModel.ObservableCollection<WorkflowInstance>();
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
            //parseparameters();
            SaveFile();
            projectid = Project._id;
            if (!global.isConnected) return;
            if (string.IsNullOrEmpty(_id))
            {
                var result = await global.webSocketClient.InsertOne("openrpa", 0, false, this);
                _id = result._id;
                _acl = result._acl;
            } else
            {
                await global.webSocketClient.UpdateOne("openrpa", 0, false, this);
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
                            var exists = WorkflowInstance.Instances.Where(x => x.InstanceId == i.InstanceId).FirstOrDefault();
                            if (exists != null) continue;
                            WorkflowInstance.Instances.Add(i);
                            if (i.state != "failed" && i.state != "aborted" && i.state != "completed")
                            {
                                i.createApp();
                                await i.Run();
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
                        await i.Save();
                        continue;
                    }
                    WorkflowInstance.Instances.Add(i);
                    i.createApp();
                    await i.Run();
                }
                catch (Exception ex)
                {
                    i.state = "failed";
                    i.Exception = ex;
                    i.errormessage = ex.Message;
                    await i.Save();
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
                    Filename = name.Replace(" ", "_").Replace(".", "") + ".xaml";
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
        public WorkflowInstance CreateInstance() { return CreateInstance(new Dictionary<string, object>(), null, null, null); }
        public WorkflowInstance CreateInstance(Dictionary<string, object> Parameters, string queuename, string correlationId, WorkflowInstance.idleOrComplete idleOrComplete)
        {
            var instance = WorkflowInstance.Create(this, Parameters);
            instance.queuename = queuename; instance.correlationId = correlationId;
            if (idleOrComplete != null) instance.OnIdleOrComplete += idleOrComplete;
            Instances.Add(instance);
            //instance.Run();
            return instance;
        }
        //public WorkflowInstance Run() { return Run(new Dictionary<string, object>(), null, null, null); }
        //public WorkflowInstance Run(Dictionary<string, object> Parameters, string queuename, string correlationId, WorkflowInstance.idleOrComplete idleOrComplete)
        //{
        //    var instance = WorkflowInstance.Create(this, Parameters);
        //    instance.queuename = queuename; instance.correlationId = correlationId;
        //    if (idleOrComplete != null) instance.OnIdleOrComplete += idleOrComplete;
        //    Instances.Add(instance);
        //    instance.Run();
        //    return instance;
        //}
        //public void onIdleOrComplete(WorkflowInstance instance)
        //{
        //    Log.Debug("onIdleOrComplete state: " + instance.state);
        //    if (!string.IsNullOrEmpty(instance.errormessage)) Log.Error(instance.errormessage);
        //    if(instance.state != "idle")
        //    {
        //        Log.Output("Workflow " + instance.state + " in " + string.Format("{0:mm\\:ss\\.fff}", instance.runWatch.Elapsed));
        //        GenericTools.restore(GenericTools.mainWindow);
        //    }
        //    OnIdleOrComplete?.Invoke(instance, EventArgs.Empty);
        //}
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
