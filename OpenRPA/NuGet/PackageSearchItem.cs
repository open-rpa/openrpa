using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    public class PackageSearchItem : ObservableObject
    {
        public PackageSearchItem() { }
        public PackageSearchItem(Project project, NuGet.Protocol.Core.Types.IPackageSearchMetadata searchItem)
        {
            Identity = searchItem.Identity;
            Id = Identity.Id;
            Title = searchItem.Title;
            IsInstalled = isPackageIdInProjectDependencies(project, searchItem.Identity.Id);

            InstalledVersion = "";
            // Find installed version !
            var LocalVersion = NuGetPackageManager.Instance.getLocal(Id);
            if (LocalVersion != null)
            {
                InstalledVersion = LocalVersion.Identity.Version.ToString();
                SelectedVersion = InstalledVersion;
            }
            NotifyPropertyChanged("InstalledVersion");

            LicenseUrl = searchItem.LicenseUrl?.AbsoluteUri;
            ProjectUrl = searchItem.ProjectUrl?.AbsoluteUri;
            PublishTime = searchItem.Published?.ToLocalTime().ToString("G");
            Tags = searchItem.Tags;
            Description = searchItem.Description;
            if (searchItem.IconUrl != null) IconUrl = searchItem.IconUrl.ToString();
            Authors = searchItem.Authors;
            Dependencies = searchItem.DependencySets.ToList();
        }
        private bool isPackageIdInProjectDependencies(Project project, string package_id)
        {
            if (project.dependencies == null) return false;
            foreach (JProperty jp in (JToken)project.dependencies)
            {
                if (jp.Name.ToLower() == package_id.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
        public async Task LoadVersions(bool IncludePrerelease)
        {
            try
            {
                if (VersionList != null) return;
                var _VersionList = new System.Collections.ObjectModel.ObservableCollection<string>();
                List<IPackageSearchMetadata> search_package_versions = await NuGetPackageManager.Instance.SearchPackageVersions(Id, IncludePrerelease);
                search_package_versions.Sort((x, y) => -x.Identity.Version.CompareTo(y.Identity.Version));
                foreach (var ver in search_package_versions)
                {
                    _VersionList.Add(ver.Identity.Version.OriginalVersion);
                }
                if (IsInstalled)
                {

                }
                if (_VersionList != null && _VersionList.Count > 0 && string.IsNullOrEmpty(SelectedVersion)) SelectedVersion = _VersionList.First();
                VersionList = _VersionList;
                NotifyPropertyChanged("SelectedVersion");
                NotifyPropertyChanged("InstalledVersion");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public PackageIdentity Identity { get { return GetProperty<PackageIdentity>(); } set { SetProperty(value); } }
        public string Id { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string Authors  { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string Description  { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string IconUrl { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string Title { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool IsInstalled { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string InstalledVersion { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string LicenseUrl { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string ProjectUrl { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string PublishTime { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public string Tags { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool RequireLicenseAcceptance { get { return GetProperty<bool>(); } set { SetProperty(value); } }
        public string SelectedVersion { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public List<PackageDependencyGroup> Dependencies { get { return GetProperty<List<PackageDependencyGroup>>(); } set { SetProperty(value); } }
        public System.Collections.ObjectModel.ObservableCollection<string> VersionList { get { return GetProperty<System.Collections.ObjectModel.ObservableCollection<string>>(); } set { SetProperty(value); } }
    }
}
