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
        bool IsSelected { get; set; }
        bool disable_local_caching { get; set; }
        string Path { get; }
        string Filename { get; set; }
        System.Collections.ObjectModel.ObservableCollection<IWorkflow> Workflows { get; }
        // List<IWorkflow> Workflows { get; }
        // Newtonsoft.Json.Linq.JObject dependencies { get; set; }
        Dictionary<string, string> dependencies { get; set; }
        void NotifyPropertyChanged(string propertyName);
        Task Save();
        Task InstallDependencies(bool LoadDlls);
        void UpdateWorkflowsList();
        void UpdateDetectorsList();
        void UpdateWorkItemQueuesList();
    }
}
