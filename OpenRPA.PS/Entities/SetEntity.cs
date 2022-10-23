using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using System.Management.Automation;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.Diagnostics;

namespace OpenRPA.PS
{

    [Cmdlet(VerbsCommon.Add, "Entity")]
    public class AddEntity : SetEntity
    { }
    [Cmdlet(VerbsCommon.Set, "Entity")]
    public class SetEntity : OpenRPACmdlet, IDynamicParameters
    {
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = true, ParameterSetName = "withObjects")]
        public List<Object> Objects { get; set; }
        [Parameter(ValueFromPipeline = true, Position = 1, Mandatory = true, ParameterSetName = "withJson")]
        public string json { get; set; }
        [Parameter()] public string[] UniqueKeys { get; set; }
        [Parameter()] public string Type { get; set; }
        [Parameter()] public SwitchParameter SkipLowercaseName { get; set; }
        [Parameter(ParameterSetName = "withObjects")] public SwitchParameter SkipResults { get; set; }
        private static RuntimeDefinedParameterDictionary _staticStorage;
        [Parameter(Position = 2, ParameterSetName = "withObjects")]
        public int BatchSize { get; set; }
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

                // If not using UniqueKeys and we are trying to ADD multiple new items, use add many for better performance.
                if (Objects != null && Objects.Count > 1 && MyInvocation.InvocationName == "Add-Entity" && (UniqueKeys == null || UniqueKeys.Length == 0))
                {
                    var colls = new Dictionary<string, List<JObject>>();
                    var col = Collection;
                    foreach (PSObject obj in Objects)
                    {
                        var json = obj.toJson();
                        JObject tmpObject = JObject.Parse(json);
                        col = Collection;
                        if (tmpObject.ContainsKey("__pscollection") && tmpObject["__pscollection"] != null)
                        {
                            col = tmpObject.Value<string>("__pscollection");
                            tmpObject.Remove("__pscollection");
                        }
                        if (!colls.ContainsKey(col)) colls.Add(col, new List<JObject>());
                        colls[col].Add(tmpObject);
                    }
                    if (BatchSize <= 0) BatchSize = 15;
                    if (BatchSize < 2) BatchSize = 2;
                    foreach (var kv in colls)
                    {
                        var total = kv.Value.Count;
                        for (var i = 0; i < total; i = i + BatchSize)
                        {
                            if (Stopping) break;
                            var count = BatchSize;
                            if ((i + count) > total) count = total - i;
                            var items = kv.Value.GetRange(i, count).ToArray();
                            var affectedrows = await global.webSocketClient.InsertMany(kv.Key, 1, false, SkipResults.IsPresent, items, traceId, spanId);
                            WriteVerbose("Added " + (i + count) + " rows out of " + total + " to collection " + col);
                            foreach (var obj in affectedrows)
                            {
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
                            }
                        }
                    }
                    return;
                }
                else
                {
                    var PSObjects = new List<PSObject>();
                    if (Objects == null || Objects.Count == 0)
                    {
                        PSObjects.Add(json.JsonToPSObject());
                    }
                    else
                    {
                        foreach (var obj in Objects)
                        {
                            PSObjects.Add(JObject.FromObject(obj, new JsonSerializer()
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            }).ToString().JsonToPSObject());
                        }
                    }
                    foreach (PSObject Object in PSObjects)
                    {
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
                            obj = await global.webSocketClient.InsertOrUpdateOne(col, 1, false, uniqeness, tmpObject, traceId, spanId);
                        }
                        else
                        {
                            if (tmpObject.ContainsKey("_id"))
                            {
                                obj = await global.webSocketClient.UpdateOne(col, 1, false, tmpObject, traceId, spanId);
                            }
                            else
                            {
                                if (MyInvocation.InvocationName == "Add-Entity")
                                {
                                    obj = await global.webSocketClient.InsertOne(col, 1, false, tmpObject, traceId, spanId);
                                }
                                else
                                {
                                    WriteError(new ErrorRecord(new Exception("Missing _id and UniqueKeys, either use Add-Entity or set UniqueKeys"), "", ErrorCategory.NotSpecified, null));
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
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "", ErrorCategory.NotSpecified, null));
            }


        }
    }
}
