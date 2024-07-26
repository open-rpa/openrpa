// Inspired by Keras.NET 
// https://github.com/SciSharp/Keras.NET/blob/master/Keras/Setup.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Script.PythonUtil
{
    public class ModuleInfo
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string Summary { get; set; }

        public string HomePage { get; set; }

        public string Author { get; set; }

        public string AuthorEmail { get; set; }

        public string License { get; set; }

        public string Location { get; set; }

        public string Requires { get; set; }

        public string RequiredBy { get; set; }
    }
}
