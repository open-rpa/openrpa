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
    public class WorkflowInstance : apibase
    {
        public WorkflowInstance()
        {
            _type = "workflowinstance";
            _id = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
        }
        public static List<WorkflowInstance> Instances = new List<WorkflowInstance>();

        public delegate void VisualTrackingHandler(WorkflowInstance Instance, string ActivityId, string ChildActivityId, string State);
        public event VisualTrackingHandler OnVisualTracking;

        public delegate void idleOrComplete(WorkflowInstance sender, EventArgs e);

        public event idleOrComplete OnIdleOrComplete;
        public Dictionary<string, object> Parameters { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public Dictionary<string, object> Bookmarks { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public string Path { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string correlationId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string queuename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public Dictionary<string, ValueType> Variables { get { return GetProperty<Dictionary<string, ValueType>>(); } set { SetProperty(value); } }
        public string InstanceId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string WorkflowId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string xml { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string owner { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string ownerid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string host { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string fqdn { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errormessage { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public Exception Exception { get { return GetProperty<Exception>(); } set { SetProperty(value); } }
        public bool isCompleted { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool hasError { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string state { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public Workflow Workflow { get { return GetProperty<Workflow>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public System.Activities.WorkflowApplication wfApp { get; set; }
        public static WorkflowInstance Create(Workflow Workflow, Dictionary<string, object> Parameters)
        {
            var result = new WorkflowInstance() { Workflow = Workflow, WorkflowId = Workflow._id, Parameters = Parameters, name = Workflow.name, Path = Workflow.Project.Path };
            Instances.Add(result);
            if (global.isConnected)
            {
                result.owner = global.webSocketClient.user.name;
                result.ownerid = global.webSocketClient.user._id;
            }
            result.host = Environment.MachineName.ToLower();
            result.fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            result.createApp();
            foreach(var i in Instances.ToList())
            {
                if (i.isCompleted) Instances.Remove(i);
            }
            return result;
        }
        public void createApp()
        {
            //var xh = new XamlHelper(workflow.xaml);
            //extraextension.updateProfile(xh.Variables.ToArray(), xh.ArgumentNames.ToArray());
            var CustomTrackingParticipant = new WorkflowTrackingParticipant();
            CustomTrackingParticipant.OnVisualTracking += Participant_OnVisualTracking;

            if (string.IsNullOrEmpty(InstanceId))
            {
                // Remove unknown Parameters, if we don't the workflow will fail
                foreach (var param in Parameters.ToList())
                {
                    var allowed = Workflow.Parameters.Where(x => x.name == param.Key).FirstOrDefault();
                    if (allowed == null || allowed.direction == workflowparameterdirection.@out)
                    {
                        Parameters.Remove(param.Key);
                    }
                }
                // Ensure Type
                foreach (var wfparam in Workflow.Parameters)
                {
                    if (Parameters.ContainsKey(wfparam.name) && wfparam.type == "System.Int32")
                    {
                        if (Parameters[wfparam.name] != null)
                        {
                            Parameters[wfparam.name] = int.Parse(Parameters[wfparam.name].ToString());
                        }
                    }
                    else if (Parameters.ContainsKey(wfparam.name) && wfparam.type == "System.Boolean")
                    {
                        if (Parameters[wfparam.name] != null)
                        {
                            Parameters[wfparam.name] = bool.Parse(Parameters[wfparam.name].ToString());
                        }
                    }
                }
                wfApp = new System.Activities.WorkflowApplication(Workflow.Activity, Parameters);
                wfApp.Extensions.Add(CustomTrackingParticipant);
                if (Workflow.Serializable )
                {
                    //if (Config.local.localstate)
                    //{
                    //    if (!System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "\\state")) System.IO.Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory() + "\\state");
                    //    wfApp.InstanceStore = new Store.XMLFileInstanceStore(System.IO.Directory.GetCurrentDirectory() + "\\state");
                    //}
                    //else
                    //{
                    //    wfApp.InstanceStore = new Store.OpenFlowInstanceStore();
                    //}
                    wfApp.InstanceStore = new Store.OpenFlowInstanceStore();
                }
                addwfApphandlers(wfApp);
            }
            else
            {
                wfApp = new System.Activities.WorkflowApplication(Workflow.Activity);
                wfApp.Extensions.Add(CustomTrackingParticipant);
                addwfApphandlers(wfApp);
                if (Workflow.Serializable || !Workflow.Serializable)
                {
                    //if (Config.local.localstate)
                    //{
                    //    if (!System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + "\\state")) System.IO.Directory.CreateDirectory(System.IO.Directory.GetCurrentDirectory() + "\\state");
                    //    wfApp.InstanceStore = new Store.XMLFileInstanceStore(System.IO.Directory.GetCurrentDirectory() + "\\state");
                    //}
                    //else
                    //{
                    //    wfApp.InstanceStore = new Store.OpenFlowInstanceStore();
                    //}
                    wfApp.InstanceStore = new Store.OpenFlowInstanceStore();
                }
                wfApp.Load(new Guid(InstanceId));
            }
            state = "loaded";
        }

        private void Participant_OnVisualTracking(WorkflowInstance Instance, string ActivityId, string ChildActivityId, string State)
        {
            OnVisualTracking?.Invoke(Instance, ActivityId, ChildActivityId, State);
        }

        public void Abort(string Reason)
        {
            if (wfApp == null) return;
            var _state = typeof(System.Activities.WorkflowApplication).GetField("state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(wfApp);
            if (_state.ToString() != "Aborted")
            {
                wfApp.Abort(Reason);
                return;
            }
            hasError = true;
            isCompleted = true;
            state = "aborted";
            Exception = new Exception(Reason);
            errormessage = Reason;
            _ = Save();
            if (runWatch != null) runWatch.Stop();
            OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
        }
        public void ResumeBookmark(string bookmarkName, object value)
        {
            try
            {
                Log.Verbose("[workflow] Resume workflow at bookmark '" + bookmarkName + "'");
                if (isCompleted)
                {
                    throw new ArgumentException("cannot resume bookmark on completed workflow!");
                }
                // Log.Debug(String.Format("Workflow {0} resuming at bookmark '{1}' value '{2}'", wfApp.Id.ToString(), bookmarkName, value));
                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(50);
                    try
                    {
                        wfApp.ResumeBookmark(bookmarkName, value);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
                state = "running";
                // Log.Debug(String.Format("Workflow {0} resumed bookmark '{1}' value '{2}'", wfApp.Id.ToString(), bookmarkName, value));
                _ = Save();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public System.Diagnostics.Stopwatch runWatch { get; private set; }
        public async Task Run()
        {
            try
            {
                runWatch = new System.Diagnostics.Stopwatch();
                runWatch.Start();
                if (string.IsNullOrEmpty(InstanceId))
                {
                    wfApp.Run();
                    InstanceId = wfApp.Id.ToString();
                    state = "running";
                    await Save();
                }
                else
                {
                    foreach (var b in Bookmarks)
                    {
                        if (b.Value != null && !string.IsNullOrEmpty(b.Value.ToString())) wfApp.ResumeBookmark(b.Key, b.Value);
                    }
                    if(Bookmarks.Count() == 0)
                    {
                        wfApp.Run();
                    }
                    state = "running";
                    await Save();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
                hasError = true;
                isCompleted = true;
                //isUnloaded = true;
                state = "failed";
                Exception = ex;
                errormessage = ex.Message;
                await Save();
                if (runWatch != null) runWatch.Stop();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            }
        }
        private void addwfApphandlers(System.Activities.WorkflowApplication wfApp)
        {
            wfApp.Completed = delegate (System.Activities.WorkflowApplicationCompletedEventArgs e)
            {
                isCompleted = true;
                if (e.CompletionState == System.Activities.ActivityInstanceState.Faulted)
                {
                }
                else if (e.CompletionState == System.Activities.ActivityInstanceState.Canceled)
                {
                }
                else if (e.CompletionState == System.Activities.ActivityInstanceState.Closed)
                {
                    state = "completed";
                    foreach (var o in e.Outputs) Parameters[o.Key] = o.Value;
                    if (runWatch != null) runWatch.Stop();
                    OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
                }
                else if (e.CompletionState == System.Activities.ActivityInstanceState.Executing)
                {
                }
                else
                {
                    throw new Exception("Unknown completetion state!!!" + e.CompletionState);
                }
            };

            wfApp.Aborted = delegate (System.Activities.WorkflowApplicationAbortedEventArgs e)
            {
                hasError = true;
                isCompleted = true;
                state = "aborted";
                Exception =  e.Reason;
                errormessage = e.Reason.Message;
                _ = Save();
                if(runWatch!=null) runWatch.Stop();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            };

            wfApp.Idle = delegate (System.Activities.WorkflowApplicationIdleEventArgs e)
            {
                var bookmarks = new Dictionary<string, object>();
                foreach (var b in e.Bookmarks)
                {
                    bookmarks.Add(b.BookmarkName, null);
                }
                Bookmarks = bookmarks;
                state = "idle";
                _ = Save();
                if (state != "completed")
                {
                    OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
                }
            };

            wfApp.PersistableIdle = delegate (System.Activities.WorkflowApplicationIdleEventArgs e)
            {
                //return PersistableIdleAction.Unload;
                _ = Save();
                return System.Activities.PersistableIdleAction.Persist;
            };

            wfApp.Unloaded = delegate (System.Activities.WorkflowApplicationEventArgs e)
            {
                if (!isCompleted && !hasError)
                {
                    state = "unloaded";

                } else
                {
                    DeleteFile();
                }
                //isUnloaded = true;
                if(global.isConnected)
                {
                    _ = Save();
                }
            };

            wfApp.OnUnhandledException = delegate (System.Activities.WorkflowApplicationUnhandledExceptionEventArgs e)
            {
                hasError = true;
                isCompleted = true;
                state = "failed";
                Exception = e.UnhandledException;
                errormessage = e.UnhandledException.ToString();
                //exceptionsource = e.ExceptionSource.Id;
                if (runWatch != null) runWatch.Stop();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
                return System.Activities.UnhandledExceptionAction.Terminate;
            };

        }
        private object filelock = new object();
        public void SaveFile()
        {
            if (string.IsNullOrEmpty(InstanceId)) return;
            if (string.IsNullOrEmpty(Path)) return;
            if (isCompleted || hasError) return;
            if (!System.IO.Directory.Exists(System.IO.Path.Combine(Path, "state"))) System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, "state"));
            var Filepath = System.IO.Path.Combine(Path, "state", InstanceId + ".json");
            lock(filelock)
            {
                System.IO.File.WriteAllText(Filepath, JsonConvert.SerializeObject(this));
            }
        }
        public void DeleteFile()
        {
            if (string.IsNullOrEmpty(InstanceId)) return;
            if (string.IsNullOrEmpty(Path)) return;
            var Filepath = System.IO.Path.Combine(Path, "state", InstanceId + ".json");
            try
            {
                if (System.IO.File.Exists(Filepath)) System.IO.File.Delete(Filepath);
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
        }
        public async Task Save()
        {
            SaveFile();
            try
            {
                if (!global.isConnected) return;
                var result = await global.webSocketClient.InsertOrUpdateOne("openrpa_instances", 1, false, this);
                _id = result._id;
                _acl = result._acl;
                Log.Debug("Saved with id: " + _id);
                    
                
                // Catch up if others havent been saved
                foreach (var i in Instances.ToList())
                {
                    //if (string.IsNullOrEmpty(_id)) await i.Save();
                    if (string.IsNullOrEmpty(_id)) await i.Save();
                }

                if (isCompleted || hasError)
                {
                    DeleteFile();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                // throw;
            }
        }
    }

    public class ValueType
    {
        public ValueType(Type type, object value) { this.type = type; this.value = value; }
        public Type type { get; set; }
        public object value { get; set; }
    }
}
