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
            
            if (System.IO.File.Exists(fileName))
            {
                t = JsonConvert.DeserializeObject<T>(System.IO.File.ReadAllText(fileName));
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
