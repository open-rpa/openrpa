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
        }
        public static List<WorkflowInstance> Instances = new List<WorkflowInstance>();
        [JsonIgnore]
        public Action<WorkflowInstance> idleOrComplete { get; set; }
        public Dictionary<string, object> Parameters { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public Dictionary<string, object> Bookmarks { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public string InstanceId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string WorkflowId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string xml { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string owner { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string ownerid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string host { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string fqdn { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errormessage { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool isCompleted { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool hasError { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string state { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public Workflow Workflow { get { return GetProperty<Workflow>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public System.Activities.WorkflowApplication wfApp { get; set; }
        public static async Task<WorkflowInstance> Create(Workflow Workflow, Dictionary<string, object> Parameters)
        {
            var result = new WorkflowInstance() { Workflow = Workflow, WorkflowId = Workflow._id, Parameters = Parameters, name = Workflow.name };
            Instances.Add(result);
            if (global.isConnected) { 
                result.owner = global.webSocketClient.user.name;
                result.ownerid = global.webSocketClient.user._id;
            }
            result.host = Environment.MachineName.ToLower();
            result.fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            result.createApp();
            await result.Save();
            return result;
        }
        public void createApp()
        {
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
                if (Workflow.Serializable)
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
                addwfApphandlers(wfApp);
                if (Workflow.Serializable)
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
            errormessage = Reason;
            _ = Save();
            if (runWatch != null) runWatch.Stop();
            idleOrComplete?.Invoke(this);
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
                errormessage = ex.Message;
                await Save();
                if (runWatch != null) runWatch.Stop();
                idleOrComplete?.Invoke(this);
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
                    foreach (var prop in Parameters.ToList())
                    {
                        if (e.Outputs.ContainsKey(prop.Key))
                        {
                            Parameters[prop.Key] = prop.Value;
                        }
                    }
                    foreach (var o in e.Outputs) e.Outputs.Add(o);
                    if (runWatch != null) runWatch.Stop();
                    idleOrComplete?.Invoke(this);
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
                errormessage = e.Reason.Message;
                _ = Save();
                if(runWatch!=null) runWatch.Stop();
                idleOrComplete?.Invoke(this);
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
                    idleOrComplete?.Invoke(this);
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

                }
                //isUnloaded = true;
                _ = Save();
            };

            wfApp.OnUnhandledException = delegate (System.Activities.WorkflowApplicationUnhandledExceptionEventArgs e)
            {
                hasError = true;
                isCompleted = true;
                state = "failed";
                errormessage = e.UnhandledException.ToString();
                //exceptionsource = e.ExceptionSource.Id;
                if (runWatch != null) runWatch.Stop();
                idleOrComplete?.Invoke(this);
                return System.Activities.UnhandledExceptionAction.Terminate;
            };

        }
        public async Task Save()
        {
            try
            {
                if (!global.isConnected) return;
                Log.Debug("Saving workflow instance");
                if (string.IsNullOrEmpty(_id))
                {
                    var result = await global.webSocketClient.InsertOne("openrpa_instances", this);
                    _id = result._id;
                }
                else
                {
                    await global.webSocketClient.UpdateOne("openrpa_instances", this);
                }
                // Catch up if others havent been saved
                foreach(var i in Instances.ToList())
                {
                    if (string.IsNullOrEmpty(_id)) await i.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
    }
}
