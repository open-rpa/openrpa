using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class Plugins
    {
        public static ObservableCollection<IRecordPlugin> recordPlugins = new ObservableCollection<IRecordPlugin>();
        public static ExtendedObservableCollection<IDetectorPlugin> detectorPlugins = new ExtendedObservableCollection<IDetectorPlugin>();
        public static Dictionary<string, Type> detectorPluginTypes = new Dictionary<string, Type>();
        public static ObservableCollection<IRunPlugin> runPlugins = new ObservableCollection<IRunPlugin>();
        public static ObservableCollection<ISnippet> Snippets = new ObservableCollection<ISnippet>();
        public static IDetectorPlugin AddDetector(IOpenRPAClient client, entity.Detector entity)
        {
            foreach(var d in detectorPluginTypes)
            {
                if(d.Key == entity.Plugin)
                {
                    try
                    {
                        IDetectorPlugin plugin = (IDetectorPlugin)Activator.CreateInstance(d.Value);
                        if(string.IsNullOrEmpty(entity.name)) entity.name = plugin.Name;
                        plugin.Initialize(client, entity);
                        Plugins.detectorPlugins.Add(plugin);
                        return plugin;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("OpenRPA.Interfaces.Plugins.AddDetector: " + ex.ToString());
                    }
                }
            }
            return null;
        }
        public static void LoadPlugins(IOpenRPAClient client, string projectsDirectory)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Information("LoadPlugins::begin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
            ICollection<Type> alltypes = new List<Type>();
            ICollection<Type> pluginTypes = new List<Type>();
            ICollection<Type> snippetTypes = new List<Type>();
            ICollection<Type> runPluginTypes = new List<Type>();
            ICollection<Type> IDetectorPluginTypes = new List<Type>(); 

            List<string> dllFileNames = new List<string>();
            foreach (var path in System.IO.Directory.GetFiles(projectsDirectory, "*.dll")) dllFileNames.Add(path);
            ICollection<Assembly> assemblies = new List<Assembly>();
            foreach (string dllFile in dllFileNames)
            {
                try
                {
                    // if (dllFile.Contains("OpenRPA.Interfaces.")) continue;
                    if (dllFile.Contains("DotNetProjects.")) continue;
                    if (dllFile.Contains("Emgu.")) continue;
                    if (dllFile.Contains("Microsoft.CodeAnalysis.")) continue;
                    if (dllFile.Contains("Microsoft.Office.")) continue;
                    if (dllFile.Contains("NuGet.")) continue;
                    if (dllFile.Contains("System.Collections.")) continue;
                    if (dllFile.Contains("System.ComponentModel.")) continue;
                    if (dllFile.Contains("System.Composition.")) continue;
                    if (dllFile.Contains("System.Data.")) continue;
                    if (dllFile.Contains("System.Diagnostics.")) continue;
                    if (dllFile.Contains("System.Globalization.")) continue;
                    if (dllFile.Contains("System.IO.")) continue;
                    if (dllFile.Contains("System.Linq.")) continue;
                    if (dllFile.Contains("System.Net.")) continue;
                    if (dllFile.Contains("System.Reflection.")) continue;
                    if (dllFile.Contains("System.Resources.")) continue;
                    if (dllFile.Contains("System.Runtime.")) continue;
                    if (dllFile.Contains("System.Security.")) continue;
                    if (dllFile.Contains("System.Text.")) continue;
                    if (dllFile.Contains("System.Threading.")) continue;
                    if (dllFile.Contains("System.Xml.")) continue;
                    if (dllFile.Contains("System.Windows.")) continue;
                    if (dllFile.Contains("ToastNotifications.")) continue;
                    if (dllFile.Contains("Xceed.Wpf.")) continue;
                    if (dllFile.Contains("ControlzEx.")) continue;
                    if (dllFile.Contains("MahApps.")) continue;
                    AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                    Assembly assembly = Assembly.Load(an);
                    assemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "");
                }
            }
            Log.Information("LoadPlugins::Get types " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
            foreach (var a in assemblies)
            {
                try
                {
                    foreach(var s in a.GetTypes())
                    {
                        alltypes.Add(s);
                    }
                }
                catch (Exception) { }
            }

            Log.Information("LoadPlugins::Get all IRecordPlugins " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
            var IRecordPlugintype = typeof(IRecordPlugin);
            foreach(var p in alltypes)
            {
                try
                {
                    if (IRecordPlugintype.IsAssignableFrom(p) && p.IsInterface == false) pluginTypes.Add(p);
                }
                catch (Exception) { }
            }
            Log.Information("LoadPlugins::Get all IDetectorPlugin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
            var IDetectorPlugintype = typeof(IDetectorPlugin);
            foreach (var p in alltypes)
            {
                try
                {
                    if (IDetectorPlugintype.IsAssignableFrom(p) && p.IsInterface == false) IDetectorPluginTypes.Add(p);
                }
                catch (Exception) { }
            }
            
            Log.Information("LoadPlugins::Get all ISnippet " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
            var ISnippettype = typeof(ISnippet);
            foreach (var p in alltypes)
            {
                try
                {
                    if (ISnippettype.IsAssignableFrom(p) && p.IsInterface == false) snippetTypes.Add(p);
                }
                catch (Exception) { }
            }

            Log.Information("LoadPlugins::Get all IRunPlugin " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
            var IRunPlugintype = typeof(IRunPlugin);
            foreach (var p in alltypes)
            {
                try
                {
                    if (IRunPlugintype.IsAssignableFrom(p) && p.IsInterface == false) runPluginTypes.Add(p);
                }
                catch (Exception) { }
            }

            foreach (var type in IDetectorPluginTypes) Plugins.detectorPluginTypes.Add(type.FullName, type);
            foreach (Type type in pluginTypes)
            {
                try
                {
                    IRecordPlugin plugin = (IRecordPlugin)Activator.CreateInstance(type);
                    Log.Information("LoadPlugins::Initialize plugin " + plugin.Name + " " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    // SetStatus("Initialize plugin " + plugin.Name);
                    plugin.Initialize(client);
                    GenericTools.RunUI(() => recordPlugins.Add(plugin));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            foreach (Type type in snippetTypes)
            {
                try
                {
                    ISnippet plugin = (ISnippet)Activator.CreateInstance(type);
                    Log.Information("LoadPlugins::Initialize snippet " + plugin.Name + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    Snippets.Add(plugin);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            foreach (Type type in runPluginTypes)
            {
                try
                {
                    IRunPlugin plugin = (IRunPlugin)Activator.CreateInstance(type);
                    Log.Information("LoadPlugins::Initialize RunPlugin " + plugin.Name + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
                    plugin.Initialize(client);
                    GenericTools.RunUI(() => runPlugins.Add(plugin));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            Log.Information("LoadPlugins::end " + string.Format("{0:mm\\:ss\\.fff}", sw.Elapsed));
        }
    }
}
