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
    [Cmdlet(VerbsCommon.Remove, "Entity")]
    public class RemoveEntity : OpenRPACmdlet, IDynamicParameters
    {
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = true, ParameterSetName = "withObject")]
        public List<object> Objects { get; set; }
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "withJson")]
        public string json { get; set; }
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "byid", ValueFromPipelineByPropertyName = false)]
        public string Id { get; set; }
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "asquery")]
        public string Query { get; set; }
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
                string traceId = ""; string spanId = "";
                var CollectionRuntime = new RuntimeDefinedParameter();
                _staticStorage.TryGetValue("Collection", out CollectionRuntime);
                string Collection = "";
                if (CollectionRuntime.Value != null && !string.IsNullOrEmpty(CollectionRuntime.Value.ToString())) Collection = CollectionRuntime.Value.ToString();
                if (string.IsNullOrEmpty(Collection)) Collection = "entities";
                string col = Collection;
                if (!string.IsNullOrEmpty(Query))
                {
                    var affectedrows = await global.webSocketClient.DeleteMany(col, Query, traceId, spanId);
                    WriteVerbose("Removed " + affectedrows + " rows from " + col);
                    return;
                }
                if (Objects != null && Objects.Count > 0)
                {
                    var colls = new Dictionary<string, List<string>>();
                    foreach (PSObject obj in Objects)
                    {
                        col = Collection;
                        if (obj.Properties["__pscollection"] != null && obj.Properties["__pscollection"].Value != null)
                        {
                            col = obj.Properties["__pscollection"].Value.ToString();
                        }
                        if (!colls.ContainsKey(col)) colls.Add(col, new List<string>());
                        Id = obj.Properties["_id"].Value.ToString();
                        colls[col].Add(Id);
                    }
                    foreach (var kv in colls)
                    {
                        var affectedrows = await global.webSocketClient.DeleteMany(kv.Key, kv.Value.ToArray(), traceId, spanId);
                        WriteVerbose("Removed " + affectedrows + " rows from " + col);
                    }
                    return;
                }
                if (!string.IsNullOrEmpty(json))
                {
                    JObject tmpObject = JObject.Parse(json);
                    if (string.IsNullOrEmpty(col)) // If not overwriten by param, then check for old collection
                    {
                        if (tmpObject.ContainsKey("__pscollection"))
                        {
                            col = tmpObject.Value<string>("__pscollection");
                        }
                        else
                        {
                            col = "entities";
                        }
                    }
                    Id = tmpObject.Value<string>("_id");
                }
                if (string.IsNullOrEmpty(col))
                {
                    Id = null;
                    WriteError(new ErrorRecord(new ArgumentException("Collection is mandatory when not using objects"), "", ErrorCategory.InvalidArgument, null));
                    return;
                }
                WriteVerbose("Removing " + Id + " from " + col);
                await global.webSocketClient.DeleteOne(col, Id, traceId, spanId);
                Id = null;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
                Id = null;
            }
        }
    }
}
