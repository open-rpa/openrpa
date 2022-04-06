using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IWorkitem : IBase
    {
        string wiqid { get; set; }
        string wiq { get; set; }
        string state { get; set; }
        Dictionary<string, object> payload { get; set; }
        int retries { get; set; }
        int priority { get; set; }
        WorkitemFile[] files { get; set; }
        string username { get; set; }
        string userid { get; set; }
        DateTime? lastrun { get; set; }
        DateTime? nextrun { get; set; }
        string errormessage { get; set; }
        string errorsource { get; set; }
    }
    public interface IWorkitemQueue : IBase
    {
        string workflowid { get; set; }
        string robotqueue { get; set; }
        string amqpqueue { get; set; }
        string projectid { get; set; }
        int maxretries { get; set; }
        int retrydelay { get; set; }
        int initialdelay { get; set; }
    }
    public class MessageWorkitemFile
    {
        public string file { get; set; }
        public string filename { get; set; }
        public bool compressed { get; set; }

    }
    public class WorkitemFile
    {
        public string name { get; set; }
        public string filename { get; set; }
        public string _id { get; set; }
    }
    public class AddWorkitem
    {
        public Dictionary<string, object> payload { get; set; }
        public string name { get; set; }
        public DateTime? nextrun { get; set; }
        public int priority { get; set; }
        public MessageWorkitemFile[] files { get; set; }
    }
}
