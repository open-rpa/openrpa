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
            lock (_lock)
            {
                try
                {
                    var i = WorkflowInstance.Instances.Where(x => x.InstanceId == instanceId.ToString()).FirstOrDefault();
                    if(i!=null)
                    {
                        i.xml = Base64Encode(doc);
                        if(string.IsNullOrEmpty(i._id))
                        {
                            i.Save();
                        } 
                        else if (global.isConnected)
                        {
                            i.Save();
                            //var v = JObject.Parse("{}");
                            //v.Add("xml", JToken.FromObject(Base64Encode(doc)));
                            //var set = JObject.Parse("{}");
                            //set.Add("$set", v);
                            //_ = global.webSocketClient.UpdateOne("openrpa_instances", "{_id: '" + i._id + "'}", 1, false, set);
                        }   
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("OpenFlowInstanceStore.save: " + ex.Message);
                }
            }
        }
        public override string Load(Guid instanceId, Guid storeId)
        {
            try
            {
                var i = WorkflowInstance.Instances.Where(x => x.InstanceId == instanceId.ToString()).FirstOrDefault();
                if (i != null)
                {
                    if(string.IsNullOrEmpty(i.xml))
                    {
                        Log.Error("Error locating " + instanceId.ToString() + " in Instance Store ( found but state is empty!!!!) ");
                        return null;
                    }
                    Log.Debug("Loading " + instanceId.ToString() + " from Instance Store");
                    return Base64Decode(i.xml);
                }
                Log.Error("Error locating " + instanceId.ToString() + " in Instance Store");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error("OpenFlowInstanceStore.save: " + ex.Message);
            }
            return null;
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            if (string.IsNullOrEmpty(base64EncodedData)) return null;
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

    }
}
