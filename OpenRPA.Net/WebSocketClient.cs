using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
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
        // private ClientWebSocket ws = (ClientWebSocket)SystemClientWebSocket.CreateClientWebSocket();  // new ClientWebSocket(); // WebSocket
        // private System.Net.WebSockets.Managed.ClientWebSocket ws = new System.Net.WebSockets.Managed.ClientWebSocket();  // new ClientWebSocket(); // WebSocket
        // private System.Net.WebSockets.Managed.ClientWebSocket ws = null;  // new ClientWebSocket(); // WebSocket
        private WebSocket ws = null;  // new ClientWebSocket(); // WebSocket
        public int websocket_package_size = 4096;
        public string url { get; set; }
        private CancellationTokenSource src = new CancellationTokenSource();
        private List<SocketMessage> _receiveQueue = new List<SocketMessage>();
        private List<SocketMessage> _sendQueue = new List<SocketMessage>();
        private List<QueuedMessage> _messageQueue = new List<QueuedMessage>();
        // public delegate void QueueMessageDelegate(IQueueMessage message, QueueMessageEventArgs e);
        public event Action OnOpen;
        public event Action<string> OnClose;
        public event QueueMessageDelegate OnQueueMessage;
        public TokenUser user { get; private set; }
        public string jwt { get; private set; }
        public bool isConnected
        {
            get
            {
                if (ws == null) return false;
                if(ws.State != System.Net.WebSockets.WebSocketState.Open) return false;
                return true;
            }
        }
        public WebSocketClient(string url)
        {
            this.url = url;
        }
        public async Task Connect()
        {
            try
            {
                Log.Debug("Connecting to " + url);
                //if (ws != null && (ws.State == System.Net.WebSockets.WebSocketState.Aborted || ws.State == System.Net.WebSockets.WebSocketState.Closed))
                if (ws != null && (ws.State != WebSocketState.Connecting))
                {
                    ws.Dispose();
                    ws = null;
                }
                if(ws == null) {
                    // ws = new ClientWebSocket();
                    // ws = (ClientWebSocket)SystemClientWebSocket.CreateClientWebSocket();
                    if (VersionHelper.IsWindows8OrGreater())
                    {
                        // ws = new ClientWebSocket();
                        ws = new System.Net.WebSockets.Managed.ClientWebSocket();
                    }
                    else
                    {
                        ws = new System.Net.WebSockets.Managed.ClientWebSocket();
                    }
                    src = new CancellationTokenSource();
                }
                if (ws.State == System.Net.WebSockets.WebSocketState.Connecting || ws.State == System.Net.WebSockets.WebSocketState.Open) return;
                if(ws.State == System.Net.WebSockets.WebSocketState.CloseReceived)
                {
                    OnClose?.Invoke("Socket closing");
                    ws.Dispose();
                    ws = null;
                    return;
                }
                Log.Information("Connecting to " + url);
                await ws.ConnectAsync(new Uri(url), src.Token);
                Log.Information("Connected to " + url);
                Task receiveTask = Task.Run(async () => await receiveLoop(), src.Token);
                Task pingTask = Task.Run(async () => await PingLoop(), src.Token);
                OnOpen?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
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
                //ws.Dispose();
                //ws = null;
            }
            src.Cancel();
        }
        private static bool TryParseJSON(string json, out JObject jObject)
        {
            try
            {
                jObject = JObject.Parse(json);
                return true;
            }
            catch
            {
                jObject = null;
                return false;
            }
        }
        private static bool TryParseJSON(string json)
        {
            try
            {
                if (!json.StartsWith("{") && !json.StartsWith("[")) return false;
                if (!json.EndsWith("}") && !json.EndsWith("]")) return false;
                var jObject = JObject.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        string tempbuffer = null;
        private async Task receiveLoop()
        {
            byte[] buffer = new byte[websocket_package_size];
            while (true)
            {
                string json = string.Empty;
                System.Net.WebSockets.WebSocketReceiveResult result;
                try
                {
                    if (ws == null) { return; }
                    if (ws.State != System.Net.WebSockets.WebSocketState.Open) {
                        OnClose?.Invoke("");
                        return;
                    }
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), src.Token);
                    json = Encoding.UTF8.GetString(buffer.Take(result.Count).ToArray());
                    if (TryParseJSON(json))
                    {
                        var message = JsonConvert.DeserializeObject<SocketMessage>(json);
                        if (message != null) _receiveQueue.Add(message);
                    } else
                    {
                        
                        if (TryParseJSON(tempbuffer + json))
                        {
                            // Console.WriteLine("OK: " + tempbuffer);
                            var message = JsonConvert.DeserializeObject<SocketMessage>(tempbuffer + json);
                            if (message != null) _receiveQueue.Add(message);
                            tempbuffer = null;
                        } 
                        else
                        {
                            if(!string.IsNullOrEmpty(tempbuffer))
                            {
                                Log.Debug("FAILED: " + json);
                            }
                            tempbuffer += json;
                        }
                        if(!string.IsNullOrEmpty(tempbuffer) && tempbuffer.Length > (websocket_package_size*2))
                        {
                            tempbuffer = null;
                        }
                    }
                    await ProcessQueue();
                }
                catch (System.Net.WebSockets.WebSocketException ex)
                {
                    if (ws.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        OnClose?.Invoke(ex.Message);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(json)) Log.Error(json);
                        Log.Error(ex, "");
                        await Task.Delay(3000);
                        await this.Close();
                    }

                }
                catch (Exception ex)
                {
                    if (ws.State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        OnClose?.Invoke(ex.Message);
                    }
                    else
                    {
                        if(!string.IsNullOrEmpty(json)) Log.Error(json);
                        Log.Error(ex, "");
                        await Task.Delay(3000);
                        //await this.Close();
                    }
                }
            }
        }
        private async Task PingLoop()
        {
            byte[] buffer = new byte[1024];
            while (isConnected)
            {
                await Task.Delay(10000);
                var msg = new Message("ping");
                msg.SendMessage(this);
            }
        }
        static SemaphoreSlim ProcessingSemaphore = new SemaphoreSlim(1, 1);
        public async Task ProcessQueue()
        {
            try
            {
                //await ReceiveSemaphore.WaitAsync();
                await ProcessingSemaphore.WaitAsync();
                if (_receiveQueue == null) return;
                List<string> ids = new List<string>();
                for(var i = 0; i < _receiveQueue.Count; i++)
                {
                    if(_receiveQueue[i]!=null)
                    {
                        string id = _receiveQueue[i].id;
                        if (!ids.Contains(id)) ids.Add(id);
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

                        string data = "";
                        foreach (var m in msgs.OrderBy((y) => y.index))
                        {
                            data += m.data;
                        }
                        var result = new Message(first, data);
                        Process(result);
                        foreach (var m in msgs.OrderBy((y) => y.index).ToList())
                        {
                            _receiveQueue.Remove(m);
                        }
                    }
                }
            }
            finally
            {
                //ReceiveSemaphore.Release();
            }

            try
            {
                List<SocketMessage> templist;
                lock (_sendQueue)
                {
                    templist = _sendQueue.ToList();
                }
                // await SendSemaphore.WaitAsync();
                foreach (var msg in templist)
                {
                    if (await SendString(JsonConvert.SerializeObject(msg), src.Token))
                    {
                        _sendQueue.Remove(msg);
                    }
                }
            }
            finally
            {
                //SendSemaphore.Release();
                ProcessingSemaphore.Release();
            }
        }
        static SemaphoreSlim SendStringSemaphore = new SemaphoreSlim(1, 1);
        private async Task<bool> SendString(string data, CancellationToken cancellation)
        {
            if (ws == null) { return false; }
            if (ws.State != System.Net.WebSockets.WebSocketState.Open) { return false; }
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            try
            {
                await SendStringSemaphore.WaitAsync();
                //await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
                await ws.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, cancellation);
                return true;
            }
            catch (System.Net.WebSockets.WebSocketException ex)
            {
                Log.Error(ex, "");
                _ = Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }
            finally
            {
                SendStringSemaphore.Release();
            }
            return false;
        }
        public void PushMessage(SocketMessage msg)
        {
            lock(_sendQueue)
            {
                _sendQueue.Add(msg);
            }
        }
        private void Process(Message msg)
        {
            if (!string.IsNullOrEmpty(msg.replyto))
            {
                if (msg.command != "pong") { Log.Verbose(msg.command + " / replyto: " + msg.replyto); }
                    // else { Log.Verbose(msg.command + " / replyto: " + msg.replyto);  }

                foreach (var qm in _messageQueue.ToList())
                {
                    if (qm != null && qm.msg.id == msg.replyto)
                    {
                        try
                        {
                            qm.reply = msg;
                            qm.autoReset.Set();
                            _messageQueue.Remove(qm);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "");
                        }
                    }
                }
            }
            else
            {
                if (msg.command != "ping" && msg.command != "refreshtoken") { Log.Verbose(msg.command + " / " + msg.id); }
                    // else { Log.Verbose(msg.command + " / replyto: " + msg.replyto); }
                switch (msg.command)
                {
                    case "ping":
                        msg.reply("pong");
                        msg.SendMessage(this);
                        break;
                    case "refreshtoken":
                        msg.reply();
                        var signin = JsonConvert.DeserializeObject<SigninMessage>(msg.data);
                        this.user = signin.user;
                        this.jwt = signin.jwt;
                        // msg.SendMessage(this); no need to confirm
                        if(signin.websocket_package_size > 100)
                        {
                            this.websocket_package_size = signin.websocket_package_size;
                        }                        
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
                            msg.SendMessage(this);
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
                                Log.Warning("Cannot invoke, I'm busy.");
                                msg.SendMessage(this);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            msg.command = "error";
                            msg.data = ex.ToString();
                        }
                        msg.SendMessage(this);
                        // if (string.IsNullOrEmpty(qm.replyto)) msg.SendMessage(this);
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
            lock(_messageQueue)
            {
                _messageQueue.Add(qm);
            }
            
            using (qm.autoReset = new AutoResetEvent(false))
            {
                msg.SendMessage(this);
                await qm.autoReset.WaitOneAsync();
            }
            return qm.reply as Message;
        }
        public async Task<TokenUser> Signin(string username, SecureString password)
        {
            SigninMessage signin = new SigninMessage(username, password);
            signin = await signin.SendMessage<SigninMessage>(this);
            if (!string.IsNullOrEmpty(signin.error)) throw new Exception(signin.error);
            user = signin.user;
            jwt = signin.jwt;
            if (signin.websocket_package_size > 100)
            {
                this.websocket_package_size = signin.websocket_package_size;
            }
            return signin.user;
        }
        public async Task<TokenUser> Signin(string jwt)
        {
            SigninMessage signin = new SigninMessage(jwt);
            signin = await signin.SendMessage<SigninMessage>(this);
            if (!string.IsNullOrEmpty(signin.error)) throw new Exception(signin.error);
            user = signin.user;
            this.jwt = signin.jwt;
            if (signin.websocket_package_size > 100)
            {
                this.websocket_package_size = signin.websocket_package_size;
            }
            return signin.user;
        }
        public async Task<TokenUser> Signin(SecureString jwt)
        {
            SigninMessage signin = new SigninMessage(jwt);
            signin = await signin.SendMessage<SigninMessage>(this);
            if (!string.IsNullOrEmpty(signin.error)) throw new Exception(signin.error);
            user = signin.user;
            this.jwt = signin.jwt;
            if (signin.websocket_package_size > 100)
            {
                this.websocket_package_size = signin.websocket_package_size;
            }
            return signin.user;
        }
        public async Task RegisterUser(string name, string username, string password)
        {
            RegisterUserMessage RegisterQueue = new RegisterUserMessage(name, username, password);
            RegisterQueue = await RegisterQueue.SendMessage<RegisterUserMessage>(this);
            if (!string.IsNullOrEmpty(RegisterQueue.error)) throw new Exception(RegisterQueue.error);
        }
        public async Task RegisterQueue(string queuename)
        {
            RegisterQueueMessage RegisterQueue = new RegisterQueueMessage(queuename);
            RegisterQueue = await RegisterQueue.SendMessage<RegisterQueueMessage>(this);
            if (!string.IsNullOrEmpty(RegisterQueue.error)) throw new Exception(RegisterQueue.error);
        }
        public async Task<object> QueueMessage(string queuename, object data, string correlationId = null)
        {
            QueueMessage qm = new QueueMessage(queuename);
            qm.data = data;
            qm.correlationId = correlationId;
            qm = await qm.SendMessage<QueueMessage>(this);
            if (!string.IsNullOrEmpty(qm.error)) throw new Exception(qm.error);
            return qm.data;
        }
        private async Task<T[]> _Query<T>(string collectionname, string query, string projection, int top, int skip, string orderby)
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
                q.projection = projection; q.orderby = orderby;
                q.collectionname = collectionname; q.query = JObject.Parse(query);
                q = await q.SendMessage<QueryMessage<T>>(this);
                if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
                result.AddRange(q.result);
                if (q.result.Count() == _top && result.Count < top)
                {
                    cont = true;
                    _skip += _top;
                }
            } while (cont);
            return result.ToArray();
        }
        public async Task<T[]> Query<T>(string collectionname, string query, string projection, int top, int skip, string orderby)
        {
            return await _Query<T>(collectionname, query, projection, top, skip, orderby);
            //QueryMessage<T> q = new QueryMessage<T>(); q.top = top; q.skip = skip;
            //q.projection = projection; q.orderby = orderby;
            //q.collectionname = collectionname; q.query = JObject.Parse(query);
            //q = await q.SendMessage<QueryMessage<T>>(this);
            //if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            //return q.result;
        }
        public async Task<T> InsertOrUpdateOne<T>(string collectionname, int w, bool j, string uniqeness, T item)
        {
            InsertOrUpdateOneMessage<T> q = new InsertOrUpdateOneMessage<T>();
            q.w = w; q.j = j; q.uniqeness = uniqeness;
            q.collectionname = collectionname; q.item = item;
            q = await q.SendMessage<InsertOrUpdateOneMessage<T>>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            return q.result;
        }
        public async Task<T> InsertOne<T>(string collectionname, int w, bool j, T item)
        {
            InsertOneMessage<T> q = new InsertOneMessage<T>();
            q.w = w; q.j = j;
            q.collectionname = collectionname; q.item = item;
            q = await q.SendMessage<InsertOneMessage<T>>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            return q.result;
        }
        public async Task<T> UpdateOne<T>(string collectionname, int w, bool j, T item)
        {
            UpdateOneMessage<T> q = new UpdateOneMessage<T>();
            q.w = w; q.j = j;
            q.collectionname = collectionname; q.item = item;
            q = await q.SendMessage<UpdateOneMessage<T>>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            return q.result;
        }
        public async Task DeleteOne(string collectionname, string Id)
        {
            DeleteOneMessage q = new DeleteOneMessage();
            q.collectionname = collectionname; q._id = Id;
            q = await q.SendMessage<DeleteOneMessage>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
        }
        //public async Task UpdateOne(string collectionname, string query, int w, bool j, JObject UpdateDoc)
        //{
        //    UpdateOneMessage<JObject> q = new UpdateOneMessage<JObject>();
        //    q.w = w; q.j = j; q.query = query;
        //    q.collectionname = collectionname; q.item = UpdateDoc;
        //    q = await q.SendMessage<UpdateOneMessage<JObject>>(this);
        //    if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
        //    // return q.result;
        //}
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
            if(q.metadata == null) q.metadata = new metadata();
            q.metadata.name = System.IO.Path.GetFileName(filepath);
            q.metadata.filename = q.filename;
            q.metadata.path = path;
            q = await q.SendMessage<SaveFileMessage>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            return q.id;
        }
        public async Task<GetFileMessage> DownloadFile(string filename, string id)
        {
            if (string.IsNullOrEmpty(filename) && string.IsNullOrEmpty(id)) throw new ArgumentException("path or id is mandatory");
            GetFileMessage q = new GetFileMessage();
            q.filename = filename;
            q.id = id;
            q = await q.SendMessage<GetFileMessage>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            return q;
        }
        public async Task DownloadFileAndSave(string filename, string id, string filepath, bool ignorepath)
        {
            var res = await DownloadFile(filename, id);
            var path = System.IO.Path.GetFullPath(filepath);
            if (!ignorepath)
            {
                path = System.IO.Path.Combine(filepath, res.metadata.path);
            }
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            filepath = System.IO.Path.Combine(filepath, res.metadata.filename);
            System.IO.File.WriteAllBytes(filepath, Convert.FromBase64String(res.file));
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
            System.IO.File.WriteAllBytes(filepath, Convert.FromBase64String(res.file));
        }
    }
}
