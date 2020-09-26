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
        public PSObject Object { get; set; }
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = true, ParameterSetName = "withJson")]
        public string json { get; set; }
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "byid", ValueFromPipelineByPropertyName = false)]
        public string Id { get; set; }
        private static RuntimeDefinedParameterDictionary _staticStorage;
        public object GetDynamicParameters()
        {
            // IEnumerable<string> Collections = new string[] { "entities", "workflow_instances", "nodered", "openrpa_instances", "workflow", "users", "audit", "forms", "openrpa" };
            var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();
            var attrib = new Collection<Attribute>()
            {
                new ParameterAttribute() {
                    HelpMessage = "What collection to query, default is entities",
                    Mandatory = false,
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
                string Collection = "";
                if (CollectionRuntime.Value != null && !string.IsNullOrEmpty(CollectionRuntime.Value.ToString())) Collection = CollectionRuntime.Value.ToString();
                string col = Collection;
                if (string.IsNullOrEmpty(Id))
                {
                    if (Object != null)
                    {
                        json = Object.toJson();
                    }
                    JObject tmpObject = JObject.Parse(json);
                    if (string.IsNullOrEmpty(col)) // If not overwriten by param, then check for old collection
                    {
                        if (tmpObject.ContainsKey("__pscollection"))
                        {
                            col = tmpObject.Value<string>("__pscollection");
                        } else
                        {
                            col = "entities";
                        }
                    }
                    Id = tmpObject.Value<string>("_id");
                }
                if(string.IsNullOrEmpty(col))
                {
                    Id = null;
                    WriteError(new ErrorRecord(new ArgumentException("Collection is mandatory when not using objects"), "", ErrorCategory.InvalidArgument, null));
                    return;
                }
                await global.webSocketClient.DeleteOne(col, Id);
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
