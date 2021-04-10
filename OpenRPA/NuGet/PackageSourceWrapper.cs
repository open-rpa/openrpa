using Newtonsoft.Json.Linq;
using NuGet.Configuration;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class PackageSourceWrapper
    {
        public NuGet.Configuration.PackageSource source { get; set; }
        public PackageSourceWrapper()
        {
            Name = "Installed";
            ToolTip = "Installed packages";
        }
        public PackageSourceWrapper(PackageSource source)
        {
            this.source = source;
            Name = source.Name;
            ToolTip = source.Description;
            if (string.IsNullOrEmpty(ToolTip)) ToolTip = Name;
        }
        // public string Icon { get; set; }
        public string ToolTip { get; set; }
        public string Name { get; set; }
        private string _currentsearchString = null;
        private List<PackageSearchItem> _cacheinstalled;
        public void ClearCache()
        {
            if(_cacheinstalled != null) _cacheinstalled.Clear();
            _cacheinstalled = null;
        }
        public async Task Search(Project project, Views.PackageManager view, bool includePrerelease, string searchString)
        {
            if(source == null)
            {
                if (view.PackageSourceItems == null) view.PackageSourceItems = new System.Collections.ObjectModel.ObservableCollection<PackageSearchItem>();
                if(_cacheinstalled == null)
                {
                    _cacheinstalled = new List<PackageSearchItem>();
                    if(project.dependencies != null)
                    {
                        foreach (var jp in project.dependencies)
                        {
                            var ver_range = NuGet.Versioning.VersionRange.Parse(jp.Value);
                            if (ver_range.IsMinInclusive)
                            {
                                var target_ver = NuGet.Versioning.NuGetVersion.Parse(ver_range.MinVersion.ToString());
                                var _temp = await NuGetPackageManager.Instance.GetLocal(project, new NuGet.Packaging.Core.PackageIdentity(jp.Key, target_ver));
                                foreach(var item in _temp)
                                {
                                    if(_cacheinstalled.Where(x=> x.Id == item.Id).FirstOrDefault() == null)
                                    {
                                        _cacheinstalled.Add(item);
                                    }                                    
                                }
                            }
                        }
                    }
                }
                view.PackageSourceItems.Clear();
                foreach (var item in _cacheinstalled)
                {
                    if (!string.IsNullOrEmpty(searchString) && item.Id.Contains(searchString))
                    {
                        view.PackageSourceItems.Add(item);
                    }
                    else
                    {
                        view.PackageSourceItems.Add(item);
                    }
                }
                return;
            }
            if (!string.IsNullOrEmpty(_currentsearchString))
            {
                // Console.WriteLine("skipping: " + _currentsearchString + " " + searchString);
                _currentsearchString = searchString;
                return;
            }
            _currentsearchString = searchString;

            _ = Task.Run(async () =>
            {
                Console.WriteLine("Searching for '" + searchString + "'");
                var result = await NuGetPackageManager.Instance.Search(project, source, includePrerelease, searchString);
                Console.WriteLine("Update list of packages based on '" + searchString + "'");
                GenericTools.RunUI(() =>
                {
                });
                if (!string.IsNullOrEmpty(_currentsearchString) && _currentsearchString != searchString)
                {
                    //Console.WriteLine("Start new search based on '" + _currentsearchString + "'");
                    var _searchString = _currentsearchString;
                    _currentsearchString = null;
                    _ = Search(project, view, includePrerelease, _searchString);
                }
                else
                {
                    _currentsearchString = null;
                    GenericTools.RunUI(() =>
                    {
                        if (view.PackageSourceItems == null) view.PackageSourceItems = new System.Collections.ObjectModel.ObservableCollection<PackageSearchItem>();
                        view.PackageSourceItems.Clear();
                        foreach (var item in result)
                        {
                            var _item = new PackageSearchItem(project, item);
                            view.PackageSourceItems.Add(_item);
                        }
                        view.IsBusy = false;
                    });

                }
                //Console.WriteLine("complete:: '" + searchString + "'");
            });
        }
    }
}
