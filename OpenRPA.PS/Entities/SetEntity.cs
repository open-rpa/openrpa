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

    [Cmdlet(VerbsCommon.Add, "Entity")]
    public class AddEntity : SetEntity
    { }
    [Cmdlet(VerbsCommon.Set, "Entity")]
    public class SetEntity : OpenRPACmdlet, IDynamicParameters
    {
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = true, ParameterSetName = "withObject")]
        public PSObject Object { get; set; }
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = true, ParameterSetName = "withJson")]
        public string json { get; set; }
        [Parameter()] public string[] UniqueKeys { get; set; }
        [Parameter()] public string Type { get; set; }
        [Parameter()] public SwitchParameter SkipLowercaseName { get; set; }
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
                string Collection = "";
                if (CollectionRuntime.Value != null && !string.IsNullOrEmpty(CollectionRuntime.Value.ToString())) Collection = CollectionRuntime.Value.ToString();
                JObject obj = null;
                if (Object != null)
                {
                    json = Object.toJson();
                }
                JObject tmpObject = JObject.Parse(json);
                string col = Collection;
                if (string.IsNullOrEmpty(col)) // If not overwriten by param, then check for old collection
                {
                    if (tmpObject.ContainsKey("__pscollection"))
                    {
                        col = tmpObject.Value<string>("__pscollection");
                        tmpObject.Remove("__pscollection");
                    }
                    else { col = "entities"; }
                }
                else
                {
                    if (tmpObject.ContainsKey("__pscollection")) tmpObject.Remove("__pscollection");
                }
                if (!string.IsNullOrEmpty(Type))
                {
                    tmpObject["_type"] = Type;
                }
                if (!SkipLowercaseName.IsPresent)
                {
                    bool loopAgain = true;
                    while (loopAgain)
                    {
                        loopAgain = false;
                        foreach (var v in tmpObject)
                        {
                            if (v.Key.ToLower() == "name" && v.Key != "name")
                            {
                                tmpObject["name"] = tmpObject[v.Key];
                                tmpObject.Remove(v.Key);
                                loopAgain = true;
                                break;
                            }
                        }

                    }
                }
                if (MyInvocation.InvocationName == "Add-Entity")
                {
                    if (tmpObject.ContainsKey("_id")) tmpObject.Remove("_id");
                } 

                if (UniqueKeys != null && UniqueKeys.Length > 0)
                {
                    string uniqeness = string.Join(",", UniqueKeys);
                    obj = await global.webSocketClient.InsertOrUpdateOne(col, 1, false, uniqeness, tmpObject);
                }
                else
                {
                    if (tmpObject.ContainsKey("_id"))
                    {
                        obj = await global.webSocketClient.UpdateOne(col, 1, false, tmpObject);
                    } else
                    {
                        if (MyInvocation.InvocationName == "Add-Entity")
                        {
                            obj = await global.webSocketClient.InsertOne(col, 1, false, tmpObject);
                        } else
                        {
                            WriteError(new ErrorRecord(new Exception("Missing _id and UniqueKeys, either use Add-Entity or set UniqueKeys") , "", ErrorCategory.NotSpecified, null));
                            return;
                        }
                            
                    }
                }
                var _obj = obj.toPSObject();
                _obj.TypeNames.Insert(0, "OpenRPA.PS.Entity");
                if (Collection == "openrpa")
                {
                    if (obj.Value<string>("_type") == "workflow") _obj.TypeNames.Insert(0, "OpenRPA.Workflow");
                    if (obj.Value<string>("_type") == "project") _obj.TypeNames.Insert(0, "OpenRPA.Project");
                    if (obj.Value<string>("_type") == "detector") _obj.TypeNames.Insert(0, "OpenRPA.Detector");
                    if (obj.Value<string>("_type") == "unattendedclient") _obj.TypeNames.Insert(0, "OpenRPA.UnattendedClient");
                    if (obj.Value<string>("_type") == "unattendedserver") _obj.TypeNames.Insert(0, "OpenRPA.UnattendedServer");
                }
                WriteObject(_obj);
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
            }


        }
    }
}
