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
    public delegate void QueueMessageDelegate(IQueueMessage message, QueueMessageEventArgs e);
    public interface IWebSocketClient
    {
        event Action OnOpen;
        event Action<string> OnClose;        
        event QueueMessageDelegate OnQueueMessage;
        TokenUser user { get; }
        string url { get; set; }
        string jwt { get; }
        bool isConnected { get; }
        Task ProcessQueue();
        Task Connect();
        Task Close();
        // Task<IMessage> SendMessage(IMessage msg);
        Task<TokenUser> Signin(string username, SecureString password);
        Task<TokenUser> Signin(string jwt);
        Task<TokenUser> Signin(SecureString jwt);
        Task RegisterUser(string name, string username, string password);
        Task RegisterQueue(string queuename);
        Task<object> QueueMessage(string queuename, object data, string correlationId = null);
        Task<T[]> Query<T>(string collectionname, string query, string projection = null, int top = 100, int skip = 0, string orderby = null);
        Task<T> InsertOrUpdateOne<T>(string collectionname, int w, bool j, string uniqeness, T item);
        Task<T> InsertOne<T>(string collectionname, int w, bool j, T item);
        Task<T> UpdateOne<T>(string collectionname, int w, bool j, T item);
        // Task UpdateOne(string collectionname, string query, int w, bool j, Newtonsoft.Json.Linq.JObject UpdateDoc);
        Task DeleteOne(string collectionname, string Id);
        Task<string> UploadFile(string filepath, string path, metadata metadata);
        Task DownloadFileAndSave(string filename, string id, string filepath, bool ignorepath);
        Task DownloadFileAndSaveAs(string filename, string id, string filepath, bool ignorepath);
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
