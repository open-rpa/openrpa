using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.IO;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace OpenRPA.Store
{

    public abstract class JsonInstanceStoreBase : InstanceStore, IDisposable
    {
        /// <summary>
        /// A unique identifier for the store of instances. There will usually be one store id for all workflows
        /// in an application. If one is not specified, then one will be generated.
        /// </summary>
        private Guid _storeId;

        /// <summary>
        /// Internal handle used to identify the workflow owner.
        /// </summary>
        private InstanceHandle _handle;

        public JsonInstanceStoreBase(Guid storeId)
        {
            _storeId = storeId;

            _handle = this.CreateInstanceHandle();
            var view = this.Execute(_handle, new CreateWorkflowOwnerCommand(), TimeSpan.FromSeconds(30));
            this.DefaultInstanceOwner = view.InstanceOwner;
        }

        public abstract void Save(Guid instanceId, Guid storeId, string doc);
        public abstract string Load(Guid instanceId, Guid storeId);

        // Synchronous version of the Begin/EndTryCommand functions
        protected override bool TryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout)
        {
            return EndTryCommand(BeginTryCommand(context, command, timeout, null, null));
        }

        // The persistence engine will send a variety of commands to the configured InstanceStore,
        // such as CreateWorkflowOwnerCommand, SaveWorkflowCommand, and LoadWorkflowCommand.
        // This method is where we will handle those commands.
        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                IDictionary<XName, InstanceValue> instanceStateData = null;

                //The CreateWorkflowOwner command instructs the instance store to create a new instance owner bound to the instanace handle
                if (command is CreateWorkflowOwnerCommand)
                {
                    context.BindInstanceOwner(_storeId, Guid.NewGuid());
                }
                //The SaveWorkflow command instructs the instance store to modify the instance bound to the instance handle or an instance key
                else if (command is SaveWorkflowCommand)
                {
                    SaveWorkflowCommand saveCommand = (SaveWorkflowCommand)command;
                    instanceStateData = saveCommand.InstanceData;

                    var instanceStateXml = DictionaryToXml(instanceStateData);
                    Save(context.InstanceView.InstanceId, this._storeId, instanceStateXml);
                }
                //The LoadWorkflow command instructs the instance store to lock and load the instance bound to the identifier in the instance handle
                else if (command is LoadWorkflowCommand)
                {
                    var xml = Load(context.InstanceView.InstanceId, this._storeId);
                    instanceStateData = XmlToDictionary(xml);
                    //load the data into the persistence Context
                    context.LoadedInstance(InstanceState.Initialized, instanceStateData, null, null, null);
                }

                return new CompletedAsyncResult<bool>(true, callback, state);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override bool EndTryCommand(IAsyncResult result)
        {
            return CompletedAsyncResult<bool>.End(result);
        }

        // Converts XML data back to the original form.
        private IDictionary<XName, InstanceValue> XmlToDictionary(string data)
        {
            try
            {
                IDictionary<System.Xml.Linq.XName, InstanceValue> result = new Dictionary<System.Xml.Linq.XName, InstanceValue>();

                //NetDataContractSerializer s = new NetDataContractSerializer();
                JArray doc = JArray.Parse(data);
                foreach (JObject instanceElement in doc)
                {
                    try
                    {
                        //XmlElement keyElement = (XmlElement)instanceElement.SelectSingleNode("descendant::key");
                        //System.Xml.Linq.XName key = (System.Xml.Linq.XName)DeserializeObject(s, keyElement);

                        //XmlElement valueElement = (XmlElement)instanceElement.SelectSingleNode("descendant::value");
                        //object value = DeserializeObject(s, valueElement);
                        var typestring = instanceElement.Value<string>("type");
                        Type type = Type.GetType(typestring);
                        var json = instanceElement.Value<string>("value");
                        if (type.FullName == "System.Activities.Runtime.ActivityExecutor")
                        {
                            //var h = new System.Activities.Hosting.WorkflowInstance(null);
                            
                            object o = Activator.CreateInstance(type); // an instance of target type
                        }
                        var val = JsonConvert.DeserializeObject(json, type, new JsonSerializerSettings
                        {
                            Error = (sender, errorArgs) =>
                            {
                                var currentError = errorArgs.ErrorContext.Error.Message;
                                errorArgs.ErrorContext.Handled = true;
                            }
                        });
                        InstanceValue instVal = new InstanceValue(val);
                        var name = instanceElement.Value<string>("key");
                        var xname = (XName)name;
                        result.Add(xname, instVal);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }

                return result;

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        //object DeserializeObject(NetDataContractSerializer serializer, JObject element)
        //{
        //    try
        //    {
        //        object deserializedObject = null;

        //        MemoryStream stm = new MemoryStream();
        //        XmlDictionaryWriter wtr = XmlDictionaryWriter.CreateTextWriter(stm);
        //        element.WriteContentTo(wtr);
        //        wtr.Flush();
        //        stm.Position = 0;

        //        deserializedObject = serializer.Deserialize(stm);

        //        return deserializedObject;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.ToString(), rpanet.tracecategory.Error);
        //        throw;
        //    }
        //}

        // Converts the persistence data to XML form.
        string DictionaryToXml(IDictionary<XName, InstanceValue> instanceData)
        {
            try
            {
                JArray doc = new JArray();
                //doc.LoadXml("<InstanceValues/>");

                foreach (KeyValuePair<XName, InstanceValue> valPair in instanceData)
                {
                    var json = JsonConvert.SerializeObject(valPair.Value.Value, Newtonsoft.Json.Formatting.Indented,
                        new JsonSerializerSettings()
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        });
                    var type = valPair.Value.Value.GetType();
                    //var json = JsonConvert.SerializeObject(valPair.Value.Value);
                    var o = new JObject( 
                        new JProperty[] { new JProperty("key", valPair.Key.ToString()), new JProperty("value", json)
                        , new JProperty("type", type.AssemblyQualifiedName  )});
                    // type.FullName    type.AssemblyQualifiedName
                    doc.Add(o);

                    //XmlElement newInstance = doc.CreateElement("InstanceValue");

                    //XmlElement newKey = SerializeObject("key", valPair.Key, doc);
                    //newInstance.AppendChild(newKey);

                    //XmlElement newValue = SerializeObject("value", valPair.Value.Value, doc);
                    //newInstance.AppendChild(newValue);

                    //doc.DocumentElement.AppendChild(newInstance);
                }

                return doc.ToString();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        //XmlElement SerializeObject(string elementName, object o, JObject doc)
        //{
        //    try
        //    {
        //        NetDataContractSerializer s = new NetDataContractSerializer();
        //        XmlElement newElement = doc.CreateElement(elementName);
        //        MemoryStream stm = new MemoryStream();

        //        s.Serialize(stm, o);
        //        stm.Position = 0;
        //        StreamReader rdr = new StreamReader(stm);
        //        newElement.InnerXml = rdr.ReadToEnd();

        //        return newElement;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.ToString());
        //        throw;
        //    }
        //}

        public void Dispose()
        {
            this.Execute(_handle, new DeleteWorkflowOwnerCommand(), TimeSpan.FromSeconds(30));
            _handle.Free();            
        }
    }
}