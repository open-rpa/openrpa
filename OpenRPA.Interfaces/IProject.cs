using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces
{
    public interface IProject : IBase
    {
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
        bool disable_local_caching { get; set; }
        string Path { get; }
        string Filename { get; set; }
        FilteredObservableCollection<IWorkflow> Workflows { get; }
        Dictionary<string, string> dependencies { get; set; }
        void NotifyPropertyChanged(string propertyName);
        Task Delete(bool skipOnline = false);
        Task Save(bool skipOnline = false);
        Task Update(IProject item, bool skipOnline = false);
        Task InstallDependencies(bool LoadDlls);
    }
}
