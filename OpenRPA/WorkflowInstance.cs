using Newtonsoft.Json;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.DurableInstancing;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class WorkflowInstance : LocallyCached, IWorkflowInstance, IDisposable
    {
        public WorkflowInstance()
        {
            _id = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
        }
        private WorkflowInstance(Workflow workflow)
        {
            Workflow = workflow;
            WorkflowId = workflow._id;
            _type = "workflowinstance";
            _id = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
            _acl = workflow._acl;
        }

        private static List<WorkflowInstance> _Instances = new List<WorkflowInstance>();
        public static List<WorkflowInstance> Instances
        {
            get
            {
                return _Instances;
                // return _Instances.Where(x => x.state != "loaded").ToList();
            }
        }
        public event VisualTrackingHandler OnVisualTracking;
        public event idleOrComplete OnIdleOrComplete;
        [JsonIgnore, LiteDB.BsonIgnore]
        public Dictionary<string, object> Parameters { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public Dictionary<string, object> Bookmarks { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        [JsonIgnore, LiteDB.BsonIgnore]
        public string Path { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string correlationId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string queuename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore, LiteDB.BsonIgnore]
        public Dictionary<string, WorkflowInstanceValueType> Variables { get { return GetProperty<Dictionary<string, WorkflowInstanceValueType>>(); } set { SetProperty(value); } }
        public string InstanceId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string WorkflowId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string caller { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string RelativeFilename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string projectid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string projectname { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string xml { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string owner { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string ownerid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string host { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string fqdn { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errormessage { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errorsource { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore, LiteDB.BsonIgnore]
        public Exception Exception { get { return GetProperty<Exception>(); } set { SetProperty(value); } }
        public bool isCompleted
        {
            get
            {
                var value = GetProperty<bool>();
                if (!value && wfApp != null)
                {
                    try
                    {
                        var _state = typeof(System.Activities.WorkflowApplication).GetField("state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(wfApp);
                        if (_state.ToString() == "Aborted" && _state.ToString() == "Unloaded")
                        {
                            value = true;
                            SetProperty(value);
                        }
                    }
                    catch (Exception)
                    {
                        value = true;
                    }
                }
                return value;
            }
            set
            {
                try
                {
                    SetProperty(value);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
        public bool hasError { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string state
        {
            get
            {
                var value = GetProperty<string>();
                if (isCompleted && (value == "loaded" || value == "running" || value == "idle" || value == "unloaded"))
                {
                    try
                    {
                        value = "completed";
                        SetProperty(value);
                    }
                    catch (Exception)
                    {
                    }
                }
                return value;
            }
            set
            {
                SetProperty(value);
                if (Workflow != null) Workflow.SetLastState(value);
                isDirty = true;
            }
        }
        [JsonIgnore, LiteDB.BsonIgnore]
        public Workflow Workflow { get { return GetProperty<Workflow>(); } set { SetProperty(value); } }
        [JsonIgnore, LiteDB.BsonIgnore]
        public System.Activities.WorkflowApplication wfApp { get; set; }
        [JsonIgnore, LiteDB.BsonIgnore]
        public WorkflowTrackingParticipant TrackingParticipant { get; set; }
        private void NotifyCompleted()
        {
            var _ref = (this as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                runner.onWorkflowCompleted(ref _ref);
            }
            if (Workflow != null) Workflow.NotifyUIState();
        }
        private void NotifyIdle()
        {
            var _ref = (this as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                runner.onWorkflowIdle(ref _ref);
            }
            if (Workflow != null) Workflow.NotifyUIState();
        }
        private void NotifyAborted()
        {
            var _ref = (this as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                runner.onWorkflowAborted(ref _ref);
            }
            if (Workflow != null) Workflow.NotifyUIState();
        }
        public static WorkflowInstance Create(Workflow Workflow, Dictionary<string, object> Parameters)
        {
            if (RobotInstance.openrpa_workflow_run_count != null) RobotInstance.openrpa_workflow_run_count.Add(1, RobotInstance.tags);
            var result = new WorkflowInstance(Workflow) { Parameters = Parameters, name = Workflow.name, Path = Workflow.Project().Path };
            result.RelativeFilename = Workflow.RelativeFilename;
            result.projectid = Workflow.projectid;
            result.projectname = Workflow.Project().name;

            var _ref = (result as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                if (!runner.onWorkflowStarting(ref _ref, false)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
            }
            if (global.isConnected && global.webSocketClient.user != null)
            {
                result.owner = global.webSocketClient.user.name;
                result.ownerid = global.webSocketClient.user._id;
            }
            result.host = Environment.MachineName.ToLower();
            result.fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            if (System.Threading.Monitor.TryEnter(Instances, 1000))
            {
                try
                {
                    Instances.Add(result);
                }
                finally
                {
                    System.Threading.Monitor.Exit(Instances);
                }
            }
            else { throw new LockNotReceivedException("Failed adding new workflow instance in Create"); }
            result.createApp(Workflow.Activity());
            CleanUp();
            CleanUp();
            return result;
        }
        private void createApp(Activity activity)
        {
            //var xh = new XamlHelper(workflow.xaml);
            //extraextension.updateProfile(xh.Variables.ToArray(), xh.ArgumentNames.ToArray());
            TrackingParticipant = new WorkflowTrackingParticipant();
            TrackingParticipant.OnVisualTracking += Participant_OnVisualTracking;

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
                wfApp = new System.Activities.WorkflowApplication(activity, Parameters);
                wfApp.Extensions.Add(TrackingParticipant);
                foreach (var t in Plugins.WorkflowExtensionsTypes)
                {
                    try
                    {
                        var ext = (ICustomWorkflowExtension)Activator.CreateInstance(t);
                        ext.Initialize(RobotInstance.instance, Workflow, this);
                        wfApp.Extensions.Add(ext);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("error init " + t.Name + ": " + ex.ToString());
                    }
                }
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
                    if (!Config.local.disable_instance_store) wfApp.InstanceStore = new Store.OpenFlowInstanceStore();
                }
                addwfApphandlers(wfApp);
            }
            else
            {
                wfApp = new System.Activities.WorkflowApplication(activity);
                wfApp.Extensions.Add(TrackingParticipant);
                foreach (var t in Plugins.WorkflowExtensionsTypes)
                {
                    try
                    {
                        var ext = (ICustomWorkflowExtension)Activator.CreateInstance(t);
                        ext.Initialize(RobotInstance.instance, Workflow, this);
                        wfApp.Extensions.Add(ext);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("error init " + t.Name + ": " + ex.ToString());
                    }
                }

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
                    if (!Config.local.disable_instance_store) wfApp.InstanceStore = new Store.OpenFlowInstanceStore();
                }
                wfApp.Load(new Guid(InstanceId));
            }
            state = "loaded";
            Task.Run(async () =>
            {
                try
                {
                    await Save<WorkflowInstance>();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });

        }
        private void Participant_OnVisualTracking(WorkflowInstance Instance, string ActivityId, string ChildActivityId, string State)
        {
            OnVisualTracking?.Invoke(Instance, ActivityId, ChildActivityId, State);
        }
        public void Abort(string Reason)
        {
            if (wfApp != null)
            {
                var _state = typeof(System.Activities.WorkflowApplication).GetField("state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(wfApp);
                if (_state.ToString() != "Aborted")
                {
                    try
                    {
                        wfApp.Abort(Reason);
                        return;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            hasError = true;
            isCompleted = true;
            state = "aborted";
            Exception = new Exception(Reason);
            errormessage = Reason;
            isDirty = true;
            if (runWatch != null) runWatch.Stop();
            OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            GenericTools.RunUI(() =>
            {
                Workflow.SetLastState("aborted");
            });
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
                var _ref = (this as IWorkflowInstance);
                foreach (var runner in Plugins.runPlugins)
                {
                    if (!runner.onWorkflowResumeBookmark(ref _ref, bookmarkName, value)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
                }
                // Log.Debug(String.Format("Workflow {0} resuming at bookmark '{1}' value '{2}'", wfApp.Id.ToString(), bookmarkName, value));
                Task.Run(() =>
                {
                    // System.Threading.Thread.Sleep(50);
                    var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                    while (true && sw.Elapsed < TimeSpan.FromSeconds(10))
                    {
                        System.Threading.Thread.Sleep(200);
                        if (wfApp != null && wfApp.OnUnhandledException != null) break;
                    }
                    try
                    {
                        if (wfApp != null) wfApp.ResumeBookmark(bookmarkName, value);
                        // Log.Information(name + " resumed in " + string.Format("{0:mm\\:ss\\.fff}", runWatch.Elapsed));
                        Log.Information(name + " resumed");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                });
                state = "running";
                // Log.Debug(String.Format("Workflow {0} resumed bookmark '{1}' value '{2}'", wfApp.Id.ToString(), bookmarkName, value));
                // Save();
            }
            catch (Exception)
            {
                throw;
            }
        }
        [JsonIgnore, LiteDB.BsonIgnore]
        public System.Diagnostics.Stopwatch runWatch { get; set; }
        [JsonIgnore, LiteDB.BsonIgnore]
        public Stack<System.Diagnostics.Activity> Activities = new Stack<System.Diagnostics.Activity>();
        [JsonIgnore, LiteDB.BsonIgnore]
        public System.Diagnostics.Activity RootActivity = null;
        [JsonProperty(propertyName: "parentspanid")]
        public string ParentSpanId { get; set; }
        [JsonProperty(propertyName: "spanid")]
        public string SpanId { get; set; }
        [JsonIgnore, LiteDB.BsonIgnore]
        public System.Diagnostics.ActivitySource source = new System.Diagnostics.ActivitySource("OpenRPA");
        IWorkflow IWorkflowInstance.Workflow { get => this.Workflow; set => this.Workflow = value as Workflow; }
        public void RunThis(Activity root, Activity activity)
        {
            createApp(activity);
            if (RobotInstance.openrpa_workflow_run_count != null) RobotInstance.openrpa_workflow_run_count.Add(1, RobotInstance.tags);
            Run();


            //try
            //{
            //    if (SystemActivities == null)
            //    {
            //        SystemActivities = typeof(System.Activities.Hosting.WorkflowInstance).Assembly;
            //    }


            //    // Type WorkflowInstancet = activity.GetType().Assembly.GetTypes().Where(x => x.FullName == "System.Activities.Hosting.WorkflowInstance").FirstOrDefault();
            //    // WorkflowInstancet.GetMethod("EnsureDefinitionReady", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(wfApp, null);

            //    wfApp.GetType().GetMethod("EnsureInitialized", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(wfApp, null);


            //    var executor = typeof(System.Activities.Hosting.WorkflowInstance).GetField("executor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(wfApp);
            //    var scheduler = executor.GetType().GetField("scheduler", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            //    var rootInstance = executor.GetType().GetField("rootInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            //    var bookmarkManager = executor.GetType().GetField("bookmarkManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            //    var rootEnvironment = executor.GetType().GetField("rootEnvironment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            //    scheduler.GetType().GetMethod("ClearAllWorkItems", BindingFlags.Public | BindingFlags.Instance).Invoke(scheduler, new object[] { executor });


            //    executor.GetType().GetMethod("ScheduleRootActivity", BindingFlags.Public | BindingFlags.Instance).Invoke(executor, new object[] { activity, null, null });
            //    wfApp.GetType().GetMethod("EnsureInitialized", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(wfApp, null);

            //    // executor.GetType().GetMethod("ScheduleSecondaryRootActivity", BindingFlags.Public | BindingFlags.Instance).Invoke(executor, new object[] { activity , rootEnvironment });
            //    wfApp.GetType().GetMethod("RunInstance", BindingFlags.NonPublic | BindingFlags.Static).Invoke(wfApp, new object[] { wfApp });
            //    //ScheduleActivity


            //    //var _enum = new ChildEnumerator(rootInstance as System.Activities.ActivityInstance);
            //    //while (_enum.MoveNext())
            //    //{
            //    //    var id = _enum.Current.Activity.Id;
            //    //}


            //    // var bookmarkManager = executor.GetType().GetField("bookmarkManager", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            //    // scheduler.GetType().GetMethod("Resume", BindingFlags.Public | BindingFlags.Instance).Invoke(scheduler, new object[] { });
            //    // scheduler.GetType().GetMethod("Resume", BindingFlags.Public | BindingFlags.Instance).Invoke(scheduler, new object[] { });


            //    // var ActivityInstanceMap = executor.GetType().GetProperty("SerializedProgramMapping", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);

            //    // *************************************************
            //    // wfApp.GetType().GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Static).Invoke(wfApp, new object[] { activity, null, wfApp.Extensions, TimeSpan.FromMinutes(200) });
            //    // *************************************************

            //    // scheduler.GetType().GetMethod("ClearAllWorkItems", BindingFlags.Public | BindingFlags.Instance).Invoke(scheduler, new object[] { executor });

            //    //executor.GetType().GetField("shouldRaiseMainBodyComplete", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(executor, true);


            //    //var controller = wfApp.GetType().GetProperty("Controller", BindingFlags.Public | BindingFlags.Instance).GetValue(wfApp);

            //    // rootInstance.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rootInstance, 2);

            //    //Type t = SystemActivities.GetType("System.Activities.Runtime.EmptyWorkItem");
            //    //var EmptyWorkItem = Activator.CreateInstance(t);
            //    // var EmptyWorkItem = executor.GetType().GetMethod("CreateEmptyWorkItem", BindingFlags.Public | BindingFlags.Instance).Invoke(executor, new object[] { rootInstance });
            //    // scheduler.GetType().GetMethod("ClearAllWorkItems", BindingFlags.Public | BindingFlags.Instance).Invoke(scheduler, new object[] { executor });
            //    // executor.GetType().GetMethod("CompleteActivityInstance", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(executor, new object[] { rootInstance });
            //    // executor.GetType().GetMethod("CancelRootActivity", BindingFlags.Public | BindingFlags.Instance).Invoke(executor, new object[] { });

            //    // executor.GetType().GetField("isAbortPending", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(executor, true);
            //    // executor.GetType().GetField("isTerminatePending", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(executor, true);



            //    // executor.GetType().GetField("executionState", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(executor, 1);

            //    // *************************************************
            //    // rootInstance.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rootInstance, 1); // Closed
            //    // wfApp.GetType().GetMethod("RunInstance", BindingFlags.NonPublic | BindingFlags.Static).Invoke(wfApp, new object[] { wfApp  });
            //    // *************************************************

            //    //var emptyParam = new Dictionary<string, object>();
            //    //executor.GetType().GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(executor, new object[] { });

            //    //executor.GetType().GetMethod("CancelRootActivity", BindingFlags.Public | BindingFlags.Instance).Invoke(executor, new object[] { });
            //    //scheduler.GetType().GetMethod("NotifyWorkCompletion", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(scheduler, new object[] { });

            //    //// rootInstance.GetType().GetMethod("OnNotifyPaused", BindingFlags.Public | BindingFlags.Instance).Invoke(rootInstance, new object[] { });

            //    //var callbacks = scheduler.GetType().GetField("callbacks", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler);
            //    //if(callbacks!=null) callbacks.GetType().GetMethod("SchedulerIdle", BindingFlags.Public | BindingFlags.Instance).Invoke(callbacks, new object[] { });
            //    //var host = executor.GetType().GetField("host", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            //    //host.GetType().GetMethod("NotifyPaused", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(host, new object[] { });

            //    //isCompleted = true;
            //    //state = "completed";
            //    //OnIdleOrComplete?.Invoke(this, EventArgs.Empty);

            //    //            public enum ActivityInstanceState
            //    //{
            //    //    [EnumMember]
            //    //    Executing = 0,
            //    //    [EnumMember]
            //    //    Closed = 1,
            //    //    [EnumMember]
            //    //    Canceled = 2,
            //    //    [EnumMember]
            //    //    Faulted = 3
            //    ////}

            //    //        this.rootInstance.IsCompleted

            //    //                public ActivityInstanceState State
            //    //{
            //    //    get
            //    //    {
            //    //        if (((this.executingSecondaryRootInstances != null) && (this.executingSecondaryRootInstances.Count > 0)) || ((this.rootInstance != null) && !this.rootInstance.IsCompleted))
            //    //        {
            //    //            return ActivityInstanceState.Executing;
            //    //        }
            //    //        return this.executionState;
            //    //    }
            //    //}

            //    //                private enum WorkflowApplicationState : byte
            //    //{
            //    //    Paused = 0,
            //    //    Runnable = 1,
            //    //    Unloaded = 2,
            //    //    Aborted = 3
            //    //}


            //    // Type t = Type.GetType("System.Activities.ActivityInstance, System.Activities");
            //    // Type t = activity.GetType().Assembly.GetTypes().Where(x => x.FullName == "System.Activities.ActivityInstance").FirstOrDefault();


            //    //var targetInstance = Activator.CreateInstance(t, BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { activity }, null, null);
            //    //targetInstance.GetType().GetMethod("Execute", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(targetInstance, new object[] { executor, bookmarkManager });
            //    // CompleteActivityInstance

            //    //scheduler.GetType().GetMethod("NotifyWorkCompletion", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(scheduler, new object[] { });
            //    //var callbacks = scheduler.GetType().GetField("callbacks", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler);
            //    //callbacks.GetType().GetMethod("SchedulerIdle", BindingFlags.Public | BindingFlags.Instance).Invoke(callbacks, new object[] { });


            //    //var callbacks = scheduler.GetType().GetField("callbacks", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler);
            //    //callbacks.GetType().GetMethod("SchedulerIdle", BindingFlags.Public | BindingFlags.Instance).Invoke(callbacks, new object[] { });
            //    //var host = executor.GetType().GetField("host", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            //    //host.GetType().GetMethod("NotifyPaused", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(host, new object[] { });


            //    //OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            //    //wfApp.Abort("done");

            //}
            //catch (Exception ex)
            //{
            //    Log.Error(ex.ToString());
            //    hasError = true;
            //    isCompleted = true;
            //    //isUnloaded = true;
            //    state = "failed";
            //    Exception = ex;
            //    errormessage = ex.Message;
            //    Save();
            //    if (runWatch != null) runWatch.Stop();
            //    OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            //}
        }
        public void RunFromHere(Activity root, Activity activity)
        {
            try
            {
                if (SystemActivities == null)
                {
                    SystemActivities = typeof(System.Activities.Hosting.WorkflowInstance).Assembly;
                }


                //TrackingParticipant.runactivityid = activityid;
                //Run();

                //runWatch = new System.Diagnostics.Stopwatch();
                //runWatch.Start();
                //// wfApp.Run();

                wfApp.GetType().GetMethod("EnsureInitialized", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(wfApp, null);
                var executor = typeof(System.Activities.Hosting.WorkflowInstance).GetField("executor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(wfApp);
                var scheduler = executor.GetType().GetField("scheduler", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);


                var firstWorkItem = scheduler.GetType().GetField("firstWorkItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler);

                wfApp.GetType().GetMethod("EnsureInitialized", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(wfApp, new object[] { });
                wfApp.GetType().GetMethod("RunCore", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(wfApp, new object[] { });

                System.Threading.SynchronizationContext synchronizationContext = scheduler.GetType().GetField("synchronizationContext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler) as System.Threading.SynchronizationContext;
                // synchronizationContext.OperationStarted();

                synchronizationContext.Post(DoStuff, scheduler);

                // scheduler.GetType().GetMethod("Resume", BindingFlags.Public | BindingFlags.Instance).Invoke(scheduler, new object[] { });


                // wfApp.GetType().GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Static).Invoke(wfApp, new object[] { root, null, wfApp.Extensions, TimeSpan.FromMinutes(200) });
                // wfApp.GetType().GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Static).Invoke(wfApp, new object[] { activity, null, wfApp.Extensions, TimeSpan.FromMinutes(200) });
                //isCompleted = true;
                //OnIdleOrComplete?.Invoke(this, EventArgs.Empty);


                //MethodInfo method = executor.GetType().GetMethod("ScheduleSecondaryRootActivity");
                //var res = method.Invoke(executor, new object[] { activity, null });

                // executor.ScheduleActivity(activity, ActivityInstance, CompletionBookmark, FaultBookmark, LocationEnvironment, IDictionary<String, Object>, Location) : ActivityInstance
                // executor.ScheduleSecondaryRootActivity(activity, null);

                // scheduler.ClearAllWorkItems(executor);


                //if (string.IsNullOrEmpty(InstanceId))
                //{
                //    wfApp.Run();
                //    InstanceId = wfApp.Id.ToString();
                //    state = "running";
                //    Save();
                //}
                //else
                //{
                //    foreach (var b in Bookmarks)
                //    {
                //        if (b.Value != null && !string.IsNullOrEmpty(b.Value.ToString())) wfApp.ResumeBookmark(b.Key, b.Value);
                //    }
                //    if (Bookmarks.Count() == 0)
                //    {
                //        wfApp.Run();
                //    }
                //    state = "running";
                //    Save();
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                hasError = true;
                isCompleted = true;
                //isUnloaded = true;
                state = "failed";
                Exception = ex;
                errormessage = ex.Message;
                isDirty = true;
                if (runWatch != null) runWatch.Stop();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            }
        }
        private Assembly SystemActivities = null;
        public void DoStuff(object scheduler)
        {

            if (SystemActivities == null)
            {
                SystemActivities = typeof(System.Activities.Hosting.WorkflowInstance).Assembly;
            }
            var executor = typeof(System.Activities.Hosting.WorkflowInstance).GetField("executor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(wfApp);
            var firstWorkItem = scheduler.GetType().GetField("firstWorkItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler);
            System.Threading.SynchronizationContext synchronizationContext = scheduler.GetType().GetField("synchronizationContext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler) as System.Threading.SynchronizationContext;
            var callbacks = scheduler.GetType().GetField("callbacks", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scheduler);
            if (firstWorkItem == null) return;
            var IsEmpty = (bool)firstWorkItem.GetType().GetProperty("IsEmpty", BindingFlags.Public | BindingFlags.Instance).GetValue(firstWorkItem);


            var rootInstance = executor.GetType().GetField("rootInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executor);
            var _enum = new ChildEnumerator(rootInstance as System.Activities.ActivityInstance);
            while (_enum.MoveNext())
            {
                var id = _enum.Current.Activity.Id;
            }

            try
            {



                if (!IsEmpty)
                {
                    firstWorkItem.GetType().GetMethod("Release", BindingFlags.Public | BindingFlags.Instance).Invoke(firstWorkItem, new object[] { executor });
                    var IsValid = (bool)firstWorkItem.GetType().GetProperty("IsValid", BindingFlags.Public | BindingFlags.Instance).GetValue(firstWorkItem);

                    var action = executor.GetType().GetMethod("TryExecuteNonEmptyWorkItem", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(executor, new object[] { firstWorkItem });
                    firstWorkItem.GetType().GetMethod("PostProcess", BindingFlags.Public | BindingFlags.Instance).Invoke(firstWorkItem, new object[] { executor });
                }
                executor.GetType().GetMethod("ScheduleRuntimeWorkItems", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(executor, new object[] { });

                firstWorkItem.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance).Invoke(firstWorkItem, new object[] { executor });

                //if (firstWorkItem.GetType().Name == "EmptyWorkItem")
                //{
                //    scheduler.GetType().GetField("isPausing", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(scheduler, false);
                //    scheduler.GetType().GetField("isRunning", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(scheduler, false);
                //    // var continueAction = callbacks.GetType().GetMethod("ExecuteWorkItem", BindingFlags.Public | BindingFlags.Instance).Invoke(callbacks, new object[] { firstWorkItem });

                //    scheduler.GetType().GetMethod("NotifyWorkCompletion", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(scheduler, new object[] { });
                //    // callbacks.GetType().GetMethod("SchedulerIdle", BindingFlags.Public | BindingFlags.Instance).Invoke(callbacks, new object[] {  });
                //}
                //else
                //{
                //    // var continueAction = callbacks.GetType().GetMethod("ExecuteWorkItem", BindingFlags.Public | BindingFlags.Instance).Invoke(callbacks, new object[] { firstWorkItem });
                //    synchronizationContext.Post(DoStuff, scheduler);
                //}
                synchronizationContext.Post(DoStuff, scheduler);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void Run()
        {
            try
            {
                runWatch = new System.Diagnostics.Stopwatch();
                runWatch.Start();
                if (string.IsNullOrEmpty(InstanceId))
                {
                    if (System.Threading.Monitor.TryEnter(Instances, 1000))
                    {
                        try
                        {
                            wfApp.Run();
                            Log.Information(name + " started in " + string.Format("{0:mm\\:ss\\.fff}", runWatch.Elapsed));
                            InstanceId = wfApp.Id.ToString();
                            state = "running";
                        }
                        finally
                        {
                            System.Threading.Monitor.Exit(Instances);
                        }
                    }
                    else { throw new LockNotReceivedException("Failed running workflow instance"); }
                    // Save();
                }
                else
                {
                    bool resumed = false;
                    if (!resumed)
                    {
                        wfApp.Run();
                    }
                    Log.Information(name + " resumed in " + string.Format("{0:mm\\:ss\\.fff}", runWatch.Elapsed));
                    state = "running";
                    // Save();

                    if (Bookmarks != null)
                        foreach (var b in Bookmarks)
                        {
                            var i = Instances.Where(x => x._id == b.Key).FirstOrDefault();
                            if (i == null) i = RobotInstance.instance.dbWorkflowInstances.FindById(b.Key);
                            if (i != null && i.isCompleted)
                            {
                                wfApp.ResumeBookmark(b.Key, i);
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                hasError = true;
                isCompleted = true;
                //isUnloaded = true;
                state = "failed";
                Exception = ex;
                errormessage = ex.Message;
                isDirty = true;
                if (runWatch != null) runWatch.Stop();
                NotifyAborted();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            }
        }
        private void addwfApphandlers(System.Activities.WorkflowApplication wfApp)
        {
            wfApp.Completed = delegate (System.Activities.WorkflowApplicationCompletedEventArgs e)
            {
                isCompleted = true;
                _ = Workflow.State;
                RobotInstance.unsavedTimer.Interval = 5000;
                if (e.CompletionState == System.Activities.ActivityInstanceState.Faulted)
                {
                    if (state == "running" || state == "idle" || state == "completed")
                    {
                        state = "aborted";
                        if (e.TerminationException != null)
                        {
                            Exception = e.TerminationException;
                            errormessage = e.TerminationException.Message;
                        }
                        else
                        {
                            errormessage = "Faulted for unknown reason";
                        }
                        isDirty = true;
                        NotifyAborted();
                        OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }
                else if (e.CompletionState == System.Activities.ActivityInstanceState.Canceled)
                {
                }
                else if (e.CompletionState == System.Activities.ActivityInstanceState.Closed)
                {
                    state = "completed";
                    if (Parameters != null) foreach (var o in e.Outputs) Parameters[o.Key] = o.Value;
                    if (runWatch != null) runWatch.Stop();
                    NotifyCompleted();
                    OnIdleOrComplete?.Invoke(this, EventArgs.Empty);

                }
                else if (e.CompletionState == System.Activities.ActivityInstanceState.Executing)
                {
                }
                else
                {
                    throw new Exception("Unknown completetion state!!!" + e.CompletionState);
                }
                isDirty = true;
                if (Workflow != null) Workflow.NotifyUIState();
            };
            wfApp.Aborted = delegate (System.Activities.WorkflowApplicationAbortedEventArgs e)
            {
                RobotInstance.unsavedTimer.Interval = 5000;
                hasError = true;
                isCompleted = true;
                state = "aborted";
                Exception = e.Reason;
                errormessage = e.Reason.Message;
                isDirty = true;
                if (runWatch != null) runWatch.Stop();
                NotifyAborted();
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
                if (Bookmarks == null || Bookmarks.Count == 0)
                {
                    Bookmarks = bookmarks;
                    _ = Save<WorkflowInstance>(true);
                }
                state = "idle";
                isDirty = true;
                NotifyIdle();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            };
            wfApp.PersistableIdle = delegate (System.Activities.WorkflowApplicationIdleEventArgs e)
            {
                //return PersistableIdleAction.Unload;
                isDirty = true;
                return System.Activities.PersistableIdleAction.Persist;
            };
            wfApp.Unloaded = delegate (System.Activities.WorkflowApplicationEventArgs e)
            {
                if (!isCompleted && !hasError)
                {
                    state = "unloaded";

                }
                isDirty = true;
            };
            wfApp.OnUnhandledException = delegate (System.Activities.WorkflowApplicationUnhandledExceptionEventArgs e)
            {
                RobotInstance.unsavedTimer.Interval = 5000;
                hasError = true;
                isCompleted = true;
                state = "failed";
                Exception = e.UnhandledException;
                while (Exception is System.Reflection.TargetInvocationException ti && Exception.InnerException != null)
                {
                    Exception = Exception.InnerException;
                }
                errormessage = Exception.Message;

                if (e.ExceptionSource != null)
                {
                    errorsource = e.ExceptionSource.Id;
                    Exception.Source = errorsource;
                }
                //exceptionsource = e.ExceptionSource.Id;
                if (runWatch != null) runWatch.Stop();
                isDirty = true;
                NotifyAborted();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
                return System.Activities.UnhandledExceptionAction.Terminate;
            };
        }
        private object filelock = new object();
        //public void SaveFile()
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(InstanceId)) return;
        //        if (string.IsNullOrEmpty(Path)) return;
        //        if (isCompleted || hasError) return;
        //        if (!System.IO.Directory.Exists(System.IO.Path.Combine(Path, "state"))) System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Path, "state"));
        //        var Filepath = System.IO.Path.Combine(Path, "state", InstanceId + ".json");
        //        string json = "";
        //        try
        //        {
        //            json = JsonConvert.SerializeObject(this);
        //        }
        //        catch (Exception)
        //        {
        //        }
        //        if (System.Threading.Monitor.TryEnter(filelock, 1000))
        //        {
        //            try
        //            {
        //                if (!string.IsNullOrEmpty(json)) System.IO.File.WriteAllText(Filepath, json);
        //            }
        //            finally
        //            {
        //                System.Threading.Monitor.Exit(filelock);
        //            }
        //        }
        //        else { throw new LockNotReceivedException("Failed saving workflow instance"); }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.ToString());
        //    }
        //}
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
        //        public void Save()
        //        {
        //            if (Workflow != null) Workflow.NotifyUIState();
        //            if (isCompleted || hasError)
        //            {
        //                _ = Task.Run(async () =>
        //                {
        //                    System.Threading.Thread.Sleep(2000);
        //                    if (isCompleted || hasError)
        //                    {
        //                        xml = null;
        //                    }
        //                    isDirty = true;
        //#if DEBUG
        //                    Log.Output("WorkflowInstance.Save()");
        //#endif

        //                    await Save<WorkflowInstance>(true);
        //                });
        //                if (Workflow != null) Workflow.NotifyUIState();
        //            }
        //        }
        private static bool hasRanPending = false;
        public static async Task RunPendingInstances()
        {
            if (hasRanPending) return;
            if (Config.local.disable_instance_store) return;
            // var span = RobotInstance.instance.source.StartActivity("RunPendingInstances", System.Diagnostics.ActivityKind.Internal);
            Log.FunctionIndent("RobotInstance", "RunPendingInstances");
            try
            {
                //if (!global.isConnected)
                //{
                //    Log.FunctionOutdent("RobotInstance", "RunPendingInstances", "Not connected");
                //    return;
                //}
                var host = Environment.MachineName.ToLower();
                var fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
                //var results = await global.webSocketClient.Query<WorkflowInstance>("openrpa_instances", "{'$or':[{state: 'idle'}, {state: 'running'}], fqdn: '" + fqdn + "'}", top: 1000);

                var results = RobotInstance.instance.dbWorkflowInstances.Find(x => (x.state == "idle" || x.state == "running") && x.fqdn == fqdn).ToList();
                if (results.Count > 0) Log.Information("Try running " + results.Count + " pending workflows");
                foreach (WorkflowInstance i in results)
                {
                    if (i.Workflow != null && i.Workflow.Serializable == false) return;
                    try
                    {
                        if (!string.IsNullOrEmpty(i.InstanceId) && string.IsNullOrEmpty(i.xml))
                        {
                            var folder = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "state");
                            var filename = System.IO.Path.Combine(folder, i.InstanceId + ".xml");
                            if (!System.IO.File.Exists(filename))
                            {
                                Log.Error("Refuse to load instance " + i.RelativeFilename + " / " + i.name + " (" + i.InstanceId + ") it contains no state!");
                                i.state = "aborted";
                                i.errormessage = "Refuse to load instance " + i.InstanceId + " it contains no state!";
                                i.isDirty = true;
                                await i.Save<WorkflowInstance>();
                                if (i.Workflow != null) i.Workflow.NotifyUIState();
                                continue;
                            }
                        }
                        var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(i.WorkflowId) as Workflow;
                        if (workflow == null)
                        {
                            Log.Error("Cannot run instance " + i.RelativeFilename + " / " + i.name + " (" + i.InstanceId + "), unknown workflow id " + i.WorkflowId);
                            i.state = "aborted";
                            i.errormessage = "Cannot run instance " + i.InstanceId + ", unknown workflow id " + i.WorkflowId;
                            i.isDirty = true;
                            await i.Save<WorkflowInstance>();
                            if (i.Workflow != null) i.Workflow.NotifyUIState();
                            continue;
                        }
                        i.Workflow = workflow;
                        if (RobotInstance.instance.Window != null) i.OnIdleOrComplete += RobotInstance.instance.Window.IdleOrComplete;
                        //if (VisualTracking != null) i.OnVisualTracking += VisualTracking;
                        if (System.Threading.Monitor.TryEnter(Instances, 1000))
                        {
                            try
                            {
                                Instances.Add(i);
                            }
                            finally
                            {
                                System.Threading.Monitor.Exit(Instances);
                            }
                        }
                        else { throw new LockNotReceivedException("Failed adding workflow instance in running pending"); }
                        var _ref = (i as IWorkflowInstance);
                        foreach (var runner in Plugins.runPlugins)
                        {
                            if (!runner.onWorkflowStarting(ref _ref, true)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
                        }
                        i.createApp(workflow.Activity());
                        i.Run();
                        await Task.Delay(250);
                    }
                    catch (Exception ex)
                    {
                        i.state = "failed";
                        i.Exception = ex;
                        i.errormessage = ex.Message;
                        try
                        {
                            i.isDirty = true;
                        }
                        catch (Exception)
                        {

                        }
                        Log.Error("RunPendingInstances: " + ex.ToString());
                    }
                }
                hasRanPending = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {

            }
            Log.FunctionOutdent("RobotInstance", "RunPendingInstances");
        }
        public static void CleanUp()
        {
            if (System.Threading.Monitor.TryEnter(Instances, 1000))
            {
                try
                {
                    foreach (var i in Instances.ToList())
                    {
                        // if (i.isCompleted && i._modified > DateTime.Now.AddMinutes(5))
                        if (i.isCompleted && i._modified < DateTime.Now.AddMinutes(-15) && !i.isDirty)
                        {
                            Log.Verbose("[workflow] Remove workflow with id '" + i.WorkflowId + "'");
                            Instances.Remove(i);
                        }
                    }
                }
                finally
                {
                    System.Threading.Monitor.Exit(Instances);
                }
            }
            else { throw new LockNotReceivedException("Failed cleaning up old workflow instance"); }
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(InstanceId)) return "No InstanceId";
            return InstanceId;
        }
        private bool isDisposing = false;
        public void Dispose()
        {
            if (isDisposing) return;
            isDisposing = true;
        }
    }
    public class UnsavedWorkflowInstance
    {
        public WorkflowInstance Instance;
    }
}
