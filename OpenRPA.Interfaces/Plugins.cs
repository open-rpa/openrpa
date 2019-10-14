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
        public static ObservableCollection<IPlugin> recordPlugins = new ObservableCollection<IPlugin>();
        public static ExtendedObservableCollection<IDetectorPlugin> detectorPlugins = new ExtendedObservableCollection<IDetectorPlugin>();
        public static Dictionary<string, Type> detectorPluginTypes = new Dictionary<string, Type>();
        public static ObservableCollection<IRunPlugin> runPlugins = new ObservableCollection<IRunPlugin>();
        public static ObservableCollection<ISnippet> Snippets = new ObservableCollection<ISnippet>();
        public static IDetectorPlugin AddDetector(entity.Detector entity)
        {
            foreach(var d in detectorPluginTypes)
            {
                if(d.Key == entity.Plugin)
                {
                    try
                    {
                        IDetectorPlugin plugin = (IDetectorPlugin)Activator.CreateInstance(d.Value);
                        if(string.IsNullOrEmpty(entity.name)) entity.name = plugin.Name;
                        plugin.Initialize(entity);
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
        public async static Task LoadPlugins(string projectsDirectory)
        {
            ICollection<Type> pluginTypes = new List<Type>();
            ICollection<Type> snippetTypes = new List<Type>();
            ICollection<Type> runPluginTypes = new List<Type>();

            await Task.Run(() =>
            {
                List<string> dllFileNames = new List<string>();
                foreach (var path in System.IO.Directory.GetFiles(projectsDirectory, "*.dll")) dllFileNames.Add(path);
                ICollection<Assembly> assemblies = new List<Assembly>();
                foreach (string dllFile in dllFileNames)
                {
                    try
                    {
                        AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                        Assembly assembly = Assembly.Load(an);
                        assemblies.Add(assembly);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "");
                    }
                }
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        try
                        {
                            Type[] types = assembly.GetTypes();
                            foreach (Type type in types)
                            {
                                if (type.IsInterface || type.IsAbstract)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (type.GetInterface(typeof(IPlugin).FullName) != null)
                                    {
                                        pluginTypes.Add(type);
                                    }
                                    if (type.GetInterface(typeof(IDetectorPlugin).FullName) != null)
                                    {
                                        Plugins.detectorPluginTypes.Add(type.FullName, type);
                                    }
                                    if (type.GetInterface(typeof(ISnippet).FullName) != null)
                                    {
                                        snippetTypes.Add(type);
                                    }
                                    if (type.GetInterface(typeof(IRunPlugin).FullName) != null)
                                    {
                                        runPluginTypes.Add(type);
                                    }                                    
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "loadPlugins");
                        }
                    }
                }
            });
            foreach (Type type in pluginTypes)
            {
                try
                {
                    IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                    Log.Information("Initialize plugin " + plugin.Name);
                    // SetStatus("Initialize plugin " + plugin.Name);
                    plugin.Initialize();
                    recordPlugins.Add(plugin);
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
                    Log.Information("Initialize snippet " + plugin.Name);
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
                    Log.Information("Initialize RunPlugin " + plugin.Name);
                    runPlugins.Add(plugin);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }

        }

    }
}
