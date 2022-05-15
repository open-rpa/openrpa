using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.WorkItems.Activities
{
    public class RobotInstance : IOpenRPAClient, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event StatusEventHandler Status;
        public event SignedinEventHandler Signedin;
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event ReadyForActionEventHandler ReadyForAction;

        public System.Collections.ObjectModel.ObservableCollection<IWorkitem> Workitems =
            new ObservableCollection<IWorkitem>();
        public System.Collections.ObjectModel.ObservableCollection<IWorkitemQueue> WorkItemQueues { get; set; }
        = new ObservableCollection<IWorkitemQueue>();

        public async Task init()
        {
            try
            {
                if (!string.IsNullOrEmpty(Config.local.wsurl))
                {
                    global.webSocketClient = Net.WebSocketClient.Get(Config.local.wsurl);
                    global.webSocketClient.OnOpen += RobotInstance_WebSocketClient_OnOpen;
                    global.webSocketClient.OnClose += WebSocketClient_OnClose;
                    global.webSocketClient.OnQueueClosed += WebSocketClient_OnQueueClosed;
                    global.webSocketClient.OnQueueMessage += WebSocketClient_OnQueueMessage;
                    await Connect();
                } else
                {
                    System.Windows.MessageBox.Show("Config missing or config missing wsurl");
                }
                global.OpenRPAClient = this;
                AutomationHelper.init();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }
        public void NotifyPropertyChanged(string propertyName)
        {
            GenericTools.RunUI(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
        private readonly System.Timers.Timer reloadTimer = null;
        private static RobotInstance _instance = null;
        private List<AutoResetEvent> WaitingSignin = new List<AutoResetEvent>();
        public async Task<Boolean> WaitForSignedIn(TimeSpan Timeout)
        {
            await Connect();
            if (global.webSocketClient.user != null && global.webSocketClient.isConnected) return true;
            var arevent = new AutoResetEvent(false);
            WaitingSignin.Add(arevent);
            bool wasraised = await arevent.WaitOneAsync(Timeout, CancellationToken.None);
            return wasraised;
        }
        public static RobotInstance instance
        {
            get
            {
                if (_instance == null)
                {
                    // System.Diagnostics.Debugger.Launch();
                    
                    if(AutomationHelper.syncContext == null) AutomationHelper.syncContext = System.Threading.SynchronizationContext.Current;
                    if (AutomationHelper.syncContext == null) System.Windows.MessageBox.Show("Failed locating UI thread!!!");
                    _instance = new RobotInstance();
                    _ = _instance.init();
                    global.OpenRPAClient = _instance;
                    // Interfaces.IPCService.OpenRPAServiceUtil.InitializeService();
                }
                return _instance;
            }
        }
        int ReconnectDelay = 0;
        private int connect_attempts = 0;
        async internal Task Connect()
        {
            try
            {
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Connecting) return;
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Open) return;
                await Task.Delay(ReconnectDelay);
                ReconnectDelay += 5000;
                if (ReconnectDelay > 60000 * 2) ReconnectDelay = 60000 * 2;
                connect_attempts++;
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Connecting) return;
                if (global.webSocketClient != null && global.webSocketClient.ws != null && global.webSocketClient.ws.State == System.Net.WebSockets.WebSocketState.Open) return;
                await global.webSocketClient.Connect();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                _ = Connect();
            }
        }
        public bool isReadyForAction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool isRunningInChildSession => throw new NotImplementedException();
        public IMainWindow Window { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDesigner CurrentDesigner => throw new NotImplementedException();
        public IDesigner[] Designers => throw new NotImplementedException();
        public List<IWorkflowInstance> WorkflowInstances => throw new NotImplementedException();
        private RobotInstance()
        {
            reloadTimer = new System.Timers.Timer(Config.local.reloadinterval.TotalMilliseconds);
            reloadTimer.Elapsed += ReloadTimer_Elapsed;
            reloadTimer.Stop();
        }
        private void ReloadTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            reloadTimer.Stop();
            _ = LoadServerData();
        }

        public IDesigner GetWorkflowDesignerByIDOrRelativeFilename(string IDOrRelativeFilename)
        {
            throw new NotImplementedException();
        }

        public IWorkflow GetWorkflowByIDOrRelativeFilename(string IDOrRelativeFilename)
        {
            throw new NotImplementedException();
        }

        public IWorkflowInstance GetWorkflowInstanceByInstanceId(string InstanceId)
        {
            throw new NotImplementedException();
        }

        public void ParseCommandLineArgs(IList<string> args)
        {
            throw new NotImplementedException();
        }

        private async void RobotInstance_WebSocketClient_OnOpen()
        {
            Log.FunctionIndent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
            try
            {
                Connected?.Invoke();
                ReconnectDelay = 5000;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Interfaces.entity.TokenUser user = null;
            try
            {
                string url = "http";
                var u = new Uri(Config.local.wsurl);
                if (u.Scheme == "wss" || u.Scheme == "https") url = "https";
                url = url + "://" + u.Host;
                if (!u.IsDefaultPort) url = url + ":" + u.Port.ToString();
                // App.notifyIcon.ShowBalloonTip(5000, "tooltiptitle", "tipMessage", System.Windows.Forms.ToolTipIcon.Info);
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Log.Debug("RobotInstance_WebSocketClient_OnOpen::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                while (user == null)
                {
                    string errormessage = string.Empty;
                    if (!string.IsNullOrEmpty(Config.local.username) && Config.local.password != null && Config.local.password.Length > 0)
                    {
                        try
                        {
                            Log.Debug("Signing in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.username, Config.local.UnprotectString(Config.local.password), clientagent: "workitemclient");
                            Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RobotInstance.RobotInstance_WebSocketClient_OnOpen.userlogin: " + ex.Message);
                            errormessage = ex.Message;
                        }
                    }
                    if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                    {
                        try
                        {
                            if (global.webSocketClient == null || !global.webSocketClient.isConnected) return;
                            Log.Debug("Signing in with token " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                            user = await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt), clientagent: "workitemclient");
                            if (user == null)
                            {
                                return;
                            }
                            Config.local.username = user.username;
                            Config.local.password = new byte[] { };
                            // Config.Save();
                            Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RobotInstance.RobotInstance_WebSocketClient_OnOpen.tokenlogin: " + ex.Message);
                            errormessage = ex.Message;
                        }
                    }
                    if (global.webSocketClient == null || !global.webSocketClient.isConnected) return;
                    if (user == null && global.webSocketClient.isConnected && !global.webSocketClient.signedin)
                    {
                        string jwt = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(jwt))
                            {
                                Config.local.jwt = Config.local.ProtectString(jwt);
                                user = await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt), clientagent: "workitemclient");
                                if (user != null)
                                {
                                    Config.local.username = user.username;
                                    Config.Save();
                                    Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                }
                            }
                            else if (Config.local.jwt != null && Config.local.jwt.Length > 0)
                            {
                                // user closed window or login failed,
                                // try once more with the jwt from the config file incase it was a network issue and login window is still open
                                // else, we assume user closed the window and wants openrpa to close as well
                                try
                                {
                                    user = await global.webSocketClient.Signin(Config.local.UnprotectString(Config.local.jwt), clientagent: "workitemclient");
                                    if (user != null)
                                    {
                                        Config.local.username = user.username;
                                        Config.Save();
                                        Log.Debug("Signed in as " + Config.local.username + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                    }
                                }
                                catch (Exception)
                                {
                                    return;
                                }
                            }
                            else
                            {

                                if (global.webSocketClient != null && global.webSocketClient.isConnected && global.webSocketClient.user != null)
                                {
                                    user = global.webSocketClient.user;
                                    Config.local.username = user.username;
                                }
                                else
                                {
                                    Log.Debug("Call close " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message);
                        }
                        finally
                        {
                        }
                    }
                }
                Log.Debug("RobotInstance_WebSocketClient_OnOpen::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));

                if (global.webSocketClient != null && global.webSocketClient.isConnected && global.webSocketClient.user != null)
                {
                    foreach(var e in WaitingSignin)
                    {
                        e.Set();
                    }
                }
                    

                System.Diagnostics.Process.GetCurrentProcess().PriorityBoostEnabled = true;
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
                try
                {
                    Signedin?.Invoke(user);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            try
            {
                if (global.openflowconfig != null && global.openflowconfig.supports_watch && global.webSocketClient != null && global.webSocketClient.isConnected && global.webSocketClient.signedin)
                {
                    await global.webSocketClient.Watch("openrpa",
                        "[\"$.[?(@ && @._type == 'workflow')]\", \"$.[?(@ && @._type == 'project')]\", \"$.[?(@ && @._type == 'detector')]\"]", onWatchEvent);
                    await global.webSocketClient.Watch("mq", "[\"$.[?(@ && @._type == 'workitemqueue')]\"]", onWatchEvent);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            try
            {
                _ = LoadServerData();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            Log.FunctionOutdent("RobotInstance", "RobotInstance_WebSocketClient_OnOpen");
        }
        private async void WebSocketClient_OnClose(string reason)
        {
            try
            {
                Log.FunctionIndent("RobotInstance", "WebSocketClient_OnClose", reason);
                if (global.webSocketClient != null && global.webSocketClient.isConnected) Log.Information("Disconnected " + reason);
                try
                {
                    Disconnected?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            _ = Connect();
            Log.FunctionOutdent("RobotInstance", "WebSocketClient_OnClose");
        }
        protected internal async void WebSocketClient_OnQueueClosed(IQueueClosedMessage message, QueueMessageEventArgs e)
        {
        }
        private async void WebSocketClient_OnQueueMessage(IQueueMessage message, QueueMessageEventArgs e)
        {
            Log.FunctionIndent("RobotInstance", "WebSocketClient_OnQueueMessage");
            Log.FunctionOutdent("RobotInstance", "WebSocketClient_OnQueueMessage");
        }
        public bool AutoReloading
        {
            get
            {
                return reloadTimer.Enabled;
            }
            set
            {
                if (global.openflowconfig != null && !global.openflowconfig.supports_watch)
                {
                    if (reloadTimer.Enabled = value)
                    {
                        reloadTimer.Stop();
                        reloadTimer.Start();
                        return;
                    }
                    if (value == true) reloadTimer.Start();
                    if (value == false) reloadTimer.Stop();
                }
                else
                {
                    reloadTimer.Stop();
                }
            }
        }
        public async Task LoadServerData()
        {
            Log.Debug("LoadServerData::begin");
            try
            {
                Log.FunctionIndent("RobotInstance", "LoadServerData");
                if (!global.isSignedIn)
                {
                    Log.FunctionOutdent("RobotInstance", "LoadServerData", "Not signed in");
                    return;
                }

                Log.Debug("LoadServerData::query workitemqueues versions");
                var server_workitemqueues = await global.webSocketClient.Query<WorkitemQueue>("mq", "{\"_type\": 'workitemqueue'}", "{\"_version\": 1}");
                var local_workitemqueues = WorkItemQueues.ToList();
                var reload_ids = new List<string>();
                foreach (var WorkItemQueue in server_workitemqueues)
                {
                    try
                    {
                        var exists = local_workitemqueues.Where(x => x._id == WorkItemQueue._id).FirstOrDefault();
                        if (exists != null)
                        {
                            if (exists._version < WorkItemQueue._version) reload_ids.Add(WorkItemQueue._id);
                            if (exists._version > WorkItemQueue._version && WorkItemQueue.isDirty)
                            {
                                await exists.Update(WorkItemQueue, true);
                            }
                        }
                        else
                        {
                            reload_ids.Add(WorkItemQueue._id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                foreach (var WorkItemQueue in local_workitemqueues)
                {
                    try
                    {
                        var exists = server_workitemqueues.Where(x => x._id == WorkItemQueue._id).FirstOrDefault();
                        if (exists == null)
                        {
                            var _id = WorkItemQueue._id;
                            Log.Debug("Removing local WorkItemQueue " + WorkItemQueue.name);
                            await WorkItemQueue.Delete(true);
                        }
                        else if (WorkItemQueue.isDirty)
                        {
                            if (WorkItemQueue.isDeleted)
                            {
                                if (exists != null)
                                {
                                    await exists.Delete(true);
                                }
                            }
                            if (!WorkItemQueue.isDeleted)
                            {
                                reload_ids.Add(exists._id);
                                // await WorkItemQueue.Update(exists, true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }
                }
                if (reload_ids.Count > 0)
                {
                    for (var i = 0; i < reload_ids.Count; i++) reload_ids[i] = "'" + reload_ids[i] + "'";
                    var q = "{ _type: 'workitemqueue', '_id': {'$in': [" + string.Join(",", reload_ids) + "]}}";
                    Log.Debug("LoadServerData::Featching fresh version of ´" + reload_ids.Count + " workitemqueues");
                    server_workitemqueues = await global.webSocketClient.Query<WorkitemQueue>("mq", q, orderby: "{\"name\":-1}");
                    foreach (var workitemqueue in server_workitemqueues)
                    {
                        var exists = local_workitemqueues.Where(x => x._id == workitemqueue._id).FirstOrDefault();
                        // workitemqueue.isDirty = false;
                        if (exists != null)
                        {
                            await exists.Update(workitemqueue, true);
                        }
                        else
                        {
                            await workitemqueue.Save(true);
                        }
                    }
                }
                GenericTools.RunUI(() =>
                {
                    NotifyPropertyChanged("FilterText");
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                AutoReloading = true;
                Log.Debug("LoadServerData::end");
            }
        }
        private async void onWatchEvent(string id, Newtonsoft.Json.Linq.JObject data)
        {
            try
            {
                string _type = data["fullDocument"].Value<string>("_type");
                string _id = data["fullDocument"].Value<string>("_id");
                string operationType = data.Value<string>("operationType");
                Log.Debug("WatchEvent: " + _type + " with id " + _id + " operation: " + operationType);
                long _version = data["fullDocument"].Value<long>("_version");
                string collection = "";
                if (data.ContainsKey("ns"))
                {
                    var ns = data["ns"].Value<JObject>();
                    if (ns.ContainsKey("coll"))
                    {
                        collection = data["ns"].Value<string>("coll");
                    }
                }
                if (string.IsNullOrEmpty(collection)) collection = "openrpa";
                if (operationType != "replace" && operationType != "insert" && operationType != "update" && operationType != "delete") return;
                if (_type == "workitemqueue" && collection == "mq")
                {
                    var wiq = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkitemQueue>(data["fullDocument"].ToString());
                    wiq.isDirty = false;
                    GenericTools.RunUI(async () =>
                    {
                        try
                        {
                            IWorkitemQueue exists = null;
                            if (System.Threading.Monitor.TryEnter(WorkItemQueues, Config.local.thread_lock_timeout_seconds * 1000))
                            {
                                try
                                {
                                    exists = WorkItemQueues.FindById(_id);
                                }
                                finally
                                {
                                    System.Threading.Monitor.Exit(WorkItemQueues);
                                }
                            }
                            if (operationType == "delete")
                            {
                                if (exists == null) return;
                                await exists.Delete(true);
                                return;
                            }
                            if (exists != null && wiq._version != exists._version)
                            {
                                EnumerableExtensions.CopyPropertiesTo(wiq, exists, true);
                                exists.isDirty = false;
                                await wiq.Save(true);
                            }
                            else if (exists == null)
                            {
                                await wiq.Save(true);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
