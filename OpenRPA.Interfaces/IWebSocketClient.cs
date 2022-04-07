using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    using OpenRPA.Interfaces.entity;
    using System.Security;
    public interface IBaseMessage
    {
        string id { get; set; }
        string replyto { get; set; }
        string command { get; set; }
        string data { get; set; }
        void reply(string command);
        void reply();

    }
    public interface IMessage : IBaseMessage
    {
        // void SendMessage(IWebSocketClient ws);
    }
    public interface ISocketCommand
    {
        string error { get; set; }
        string jwt { get; set; }
        IMessage msg { get; set; }
        // Task<T> SendMessage<T>(IWebSocketClient ws);
    }
    public interface IQueueMessage : ISocketCommand
    {
        string queuename { get; set; }
        object data { get; set; }
        string correlationId { get; set; }
        string replyto { get; set; }
    }
    public interface IQueueClosedMessage : ISocketCommand
    {
        string queuename { get; set; }
    }
    public delegate void QueueMessageDelegate(IQueueMessage message, QueueMessageEventArgs e);
    public delegate void QueueClosedDelegate(IQueueClosedMessage message, QueueMessageEventArgs e);
    public interface ICollection
    {
        string name { get; set; }
        string type { get; set; }
    }
    public delegate void WatchEventDelegate(string id, Newtonsoft.Json.Linq.JObject data);
    public interface IWebSocketClient
    {
        event Action OnOpen;
        event Action<string> OnClose;
        event QueueMessageDelegate OnQueueMessage;
        event QueueClosedDelegate OnQueueClosed;
        int MessageQueueSize { get; }
        System.Net.WebSockets.WebSocket ws { get; }
        TokenUser user { get; set; }
        bool signedin { get; }
        string url { get; set; }
        string jwt { get; }
        bool isConnected { get; }
        Task ProcessQueue();
        Task Connect();
        Task Close();
        // Task<IMessage> SendMessage(IMessage msg);
        Task<TokenUser> Signin(string username, SecureString password, string clientagent = "", string clientversion = "");
        Task<TokenUser> Signin(string jwt, string clientagent = "", string clientversion = "");
        Task<TokenUser> Signin(SecureString jwt, string clientagent = "", string clientversion = "");
        Task<string> Signin(bool validate_only, bool longtoken, string clientagent = "", string clientversion = "");
        Task RegisterUser(string name, string username, string password);
        Task<string> RegisterQueue(string queuename);
        Task<string> RegisterExchange(string exchangename, string algorithm, bool addqueue);
        Task<string> RegisterExchange(string exchangename, string algorithm, string routingkey, bool addqueue);
        Task<object> QueueMessage(string exchange, string routingkey, object data, string replyto, string correlationId, int expiration);
        Task<object> QueueMessage(string queuename, object data, string replyto, string correlationId, int expiration);
        Task<string> CreateWorkflowInstance(string workflowid, string resultqueue, string targetid, object payload, bool initialrun, string correlationId = null, string parentid = null);
        Task<T[]> Query<T>(string collectionname, string query, string projection = null, int top = 100, int skip = 0, string orderby = null, string queryas = null);
        Task<T> InsertOrUpdateOne<T>(string collectionname, int w, bool j, string uniqeness, T item);
        Task<T> InsertOne<T>(string collectionname, int w, bool j, T item);
        Task<T> UpdateOne<T>(string collectionname, int w, bool j, T item);
        // Task UpdateOne(string collectionname, string query, int w, bool j, Newtonsoft.Json.Linq.JObject UpdateDoc);
        Task DeleteOne(string collectionname, string Id);
        Task<int> DeleteMany(string collectionname, string[] Ids);
        Task<int> DeleteMany(string collectionname, string query);
        Task<T[]> InsertMany<T>(string collectionname, int w, bool j, bool skipresults, T[] items);
        Task<string> UploadFile(string filepath, string path, metadata metadata);
        Task DownloadFileAndSave(string filename, string id, string filepath, bool ignorepath);
        Task DownloadFileAndSaveAs(string filename, string id, string filepath, bool ignorepath);
        Task<ICollection[]> ListCollections(bool includehist = false);
        Task PushMetrics(string metrics);
        Task<string> Watch(string collectionname, string query, WatchEventDelegate onWatchEvent);
        Task UnWatch(string id);
        Task EnsureNoderedInstance(string _id);
        Task DeleteNoderedInstance(string _id);
        Task RestartNoderedInstance(string _id);

        Task<T> AddWorkitem<T>(IWorkitem item, string[] files) where T : IWorkitem;
        Task<T> UpdateWorkitem<T>(IWorkitem item, string[] files, bool ignoremaxretries) where T : IWorkitem;
        Task<T> PopWorkitem<T>(string wiq, string wiqid) where T : IWorkitem;
        Task DeleteWorkitem(string _id);
        Task<T> AddWorkitemQueue<T>(T item) where T : IWorkitemQueue;
        Task AddWorkitems(string wiqid, string wiq, AddWorkitem[] items);
        Task<T> UpdateWorkitemQueue<T>(T item, bool purge) where T : IWorkitemQueue;
        Task DeleteWorkitemQueue(IWorkitemQueue item, bool purge);
    }
    public class QueueMessageEventArgs : EventArgs
    {
        public bool isBusy { get; set; }
        public QueueMessageEventArgs()
        {
            isBusy = false;
        }
    }
}
