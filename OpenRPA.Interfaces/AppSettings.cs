using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class AppSettings<T> where T : new()
    {
        //private const string DEFAULT_FILENAME = "settings.json";

        public void Save(string fileName)
        {
            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static void Save(T pSettings, string fileName)
        {
            System.IO.File.WriteAllText(fileName, JsonConvert.SerializeObject(pSettings, Formatting.Indented));
        }

        public static T Load(string fileName)
        {
            T t = new T();
            var fi = new System.IO.FileInfo(fileName);
            var _fileName = System.IO.Path.GetFileName(fileName);
            var di = fi.Directory;
            if (System.IO.File.Exists(fileName))
            {
                t = JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(fileName));
            }
            else if (System.IO.File.Exists(System.IO.Path.Combine(di.Parent.FullName, _fileName)))
            {
                t = JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(System.IO.Path.Combine(di.Parent.FullName, _fileName)));
            }
            else
            {
                try
                {
                    Save(t, fileName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString(), "Error");
                }
            }
            return t;
        }
    }
}
