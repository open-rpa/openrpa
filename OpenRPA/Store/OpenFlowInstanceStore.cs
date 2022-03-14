using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Store
{
    class OpenFlowInstanceStore : CustomInstanceStoreBase
    {
        private static Guid storeId = new Guid("0bfcc3a5-3c77-421b-b575-73533563a1f3");
        public string fqdn { get; set; }
        public string host { get; set; }
        public OpenFlowInstanceStore() : base(storeId)
        {
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName.ToLower();
            host = Environment.MachineName.ToLower();
        }
        private static object _lock = new object();
        public override void Save(Guid instanceId, Guid storeId, string doc)
        {
            try
            {
                var instance = WorkflowInstance.Instances.Where(x => x.InstanceId == instanceId.ToString()).FirstOrDefault();
                if (instance != null)
                {
                    if (instance != null && instance.Workflow != null && instance.Workflow.Serializable == true)
                    {
                        instance.xml = Interfaces.Extensions.Base64Encode(doc);
                        _ = instance.Save<WorkflowInstance>(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("OpenFlowInstanceStore.save: " + ex.Message);
            }
        }
        public override string Load(Guid instanceId, Guid storeId)
        {
            try
            {
                var instance = WorkflowInstance.Instances.Where(x => x.InstanceId == instanceId.ToString()).FirstOrDefault();
                if (instance != null)
                {
                    if (string.IsNullOrEmpty(instance.xml))
                    {
                        Log.Error("Error locating " + instanceId.ToString() + " in Instance Store ( found but state is empty!!!!) ");
                        return null;
                    }
                    if (instance.Workflow != null && instance.Workflow.Serializable == false)
                    {
                        Log.Error("Instance " + instanceId.ToString() + " was found in Instance Store, but workflow now has Serializable=false");
                        return null;
                    }
                    Log.Debug("Loading " + instanceId.ToString() + " from Instance Store");
                    return Interfaces.Extensions.Base64Decode(instance.xml);
                }
                Log.Error("Error locating " + instanceId.ToString() + " in Instance Store");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error("OpenFlowInstanceStore.Load: " + ex.Message);
            }
            return null;
        }
    }
}
