using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IProject : INotifyPropertyChanged, IBase
    {
        bool IsExpanded { get; set; }
        bool disable_local_caching { get; set; }
        string Path { get; set; }
        System.Collections.ObjectModel.ObservableCollection<IWorkflow> Workflows { get; set; }
        Newtonsoft.Json.Linq.JObject dependencies { get; set; }
    }
}
