using OpenRPA.Interfaces;
using OpenRPA.NamedPipeWrapper;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.SAP
{
    public class SAPhook
    {
        public delegate void SAPHandler();
        public delegate void SAPHandlerEvent(SAPEvent @event);
        public event SAPHandler Connected;
        public event SAPHandler Disconnected; 
        public NamedPipeClient<SAPEvent> pipeclient = null;

        public Action<SAPElement> OnRecordEvent;
        private static SAPhook _instance = null;
        private SAPEventElement LastEventElement = null;
        public SAPElement LastElement = null;
        public static SAPhook Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SAPhook();
                    _instance.init();
                }
                return _instance;
            }
        }
        public bool Initilized { get; set; } = false;
        public void init()
        {
            if (PluginConfig.auto_launch_sap_bridge)
            {
                EnsureSAPBridge();
            }
            if (pipeclient == null)
            {
                var SessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                pipeclient = new NamedPipeClient<SAPEvent>(SessionId + "_openrpa_sapbridge");
                pipeclient.ServerMessage += Pipeclient_ServerMessage;
                pipeclient.Connected += Pipeclient_Connected;
                pipeclient.Disconnected += Pipeclient_Disconnected;
                pipeclient.AutoReconnect = true;
                pipeclient.Start();
            }
            if (Initilized) return;

        }
        private void Pipeclient_Disconnected(NamedPipeConnection<SAPEvent, SAPEvent> connection)
        {
            Disconnected?.Invoke();
        }
        private void Pipeclient_Connected(NamedPipeConnection<SAPEvent, SAPEvent> connection)
        {
            Connected?.Invoke();
        }
        private void Pipeclient_ServerMessage(NamedPipeConnection<SAPEvent, SAPEvent> connection, SAPEvent message)
        {
            try
            {
                if (message.action == "recorderevent")
                {
                    var data = message.Get<SAPRecordingEvent>();
                    if (data != null)
                    {
                        Log.Debug(message.action + " " + data.ActionName + " " + data.Id);
                    }
                    else
                    {
                        Log.Debug(message.action);
                    }

                    var r = new RecordEvent();
                    r.SupportInput = false;
                    r.SupportSelect = false;
                    r.ClickHandled = true;
                    if (data.Action == "InvokeMethod")
                    {
                        var a = new InvokeMethod();
                        a.Path = data.Id; a.ActionName = data.ActionName; a.SystemName = data.SystemName;
                        if(data.Parameters != null)
                        {
                            a.Parameters = data.Parameters;
                            for (var i = 0; i < data.Parameters.Length; i++)
                            {
                                var name = "param" + i.ToString();
                            }
                        }
                        r.a = new GetElementResult(a);
                    }
                    if (data.Action == "SetProperty")
                    {
                        var a = new SetProperty();
                        a.Path = data.Id; a.ActionName = data.ActionName; a.SystemName = data.SystemName;
                        if (data.Parameters != null)
                        {
                            a.Parameters = data.Parameters;
                            for (var i = 0; i < data.Parameters.Length; i++)
                            {
                                var name = "param" + i.ToString();

                            }
                        }
                        r.a = new GetElementResult(a);
                    }
                    if(r != null)
                    {
                        Plugin.Instance.RaiseUserAction(r);
                    }
                }
                if (message.action == "mousedown")
                {
                    LastEventElement = message.Get<SAPEventElement>();
                    LastElement = new SAPElement(null, LastEventElement);
                    Log.Output("SAP mousedown on " + LastElement.id);
                }
                if (message.action == "mousemove")
                {
                    LastEventElement = message.Get<SAPEventElement>();
                    LastElement = new SAPElement(null, LastEventElement);
                    Log.Output("SAP mousemove on " + LastElement.id);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            try
            {
                if (replyqueue.ContainsKey(message.messageid))
                {
                    lock (replyqueue)
                    {
                        var e = replyqueue[message.messageid];
                        e.reply = message;
                        if (e.reset != null) e.reset.Set();
                        replyqueue.Remove(message.messageid);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public void SendMessage(SAPEvent message)
        {
            pipeclient.PushMessage(message);
        }
        public class replyqueueitem
        {
            public replyqueueitem(SAPEvent message) { this.message = message; reset = new System.Threading.AutoResetEvent(false); }
            public System.Threading.AutoResetEvent reset { get; set; }
            public SAPEvent message { get; set; }
            public SAPEvent reply { get; set; }
            public override string ToString()
            {
                try
                {
                    return message.action + " " + message.messageid;
                }
                catch (Exception)
                {
                }
                return base.ToString();
            }
        }
        private Dictionary<string, replyqueueitem> replyqueue = new Dictionary<string, replyqueueitem>();
        public SAPSession[] Sessions { get; private set; }
        public SAPConnection[] Connections { get; private set; }
        public bool isSapRunning { get; private set; } = false;
        public void RefreshConnections()
        {
            var msg = new SAPEvent("getconnections");
            msg = SAPhook.Instance.SendMessage(msg, TimeSpan.FromSeconds(5));
            if (msg != null)
            {
                isSapRunning = true;
                var sessions = new List<SAPSession>();
                var data = msg.Get<SAPGetSessions>();
                Connections = data.Connections;
                foreach(var con in Connections)
                {
                    foreach(var ses in con.sessions) sessions.Add(ses);
                }
                Sessions = sessions.ToArray();
            }
            else
            {
                isSapRunning = false;
                Sessions = new SAPSession[] { };
                Connections = new SAPConnection[] { };
            }
        }
        public bool isConnected
        {
            get
            {
                if (pipeclient == null) return false;
                return pipeclient.isConnected;
            }
        }
        public SAPEvent SendMessage(SAPEvent message, TimeSpan timeout)
        {
            if (!isConnected) throw new Exception("Pipe not connected to SAP bridge");
            if (string.IsNullOrEmpty(message.messageid)) throw new ArgumentException("message id is mandatory", "messageid");
            if (replyqueue.ContainsKey(message.messageid)) throw new Exception("Already waiting on message with id " + message.messageid);
            var e = new replyqueueitem(message);
            lock (replyqueue)
            {
                replyqueue.Add(message.messageid, e);
            }
            Log.Debug("Sending: " + message.messageid + " " + message.action);
            pipeclient.PushMessage(message);
            if (timeout == TimeSpan.Zero) e.reset.WaitOne(); else e.reset.WaitOne(timeout);
            Log.Debug("Received reply: " + message.messageid + " " + message.action);
            if (e.reply != null && e.reply.error != null && !string.IsNullOrEmpty(e.reply.error.ToString())) throw new Exception(e.reply.error.ToString());
            return e.reply;
        }
        public void SendMessage(string action)
        {
            pipeclient.PushMessage(new SAPEvent(action));
        }
        public static void EnsureSAPBridge()
        {
            bool isrunning = false;
            var me = System.Diagnostics.Process.GetCurrentProcess();
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("OpenRPA.SAPBridge"))
            {
                if (process.Id != me.Id && process.SessionId == me.SessionId)
                {
                    isrunning = true;
                }
            }
            if (!isrunning)
            {
                var filename = System.IO.Path.Combine(Interfaces.Extensions.PluginsDirectory, "OpenRPA.SAPBridge.exe");
                if (!System.IO.File.Exists(filename))
                {
                    filename = System.IO.Path.Combine(Interfaces.Extensions.PluginsDirectory, "SAP\\OpenRPA.SAPBridge.exe");
                }
                if (System.IO.File.Exists(filename))
                {
                    try
                    {
                        var _childProcess = new System.Diagnostics.Process();
                        _childProcess.StartInfo.FileName = filename;
                        _childProcess.StartInfo.UseShellExecute = false;
                        if (!_childProcess.Start())
                        {
                            throw new Exception("Failed starting OpenRPA SAPBridge");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed launching OpenRPA.SAPBridge.exe");
                    }
                }
            }
        }

    }
}
