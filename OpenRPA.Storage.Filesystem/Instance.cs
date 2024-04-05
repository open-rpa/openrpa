using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OpenRPA.Storage.Filesystem
{
    public class Instance : IStorage
    {
        public string Name { get; set; }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Initialize()
        {
            _ = PluginConfig.strict;
            if (!PluginConfig.enabled) return;
        }
        private string Collection<T>() where T : class
        {
            var basepath = System.IO.Path.GetDirectoryName(Config.SettingsFile);
            if(Config.local.wsurl == null || Config.local.wsurl == "")
            {
                basepath = System.IO.Path.Combine(basepath, "offline");
            } 
            else
            {
                basepath = System.IO.Path.Combine(basepath, new Uri(Config.local.wsurl).Host);
            }
            if (typeof(IWorkflow).IsAssignableFrom(typeof(T)))
            {
                return System.IO.Path.Combine(basepath, "workflows");
            }
            else if (typeof(IDetector).IsAssignableFrom(typeof(T)))
            {
                return System.IO.Path.Combine(basepath, "detectors");
            }
            else if (typeof(IProject).IsAssignableFrom(typeof(T)))
            {
                return System.IO.Path.Combine(basepath, "projects");
            }
            else if (typeof(IWorkitemQueue).IsAssignableFrom(typeof(T)))
            {
                return System.IO.Path.Combine(basepath, "workitemqueues");
            }
            else if (typeof(IWorkitem).IsAssignableFrom(typeof(T)))
            {
                return System.IO.Path.Combine(basepath, "workitems");
            }
            else if (typeof(IWorkflowInstance).IsAssignableFrom(typeof(T)))
            {
                return System.IO.Path.Combine(basepath, "openrpa_instances");
            }
            return null;
        }
        public async Task<T[]> FindAll<T>() where T : apibase
        {
            if (!PluginConfig.enabled) return Array.Empty<T>();
            var result = new System.Collections.Generic.List<T>();
            string path = Collection<T>();
            if (!System.IO.Directory.Exists(path)) return result.ToArray();
            string[] files = System.IO.Directory.GetFiles(path);
            foreach (string filepath in files)
            {
                string json = System.IO.File.ReadAllText(filepath);
                var o = JObject.Parse(json);
                var isDirty = false;
                if (o.ContainsKey("isDirty")) isDirty = (bool)o["isDirty"];
                var IsExpanded = false;
                if(o.ContainsKey("IsExpanded")) IsExpanded = (bool)o["IsExpanded"];
                var IsSelected = false;
                if (o.ContainsKey("IsSelected")) IsSelected = (bool)o["IsSelected"];
                T item = JsonConvert.DeserializeObject<T>(json);
                if (typeof(IWorkflow).IsAssignableFrom(typeof(T))) // TODO: Why is this needed ?????
                {
                    var wf = (IWorkflow)item;
                    wf.IsExpanded = IsExpanded;
                    wf.IsSelected = IsSelected;
                }
                if (typeof(IProject).IsAssignableFrom(typeof(T))) // TODO: Why is this needed ?????
                {
                    var p = (IProject)item;
                    p.IsExpanded = IsExpanded;
                    p.IsSelected = IsSelected;
                }
                if (item._id == "6290c42282d133c035022ae0")
                {
                    var b = true;
                }
                item.isDirty = isDirty; // Deserializing will make the object dirty, so restore the saved state
                result.Add(item);
            }
            return result.ToArray();
        }
        public async Task<T> FindById<T>(string id) where T : apibase
        {
            if (!PluginConfig.enabled) return null;
            string path = Collection<T>();
            string filepath = System.IO.Path.Combine(path, id) + ".json";
            if (!System.IO.File.Exists(filepath)) return null;
            string json = System.IO.File.ReadAllText(filepath);
            var o = JObject.Parse(json);
            var isDirty = (bool)o["isDirty"];
            T item = JsonConvert.DeserializeObject<T>(json);
            item.isDirty = isDirty; // Deserializing will make the object dirty, so restore the saved state
            return item;
        }
        public async Task<T> Insert<T>(T item) where T : apibase
        {
            if (!PluginConfig.enabled) return item;
            // When working locally, we do NOT want to ignore these properties
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DoNotIgnoreResolver()
            };
            var json = JsonConvert.SerializeObject(item, settings);
            var o = JObject.Parse(json);
            string path = Collection<T>();
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            var id = (string)o["_id"];
            if (id == null)
            {
                id = Guid.NewGuid().ToString();
                o["_id"] = id;
                json = JsonConvert.SerializeObject(o, settings);
            }
            string filepath = System.IO.Path.Combine(path, id) + ".json";
            if (PluginConfig.strict == true && System.IO.File.Exists(filepath)) throw new Exception("Object with " + id + " already exists!");
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(filepath, json);
            var o2 = JObject.Parse(json);
            var isDirty = (bool)o2["isDirty"];
            T newitem = JsonConvert.DeserializeObject<T>(json);
            item.isDirty = isDirty; // Deserializing will make the object dirty, so restore the saved state
            return newitem;
        }
        public async Task<T> Update<T>(T item) where T : apibase
        {
            if (!PluginConfig.enabled) return item;
            // When working locally, we do NOT want to ignore these properties
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DoNotIgnoreResolver()
            };
            var json = JsonConvert.SerializeObject(item, settings);
            var o = JObject.Parse(json);
            string path = Collection<T>();
            var id = (string)o["_id"];
            if (id == null || id == "") throw new Exception("object is missing an _id");
            string filepath = System.IO.Path.Combine(path, id) + ".json";
            if (PluginConfig.strict == true && !System.IO.File.Exists(filepath)) throw new Exception("Object with " + id + " does not exists!");
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(filepath, json);
            return item;
        }
        public async Task Delete<T>(string id) where T : apibase
        {
            if (!PluginConfig.enabled) return;
            string path = Collection<T>();
            string filepath = System.IO.Path.Combine(path, id) + ".json";
            if (System.IO.File.Exists(filepath))
            {
                System.IO.File.Delete(filepath);
            }
            else if (PluginConfig.strict == true)
            {
                throw new Exception("Object with " + id + " does not exists!");
            }
        }
        public void Dispose()
        {
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
