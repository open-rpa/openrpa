using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public class Plugins
    {
        public static ICollection<IPlugin> recordPlugins = new List<IPlugin>();
        public static ExtendedObservableCollection<IDetectorPlugin> detectorPlugins = new ExtendedObservableCollection<IDetectorPlugin>();
        public static Dictionary<string, Type> detectorPluginTypes = new Dictionary<string, Type>();


        public static IDetectorPlugin AddDetector(entity.Detector entity)
        {
            foreach(var d in detectorPluginTypes)
            {
                if(d.Key == entity.Plugin)
                {
                    try
                    {
                        IDetectorPlugin plugin = (IDetectorPlugin)Activator.CreateInstance(d.Value);
                        entity.name = plugin.Name;
                        plugin.Entity = entity;
                        plugin.Initialize();
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
        public static void loadPlugins(string projectsDirectory)
        {
            List<string> dllFileNames = new List<string>();
            win32.Zone.UnblockPath(projectsDirectory);
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
            ICollection<Type> pluginTypes = new List<Type>();
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
                                    detectorPluginTypes.Add(type.FullName, type);
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

            //ICollection<IRecording> plugins = new List<IRecording>();
            foreach (Type type in pluginTypes)
            {
                IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
                plugin.Initialize();
                Plugins.recordPlugins.Add(plugin);
            }
        }

    }
}
