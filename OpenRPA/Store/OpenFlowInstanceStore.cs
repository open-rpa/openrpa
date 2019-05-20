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
                        _ = i.Save();
                    }
                    //if (!global.isConnected) return;
                    //Console.WriteLine(instanceId.ToString());
                    //var results = global.webSocketClient.Query<WorkflowInstanceState>("openrpa_instances", "{InstanceId: '" + instanceId.ToString() + "'}").Result;
                    //if(results!=null && results.Count()>0)
                    //{
                    //    var i = results[0];
                    //    i.xml = Base64Encode(doc);
                    //    _ = global.webSocketClient.UpdateOne("openrpa_instances", i);
                    //}

                    //var s = new WorkflowInstanceState { fqdn = fqdn, host = host, InstanceId = instanceId.ToString(), xml = Base64Encode(doc) };
                    //if (!global.isConnected) return;
                    //_ = global.webSocketClient.InsertOne("openrpa_instances", s);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("OpenFlowInstanceStore.save: " + ex.Message);
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
                    return Base64Decode(i.xml);
                }
                return null;
                //if (!global.isConnected) return null;
                //var results = global.webSocketClient.Query< WorkflowInstanceState>("openrpa_instances", "{instance: '" + instanceId.ToString() + "'}").Result;
                //if (results == null)
                //{
                //    System.Diagnostics.Trace.WriteLine("Cannot resume workflow instanse with id " + instanceId.ToString() + " it has no state in OpenFlow");
                //    throw new ArgumentException("Cannot resume workflow instanse with id " + instanceId.ToString() + " it has no state in OpenFlow");
                //}
                //if (results.Count() == 0)
                //{
                //    System.Diagnostics.Trace.WriteLine("Cannot resume workflow instanse with id " + instanceId.ToString() + " it has no state in OpenFlow");
                //    throw new ArgumentException("Cannot resume workflow instanse with id " + instanceId.ToString() + " it has no state in OpenFlow");
                //}

                //if (results.Count() > 2)
                //{
                //    var s2 = results.First();
                //}
                //var s = results.First();
                //return Base64Decode(s.xml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("OpenFlowInstanceStore.save: " + ex.Message);
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
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

    }
}
