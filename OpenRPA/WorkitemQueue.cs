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
        public string projectid { 
            get { 
                return GetProperty<string>(); 
            } 
            set {
                var current = projectid;
                if(current != value)
                {
                    SetProperty(value);
                }
                if(RobotInstance.instance.WorkItemQueues.Contains(this))
                {
                    var index = RobotInstance.instance.WorkItemQueues.IndexOf(this);
                    RobotInstance.instance.WorkItemQueues.Remove(this);
                    RobotInstance.instance.WorkItemQueues.Insert(index, this);
                }
            } 
        }
        public string workflowid { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string robotqueue { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string amqpqueue { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public int maxretries { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public int retrydelay { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public int initialdelay { get { return GetProperty<int>(); } set { SetProperty(value); } }
        public async Task Save(bool skipOnline = false)
        {
            if(string.IsNullOrEmpty(_id) && global.webSocketClient != null && global.webSocketClient.isConnected)
            {
                try
                {
                    var wiq = await global.webSocketClient.AddWorkitemQueue(this);
                    EnumerableExtensions.CopyPropertiesTo(wiq, this, true);
                }
                catch (Exception)
                {
                    throw;
                }
            } else if (string.IsNullOrEmpty(_id) && !string.IsNullOrEmpty(Config.local.wsurl))
            {
                throw new Exception("Failed adding WorkitemQueue, not online!");
            }
            var ___id = _id;
            bool hadError = true;
            try
            {
                isDirty = false;
                await Save<WorkitemQueue>(skipOnline);
                hadError = false;
            }
            catch (Exception ex)
            {
                isDirty = true;
                Log.Error(ex.Message);
            }
            if(hadError)
            {
                try
                {
                    var q = await global.webSocketClient.Query<WorkitemQueue>("mq", "{\"_id\": \"" + _id + "\"}", null, 1);
                    if (q.Length > 0)
                    {
                        EnumerableExtensions.CopyPropertiesTo(q[0], this, true);
                        isDirty = false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
            else if (System.Threading.Monitor.TryEnter(RobotInstance.instance.WorkItemQueues, 1000))
            {
                try
                {
                    var exists = RobotInstance.instance.WorkItemQueues.FindById(_id);
                    if (exists == null) RobotInstance.instance.WorkItemQueues.Add(this);
                    if (exists != null) RobotInstance.instance.WorkItemQueues.UpdateItem(exists, this);
                }
                finally
                {
                    System.Threading.Monitor.Exit(RobotInstance.instance.WorkItemQueues);
                }
            }
        }
        public async Task Update(IWorkitemQueue item, bool skipOnline = false)
        {
            RobotInstance.instance.WorkItemQueues.UpdateItem(this, item);
            isDirty = false;
            await Save<WorkitemQueue>(skipOnline);
        }
        public async Task Delete(bool skipOnline = false)
        {
            if(!skipOnline) await Delete<WorkitemQueue>();
            if (System.Threading.Monitor.TryEnter(RobotInstance.instance.WorkItemQueues, 1000))
            {
                try
                {
                    RobotInstance.instance.WorkItemQueues.Remove(this);
                }
                finally
                {
                    System.Threading.Monitor.Exit(RobotInstance.instance.WorkItemQueues);
                }
            }
        }
        public void ExportFile(string filepath)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText(filepath, json);
        }
        public async Task Purge()
        {
            if (string.IsNullOrEmpty(_id) && global.webSocketClient != null && global.webSocketClient.isConnected)
            {
                var wiq = await global.webSocketClient.UpdateWorkitemQueue(this, true); ;
            } else if (string.IsNullOrEmpty( Config.local.wsurl))
            {
                RobotInstance.instance.dbWorkitems.DeleteMany(x => x.wiq == name);
                RobotInstance.instance.Workitems.Clear();
                RobotInstance.instance.Workitems.AddRange(RobotInstance.instance.dbWorkitems.FindAll().OrderBy(x => x.name));
            }
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
        public string errortype { get { return GetProperty<string>(); } set { SetProperty(value); } }

        public async Task Save(bool skipOnline = false)
        {
            await Save<Workitem>(skipOnline);
            if (string.IsNullOrEmpty(Config.local.wsurl))
            {
                GenericTools.RunUI(() => {
                    RobotInstance.instance.Workitems.Add(this);
                });
                
            }
        }
    }
    public class WorkitemFile : Interfaces.WorkitemFile
    {
        //public string name { get; set; }
        //public string filename { get; set; }
        //public string _id { get; set; }
    }
}
