using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace OpenRPA.PS
{
    [Cmdlet(VerbsCommon.Get, "Entity")]
    public class GetEntity : OpenRPACmdlet, IDynamicParameters
    {
        [Parameter()] public string Query { get; set; }
        [Parameter()] public string Projection { get; set; }
        [Parameter()] public string Orderby { get; set; }
        [Parameter()] public int Top { get; set; }
        [Parameter()] public int Skip { get; set; }
        [Parameter()] public string QueryAs { get; set; }
        [Parameter()] public string Type { get; set; }
        private static RuntimeDefinedParameterDictionary _staticStorage;
        public object GetDynamicParameters()
        {
            if (_Collections == null)
            {
                Initialize().Wait();
            }
            // IEnumerable<string> Collections = new string[] { "entities", "workflow_instances", "nodered", "openrpa_instances", "workflow", "users", "audit", "forms", "openrpa" };
            var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();
            var attrib = new Collection<Attribute>()
            {
                // Mandatory = true,
                new ParameterAttribute() {
                    HelpMessage = "What collection to query, default is entities",
                    Position = 1
                },
                new ValidateSetAttribute(_Collections)
            };
            var parameter = new RuntimeDefinedParameter("Collection", typeof(string), attrib);
            runtimeDefinedParameterDictionary.Add("Collection", parameter);
            _staticStorage = runtimeDefinedParameterDictionary;
            return runtimeDefinedParameterDictionary;
        }
        protected override async Task ProcessRecordAsync()
        {
            try
            {
                var CollectionRuntime = new RuntimeDefinedParameter();
                _staticStorage.TryGetValue("Collection", out CollectionRuntime);
                string Collection = "entities";
                if (CollectionRuntime.Value != null && !string.IsNullOrEmpty(CollectionRuntime.Value.ToString())) Collection = CollectionRuntime.Value.ToString();
                // if (Top == 0) Top = 100;

                JObject q = null;
                string json = "";
                try
                {
                    q = JObject.Parse(Query);
                    json = q.ToString();
                }
                catch (Exception)
                {
                }
                if (string.IsNullOrEmpty(json) && Query != null)
                {
                    Query = Query.Trim();
                    if (!Query.StartsWith("{") && !Query.EndsWith("}")) Query = "{" + Query + "}";
                    q = JObject.Parse(Query);
                    json = q.ToString();
                }
                if (!string.IsNullOrEmpty(Type))
                {
                    if (q == null) q = JObject.Parse("{}");
                    q["_type"] = Type;
                    json = q.ToString();
                }
                var entities = await global.webSocketClient.Query<JObject>(Collection, json, Projection, Top, Skip, Orderby, QueryAs);
                var results = new List<PSObject>();
                int index = 0;
                foreach (var entity in entities)
                {
                    if (entity.ContainsKey("name"))
                    {
                        WriteVerbose("Parsing " + entity.Value<string>("_id") + " " + entity.Value<string>("name"));
                    }
                    else
                    {
                        WriteVerbose("Parsing " + entity.Value<string>("_id"));
                    }
                    results.Add(entity.toPSObjectWithTypeName(Collection));
                    // WriteObject(entity.toPSObjectWithTypeName(Collection));
                    index++;
                    if (index % 10 == 9) await Task.Delay(1);
                }
                WriteObject(results, false);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
            }
        }
    }
}
