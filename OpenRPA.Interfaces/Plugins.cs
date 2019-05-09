using Serilog;
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
        public static ICollection<IRecording> recordPlugins = new List<IRecording>();
        public static void loadPlugins(string projectsDirectory)
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
            Type pluginType = typeof(IRecording);
            ICollection<Type> pluginTypes = new List<Type>();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly != null)
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
                            if (type.GetInterface(pluginType.FullName) != null)
                            {
                                pluginTypes.Add(type);
                            }
                        }
                    }
                }
            }

            //ICollection<IRecording> plugins = new List<IRecording>();
            foreach (Type type in pluginTypes)
            {
                IRecording plugin = (IRecording)Activator.CreateInstance(type);
                plugin.Initialize();
                Plugins.recordPlugins.Add(plugin);
            }
        }

    }
}
