using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Updater
{
    public static class Extensions
    {
        public static void Dump(this object o)
        {
            System.Diagnostics.Debug.WriteLine(o.ToString());
        }
    }
}
