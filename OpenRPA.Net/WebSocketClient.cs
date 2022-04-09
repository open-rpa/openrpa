using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// using System.Net.WebSockets.Managed;
using System.Net.WebSockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class WebSocketClient : IWebSocketClient
    {
        static SemaphoreSlim ProcessingSemaphore = new SemaphoreSlim(1, 1);
        static SemaphoreSlim SendStringSemaphore = new SemaphoreSlim(1, 1);
        public WebSocket ws { get; private set; } = null;
        public int websocket_package_size = 4096;
        public string url { get; set; }
        private CancellationTokenSource src = new CancellationTokenSource();
        private static object _sendQueuelock = new object();
        private List<SocketMessage> _receiveQueue = new List<SocketMessage>();
        private List<SocketMessage> _sendQueue = new List<SocketMessage>();
        private List<QueuedMessage> _messageQueue = new List<QueuedMessage>();
        public event Action OnOpen;
        public event Action<string> OnClose;
        public event QueueMessageDelegate OnQueueMessage;
        public event QueueClosedDelegate OnQueueClosed;
        public int MessageQueueSize { get { return _messageQueue.Count; } }
        public TokenUser user { get; set; }
        public bool signedin { get; private set; }
        public string jwt { get; private set; }
        public bool isConnected
        {
            get
            {
                if (ws == null) return false;
                if (ws.State != System.Net.WebSockets.WebSocketState.Open) return false;
                return true;
            }
        }
        private WebSocketClient() { }
        public string id = Guid.NewGuid().ToString();
        private static WebSocketClient instance = null;
        public static WebSocketClient Get(string url)
        {
            if (instance == null) instance = new WebSocketClient(url);
            return instance;
        }
        private WebSocketClient(string url)
        {
            this.url = url;
            signedin = false;
        }
        public async Task Connect()
        {
            try
            {
                Log.Network("Connecting to " + url);
                //if (ws != null && (ws.State == System.Net.WebSockets.WebSocketState.Aborted || ws.State == System.Net.WebSockets.WebSocketState.Closed))
                if (ws != null && (ws.State != WebSocketState.Connecting))
                {
                    try
                    {
                        ws.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                    ws = null;
                }
                if (ws == null)
                {
                    // ws = (ClientWebSocket)SystemClientWebSocket.CreateClientWebSocket();
                    if (VersionHelper.IsWindows8OrGreater())
                    {
                        ws = new ClientWebSocket();
                    }
                    else
                    {
                        ws = new System.Net.WebSockets.Managed.ClientWebSocket();
                    }
                    src = new CancellationTokenSource();
                }
                if (ws.State == System.Net.WebSockets.WebSocketState.Connecting || ws.State == System.Net.WebSockets.WebSocketState.Open) return;
                if (ws.State == System.Net.WebSockets.WebSocketState.CloseReceived)
                {
                    signedin = false;
                    OnClose?.Invoke("Socket closing");
                    ws.Dispose();
                    ws = null;
                    return;
                }
                Log.Network("Connecting to " + url);
                await ws.ConnectAsync(new Uri(url), src.Token);
                Log.Information("Connected to " + url);
                tempbuffer = "";
                Task receiveTask = Task.Run(async () => await receiveLoop(), src.Token);
                Task pingTask = Task.Run(async () => await PingLoop(), src.Token);
                OnOpen?.Invoke();
            }
            catch (Exception ex)
            {
                signedin = false;
                OnClose?.Invoke(ex.Message);
            }
        }
        public async Task Close()
        {
            if (ws != null)
            {
                try
                {
                    await ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.Empty, "", src.Token);
                }
                catch (Exception)
                {
                }
                try
                {
                    ws.Dispose();
                }
                catch (Exception)
                {
                }
                //ws = null;
            }
            signedin = false;
            lock (_sendQueuelock) _sendQueue.Clear();
            src.Cancel();
        }
        string tempbuffer = null;
        private static object lockobject = new object();
        public static DateTime lastmessage = DateTime.Now;
        private async Task receiveLoop()
        {
            var FileName = System.IO.Path.Combine(OpenRPA.Interfaces.Extensions.ProjectsDirectory, "network.txt");
            byte[] buffer = new byte[websocket_package_size];
            while (true)
            {
                string json = string.Empty;
                System.Net.WebSockets.WebSocketReceiveResult result;
                try
                {
                    if (ws == null) { return; }
                    if (ws.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        signedin = false;
                        OnClose?.Invoke("");
                        return;
                    }
                    result = ws.ReceiveAsync(new ArraySegment<byte>(buffer), src.Token).Result;
                    bool foundone = false;
                    lock (lockobject)
                    {
                        json = Encoding.UTF8.GetString(buffer.Take(result.Count).ToArray());
                        if (lastmessage.AddMinutes(10) < DateTime.Now && !string.IsNullOrEmpty(tempbuffer))
                        {
                            //Log.Error("Recevied no ping/message for more than a minute, clearing local buffer!");
                            //tempbuffer = "";
                        }

                        var serializer = new JsonSerializer();
                        var workingjson = tempbuffer + json;
                        // System.IO.File.AppendAllText(FileName, json + Environment.NewLine);


                        if ((workingjson.StartsWith("{") && workingjson.EndsWith("}")) || (workingjson.StartsWith("[") && workingjson.EndsWith("]")))
                        {
                            //int begincount = System.Text.RegularExpressions.Regex.Matches(workingjson, "{").Count;
                            //int endcount = System.Text.RegularExpressions.Regex.Matches(workingjson, "}").Count;
                            //if (begincount == endcount)
                            try
                            {
                                using (StringReader sr = new StringReader(workingjson))
                                using (JsonTextReader reader = new JsonTextReader(sr))
                                {
                                    reader.SupportMultipleContent = true;
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonToken.StartObject)
                                        {
                                            var message = serializer.Deserialize<SocketMessage>(reader);
                                            if (message != null && !string.IsNullOrEmpty(message.id) && !string.IsNullOrEmpty(message.command) && message.data != null)
                                            {
                                                lock (_receiveQueue)
                                                {
                                                    foundone = true;
                                                    tempbuffer += json;
                                                    tempbuffer = tempbuffer.Substring(reader.LinePosition);
                                                    if (message.index % 100 == 99) Log.Network("Adding " + message.id + " to receiveQueue " + (message.index + 1) + " of " + message.count);
                                                    _receiveQueue.Add(message);
                                                    lastmessage = DateTime.Now;
                                                }

                                            }
                                            else { tempbuffer += json; }
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                tempbuffer += json;
                            }
                        }
                        else { tempbuffer += json; }
                    }
                    if (foundone) await ProcessQueue();
                }
                catch (System.Net.WebSockets.WebSocketException ex)
                {
                    if (ws.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        signedin = false;
                        OnClose?.Invoke(ex.Message);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(json)) Log.Error(json);
                        Log.Error(ex.ToString());
                        await Task.Delay(3000);
                        // await this.Close();
                    }
                }
                catch (Exception ex)
                {
                    if (ws.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        signedin = false;
                        OnClose?.Invoke(ex.Message);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(json)) Log.Error(json);
                        Log.Error(ex.ToString());
                        await Task.Delay(3000);
                        //await this.Close();
                    }
                }
            }
        }
        private async Task PingLoop()
        {
            while (isConnected)
            {
                await Task.Delay(10000);
                var msg = new Message("ping");
                msg.SendMessage(this, 5);
            }
        }
        public async Task ProcessQueue()
        {
            try
            {
                await ProcessingSemaphore.WaitAsync();
                if (_receiveQueue == null) return;
                List<string> ids = new List<string>();
                lock (_receiveQueue)
                {
                    for (var i = 0; i < _receiveQueue.Count; i++)
                    {
                        if (_receiveQueue[i] != null)
                        {
                            string id = _receiveQueue[i].id;
                            if (!ids.Contains(id)) ids.Add(id);
                        }
                    }
                }

                // ids = (from m in _receiveQueue group m by new { m.id } into mygroup select mygroup.Key.id).ToList();
                foreach (var id in ids)
                {
                    SocketMessage first = null;
                    List<SocketMessage> msgs = null;
                    lock (_receiveQueue)
                    {
                        first = _receiveQueue.ToList().Where((x) => x.id == id).First();
                        msgs = _receiveQueue.ToList().Where((x) => x.id == id).ToList();
                    }
                    if (first.count == msgs.Count)
                    {
                        if (msgs.Count > 100) Log.Network("Stiching together " + first.count + " messages for message id " + first.id);
                        string data = "";
                        foreach (var m in msgs.OrderBy((y) => y.index))
                        {
                            data += m.data;
                        }
                        var result = new Message(first, data);
                        Log.Network("Processing message " + result.id);
                        lock (_receiveQueue)
                        {
                            foreach (var m in msgs.OrderBy((y) => y.index).ToList())
                            {
                                _receiveQueue.Remove(m);
                            }
                        }
                        Process(result);
                        Log.Network("Processing message " + result.id + " complete");
                    }
                }
            }
            finally
            {
            }

            try
            {
                List<SocketMessage> templist;
                lock (_sendQueuelock)
                {
                    templist = _sendQueue.OrderBy(x => x.priority).ToList();
                }
                foreach (var msg in templist)
                {
                    if (await SendString(JsonConvert.SerializeObject(msg), src.Token))
                    {
                        lock (_sendQueuelock) _sendQueue.Remove(msg);
                    }
                }
            }
            finally
            {
                ProcessingSemaphore.Release();
            }
        }
        private async Task<bool> SendString(string data, CancellationToken cancellation)
        {
            if (ws == null) { return false; }
            if (ws.State != WebSocketState.Open) return false;
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            try
            {
                await SendStringSemaphore.WaitAsync();
                await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
                return true;
            }
            catch (WebSocketException ex)
            {
                if(ws.State != WebSocketState.Open)
                {
                    Log.Error(ex.ToString());
                    // _ = Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            finally
            {
                SendStringSemaphore.Release();
            }
            return false;
        }
        public void PushMessage(SocketMessage msg)
        {
            lock (_sendQueuelock)
            {
                var exists = _sendQueue.Where(x => x.id == msg.id && x.index == msg.index).Count();
                if (exists == 0)
                {
                    _sendQueue.Add(msg);
                }
            }
            _ = ProcessQueue();
        }
        private void Process(Message msg)
        {
            if (!string.IsNullOrEmpty(msg.replyto))
            {
                if (msg.command != "pong") { Log.Network("(" + _messageQueue.Count + ") " + msg.command + " RESC: " + msg.replyto + "/" + msg.id); }
                // else { Log.Network(msg.command + " / replyto: " + msg.replyto);  }
                foreach (var qm in _messageQueue.ToList())
                {
                    if (qm != null && qm.msg.id == msg.replyto)
                    {
                        qm.reply = msg;
                        qm.autoReset.Set();
                        _messageQueue.Remove(qm);
                        break;
                    }
                }
            }
            else
            {
                if (msg.command != "ping" && msg.command != "refreshtoken") { Log.Network(msg.command + " / " + msg.id); }
                // else { Log.Network(msg.command + " / replyto: " + msg.replyto); }

                switch (msg.command)
                {
                    case "ping":
                        msg.reply("pong");
                        msg.SendMessage(this, 5);
                        break;
                    case "refreshtoken":
                        msg.reply();
                        var signin = JsonConvert.DeserializeObject<SigninMessage>(msg.data);
                        this.user = signin.user;
                        this.jwt = signin.jwt;
                        // msg.SendMessage(this); no need to confirm
                        if (signin.websocket_package_size > 100)
                        {
                            this.websocket_package_size = signin.websocket_package_size;
                        }
                        break;
                    case "queueclosed":
                        msg.reply();
                        QueueClosedMessage qc = null;
                        try
                        {
                            qc = JsonConvert.DeserializeObject<QueueClosedMessage>(msg.data);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            msg.SendMessage(this, 3);
                            break;
                        }
                        try
                        {
                            var e = new QueueMessageEventArgs();
                            OnQueueClosed?.Invoke(qc, e);
                            msg.data = JsonConvert.SerializeObject(qc);
                            if (e.isBusy)
                            {
                                msg.command = "error";
                                msg.data = "Sorry, I'm bussy";
                                msg.SendMessage(this, 3);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            msg.command = "error";
                            msg.data = ex.ToString();
                        }
                        msg.SendMessage(this, 3);
                        // if (string.IsNullOrEmpty(qm.replyto)) msg.SendMessage(this);
                        break;
                    case "queuemessage":
                        msg.reply();
                        QueueMessage qm = null;
                        try
                        {
                            qm = JsonConvert.DeserializeObject<QueueMessage>(msg.data);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            msg.SendMessage(this, 3);
                            break;
                        }
                        try
                        {
                            var e = new QueueMessageEventArgs();
                            OnQueueMessage?.Invoke(qm, e);
                            msg.data = JsonConvert.SerializeObject(qm);
                            if (e.isBusy)
                            {
                                msg.command = "error";
                                msg.data = "Sorry, I'm bussy";
                                msg.SendMessage(this, 3);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            msg.command = "error";
                            msg.data = ex.ToString();
                        }
                        msg.SendMessage(this, 3);
                        // if (string.IsNullOrEmpty(qm.replyto)) msg.SendMessage(this);
                        break;
                    case "watchevent":
                        msg.reply();
                        WatchEventMessage wem;
                        try
                        {
                            wem = JsonConvert.DeserializeObject<WatchEventMessage>(msg.data);
                            if (!string.IsNullOrEmpty(wem.id) && watches.ContainsKey(wem.id))
                            {
                                try
                                {
                                    watches[wem.id](wem.id, wem.result);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.ToString());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            msg.SendMessage(this, 3);
                            break;
                        }
                        //msg.SendMessage(this);
                        break;
                    default:
                        Log.Error("Received unknown command '" + msg.command + "' with data: " + msg.data);
                        break;
                }
            }
        }
        public async Task<Message> SendMessage(Message msg)
        {
            var qm = new QueuedMessage(msg);
            lock (_messageQueue)
            {
                _messageQueue.Add(qm);
            }
            try
            {
                using (qm.autoReset = new AutoResetEvent(false))
                {
                    while (qm.reply == null)
                    {
                        if (msg.command != "queuemessage" && msg.command != "insertone" && msg.command != "insertorupdateone" 
                         && msg.command != "savefile" && msg.command != "updateone" && msg.command != "deleteone" && msg.command != "deletemany" && msg.command != "query")
                        {
                            if (msg.sendcount == 0 && ws.State == WebSocketState.Open )
                            {
                                if(signedin || msg.command == "signin" )
                                {
                                    Log.Network("(" + _messageQueue.Count + ") " + msg.command + " SEND: " + msg.id);
                                    msg.SendMessage(this, 1);
                                }
                                else
                                {
                                    Log.Error("Gave up on " + qm.msg.id + " " + qm.msg.command + " not signed in (" + (_messageQueue.Count - 1)  + ")");
                                    lock (_messageQueue)
                                    {
                                        _messageQueue.Remove(qm);
                                    }
                                    throw new Exception("Not connected/signed in to OpenFlow");
                                }
                            } else
                            {
                                Log.Error("Gave up on " + qm.msg.id + " " + qm.msg.command + " not connected");
                                lock (_messageQueue)
                                {
                                    _messageQueue.Remove(qm);
                                }
                                throw new Exception("Not connected/signed in to OpenFlow");
                            }
                        } else
                        {
                            if (msg.sendcount > 0)
                            {
                                // for now, lets only retry these specefic commands
                                // insertmany skipped for now, always skip ping and poing and signin
                                if (signedin)
                                {
                                    //lock (_messageQueue)
                                    //{
                                    //    _messageQueue.Remove(qm);
                                    //    _messageQueue.Add(qm);
                                    //}
                                    Log.Network("(" + _messageQueue.Count + ") " + msg.command + " RSND: " + msg.id);
                                    msg.SendMessage(this, 3);
                                }
                                else
                                {
                                    Log.Warning("Message NOOP " + qm.msg.id + " " + qm.msg.command);
                                }
                            }
                            else if (signedin)
                            {
                                Log.Network("(" + _messageQueue.Count + ") " + msg.command + " SEND: " + msg.id);
                                msg.SendMessage(this, 3);
                            }
                        }
                        // Log.Information("WaitOneAsync(" + msg.id + " / " + msg.sendcount + ") " + msg.command);
                        bool wasraised = await qm.autoReset.WaitOneAsync(Config.local.network_message_timeout, CancellationToken.None);
                        // if (qm.reply == null || (!string.IsNullOrEmpty(qm.reply.data) && qm.reply.data.Contains("\"error\":\"Not signed in, and missing jwt\"")))
                        if (qm.reply != null && !string.IsNullOrEmpty(qm.reply.data) && qm.reply.data.Contains("Not signed in, and missing jwt"))
                        {
                            // Log.Information("WebSocketClient.SendMessage.Reply.data has Not signed in, and missing jwt, so closing connection (" + msg.command + ")");
                            // global.webSocketClient.Close();
                            // await Close();
                        }
                        if (qm.reply == null || (!string.IsNullOrEmpty(qm.reply.data) && qm.reply.data.Contains("jwt must be provided")))
                        {
                            qm.autoReset.Reset();
                            //qm.autoReset.Dispose();
                            //qm.autoReset = null;
                            //qm.autoReset = qm.autoReset = new AutoResetEvent(false);
                            if (msg.command == "insertorupdateone")
                            {
                                var data = JObject.Parse(msg.data);
                                var _id = data["item"].Value<string>("_id");
                                var state = data["item"].Value<string>("state");
                                if (state == "running" || state == "idle" || msg.sendcount > 50)
                                {
                                    lock (_messageQueue)
                                    {
                                        _messageQueue.Remove(qm);
                                    }
                                    return null;
                                }
                                Log.Warning("Message " + qm.msg.id + " (" + qm.msg.command + ") state " + state + " timed out, retrying state: ");
                            }
                            else
                            {
                                Log.Warning("Message " + qm.msg.id + " (" + qm.msg.command + ") timed out, retrying");
                            }
                            qm.reply = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("WebSocketClient.SendMessage: " + ex.Message);
            }
            if (msg.sendcount > 0 && qm.reply == null)
            {
                if (qm.msg.command == "queuemessage")
                {
                    // Log.Error("Gave up on " + qm.msg.id + " " + qm.msg.command);
                    throw new Exception("Gave up on " + qm.msg.id + " " + qm.msg.command);
                }
                else
                {
                    // Log.Error("Gave up on " + qm.msg.id + " " + qm.msg.command);
                    throw new Exception("Gave up on " + qm.msg.id + " " + qm.msg.command);
                }
            }
            return qm.reply as Message;
        }
        public async Task<TokenUser> Signin(string username, SecureString password, string clientagent = "", string clientversion = "")
        {
            SigninMessage signin = new SigninMessage(username, password, global.version);
            if (!string.IsNullOrEmpty(clientagent)) signin.clientagent = clientagent;
            if (!string.IsNullOrEmpty(clientversion)) signin.clientversion = clientversion;
            signin = await signin.SendMessage<SigninMessage>(this);
            if (!string.IsNullOrEmpty(signin.error)) throw new SocketException(signin.error);
            if (signin.user == null) throw new SocketException("signin failed, received a null user object");
            user = signin.user;
            jwt = signin.jwt;
            if (!string.IsNullOrEmpty(signin.openflow_uniqueid))
            {
                Config.local.openflow_uniqueid = signin.openflow_uniqueid;
                Config.local.enable_analytics = signin.enable_analytics;
            }
            if (!string.IsNullOrEmpty(signin.otel_trace_url)) Config.local.otel_trace_url = signin.otel_trace_url;
            if (!string.IsNullOrEmpty(signin.otel_metric_url)) Config.local.otel_metric_url = signin.otel_metric_url;
            if (signin.otel_trace_interval > 0) Config.local.otel_trace_interval = signin.otel_trace_interval;
            if (signin.otel_metric_interval > 0) Config.local.otel_metric_interval = signin.otel_metric_interval;
            if (signin.websocket_package_size > 100)
            {
                this.websocket_package_size = signin.websocket_package_size;
            }
            signedin = true;
            return signin.user;
        }
        public async Task<TokenUser> Signin(string jwt, string clientagent = "", string clientversion = "")
        {
            SigninMessage signin = new SigninMessage(jwt, global.version);
            if (!string.IsNullOrEmpty(clientagent)) signin.clientagent = clientagent;
            if (!string.IsNullOrEmpty(clientversion)) signin.clientversion = clientversion;
            signin = await signin.SendMessage<SigninMessage>(this);
            if (!string.IsNullOrEmpty(signin.error)) throw new SocketException(signin.error);
            if (signin.user == null) throw new SocketException("signin failed, received a null user object");
            user = signin.user;
            this.jwt = signin.jwt;
            if (!string.IsNullOrEmpty(signin.openflow_uniqueid))
            {
                Config.local.openflow_uniqueid = signin.openflow_uniqueid;
                Config.local.enable_analytics = signin.enable_analytics;
            }
            if (!string.IsNullOrEmpty(signin.otel_trace_url)) Config.local.otel_trace_url = signin.otel_trace_url;
            if (!string.IsNullOrEmpty(signin.otel_metric_url)) Config.local.otel_metric_url = signin.otel_metric_url;
            if (signin.otel_trace_interval > 0) Config.local.otel_trace_interval = signin.otel_trace_interval;
            if (signin.otel_metric_interval > 0) Config.local.otel_metric_interval = signin.otel_metric_interval;
            if (signin.websocket_package_size > 100)
            {
                this.websocket_package_size = signin.websocket_package_size;
            }
            signedin = true;
            return signin.user;
        }
        public async Task<TokenUser> Signin(SecureString jwt, string clientagent = "", string clientversion = "")
        {
            try
            {
                SigninMessage signin = new SigninMessage(jwt, global.version);
                if (!string.IsNullOrEmpty(clientagent)) signin.clientagent = clientagent;
                if (!string.IsNullOrEmpty(clientversion)) signin.clientversion = clientversion;
                signin = await signin.SendMessage<SigninMessage>(this);
                if (!string.IsNullOrEmpty(signin.error)) throw new SocketException(signin.error);
                if (signin.user == null) throw new SocketException("signin failed, received a null user object");
                user = signin.user;
                this.jwt = signin.jwt;
                if (!string.IsNullOrEmpty(signin.openflow_uniqueid))
                {
                    Config.local.openflow_uniqueid = signin.openflow_uniqueid;
                    Config.local.enable_analytics = signin.enable_analytics;
                }
                if (!string.IsNullOrEmpty(signin.otel_trace_url)) Config.local.otel_trace_url = signin.otel_trace_url;
                if (!string.IsNullOrEmpty(signin.otel_metric_url)) Config.local.otel_metric_url = signin.otel_metric_url;
                if (signin.otel_trace_interval > 0) Config.local.otel_trace_interval = signin.otel_trace_interval;
                if (signin.otel_metric_interval > 0) Config.local.otel_metric_interval = signin.otel_metric_interval;
                if (signin.websocket_package_size > 100)
                {
                    this.websocket_package_size = signin.websocket_package_size;
                }
                signedin = true;
                return signin.user;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<string> Signin(bool validate_only, bool longtoken, string clientagent = "", string clientversion = "")
        {
            SigninMessage signin = new SigninMessage(jwt, global.version);
            signin.validate_only = validate_only;
            signin.longtoken = longtoken;
            if (!string.IsNullOrEmpty(clientagent)) signin.clientagent = clientagent;
            if (!string.IsNullOrEmpty(clientversion)) signin.clientversion = clientversion;
            signin = await signin.SendMessage<SigninMessage>(this);
            if (!string.IsNullOrEmpty(signin.error)) throw new Exception(signin.error);
            if (signin.user == null) throw new SocketException("signin failed, received a null user object");
            user = signin.user;
            this.jwt = signin.jwt;
            if (!string.IsNullOrEmpty(signin.openflow_uniqueid))
            {
                Config.local.openflow_uniqueid = signin.openflow_uniqueid;
                Config.local.enable_analytics = signin.enable_analytics;
            }
            if (!string.IsNullOrEmpty(signin.otel_trace_url)) Config.local.otel_trace_url = signin.otel_trace_url;
            if (!string.IsNullOrEmpty(signin.otel_metric_url)) Config.local.otel_metric_url = signin.otel_metric_url;
            if (signin.otel_trace_interval > 0) Config.local.otel_trace_interval = signin.otel_trace_interval;
            if (signin.otel_metric_interval > 0) Config.local.otel_metric_interval = signin.otel_metric_interval;
            if (signin.websocket_package_size > 100)
            {
                this.websocket_package_size = signin.websocket_package_size;
            }
            signedin = true;
            return signin.jwt;
        }
        public async Task RegisterUser(string name, string username, string password)
        {
            RegisterUserMessage RegisterQueue = new RegisterUserMessage(name, username, password);
            RegisterQueue = await RegisterQueue.SendMessage<RegisterUserMessage>(this);
            if (!string.IsNullOrEmpty(RegisterQueue.error)) throw new SocketException(RegisterQueue.error);
        }
        public async Task<string> RegisterQueue(string queuename)
        {
            try
            {
                RegisterQueueMessage RegisterQueue = new RegisterQueueMessage(queuename);
                RegisterQueue = await RegisterQueue.SendMessage<RegisterQueueMessage>(this);
                if (!string.IsNullOrEmpty(RegisterQueue.error)) throw new SocketException(RegisterQueue.error);
                return RegisterQueue.queuename;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }
        public async Task<string> RegisterExchange(string exchangename, string algorithm, bool addqueue)
        {
            try
            {
                RegisterExchangeMessage RegisterExchange = new RegisterExchangeMessage(exchangename, algorithm);
                RegisterExchange.addqueue = addqueue;
                RegisterExchange = await RegisterExchange.SendMessage<RegisterExchangeMessage>(this);
                if (!string.IsNullOrEmpty(RegisterExchange.error)) throw new SocketException(RegisterExchange.error);
                return RegisterExchange.queuename;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
            }
        }
        public async Task<string> RegisterExchange(string exchangename, string algorithm, string routingkey, bool addqueue)
        {
            try
            {
                RegisterExchangeMessage RegisterExchange = new RegisterExchangeMessage(exchangename, algorithm);
                RegisterExchange.routingkey = routingkey; RegisterExchange.addqueue = addqueue;
                RegisterExchange = await RegisterExchange.SendMessage<RegisterExchangeMessage>(this);
                if (!string.IsNullOrEmpty(RegisterExchange.error)) throw new SocketException(RegisterExchange.error);
                return RegisterExchange.queuename;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }
        public async Task<object> QueueMessage(string exchange, string routingkey, object data, string replyto, string correlationId, int expiration)
        {
            QueueMessage qm = new QueueMessage();
            qm.expiration = expiration; qm.exchange = exchange; qm.routingkey = routingkey; 
            qm.data = data; qm.replyto = replyto;
            qm.correlationId = correlationId;
            qm = await qm.SendMessage<QueueMessage>(this);
            if (!string.IsNullOrEmpty(qm.error)) throw new SocketException(qm.error);
            return qm.data;
        }
        public async Task<object> QueueMessage(string queuename, object data, string replyto, string correlationId, int expiration)
        {
            QueueMessage qm = new QueueMessage(queuename);
            qm.expiration = expiration;
            qm.data = data; qm.replyto = replyto;
            qm.correlationId = correlationId;
            qm = await qm.SendMessage<QueueMessage>(this);
            if (!string.IsNullOrEmpty(qm.error)) throw new SocketException(qm.error);
            return qm.data;
        }
        private async Task<T[]> _Query<T>(string collectionname, string query, string projection, int top, int skip, string orderby, string queryas)
        {
            try
            {
                var result = new List<T>();
                bool cont = false;
                int _top = top;
                int _skip = skip;
                if (_top > Config.local.querypagesize) _top = Config.local.querypagesize;
                do
                {
                    cont = false;
                    QueryMessage<T> q = new QueryMessage<T>(); q.top = _top; q.skip = _skip;
                    q.projection = projection; q.orderby = orderby; q.queryas = queryas;
                    q.collectionname = collectionname;
                    if (string.IsNullOrEmpty(query)) query = "{}";
                    q.query = JObject.Parse(query);
                    q = await q.SendMessage<QueryMessage<T>>(this);
                    if (q == null) throw new SocketException("Server returned an empty response");
                    if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
                    if(q.result == null) throw new SocketException("Server returned an empty response");
                    if (q.result != null)
                    {
                        result.AddRange(q.result);
                        if (q.result.Count() == _top && result.Count < top)
                        {
                            cont = true;
                            _skip += _top;
                        }
                    }
                } while (cont);
                return result.ToArray();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }
        public async Task<T[]> Query<T>(string collectionname, string query, string projection, int top, int skip, string orderby, string queryas)
        {
            return await _Query<T>(collectionname, query, projection, top, skip, orderby, queryas);
        }
        public async Task<T> InsertOrUpdateOne<T>(string collectionname, int w, bool j, string uniqeness, T item)
        {
            InsertOrUpdateOneMessage<T> q = new InsertOrUpdateOneMessage<T>();
            q.w = w; q.j = j; q.uniqeness = uniqeness;
            q.collectionname = collectionname; q.item = item;
            q = await q.SendMessage<InsertOrUpdateOneMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task<T> InsertOne<T>(string collectionname, int w, bool j, T item)
        {
            InsertOneMessage<T> q = new InsertOneMessage<T>();
            q.w = w; q.j = j;
            q.collectionname = collectionname; q.item = item;
            q = await q.SendMessage<InsertOneMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task<T> UpdateOne<T>(string collectionname, int w, bool j, T item)
        {
            UpdateOneMessage<T> q = new UpdateOneMessage<T>();
            q.w = w; q.j = j;
            q.collectionname = collectionname; q.item = item;
            q = await q.SendMessage<UpdateOneMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task DeleteOne(string collectionname, string Id)
        {
            DeleteOneMessage q = new DeleteOneMessage();
            q.collectionname = collectionname; q._id = Id;
            q = await q.SendMessage<DeleteOneMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
        }
        public async Task<int> DeleteMany(string collectionname, string[] Ids)
        {
            DeleteManyMessage q = new DeleteManyMessage();
            q.collectionname = collectionname; q.ids = Ids;
            q = await q.SendMessage<DeleteManyMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.affectedrows;
        }
        public async Task<int> DeleteMany(string collectionname, string query)
        {
            DeleteManyMessage q = new DeleteManyMessage();
            q.collectionname = collectionname; q.query = query;
            q = await q.SendMessage<DeleteManyMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.affectedrows;
        }
        public async Task<T[]> InsertMany<T>(string collectionname, int w, bool j, bool skipresults, T[] items)
        {
            var result = new List<T>();
            for (var i = 0; i < items.Length; i = i + 50)
            {
                InsertManyMessage<T> q = new InsertManyMessage<T>();
                q.w = w; q.j = j;
                q.collectionname = collectionname; q.skipresults = skipresults; q.items = items.Skip(i).Take(50).ToArray();
                q = await q.SendMessage<InsertManyMessage<T>>(this);
                if (q == null) throw new SocketException("Server returned an empty response");
                if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
                if (q.results != null) result.AddRange(q.results);
            }
            return result.ToArray();
        }
        public async Task<string> UploadFile(string filepath, string path, metadata metadata)
        {
            if (string.IsNullOrEmpty(path)) path = "";
            byte[] bytes = System.IO.File.ReadAllBytes(filepath);
            string base64 = Convert.ToBase64String(bytes);
            SaveFileMessage q = new SaveFileMessage();
            q.filename = System.IO.Path.Combine(path, System.IO.Path.GetFileName(filepath));
            q.mimeType = MimeTypeHelper.GetMimeType(System.IO.Path.GetExtension(filepath));
            q.file = base64;
            q.metadata = metadata;
            if (q.metadata == null) q.metadata = new metadata();
            q.metadata.name = System.IO.Path.GetFileName(filepath);
            q.metadata.filename = q.filename;
            q.metadata.path = path;
            q = await q.SendMessage<SaveFileMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.id;
        }
        public async Task<GetFileMessage> DownloadFile(string filename, string id)
        {
            if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(id)) throw new ArgumentException("path or id is mandatory");
            GetFileMessage q = new GetFileMessage();
            q.filename = filename;
            q.id = id;
            q = await q.SendMessage<GetFileMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q;
        }
        public class apifile : apibase  
        {
            public Interfaces.entity.metadata metadata { get; set; }
        }
        public async Task DownloadFileAndSave(string filename, string id, string filepath, bool ignorepath)
        {
            apifile file = null;
            if(!string.IsNullOrEmpty(id))
            {
                var files = await Query<apifile>("fs.files", "{_id: '" + id + "'}", null, 1, 0, null, null);
                if(files.Length > 0) file = files[0];
            } else
            {
                var files = await Query<apifile>("fs.files", "{filename: '" + filename + "'}", null, 1, 0, null, null);
                if (files.Length > 0) file = files[0];
            }
            if (file == null) throw new SocketException("File not found or access denied");

            var path = System.IO.Path.GetFullPath(filepath);
            if (!ignorepath)
            {
                path = System.IO.Path.Combine(filepath, file.metadata.path);
            }
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            filepath = System.IO.Path.Combine(filepath, file.metadata.filename);
            if(System.IO.File.Exists(filepath))
            {
                var created = File.GetCreationTime(filepath);
                var modified = File.GetLastWriteTime(filepath);
                if(created == file.metadata._created && modified == file.metadata._modified)
                {
                    return;
                }
            }
            var res = await DownloadFile(filename, id);
            System.IO.File.WriteAllBytes(filepath, Convert.FromBase64String(res.file));
            File.SetCreationTime(filepath, res.metadata._created);
            File.SetLastWriteTime(filepath, res.metadata._modified);
        }
        public async Task DownloadFileAndSaveAs(string filename, string id, string filepath, bool ignorepath)
        {
            var res = await DownloadFile(filename, id);
            var path = System.IO.Path.GetFullPath(filepath);
            if (!ignorepath)
            {
                path = System.IO.Path.Combine(filepath, res.metadata.path);
            }
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            filepath = System.IO.Path.Combine(filepath, filename);
            if (System.IO.File.Exists(filepath))
            {
                var created = File.GetCreationTime(filepath);
                var modified = File.GetLastWriteTime(filepath);
                if (created == res.metadata._created && modified == res.metadata._modified)
                {
                    return;
                }
            }
            System.IO.File.WriteAllBytes(filepath, Convert.FromBase64String(res.file));
            File.SetCreationTime(filepath, res.metadata._created);
            File.SetLastWriteTime(filepath, res.metadata._modified);
        }
        public async Task<string> CreateWorkflowInstance(string workflowid, string resultqueue, string targetid, object payload, bool initialrun, string correlationId = null, string parentid = null)
        {
            var q = new CreateWorkflowInstanceMessage();
            q.targetid = targetid; q.workflowid = workflowid; q.resultqueue = resultqueue; q.initialrun = initialrun;
            q.correlationId = correlationId; q.parentid = parentid; q.jwt = jwt; q.payload = payload;
            q = await q.SendMessage<CreateWorkflowInstanceMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.newinstanceid;
        }
        public async Task EnsureNoderedInstance(string _id)
        {
            var q = new EnsureNoderedInstanceMessage();
            q._id = _id;
            q = await q.SendMessage<EnsureNoderedInstanceMessage>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
        }
        public async Task DeleteNoderedInstance(string _id)
        {
            var q = new DeleteNoderedInstanceMessage();
            q._id = _id;
            q = await q.SendMessage<DeleteNoderedInstanceMessage>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
        }
        public async Task RestartNoderedInstance(string _id)
        {
            var q = new RestartNoderedInstanceMessage();
            q._id = _id;
            q = await q.SendMessage<RestartNoderedInstanceMessage>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
        }
        public async Task<Interfaces.ICollection[]> ListCollections(bool includehist = false)
        {
            var q = new ListCollectionsMessage();
            q.includehist = includehist; q.jwt = jwt;
            q = await q.SendMessage<ListCollectionsMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task PushMetrics(string metrics)
        {
            var q = new PushMetricsMessage();
            q.metrics = metrics; q.jwt = jwt;
            q = await q.SendMessage<PushMetricsMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
        }
        private Dictionary<string, WatchEventDelegate> watches = new Dictionary<string, WatchEventDelegate>();
        public async Task<string> Watch(string collectionname, string aggregates, WatchEventDelegate onWatchEvent)
        {
            WatchMessage q = new WatchMessage();
            q.collectionname = collectionname; q.aggregates = JArray.Parse(aggregates);
            q = await q.SendMessage<WatchMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (string.IsNullOrEmpty(q.id)) throw new SocketException("Failed registering watch, id is null");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            if (!watches.ContainsKey(q.id)) watches.Add(q.id, onWatchEvent);
            return q.id;
        }
        public async Task UnWatch(string id)
        {
            WatchMessage q = new WatchMessage(); q.msg.command = "unwatch";
            q.id = id;
            q = await q.SendMessage<WatchMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            if (watches.ContainsKey(id)) watches.Remove(id);
        }



        public async Task<T> AddWorkitem<T> (IWorkitem item, string[] files) where T : IWorkitem
        {
            var q = new AddWorkitemMessage<T>(); q.msg.command = "addworkitem";
            q.wiqid = item.wiqid; q.wiq = item.wiq; q.name = item.name; q.nextrun = item.nextrun; q.priority = item.priority;
            q.payload = item.payload; q.files = new MessageWorkitemFile[] { };
            var _files = new List<MessageWorkitemFile>();
            if(files != null)
                foreach(var f in files)
                {
                    if (!System.IO.File.Exists(f)) continue;
                    var newf = new MessageWorkitemFile();
                    newf.compressed = false;
                    newf.filename = System.IO.Path.GetFileName(f);
                    byte[] bytes = System.IO.File.ReadAllBytes(f);
                    newf.file = Convert.ToBase64String(bytes);
                    _files.Add(newf);
                }
            q.files = _files.ToArray();
            q = await q.SendMessage<AddWorkitemMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task AddWorkitems(string wiqid, string wiq, AddWorkitem[] items)
        {
            var q = new AddWorkitemsMessage(); q.msg.command = "addworkitems";
            q.wiqid = wiqid; q.wiq = wiq; 
            q.items = items;
            foreach(var item in q.items)
                if (item.files != null)
                    foreach (var f in item.files)
                    {
                        if (!System.IO.File.Exists(f.filename)) continue;
                        f.compressed = false;
                        byte[] bytes = System.IO.File.ReadAllBytes(f.filename);
                        f.file = Convert.ToBase64String(bytes);
                        f.filename = System.IO.Path.GetFileName(f.filename);
                    }
            q = await q.SendMessage<AddWorkitemsMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
        }
        public async Task<T> UpdateWorkitem<T>(IWorkitem item, string[] files, bool ignoremaxretries) where T : IWorkitem
        {
            var q = new UpdateWorkitemMessage<T>(); q.msg.command = "updateworkitem";
            q._id = item._id; q.name = item.name; q.state = item.state; q.nextrun = item.nextrun;
            q.errormessage = item.errormessage; q.errorsource = item.errorsource; q.ignoremaxretries = ignoremaxretries;
            q.payload = item.payload; q.files = new MessageWorkitemFile[] { };
            var _files = new List<MessageWorkitemFile>();
            if (files != null)
                foreach (var f in files)
                {
                    if (!System.IO.File.Exists(f)) continue;
                    var newf = new MessageWorkitemFile();
                    newf.compressed = false;
                    newf.filename = System.IO.Path.GetFileName(f);
                    byte[] bytes = System.IO.File.ReadAllBytes(f);
                    newf.file = Convert.ToBase64String(bytes);
                    _files.Add(newf);
                }
            q.files = _files.ToArray();
            q = await q.SendMessage<UpdateWorkitemMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task<T> PopWorkitem<T>(string wiq, string wiqid) where T : IWorkitem
        {
            var q = new PopWorkitemMessage<T>(); q.msg.command = "popworkitem";
            q.wiqid = wiqid; q.wiq = wiq;
            q = await q.SendMessage<PopWorkitemMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task DeleteWorkitem(string _id)
        {
            var q = new DeleteWorkitemMessage(); q.msg.command = "deleteworkitem";
            q._id = _id;
            q = await q.SendMessage<DeleteWorkitemMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
        }
        public async Task<T> AddWorkitemQueue<T>(T item) where T : IWorkitemQueue
        {
            var q = new AddWorkitemQueueMessage<T>(); q.msg.command = "addworkitemqueue";
            q.skiprole = true; q._acl = item._acl;
            q.name = item.name; q.workflowid = item.workflowid; q.robotqueue = item.robotqueue;
            q.projectid = item.projectid; q.amqpqueue = item.amqpqueue;
            q.maxretries = item.maxretries; q.retrydelay = item.retrydelay; q.initialdelay = item.initialdelay;
            q = await q.SendMessage<AddWorkitemQueueMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task<T> UpdateWorkitemQueue<T>(T item, bool purge) where T : IWorkitemQueue
        {
            var q = new UpdateWorkitemQueueMessage<T>(); q.msg.command = "updateworkitemqueue";
            q._id = item._id; q._acl = item._acl; q.projectid = item.projectid;
            q.name = item.name; q.workflowid = item.workflowid; q.robotqueue = item.robotqueue;
            q.amqpqueue = item.amqpqueue; q.purge = purge;
            q.maxretries = item.maxretries; q.retrydelay = item.retrydelay; q.initialdelay = item.initialdelay;
            q = await q.SendMessage<UpdateWorkitemQueueMessage<T>>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
            return q.result;
        }
        public async Task DeleteWorkitemQueue(IWorkitemQueue item, bool purge)
        {
            var q = new DeleteWorkitemQueueMessage(); q.msg.command = "deleteworkitemqueue";
            q._id = item._id; q.name = item.name; q.purge = purge;
            q = await q.SendMessage<DeleteWorkitemQueueMessage>(this);
            if (q == null) throw new SocketException("Server returned an empty response");
            if (!string.IsNullOrEmpty(q.error)) throw new SocketException(q.error);
        }

    }


}
