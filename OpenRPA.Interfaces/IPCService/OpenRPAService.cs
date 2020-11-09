using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.IPCService
{
    public static class OpenRPAServiceUtil
    {
        private const string Delimiter = ":";
        private const string ChannelNameSuffix = "OpenRPAIPCChannel";
        private const string RemoteServiceName = "OpenRPAService";
        private const string IpcProtocol = "ipc://";
        private static IpcServerChannel channel;
        private static System.Threading.Mutex OpenRPAMutex;
        /// <summary>
        /// Cleans up single-instance code, clearing shared resources, mutexes, etc.
        /// </summary>
        public static void Cleanup()
        {
            if (OpenRPAMutex != null)
            {
                OpenRPAMutex.Close();
                OpenRPAMutex = null;
            }

            if (channel != null)
            {
                ChannelServices.UnregisterChannel(channel);
                channel = null;
            }
        }
        private static string GetUsersGroupName()
        {
            const string builtInUsersGroup = "S-1-5-32-545";
            var sid = new System.Security.Principal.SecurityIdentifier(builtInUsersGroup);
            var ntAccount = (System.Security.Principal.NTAccount)sid.Translate(typeof(System.Security.Principal.NTAccount));
            return ntAccount.Value;
        }
        private static void CreateRemoteService(string channelName)
        {
            BinaryServerFormatterSinkProvider serverProvider = new BinaryServerFormatterSinkProvider();
            serverProvider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Dictionary<string, string>();

            props["authorizedGroup"] = GetUsersGroupName();
            props["name"] = channelName;
            props["portName"] = channelName;
            props["exclusiveAddressUse"] = "false";

            // Create the IPC Server channel with the channel properties
            channel = new IpcServerChannel(props, serverProvider);

            // Register the channel with the channel services
            ChannelServices.RegisterChannel(channel, true);

            // Expose the remote service with the REMOTE_SERVICE_NAME
            RemoteInstance = new OpenRPAService();
            RemotingServices.Marshal(RemoteInstance, RemoteServiceName);
            RemoteInstance.Ping();
        }
        /// <summary>
        /// Checks if the instance of the application attempting to start is the first instance. 
        /// If not, activates the first instance.
        /// </summary>
        /// <returns>True if this is the first instance of the application.</returns>
        public static bool InitializeService(string uniqueName = "OpenRPAService")
        {
            // Build unique application Id and the IPC channel name.
            string applicationIdentifier = uniqueName + Environment.UserName;
            string channelName = String.Concat(applicationIdentifier, Delimiter, ChannelNameSuffix);
            // Create mutex based on unique application Id to check if this is the first instance of the application. 
            bool firstInstance;
            OpenRPAMutex = new Mutex(true, applicationIdentifier, out firstInstance);
            if (firstInstance)
            {
                CreateRemoteService(channelName);
            } 
            return firstInstance;
        }
        public static OpenRPAService RemoteInstance;
        public static IpcClientChannel secondInstanceChannel;
        public static void GetInstance(string uniqueName = "OpenRPAService")
        {

            if (RemoteInstance != null)
            {
                try
                {
                    RemoteInstance.Ping();
                    return;
                }
                catch (Exception)
                {
                }
            }
            //if (secondInstanceChannel == null)
            //{
            //    secondInstanceChannel = new IpcClientChannel();
            //    ChannelServices.RegisterChannel(secondInstanceChannel, true);
            //}
            try
            {
                IpcClientChannel secondInstanceChannel = new IpcClientChannel();
                ChannelServices.RegisterChannel(secondInstanceChannel, true);
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.ToString());
            }
            string applicationIdentifier = uniqueName + Environment.UserName;
            string channelName = String.Concat(applicationIdentifier, Delimiter, ChannelNameSuffix);
            string remotingServiceUrl = IpcProtocol + channelName + "/" + RemoteServiceName;
            // Obtain a reference to the remoting service exposed by the server i.e the first instance of the application
            RemoteInstance = (OpenRPAService)RemotingServices.Connect(typeof(OpenRPAService), remotingServiceUrl);
            RemoteInstance.Ping();
        }
    }
    //public interface IOpenRPAService
    //{
    //    void ParseCommandLineArgs(IList<string> args);
    //}
    public class RunWorkflowInstance
    {
        public RunWorkflowInstance() { }
        public RunWorkflowInstance(string UniqueId, string IDOrRelativeFilename, bool WaitForCompleted, Dictionary<string, object> Arguments) {
            this.UniqueId = UniqueId;
            this.IDOrRelativeFilename = IDOrRelativeFilename;
            this.WaitForCompleted = WaitForCompleted;
            this.Arguments = Arguments;
            Started = false;
            Pending = new System.Threading.AutoResetEvent(false);
        }
        public string UniqueId { get; set; }
        public bool Started { get; set; }
        public string IDOrRelativeFilename { get; set; }
        public bool WaitForCompleted { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
        public AutoResetEvent Pending { get; set; }
        public Dictionary<string, object> Result { get; set; }
        public Exception Error { get; set; }
    }
    public class OpenRPAService : MarshalByRefObject //, IOpenRPAService
    {
        public void ParseCommandLineArgs(IList<string> args)
        {
            global.OpenRPAClient.ParseCommandLineArgs(args);
        }
        public static Dictionary<string, RunWorkflowInstance> RunWorkflowInstances = new Dictionary<string, RunWorkflowInstance>();
        private System.Timers.Timer pendingTimer = null;
        public void StartWorkflowInstances()
        {
            if(global.OpenRPAClient== null || !global.OpenRPAClient.isReadyForAction)
            {
                if(pendingTimer == null)
                {
                    pendingTimer = new System.Timers.Timer(500);
                    pendingTimer.Elapsed += (e, r) =>
                    {
                        pendingTimer.Stop();
                        if (global.OpenRPAClient == null || !global.OpenRPAClient.isReadyForAction)
                        {
                            pendingTimer.Start();
                            return;
                        }
                        StartWorkflowInstances();
                        pendingTimer = null;
                    };
                    pendingTimer.AutoReset = false;
                    pendingTimer.Start();
                }
                return;
            }
            foreach(var _instance in RunWorkflowInstances.ToList())
            {
                if(!_instance.Value.Started)
                {
                    try
                    {
                        _instance.Value.Started = true;
                        var workflow = global.OpenRPAClient.GetWorkflowByIDOrRelativeFilename(_instance.Value.IDOrRelativeFilename);
                        IWorkflowInstance instance = null;
                        IDesigner designer = null;
                        GenericTools.RunUI(() =>
                        {
                            try
                            {
                                designer = global.OpenRPAClient.GetWorkflowDesignerByIDOrRelativeFilename(_instance.Value.IDOrRelativeFilename);
                                if (designer != null)
                                {
                                    designer.BreakpointLocations = null;
                                    // instance = workflow.CreateInstance(Arguments, null, null, designer.IdleOrComplete, designer.OnVisualTracking);
                                    instance = workflow.CreateInstance(_instance.Value.Arguments, null, null, IdleOrComplete, designer.OnVisualTracking);
                                }
                                else
                                {
                                    instance = workflow.CreateInstance(_instance.Value.Arguments, null, null, IdleOrComplete, null);
                                }
                                instance.caller = _instance.Value.UniqueId;

                            }
                            catch (Exception ex)
                            {
                                _instance.Value.Error = ex;
                                if (_instance.Value.Pending != null) _instance.Value.Pending.Set();
                            }
                            if (designer != null)
                            {
                                designer.Run(designer.VisualTracking, designer.SlowMotion, instance);
                            }
                            else
                            {
                                if (instance != null) instance.Run();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _instance.Value.Error = ex;
                        if (_instance.Value.Pending != null) _instance.Value.Pending.Set();
                    }

                }
            }
        }
        public Dictionary<string, object> RunWorkflowByIDOrRelativeFilename(string IDOrRelativeFilename, bool WaitForCompleted, Dictionary<string, object> Arguments)
        {
            string uniqueid = Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
            if (Arguments == null) Arguments = new Dictionary<string, object>();
            var _instance = new RunWorkflowInstance(uniqueid, IDOrRelativeFilename, WaitForCompleted, Arguments);
            RunWorkflowInstances.Add(uniqueid, _instance);
            StartWorkflowInstances();
            if(WaitForCompleted) _instance.Pending.WaitOne();
            if (_instance.Error != null) throw _instance.Error;
            return _instance.Result;
        }
        public void IdleOrComplete(IWorkflowInstance instance, EventArgs e)
        {
            if (string.IsNullOrEmpty(instance.caller)) return;
            if(!RunWorkflowInstances.ContainsKey(instance.caller)) return;
            if (instance.isCompleted || instance.Exception != null)
            {
                var _instance = RunWorkflowInstances[instance.caller];
                if (instance.Parameters != null)
                {
                    _instance.Result = instance.Parameters;
                } else { _instance.Result = new Dictionary<string, object>(); }
                RunWorkflowInstances.Remove(instance.caller);
                if (!string.IsNullOrEmpty(instance.errormessage)) _instance.Error = new Exception(instance.errormessage);
                if (instance.Exception != null) _instance.Error = instance.Exception;
                _instance.Pending.Set();
                GenericTools.RunUI(() =>
                {
                    // if ran in designer, call IdleOrComplete to break out of debugging and make designer not readonly
                    var designer = global.OpenRPAClient.GetWorkflowDesignerByIDOrRelativeFilename(_instance.IDOrRelativeFilename);
                    try
                    {
                        if (designer != null) designer.IdleOrComplete(instance, e);
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }
        public string Ping() { return "pong"; }
        /// <summary>
        /// Remoting Object's ease expires after every 5 minutes by default. We need to override the InitializeLifetimeService class
        /// to ensure that lease never expires.
        /// </summary>
        /// <returns>Always null.</returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
