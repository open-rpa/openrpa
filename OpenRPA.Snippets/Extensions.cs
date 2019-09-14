using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Snippets
{
    static class  Extensions
    {
        static public string ResourceAsString(this Type type, string resourceName)
        {
            // string[] names = typeof(Extensions).Assembly.GetManifestResourceNames();
            string[] names = type.Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                if (name.EndsWith(resourceName))
                {
                    using (var s = type.Assembly.GetManifestResourceStream(name))
                    {
                        using (var reader = new StreamReader(s))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                } else
                {
                    try
                    {
                        var set = new System.Resources.ResourceSet(type.Assembly.GetManifestResourceStream(names[0]));
                        foreach (System.Collections.DictionaryEntry resource in set)
                        {
                            // Console.WriteLine("\n[{0}] \t{1}", resource.Key, resource.Value);
                            if(((string)resource.Key).EndsWith(resourceName.ToLower()))
                            {
                                using (var reader = new StreamReader(resource.Value as Stream))
                                {
                                    return reader.ReadToEnd();
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex.ToString());
                    }
                }
            }
            return null;
        }
        static public string ResourceAsString(string resourceName)
        {
            return ResourceAsString(typeof(Extensions), resourceName);
        }

    }
}
