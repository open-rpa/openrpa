using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class WebSocketClient
    {
        private ClientWebSocket ws = new ClientWebSocket(); // WebSocket
        private string url = "";
        private CancellationTokenSource src = new CancellationTokenSource();
        private List<SocketMessage> _receiveQueue = new List<SocketMessage>();
        private List<SocketMessage> _sendQueue = new List<SocketMessage>();
        private List<QueuedMessage> _messageQueue = new List<QueuedMessage>();

        public event Action OnOpen;
        public event Action<string> OnClose;
        // public event Action OnMessage;

        public TokenUser user { get; private set; }
        public string jwt { get; private set; }

        public WebSocketClient(string url)
        {
            this.url = url;
        }
        public async Task Connect()
        {
            try
            {
                Log.Debug("Connecting to " + url);
                if (ws.State == WebSocketState.Aborted || ws.State == WebSocketState.Closed)
                {
                    ws.Dispose();
                    ws = null;
                    ws = new ClientWebSocket();
                }
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
                    await ws.CloseAsync(WebSocketCloseStatus.Empty, "", src.Token);
                }
                catch (Exception)
                {
                }
                ws.Dispose();
                ws = null;
            }
            src.Cancel();
        }
        private async Task receiveLoop()
        {
            byte[] buffer = new byte[2048];
            while (true)
            {
                string json = string.Empty;
                try
                {
                    if (ws == null) { return; }
                    if (ws.State != WebSocketState.Open) { return; }
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), src.Token);
                    json = Encoding.UTF8.GetString(buffer.Take(result.Count).ToArray());
                    var message = JsonConvert.DeserializeObject<SocketMessage>(json);
                    _receiveQueue.Add(message);
                    await ProcessQueue();
                }
                catch (Exception ex)
                {
                    if (ws.State != WebSocketState.Open)
                    {
                        OnClose?.Invoke(ex.Message);
                    }
                    else
                    {
                        Log.Error(json);
                        Log.Error(ex, "");
                    }
                }
            }
        }
        private async Task PingLoop()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                await Task.Delay(1000);
                var msg = new Message("ping");
                msg.SendMessage(this);
            }
        }
        static SemaphoreSlim ReceiveSemaphore = new SemaphoreSlim(1, 1);
        static SemaphoreSlim SendSemaphore = new SemaphoreSlim(1, 1);
        public async Task ProcessQueue()
        {
            try
            {
                await ReceiveSemaphore.WaitAsync();
                var ids = (from m in _receiveQueue group m by new { m.id } into mygroup select mygroup.Key.id).ToList();
                foreach (var id in ids)
                {
                    var first = _receiveQueue.Where((x) => x.id == id).First();
                    var msgs = _receiveQueue.Where((x) => x.id == id).ToList();
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
                ReceiveSemaphore.Release();
            }

            try
            {
                await SendSemaphore.WaitAsync();
                foreach (var msg in _sendQueue.ToList())
                {
                    if (await SendString(JsonConvert.SerializeObject(msg), src.Token))
                    {
                        _sendQueue.Remove(msg);
                    }
                }
            }
            finally
            {
                SendSemaphore.Release();
            }
        }
        static SemaphoreSlim SendStringSemaphore = new SemaphoreSlim(1, 1);
        private async Task<bool> SendString(string data, CancellationToken cancellation)
        {
            if (ws == null) { return false; }
            if (ws.State != WebSocketState.Open) { return false; }
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            try
            {
                await SendStringSemaphore.WaitAsync();
                //await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
                await ws.SendAsync(buffer, WebSocketMessageType.Text, true, cancellation);
                return true;
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
            _sendQueue.Add(msg);
        }
        static bool dofail = false;
        private void Process(Message msg)
        {
            if (!string.IsNullOrEmpty(msg.replyto))
            {
                if (msg.command != "pong") { Log.Verbose(msg.command + " / replyto: " + msg.replyto); }
                    else { Log.Verbose(msg.command + " / replyto: " + msg.replyto);  }

                foreach (var qm in _messageQueue)
                {
                    if (qm.msg.id == msg.replyto)
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
                    else { Log.Verbose(msg.command + " / replyto: " + msg.replyto); }
                switch (msg.command)
                {
                    case "ping":
                        msg.reply("pong");
                        msg.SendMessage(this);
                        break;
                    case "refreshtoken":
                        msg.reply();
                        var singin = JsonConvert.DeserializeObject<SigninMessage>(msg.data);
                        this.user = singin.user;
                        this.jwt = singin.jwt;
                        // msg.SendMessage(this); no need to confirm
                        break;
                    case "queuemessage":
                        msg.reply();
                        var qm = JsonConvert.DeserializeObject<QueueMessage>(msg.data);
                        if (dofail)
                        {
                            qm.error = "Go away !!!!";
                        } else { 
                            qm.data = "{\"payload\": \"Reply from robot client at " + DateTime.Now + "\"}";
                        }
                        dofail = !dofail;
                        msg.data = JsonConvert.SerializeObject(qm);
                        msg.SendMessage(this);
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
            _messageQueue.Add(qm);
            using (qm.autoReset = new AutoResetEvent(false))
            {
                msg.SendMessage(this);
                await qm.autoReset.WaitOneAsync();
            }
            return qm.reply;
        }
        public async Task<TokenUser> Signin(string username, SecureString password)
        {
            SigninMessage signin = new SigninMessage(username, password);
            signin = await signin.SendMessage<SigninMessage>(this);
            if (!string.IsNullOrEmpty(signin.error)) throw new Exception(signin.error);
            user = signin.user;
            jwt = signin.jwt;
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

        public async Task<T[]> Query<T>(string collectionname, string query)
        {
            QueryMessage<T> q = new QueryMessage<T>();
            q.collectionname = collectionname; q.query = JObject.Parse(query);
            q = await q.SendMessage<QueryMessage<T>>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            return q.result;
        }
        public async Task<T> InsertOne<T>(string collectionname, T item)
        {
            InsertOneMessage<T> q = new InsertOneMessage<T>();
            q.collectionname = collectionname; q.item = item;
            q = await q.SendMessage<InsertOneMessage<T>>(this);
            if (!string.IsNullOrEmpty(q.error)) throw new Exception(q.error);
            return q.result;
        }
        public async Task<T> UpdateOne<T>(string collectionname, T item)
        {
            UpdateOneMessage<T> q = new UpdateOneMessage<T>();
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


    }
}
