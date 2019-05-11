using Newtonsoft.Json;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class WorkflowInstance : ObservableObject
    {
        public Action<WorkflowInstance> idleOrComplete { get; set; }
        public IDictionary<string, object> Parameters { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public IDictionary<string, object> Bookmarks { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public string InstanceId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errormessage { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool isCompleted { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public bool hasError { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string state { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public Workflow Workflow { get { return GetProperty<Workflow>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public System.Activities.WorkflowApplication wfApp { get; set; }
        public static WorkflowInstance Create(Workflow Workflow, Dictionary<string, object> Parameters)
        {
            var result = new WorkflowInstance() { Workflow = Workflow, Parameters = Parameters };
            result.createApp();
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
                        Parameters.Remove(param);
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
                //if (workflow.serializable)
                //{
                //    if (extension.socket.settings.localState)
                //    {
                //        wfApp.InstanceStore = new store.XMLFileInstanceStore();
                //    }
                //    else
                //    {
                //        wfApp.InstanceStore = new store.OpenFlowInstanceStore(extension.socket);
                //    }
                //}
                wfApp = new System.Activities.WorkflowApplication(Workflow.Activity, Parameters);
                addwfApphandlers(wfApp);
            }
            else
            {
                wfApp = new System.Activities.WorkflowApplication(Workflow.Activity);
                addwfApphandlers(wfApp);
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
            //Task.Run(() =>
            //{
            //    Thread.Sleep(500);
            //    try
            //    {
            //        store.XMLFileInstanceStore.cleanup(Guid.Parse(instance.instanceid));
            //    }
            //    catch (Exception ex)
            //    {
            //        Log.Error(ex, "");
            //    }

            //});
            state = "aborted";
            errormessage = Reason;
            Save();
            idleOrComplete?.Invoke(this);
        }
        public void Run()
        {
            try
            {
                if (string.IsNullOrEmpty(InstanceId))
                {
                    wfApp.Run();
                    state = "running";
                    Save();
                }
                else
                {
                    foreach (var b in Bookmarks)
                    {
                        if (b.Value != null && !string.IsNullOrEmpty(b.Value.ToString())) wfApp.ResumeBookmark(b.Key, b.Value);
                    }
                    state = "running";
                    Save();
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
                Save();
                idleOrComplete?.Invoke(this);
            }
        }
        private void addwfApphandlers(System.Activities.WorkflowApplication wfApp)
        {
            wfApp.Completed = delegate (System.Activities.WorkflowApplicationCompletedEventArgs e)
            {
                isCompleted = true;
                //Task.Run(() =>
                //{
                //    try
                //    {
                //        Thread.Sleep(500);
                //        store.XMLFileInstanceStore.cleanup(e.InstanceId);
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Error(ex, "");
                //    }
                //});
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

                    Parameters = e.Outputs;
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
                //Task.Run(() =>
                //{
                //    Thread.Sleep(500);
                //    try
                //    {
                //        store.XMLFileInstanceStore.cleanup(e.InstanceId);
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Error(ex, "");
                //    }

                //});

                state = "aborted";
                errormessage = e.Reason.Message;
                Save();
                idleOrComplete?.Invoke(this);
            };

            wfApp.Idle = delegate (System.Activities.WorkflowApplicationIdleEventArgs e)
            {
                var bookmarks = new Dictionary<string, object>();
                foreach (var b in e.Bookmarks)
                {
                    bookmarks.Add(b.BookmarkName, null);
                }
                //workflowinstance.setBookmarks(extension, instance._id, e.InstanceId, bookmarks).Wait();
                Bookmarks = bookmarks;
                state = "idle";
                Save();
                if (state != "completed")
                {
                    idleOrComplete?.Invoke(this);
                }
            };

            wfApp.PersistableIdle = delegate (System.Activities.WorkflowApplicationIdleEventArgs e)
            {
                //return PersistableIdleAction.Unload;
                Save();
                return System.Activities.PersistableIdleAction.Persist;
            };

            wfApp.Unloaded = delegate (System.Activities.WorkflowApplicationEventArgs e)
            {
                if (!isCompleted && !hasError)
                {
                    state = "unloaded";

                }
                //isUnloaded = true;
                Save();
            };

            wfApp.OnUnhandledException = delegate (System.Activities.WorkflowApplicationUnhandledExceptionEventArgs e)
            {
                hasError = true;
                isCompleted = true;
                //Task.Run(() =>
                //{
                //    Thread.Sleep(500);
                //    try
                //    {
                //        store.XMLFileInstanceStore.cleanup(e.InstanceId);
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Error(ex, "");
                //    }
                //});
                state = "failed";
                errormessage = e.UnhandledException.ToString();
                //exceptionsource = e.ExceptionSource.Id;
                idleOrComplete?.Invoke(this);
                return System.Activities.UnhandledExceptionAction.Terminate;
            };

        } // addwfApphandlers

        public void Save()
        {

        }


    }
}
