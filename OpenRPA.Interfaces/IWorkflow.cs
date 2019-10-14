using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IWorkflow : INotifyPropertyChanged
    {
        string _id { get; set; }
        string name { get; set; }
        string queue { get; set; }
        string Xaml { get; set; }
        string projectid { get; set; }
        bool Serializable { get; set; }
    }
}
