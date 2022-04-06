using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class WorkitemQueue : LocallyCached , IWorkitemQueue
    {
        public WorkitemQueue()
        {
            _type = "workitemqueue";
        }
        public string Plugin { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string projectid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string workflowid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string robotqueue { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string amqpqueue { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public int maxretries { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public int retrydelay { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public int initialdelay { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public async Task Save()
        {
            await Save<WorkitemQueue>();
        }
        public async Task Delete()
        {
            await Delete<WorkitemQueue>();
        }
        public void ExportFile(string filepath)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText(filepath, json);
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(name)) return "WorkItemQueue";
            return name;
        }
    }
    public class Workitem : LocallyCached, IWorkitem
    {
        public Workitem()
        {
            _type = "workitem";
            payload = new Dictionary<string, object>();
        }
        public Dictionary<string, object> payload { get { return GetProperty<Dictionary<string, object>>(); } set { SetProperty(value); } }
        public Interfaces.WorkitemFile[] files { get { return GetProperty<Interfaces.WorkitemFile[]>(); } set { SetProperty(value); } }
        // IWorkitemFile[] IWorkitem.files { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string state { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string wiq { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string wiqid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public int retries { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public int priority { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public string username { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string userid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public DateTime? lastrun { get { return GetProperty<DateTime?>(); } set { SetProperty(value); } }
        public DateTime? nextrun { get { return GetProperty<DateTime?>(); } set { SetProperty(value); } }
        public string errormessage { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string errorsource { get { return GetProperty<string>(); } set { SetProperty(value); } }
    }
    public class WorkitemFile : Interfaces.WorkitemFile
    {
        //public string name { get; set; }
        //public string filename { get; set; }
        //public string _id { get; set; }
    }
}
