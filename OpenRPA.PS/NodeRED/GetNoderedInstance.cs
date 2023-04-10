//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OpenRPA.Interfaces;
//using System.Management.Automation;
//using Newtonsoft.Json.Linq;
//using System.Collections.ObjectModel;

//namespace OpenRPA.PS
//{
//    [Cmdlet(VerbsCommon.Get, "NoderedInstance")]
//    public class GetNoderedInstance : OpenRPACmdlet
//    {
//        [Parameter(Position = 0, ValueFromPipeline = true)] public string _id { get; set; }
//        [Parameter(Position = 1)] public int Minutes { get; set; }
//        protected override async Task ProcessRecordAsync()
//        {
//            try
//            {
//                if (Minutes < 1) Minutes = 1;
//                // string dt = DateTime.UtcNow.AddMinutes(Minutes).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
//                string dt = DateTime.UtcNow.AddMinutes(-Minutes).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", System.Globalization.CultureInfo.InvariantCulture);
//                string json = "{\"_type\":\"user\", \"_noderedheartbeat\": { \"$gte\": \"" + dt + "\" } }";
//                if (!string.IsNullOrEmpty(_id)) { json = "{\"_type\":\"user\", \"_id\": \"" + _id + "\"}"; }
//                var entities = await global.webSocketClient.Query<JObject>("users", json, top: 100);
//                int index = 0;
//                foreach (var entity in entities)
//                {
//                    if (entity.ContainsKey("name"))
//                    {
//                        WriteVerbose("Parsing " + entity.Value<string>("_id") + " " + entity.Value<string>("name"));
//                    }
//                    else
//                    {
//                        WriteVerbose("Parsing " + entity.Value<string>("_id"));
//                    }
//                    // results.Add(obj);
//                    WriteObject(entity.toPSObjectWithTypeName("users"));
//                    index++;
//                    if (index % 10 == 9) await Task.Delay(1);
//                }

//            }
//            catch (Exception ex)
//            {
//                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
//            }


//        }
//    }
//}
