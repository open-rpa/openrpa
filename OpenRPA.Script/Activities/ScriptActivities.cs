using System;
using System.Activities.Presentation.Toolbox;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;


using OpenRPA.Interfaces;
using System.Activities;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using Python.Runtime;
using System.Collections.Concurrent;
using System.Text;
using System.Drawing;

namespace OpenRPA.Script.Activities
{
    public class ScriptItem
    {
        public string TypeKey { get; set; }
        public string DesignerIconFile { get; set; }
        public string DisplayName { get; set; }
        public string Code { get; set; }
        public string CodeFile { get; set; }
        public string Language { get; set; }
        public Type CodeType { get; set; }
    }

    public class ScriptActivities
    {
        private static List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        /// <summary>
        /// 加载动态指令脚本。
        /// </summary>
        /// <remarks>
        /// 动态指令脚本等文件存储在与settings.json同级目录下的script-activities/{host}/文件夹中。目录说明如下：
        /// <list type="bullet">
        ///     <item>
        ///         script-activities/{host}/summary.json文件格式：
        ///         <code>
        ///{
        ///    "items": [
        ///     {
        ///         "categoryName": "My Functions",
        ///         "moduleName": "MyFunctions",
        ///         "enable": true,
        ///         "scripts": [
        ///           {
        ///             "enable": true,
        ///             "dirName": "test-funs"
        ///           }
        ///         ]
        ///     }
        ///    ]
        ///}
        ///         </code>
        ///     </item>
        ///     <item>
        ///         script-activities/{host}/scripts/{dirName}/目录下存放指令脚本文件。该子目录下包含一个info.json文件和多个指令脚本文件，info.json文件格式如下：
        ///         <code>
        ///{
        ///     "activities": [
        ///          {
        ///               "enable": true,
        ///               "requires": ["selemium==4.10"],
        ///               "language": "python",
        ///               "name": "test",
        ///               "displayName": "测试",
        ///               "fileName": "test.py",
        ///               "toolboxIcon": "images/test-icon.png",
        ///               "designerIcon": "images/test-icon2.png",
        ///               "tooltip": "这是测试指令"
        ///          }
        ///     ]
        ///}
        ///         </code>
        ///     </item>
        /// </list>
        /// </remarks> 
        public static List<ToolboxCategory> LoadScriptActivities()
        {
            Log.Information("load script activities...");
            var list = new List<ToolboxCategory>();

            LoadFromLocal(list);

            //TODO: process activities[].requires
            //TODO: validate rpa_args
            //TODO: show usage of each script activity
            //TODO: `optional` load from remote
            //TODO: `optional` manage remote script activities

            Log.Information("load script activites end");

            return list;
        }

        
        private static void LoadFromLocal(List<ToolboxCategory> list)
        {
            Log.Information("load local script activities...");
            string host;
            if (string.IsNullOrEmpty(Config.local.wsurl))
            {
                host = "offline";
            }
            else
            {
                var uri = new Uri(Config.local.wsurl);
                host = uri.Host;
            }
            var fi = new FileInfo(Config.SettingsFile);
            var baseDir = Path.Combine(fi.Directory.FullName, "script-activities", host);
            if (!Directory.Exists(baseDir))
            {
                Log.Information("create script-activities dir: " + baseDir);
                Directory.CreateDirectory(baseDir);
            }

            var summaryFile = Path.Combine(baseDir, "summary.json");
            JObject summaryObject = null;
            if (File.Exists(summaryFile))
            {
                Log.Information("load script activities summary info: " + summaryFile);
                try
                {
                    var content = ReadFileContent(summaryFile);
                    summaryObject = JObject.Parse(content);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "load script activities summary info error: " + summaryFile);
                }
            }

            if (summaryObject != null)
            {
                try
                {
                    var assemblyName = new AssemblyName("LocalScriptActivitiesDynamicAssembly");
                    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

                    List<ScriptItem> pyScriptItems = new List<ScriptItem>();

                    JArray itemsArray = (JArray)summaryObject["items"];
                    foreach (JObject item in itemsArray)
                    {
                        string moduleName = (string)item["moduleName"];
                        string categoryName = (string)item["categoryName"];
                        bool enable = !item.ContainsKey("enable") || (bool)item["enable"];
                        if (!enable)
                        {
                            continue;
                        }

                        var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);

                        var category = new ToolboxCategory(categoryName);
                        list.Add(category);

                        JArray scriptsArray = (JArray)item["scripts"];
                        foreach (JObject scriptItem in scriptsArray)
                        {
                            string dirName = null;
                            try
                            {
                                dirName = (string)scriptItem["dirName"];
                                bool enableItem = !scriptItem.ContainsKey("enable") || (bool)scriptItem["enable"];
                                if (!enableItem)
                                {
                                    continue;
                                }

                                var scriptDir = Path.Combine(baseDir, "scripts", dirName);
                                var infoFile = Path.Combine(scriptDir, "info.json");

                                if (!File.Exists(infoFile))
                                {
                                    Log.Warning("script info file not found: " + infoFile);
                                    continue;
                                }

                                var infoContent = ReadFileContent(infoFile);
                                JObject infoObject = JObject.Parse(infoContent);

                                JArray activitiesArray = (JArray)infoObject["activities"];
                                foreach (JObject activityObject in activitiesArray)
                                {
                                    try
                                    {
                                        var activityItem = BuildItem(assemblyBuilder, moduleBuilder, scriptDir, activityObject, pyScriptItems);
                                        if (activityItem == null)
                                        {
                                            continue;
                                        }

                                        category.Add(activityItem);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "Failed to process script activity item: dir=" + dirName + ", item=" + activityObject);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to process script item: " + dirName);
                            }
                        }
                    }

                    ListenPyScriptChange(pyScriptItems);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to process script activities summary info.");
                }
            }

            Log.Information("load local script activities end.");
        }
        
        private static string ReadFileContent(string file)
        {
            string content;
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    content = reader.ReadToEnd();
                }
            }

            return content;
        }

        private static ToolboxItemWrapper BuildItem(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder, string scriptDir,
            JObject activityObject, List<ScriptItem> pyScriptItems)
        {
            bool enable = !activityObject.ContainsKey("enable") || (bool)activityObject["enable"];

            if(!enable)
            {
                return null;
            }

            if (activityObject.ContainsKey("requires"))
            {
                JArray requires = (JArray)activityObject["requires"];
                // TODO
            }

            string language = "Python";
            if (activityObject.ContainsKey("language"))
            {
                language = (string)activityObject["language"];
            }

            language = language.ToLower();
            switch (language)
            {
                case "python":
                    language = "Python";
                    break;
                case "vb":
                    language = "VB";
                    break;
                case "c#":
                    language = "C#";
                    break;
                case "powershell":
                    language = "PowerShell";
                    break;
                case "autohotkey":
                    language = "AutoHotkey";
                    break;
            }

            
            string name = (string)activityObject["name"];
            string displayName = activityObject.ContainsKey("displayName") ? (string)activityObject["displayName"] : name;
            string fileName = (string)activityObject["fileName"];
            var toolboxIcon = activityObject.ContainsKey("toolboxIcon") ? (string)activityObject["toolboxIcon"] : null;
            var designerIcon = activityObject.ContainsKey("designerIcon") ? (string)activityObject["designerIcon"] : null;
            var tooltip = activityObject.ContainsKey("tooltip") ? (string)activityObject["tooltip"] : null;

            string typeKey = assemblyBuilder.GetName().Name + "."+ moduleBuilder.FullyQualifiedName + "." + name;

            var codeFile = Path.Combine(scriptDir, fileName);
            if (!File.Exists(codeFile))
            {
                Log.Warning("Failed to process script activity, not found file: " + codeFile);
                return null;
            }

            var code = ReadFileContent(codeFile);


            var typeBuilder = moduleBuilder.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, typeof(InternalInvokeCode));

            var typeKeyFieldBuilder = typeBuilder.DefineField("Dynamic_TypeKey", typeof(string), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Literal);
            typeKeyFieldBuilder.SetConstant(typeKey);

            string toolboxIconFile = null;
            if (!string.IsNullOrEmpty(toolboxIcon))
            {
                toolboxIconFile = Path.Combine(scriptDir, toolboxIcon);
            }

            //set toolbox icon
            if (!string.IsNullOrEmpty(toolboxIconFile) && File.Exists(toolboxIconFile))
            {
                var attrParams = new Type[] { typeof(string) };
                var attrInfo = typeof(ToolboxBitmapAttribute).GetConstructor(attrParams);
                var attrBuilder = new CustomAttributeBuilder(attrInfo, new object[] { toolboxIconFile });
                typeBuilder.SetCustomAttribute(attrBuilder);
            }

            string designerIconFile = null;
            if (!string.IsNullOrEmpty(designerIcon))
            {
                designerIconFile = Path.Combine(scriptDir, designerIcon);
                if (!File.Exists(designerIconFile))
                {
                    designerIconFile = null;
                }
            }


            //set tooltip attribute
            if (!string.IsNullOrEmpty(tooltip))
            {
                var attrParams = new Type[] { typeof(string) };
                var attrInfo = typeof(ToolboxTooltipAttribute).GetConstructor(attrParams);
                var attrBuilder = new CustomAttributeBuilder(attrInfo, new object[] { tooltip });
                typeBuilder.SetCustomAttribute(attrBuilder);
            }

            var dynamicType = typeBuilder.CreateType();
            var item = new ToolboxItemWrapper(dynamicType, displayName);


            var scriptItem = new ScriptItem
            {
                TypeKey = typeKey,
                DesignerIconFile = designerIconFile,
                DisplayName = displayName,
                Code = code,
                Language = language,
                CodeType = dynamicType,
                CodeFile = codeFile
            };

            InternalInvokeCode.ScriptItemDict.Add(typeKey, scriptItem);

            pyScriptItems.Add(scriptItem);

            return item;
        }

        //listen python script file change
        private static void ListenPyScriptChange(List<ScriptItem> pyScriptItems)
        {
            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }
            watchers.Clear();

            foreach (var item in pyScriptItems)
            {
                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(item.CodeFile),
                    Filter = Path.GetFileName(item.CodeFile),
                    NotifyFilter = NotifyFilters.LastWrite
                };

                watcher.Changed += (sender, e) =>
                {
                    try
                    {
                        string code = ReadFileContent(item.CodeFile);

                        if (code != null && code != item.Code)
                        {
                            Log.Information("Python script file changed, reload it: " + item.CodeFile);
                            item.Code = code;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Failed to update python script code: file=" + item.CodeFile + ", errmsg=" + ex);
                    }
                };

                watcher.EnableRaisingEvents = true;

                watchers.Add(watcher);
            }
        }

        public static bool IsDictionary(object obj, Type keyType)
        {
            var isDict = obj != null && obj.GetType().GetInterfaces().Any(i => i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) &&
                (keyType == null || keyType.IsAssignableFrom(i.GetGenericArguments()[0])));

            return isDict;
        }

        public static bool IsList(object obj, Type itemType)
        {
            var isList = obj != null && obj.GetType().GetInterfaces().Any(i => i.IsGenericType &&
                           (i.GetGenericTypeDefinition() == typeof(IList<>)) &&
                                          (itemType == null || itemType.IsAssignableFrom(i.GetGenericArguments()[0])));

            return isList;
        }
    }

    [System.ComponentModel.Designer(typeof(ScriptActivitiesCodeDesigner), typeof(System.ComponentModel.Design.IDesigner))]
    public abstract class InternalInvokeCode : CodeActivity
    {
        private InvokeCode InvokeCode { get; set; }

        [Browsable(false)]
        public string[] namespaces { get; set; }
        [Browsable(false)]
        public string designerIconFile { get; set; }
        [Description("Recommended input argument types include deep attrs: primitive types(int,float,bool,string), JObject, JArray, IDictionary, IList, etc.")]
        public InArgument<object> rpa_args { get; set; }
        [Description("Recommended output argument types include deep attrs: primitive types(int,float,bool,string), list, dict, etc.")]
        public OutArgument<object> rpa_result { get; set; }
        [Browsable(false)]
        public Dictionary<string, Argument> Arguments { get; set; }
        public OutArgument<Collection<System.Management.Automation.PSObject>> PipelineOutput { get; set; }

        public static IDictionary<string, ScriptItem> ScriptItemDict = new ConcurrentDictionary<string, ScriptItem>();

        private string TypeKey { get; set; }

        public InternalInvokeCode()
        {
            TypeKey = (string)GetType().GetField("Dynamic_TypeKey", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            var builder = new System.Activities.Presentation.Metadata.AttributeTableBuilder();
            builder.AddCustomAttributes(GetType(), "Arguments",
                new EditorAttribute(typeof(OpenRPA.Interfaces.Activities.ArgumentCollectionEditor),
                typeof(System.Activities.Presentation.PropertyEditing.PropertyValueEditor)));
            System.Activities.Presentation.Metadata.MetadataStore.AddAttributeTable(builder.CreateTable());
            Arguments = new Dictionary<string, Argument>();

            InvokeCode = new InvokeCode();

            ScriptItem scriptItem = ScriptItemDict[TypeKey];
            designerIconFile = scriptItem.DesignerIconFile;
        }


        private PyObject ToPythonObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }else if (obj is JObject)
            {
                PyDict pyDict = new PyDict();
                foreach (var item in (JObject)obj)
                {
                    pyDict.SetItem(item.Key, ToPythonObject(item.Value));
                }
                return pyDict;
            }
            else if (obj is JArray)
            {
                PyList pyList = new PyList();
                foreach (var item in (JArray)obj)
                {
                    pyList.Append(ToPythonObject(item));
                }
                return pyList;
            }
            else if (obj is JValue)
            {
                object value = ((JValue)obj).Value;
                return ToPythonObject(value);
            }
            else if (ScriptActivities.IsDictionary(obj, typeof(string)))
            {
                IDictionary<string, object> dict = (IDictionary<string, object>)obj;
                PyDict pyDict = new PyDict();
                foreach (var item in dict)
                {
                    pyDict.SetItem(item.Key, ToPythonObject(item.Value));
                }
                return pyDict;
            }
            else if (ScriptActivities.IsList(obj, null))
            {
                IList<object> list = (IList<object>)obj;
                PyList pyList = new PyList();
                foreach (var item in list)
                {
                    pyList.Append(ToPythonObject(item));
                }
                return pyList;
            }
            else
            {
                return obj.ToPython();
            }
        }

        protected override void Execute(CodeActivityContext context)
        {
            ScriptItem item = ScriptItemDict[TypeKey];
            string code = item.Code;
            string language = item.Language;

            object args = rpa_args.Get(context) ?? new JObject();

            if (language == "Python")
            {
                try
                {
                    InvokeCode.InitPython();
                    using (Python.Runtime.Py.GIL())
                    {
                        args = ToPythonObject(args);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex.ToString());
                    throw new Exception("Failed for 'ToPythonObject': " + ex.ToString());
                }
            }

            rpa_args.Set(context, args);

            Arguments.Remove("rpa_args");
            Arguments.Add("rpa_args", rpa_args);
            Arguments.Remove("rpa_result");
            Arguments.Add("rpa_result", rpa_result);

            InvokeCode.namespaces = namespaces;
            InvokeCode.Execute(context, code, language, Arguments, PipelineOutput);
        }


        public new string DisplayName
        {
            get
            {
                var displayName = base.DisplayName;
                if (displayName == this.GetType().Name)
                {
                    ScriptItem item = ScriptItemDict[TypeKey];
                    displayName = item.DisplayName;
                }
                return displayName;
            }
            set
            {
                base.DisplayName = value;
            }
        }
    }
}
