using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    class CommandLineParser
    {
        public Dictionary<string, object> Parse()
        {
            return Parse(Environment.GetCommandLineArgs(), true);
        }
        public Dictionary<string, object> Parse(IList<string> args, bool containsExecutable)
        {
            // var expando = new ExpandoObject() as IDictionary<string, Object>;
            var expando = new Dictionary<string, object>();
            try
            {
                if (!containsExecutable)
                {
                    if (args.Count == 0) return expando;
                    if (args.Count == 1)
                    {
                        expando.Add("workflowid", args[0]);
                        return expando;
                    }
                } else
                {
                    if (args.Count == 1) return expando;
                    if (args.Count == 2)
                    {
                        expando.Add("workflowid", args[1]);
                        return expando;
                    }

                }
                // dynamic expando = new ExpandoObject();
                int idx = 0;
                if (containsExecutable) idx = 1;
                for (var i = idx; i < args.Count; i += 2)
                {
                    var name = args[i];
                    if (name.StartsWith("--")) name = name.Substring(2);
                    if (name.StartsWith("-")) name = name.Substring(1);
                    if (name.StartsWith("/")) name = name.Substring(1);
                    name = name.ToLower();
                    // expando[args[i]] = args[i + 1];
                    if ((i + 1) >= args.Count) throw new ArgumentOutOfRangeException("Uneven amounts of parameters, each parameter must be a key value pair");
                    expando.Add(name, args[i + 1]);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
            return expando;
        }
    }
}
