using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRPA.Interfaces;
using NuGet.Configuration;
using NuGet.Common;
using System.Threading;

namespace OpenRPA
{
    public class NuGetPackageManager
    {
        public NuGetPackageManager()
        {
            if (!System.IO.Directory.Exists(GlobalPackagesFolder))
            {
                System.IO.Directory.CreateDirectory(GlobalPackagesFolder);
            }
            if (!System.IO.Directory.Exists(PackagesInstallFolder))
            {
                System.IO.Directory.CreateDirectory(PackagesInstallFolder);
            }
            if (!System.IO.Directory.Exists(TargetFolder))
            {
                System.IO.Directory.CreateDirectory(TargetFolder);
            }
        }
        private Views.PackageManager view;
        public async Task Initialize(Views.PackageManager view)
        {
            var provider = NuGetPackageManager.Instance.DefaultSourceRepositoryProvider.PackageSourceProvider;
            var sources = provider.LoadPackageSources();
            view.PackageSources = new System.Collections.ObjectModel.ObservableCollection<PackageSource>();
            foreach (var source in sources)
            {
                view.PackageSources.Add(source);
            }
            if (view.SelectedPackageSource == null)
            {
                //view.SelectedPackageSource = sources.FirstOrDefault();

                view.treePackageSources.SelectItem(sources.FirstOrDefault());
                view.FilterText = "";
            }

        }
        private string _currentsearchString = null;
        public async Task Search(PackageSource source, Views.PackageManager view, bool includePrerelease, string searchString)
        {
            if(!string.IsNullOrEmpty(_currentsearchString))
            {
                Console.WriteLine("skipping: " + _currentsearchString  + " " + searchString);
                _currentsearchString = searchString;
                return;
            }
            _currentsearchString = searchString;
            var result = new List<IPackageSearchMetadata>();
            foreach (var sourceRepository in DefaultSourceRepositoryProvider.GetRepositories())
            {
                if (source == null || string.IsNullOrEmpty(source.Source) || sourceRepository.PackageSource.Source.ToLower() != source.Source.ToLower())
                {
                    continue;
                }

                var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
                var supportedFramework = new[] { ".NETFramework,Version=v4.6.2" };
                var searchFilter = new SearchFilter(includePrerelease)
                {
                    SupportedFrameworks = supportedFramework,
                    IncludeDelisted = true
                };

                try
                {
                    Console.WriteLine(searchString);

                    var jsonNugetPackages = await searchResource
                                .SearchAsync(searchString, searchFilter, 0, 50, NullLogger.Instance, CancellationToken.None);

                    if (string.IsNullOrEmpty(searchString))
                    {
                        foreach (var p in jsonNugetPackages)
                        {
                            var exists = result.Where(x => x.Identity.Id == p.Identity.Id).FirstOrDefault();
                            if (exists == null) result.Add(p);
                        }
                    }
                    else
                    {
                        foreach (var p in jsonNugetPackages.Where(x => x.Title.ToLower().Contains(searchString.ToLower())))
                        {
                            var exists = result.Where(x => x.Identity.Id == p.Identity.Id).FirstOrDefault();
                            if (exists == null) result.Add(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    throw;
                }
            }


            view.PackageSourceItems = new System.Collections.ObjectModel.ObservableCollection<IPackageSearchMetadata>();
            foreach (var item in result)
            {
                view.PackageSourceItems.Add(item);
            }
            if(!string.IsNullOrEmpty(_currentsearchString) && _currentsearchString != searchString)
            {
                var _searchString = _currentsearchString;
                _currentsearchString = null;
                _ = Search(source, view, includePrerelease, _searchString);
            } else
            {
                _currentsearchString = null;
            }
        }
        private static NuGetPackageManager _instance = null;
        public static NuGetPackageManager Instance
        {
            get
            {
                if (_instance == null) _instance = new NuGetPackageManager();
                return _instance;
            }
        }
        public string globalPackagesFolder = null;
        public string GlobalPackagesFolder
        {
            get
            {
                if (string.IsNullOrEmpty(globalPackagesFolder)) globalPackagesFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\OpenRPA\Packages\.nuget\packages";
                return globalPackagesFolder;
            }
        }

        public string _packagesInstallFolder = null;
        public string PackagesInstallFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_packagesInstallFolder)) _packagesInstallFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\OpenRPA\Packages\Installed";
                return _packagesInstallFolder;
            }
        }

        public string _targetFolder = null;
        public string TargetFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_targetFolder)) _targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\OpenRPA\Packages\Target";
                return _targetFolder;
            }
            set
            {
                _targetFolder = value;
            }
        }
        public ISettings _settings = null;
        public ISettings Settings
        {
            get
            {
                if (_settings != null) return _settings;
                _settings = NuGet.Configuration.Settings.LoadDefaultSettings(root: null);
                return _settings;
                //var nugetConfigFile = "Nuget.Default.Config";
                //string locale = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                //if (locale.Equals("zh") || locale.Equals("ja"))
                //{
                //    nugetConfigFile = nugetConfigFile.Replace(".Config", "_" + locale + ".Config");
                //}
                //try
                //{
                //    if (_settings == null) _settings = NuGet.Configuration.Settings.LoadSpecificSettings(Environment.CurrentDirectory, nugetConfigFile);
                //    return _settings;
                //}
                //catch (Exception ex)
                //{
                //    Log.Error(ex.ToString());
                //    return null;
                //}

            }
        }
        private SourceRepositoryProvider _defaultSourceRepositoryProvider = null;
        public SourceRepositoryProvider DefaultSourceRepositoryProvider
        {
            get
            {
                if (_defaultSourceRepositoryProvider == null)
                {
                    var psp = new PackageSourceProvider(Settings);
                    _defaultSourceRepositoryProvider = new SourceRepositoryProvider(psp, Repository.Provider.GetCoreV3());
                }
                return _defaultSourceRepositoryProvider;
            }
        }

    }
}
