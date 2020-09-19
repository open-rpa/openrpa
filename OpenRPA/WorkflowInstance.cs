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
    public class WorkflowInstance : apibase, IWorkflowInstance
    {
        public WorkflowInstance()
        {
        }
        public WorkflowInstance(Workflow workflow)
        {
            Workflow = workflow;
            WorkflowId = workflow._id;
            _type = "workflowinstance";
            _id = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
            _acl = workflow._acl;
            // LastUpdated = DateTime.Now;
        }
        [JsonIgnore]
        // public DateTime LastUpdated { get { return GetProperty<DateTime>(); } set { SetProperty(value); } } 
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
        public Dictionary<string, object> Parameters { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public Dictionary<string, object> Bookmarks { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public string Path { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string correlationId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string queuename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public Dictionary<string, WorkflowInstanceValueType> Variables { get { return GetProperty<Dictionary<string, WorkflowInstanceValueType>>(); } set { SetProperty(value); } }
        public string InstanceId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string WorkflowId { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string caller { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string RelativeFilename { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string xml { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string owner { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string ownerid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string host { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string fqdn { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errormessage { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errorsource { get { return GetProperty<string>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public Exception Exception { get { return GetProperty<Exception>(); } set { SetProperty(value); } }
        public bool isCompleted { 
            get {
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
                SetProperty(value); 
            }
        }
        public bool hasError { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string state { 
            get {
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
            set { 
                SetProperty(value); 
            } 
        }
        [JsonIgnore]
        public Workflow Workflow { get { return GetProperty<Workflow>(); } set { SetProperty(value); } }
        [JsonIgnore]
        public System.Activities.WorkflowApplication wfApp { get; set; }
        [JsonIgnore]
        public WorkflowTrackingParticipant TrackingParticipant { get; set; }
        private void NotifyState()
        {
            GenericTools.RunUI(() =>
            {
                try
                {
                    Workflow.NotifyPropertyChanged("State");
                    Workflow.NotifyPropertyChanged("StateImage");
                }
                catch (Exception)
                {
                }
            });
        }
        private void NotifyCompleted()
        {
            var _ref = (this as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                runner.onWorkflowCompleted(ref _ref);
            }
            NotifyState();
        }
        private void NotifyIdle()
        {
            var _ref = (this as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                runner.onWorkflowIdle(ref _ref);
            }
            NotifyState();
        }
        private void NotifyAborted()
        {
            var _ref = (this as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                runner.onWorkflowAborted(ref _ref);
            }
            NotifyState();
        }
        public static WorkflowInstance Create(Workflow Workflow, Dictionary<string, object> Parameters)
        {
            var result = new WorkflowInstance(Workflow) { Parameters = Parameters, name = Workflow.name, Path = Workflow.Project.Path };
            result.RelativeFilename = Workflow.RelativeFilename;
            var _ref = (result as IWorkflowInstance);
            foreach (var runner in Plugins.runPlugins)
            {
                if (!runner.onWorkflowStarting(ref _ref, false)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
            }
            if (global.isConnected)
            {
                result.owner = global.webSocketClient.user.name;
                result.ownerid = global.webSocketClient.user._id;
            }
            result.host = Environment.MachineName.ToLower();
            result.fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            result.createApp(Workflow.Activity);
            lock (Instances) Instances.Add(result);
            foreach (var i in Instances.ToList())
            {
                if (i.isCompleted) lock (Instances) Instances.Remove(i);
            }
            return result;
        }
        public void createApp(Activity activity)
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
                wfApp = new System.Activities.WorkflowApplication(activity);
                wfApp.Extensions.Add(TrackingParticipant);
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
            Save();
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
                var _ref = (this as IWorkflowInstance);
                foreach (var runner in Plugins.runPlugins)
                {
                    if (!runner.onWorkflowResumeBookmark(ref _ref, bookmarkName, value)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
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
                Save();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public System.Diagnostics.Stopwatch runWatch { get; set; }
        apibase IWorkflowInstance.Workflow { get => this.Workflow; set => this.Workflow = value as Workflow; }
        //public void Run(Activity root, string activityid)

        public void RunThis(Activity root, Activity activity)
        {
            createApp(activity);
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
            //    Log.Error(ex, "");
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
                Log.Error(ex, "");
                hasError = true;
                isCompleted = true;
                //isUnloaded = true;
                state = "failed";
                Exception = ex;
                errormessage = ex.Message;
                Save();
                if (runWatch != null) runWatch.Stop();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            }
        }
        private Assembly SystemActivities = null;
        public void DoStuff(object scheduler)
        {

            if(SystemActivities == null)
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
                    wfApp.Run();
                    InstanceId = wfApp.Id.ToString();
                    state = "running";
                    Save();
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
                Exception = ex;
                errormessage = ex.Message;
                Save();
                if (runWatch != null) runWatch.Stop();
                OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
            }
        }
        private void addwfApphandlers(System.Activities.WorkflowApplication wfApp)
        {
            wfApp.Completed = delegate (System.Activities.WorkflowApplicationCompletedEventArgs e)
            {
                isCompleted = true;
                _ = Workflow.State;
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
            };

            wfApp.Aborted = delegate (System.Activities.WorkflowApplicationAbortedEventArgs e)
            {
                hasError = true;
                isCompleted = true;
                state = "aborted";
                Exception =  e.Reason;
                errormessage = e.Reason.Message;
                Save();
                if(runWatch!=null) runWatch.Stop();
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
                state = "idle";
                Save();
                if (state != "completed")
                {
                    NotifyIdle();
                    OnIdleOrComplete?.Invoke(this, EventArgs.Empty);
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

                } else
                {
                    DeleteFile();
                }
                //isUnloaded = true;
                if(global.isConnected)
                {
                    Save();
                }
            };

            wfApp.OnUnhandledException = delegate (System.Activities.WorkflowApplicationUnhandledExceptionEventArgs e)
            {
                hasError = true;
                isCompleted = true;
                state = "failed";
                Exception = e.UnhandledException;
                errormessage = e.UnhandledException.ToString();
                if(e.ExceptionSource!=null) errorsource = e.ExceptionSource.Id;
                //exceptionsource = e.ExceptionSource.Id;
                if (runWatch != null) runWatch.Stop();
                NotifyAborted();
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
            string json = "";
            try
            {
                json = JsonConvert.SerializeObject(this);
            }
            catch (Exception)
            {
            }
            lock(filelock)
            {
                if(!string.IsNullOrEmpty(json)) System.IO.File.WriteAllText(Filepath, json);
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
        private Task SaveTask = null;
        public void Save()
        {
            SaveFile();
            if(Workflow!=null) Workflow.NotifyUIState();
            if (SaveTask==null)
            {
                //SaveTask = new Task.Delay(1000).ContinueWith(async () =>
                SaveTask = Task.Run(async () =>
                {
                    System.Threading.Thread.Sleep(1000);
                    try
                    {
                        if (isCompleted || hasError)
                        {
                            DeleteFile();
                        }
                        if (!global.isConnected) return;
                        //if ((DateTime.Now - LastUpdated).TotalMilliseconds < 2000) return;
                        //LastUpdated = DateTime.Now;
                        var result = await global.webSocketClient.InsertOrUpdateOne("openrpa_instances", 1, false, null, this);
                        _id = result._id;
                        _acl = result._acl;
                        //Log.Debug("Saved with id: " + _id);


                        // Catch up if others havent been saved
                        foreach (var i in Instances.ToList())
                        {
                            //if (string.IsNullOrEmpty(_id)) await i.Save();
                            if (string.IsNullOrEmpty(_id)) i.Save();
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        // throw;
                    }
                    finally
                    {
                        SaveTask = null;
                    }
                });
            }
        }
        public static async Task RunPendingInstances()
        {
            Log.FunctionIndent("RobotInstance", "RunPendingInstances");
            if (!global.isConnected)
            {
                Log.FunctionOutdent("RobotInstance", "RunPendingInstances", "Not connected");
                return;
            }
            try
            {
                var host = Environment.MachineName.ToLower();
                var fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
                var results = await global.webSocketClient.Query<WorkflowInstance>("openrpa_instances", "{'$or':[{state: 'idle'}, {state: 'running'}], fqdn: '" + fqdn + "'}", top: 1000);
                foreach (var i in results)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(i.InstanceId) && string.IsNullOrEmpty(i.xml))
                        {
                            Log.Error("Refuse to load instance " + i.InstanceId + " it contains no state!");
                            i.state = "aborted";
                            i.errormessage = "Refuse to load instance " + i.InstanceId + " it contains no state!";
                            i.Save();
                            continue;
                        }
                        var workflow = RobotInstance.instance.GetWorkflowByIDOrRelativeFilename(i.WorkflowId) as Workflow;
                        if (workflow == null)
                        {
                            Log.Error("Cannot run instance " + i.InstanceId + ", unknown workflow id " + i.WorkflowId);
                            i.state = "aborted";
                            i.errormessage = "Cannot run instance " + i.InstanceId + ", unknown workflow id " + i.WorkflowId;
                            i.Save();
                            continue;
                        }
                        i.Workflow = workflow;
                        //if (idleOrComplete != null) i.OnIdleOrComplete += idleOrComplete;
                        //if (VisualTracking != null) i.OnVisualTracking += VisualTracking;
                        lock (Instances) Instances.Add(i);
                        var _ref = (i as IWorkflowInstance);
                        foreach (var runner in Plugins.runPlugins)
                        {
                            if (!runner.onWorkflowStarting(ref _ref, true)) throw new Exception("Runner plugin " + runner.Name + " declined running workflow instance");
                        }
                        i.createApp(workflow.Activity);
                        i.Run();
                    }
                    catch (Exception ex)
                    {
                        i.state = "failed";
                        i.Exception = ex;
                        i.errormessage = ex.Message;
                        try
                        {
                            i.Save();
                        }
                        catch (Exception)
                        {

                        }
                        Log.Error("RunPendingInstances: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("RobotInstance", "RunPendingInstances");
        }
    }

}
