using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Net
{
    public class AddWorkitemMessage<T> : SocketCommand where T : IWorkitem
    {
        public AddWorkitemMessage() : base()
        {
            msg.command = "addworkitem";
        }
        public Dictionary<string, object> payload { get; set; }
        public string wiqid { get; set; }
        public string wiq { get; set; }
        public string name { get; set; }
        public MessageWorkitemFile[] files { get; set; }
        public string success_wiqid { get; set; }
        public string failed_wiqid { get; set; }
        public string success_wiq { get; set; }
        public string failed_wiq { get; set; }
        public T result { get; set; }
        public DateTime? nextrun { get; set; }
        public int priority { get; set; }
    }
    public class AddWorkitemsMessage : SocketCommand
    {
        public AddWorkitemsMessage() : base()
        {
            msg.command = "addworkitems";
        }
        public AddWorkitem[] items { get; set; }
        public string wiqid { get; set; }
        public string wiq { get; set; }
        public string success_wiqid { get; set; }
        public string failed_wiqid { get; set; }
        public string success_wiq { get; set; }
        public string failed_wiq { get; set; }
    }
    public class UpdateWorkitemMessage<T> : SocketCommand where T : IWorkitem
    {
        public UpdateWorkitemMessage() : base()
        {
            msg.command = "updateworkitem";
        }
        public Dictionary<string, object> payload { get; set; }
        public string _id { get; set; }
        public string name { get; set; }
        public string state { get; set; }
        public MessageWorkitemFile[] files { get; set; }
        public string success_wiqid { get; set; }
        public string failed_wiqid { get; set; }
        public string success_wiq { get; set; }
        public string failed_wiq { get; set; }
        public T result { get; set; }
        public DateTime? nextrun { get; set; }
        public bool ignoremaxretries { get; set; }
        public string errormessage { get; set; }
        public string errorsource { get; set; }
        public string errortype { get; set; }
    }
    public class PopWorkitemMessage<T> : SocketCommand where T : IWorkitem
    {
        public PopWorkitemMessage() : base()
        {
            msg.command = "popworkitem";
        }
        public string wiqid { get; set; }
        public string wiq { get; set; }
        public T result { get; set; }
    }
    public class DeleteWorkitemMessage : SocketCommand
    {
        public DeleteWorkitemMessage() : base()
        {
            msg.command = "deleteworkitem";
        }
        public string _id { get; set; }
    }
    public class AddWorkitemQueueMessage<T> : SocketCommand where T : IWorkitemQueue
    {
        public AddWorkitemQueueMessage() : base()
        {
            msg.command = "addworkitemqueue";
        }
        public string name { get; set; }
        public string workflowid { get; set; }
        public string robotqueue { get; set; }
        public string amqpqueue { get; set; }
        public string projectid { get; set; }
        public int maxretries { get; set; }
        public int retrydelay { get; set; }
        public int initialdelay { get; set; }
        public bool skiprole { get; set; }
        public string success_wiqid { get; set; }
        public string failed_wiqid { get; set; }
        public string success_wiq { get; set; }
        public string failed_wiq { get; set; }
        public ace[] _acl { get; set; }
        public T result { get; set; }
    }
    public class GetWorkitemQueueMessage<T> : SocketCommand where T : IWorkitemQueue
    {
        public GetWorkitemQueueMessage() : base()
        {
            msg.command = "getworkitemqueue";
        }
        public string _id { get; set; }
        public string name { get; set; }
        public T result { get; set; }
    }
    public class UpdateWorkitemQueueMessage<T> : SocketCommand where T : IWorkitemQueue
    {
        public UpdateWorkitemQueueMessage() : base()
        {
            msg.command = "updateworkitemqueue";
        }
        public string _id { get; set; }
        public string name { get; set; }
        public string workflowid { get; set; }
        public string robotqueue { get; set; }
        public string amqpqueue { get; set; }
        public string projectid { get; set; }
        public int maxretries { get; set; }
        public int retrydelay { get; set; }
        public int initialdelay { get; set; }
        public string success_wiqid { get; set; }
        public string failed_wiqid { get; set; }
        public string success_wiq { get; set; }
        public string failed_wiq { get; set; }
        public bool purge { get; set; }
        public ace[] _acl { get; set; }
        public T result { get; set; }
    }
    public class DeleteWorkitemQueueMessage : SocketCommand
    {
        public DeleteWorkitemQueueMessage() : base()
        {
            msg.command = "deleteworkitemqueue";
        }
        public string _id { get; set; }
        public string name { get; set; }
        public bool purge { get; set; }
    }

}
