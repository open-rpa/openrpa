using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IBodyActivity
    {
        Activity Activity { get; set; }
        void addActivity(Activity a, string Name);

    }
}
