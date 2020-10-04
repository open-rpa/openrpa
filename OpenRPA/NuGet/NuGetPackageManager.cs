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
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Frameworks;
using NuGet.Resolver;
using NuGet.Protocol;

namespace OpenRPA
{
    public class NuGetPackageManager
    {
        public string[] FilteredPackages = new string[] { "Newtonsoft.Json", "NLog", "FlaUI.", 
            "System.", "System.Collections.Immutable", "Microsoft.", 
            "AvalonEdit", "NuGet.", "Extended.Wpf.Toolkit", "DotNetProjects.Wpf", "CefSharp", "NAudio", "SharpAvi",
            "DataConnectionDialog", "Forge.Forms", "ToastNotifications", "HtmlAgilityPack", "EMGU.CV", "ZedGraph", "FastMember", "Humanizer",
            "MahApps", "ControlzEx", "MaterialDesignColors", "MaterialDesignThemes", "OpenRPA"
        };
        public NuGetFramework _nuGetFramework = null;
        public NuGetFramework NuGetFramework
        {
            get
            {
                if (_nuGetFramework == null) _nuGetFramework = NuGetFramework.ParseFolder("net462");
                return _nuGetFramework;
            }
        }
        public NuGetPackageManager()
        {
            if (!System.IO.Directory.Exists(PackagesInstallFolder))
            {
                System.IO.Directory.CreateDirectory(PackagesInstallFolder);
            }
        }
        private Views.PackageManager view;
        public void Initialize(Views.PackageManager view)
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
        public async Task Search(Project project, PackageSource source, Views.PackageManager view, bool includePrerelease, string searchString)
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
                    var jsonNugetPackages = await searchResource.SearchAsync(searchString, searchFilter, 0, 50, NullLogger.Instance, CancellationToken.None);
                    if (string.IsNullOrEmpty(searchString))
                    {
                        foreach (var p in jsonNugetPackages)
                        {
                            var exists = result.Where(x => x.Identity.Id == p.Identity.Id).FirstOrDefault();
                            var filtered = FilteredPackages.Where(x => p.Identity.Id.StartsWith(x)).FirstOrDefault();
                            if (exists == null && filtered == null) result.Add(p);
                        }
                    }
                    else
                    {
                        foreach (var p in jsonNugetPackages.Where(x => x.Title.ToLower().Contains(searchString.ToLower())))
                        {
                            var exists = result.Where(x => x.Identity.Id == p.Identity.Id).FirstOrDefault();
                            var filtered = FilteredPackages.Where(x => p.Identity.Id.StartsWith(x)).FirstOrDefault();
                            if (exists == null && filtered == null) result.Add(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    throw;
                }
                view.IsBusy = false;
            }


            view.PackageSourceItems = new System.Collections.ObjectModel.ObservableCollection<PackageSearchItem>();
            foreach (var item in result)
            {
                var _item = new PackageSearchItem(project, item);
                view.PackageSourceItems.Add(_item);
            }
            if(!string.IsNullOrEmpty(_currentsearchString) && _currentsearchString != searchString)
            {
                var _searchString = _currentsearchString;
                _currentsearchString = null;
                _ = Search(project, source, view, includePrerelease, _searchString);
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
        public string _packagesInstallFolder = null;
        public string PackagesInstallFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_packagesInstallFolder)) _packagesInstallFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\OpenRPA\Packages\Installed";
                return _packagesInstallFolder;
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
        public async Task<List<IPackageSearchMetadata>> SearchPackageVersions(string packageid, bool includePrerelease)
        {
            var ret = new List<IPackageSearchMetadata>();
            foreach (var sourceRepository in DefaultSourceRepositoryProvider.GetRepositories())
            {
                var searchResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
                using (var cacheContext = new SourceCacheContext())
                {
                    cacheContext.DirectDownload = true;
                    var metadataList = await searchResource.GetMetadataAsync(packageid, includePrerelease, true, cacheContext, NullLogger.Instance, CancellationToken.None);
                    if (metadataList.Count() > ret.Count())
                    {
                        ret = metadataList.ToList();
                    }
                }
            }
            return ret;
        }
        private List<SourceRepository> GetSortedRepositories()
        {
            var tempRepos = DefaultSourceRepositoryProvider.GetRepositories().ToList();
            var sortedRepositories = new List<SourceRepository>();

            int idx = tempRepos.FindIndex(a => a.PackageSource.IsLocal);
            if (idx >= 0)
            {
                sortedRepositories.Add(tempRepos[idx]);
                tempRepos.RemoveAt(idx);
            }
            idx = tempRepos.FindIndex(a => a.PackageSource.Name.Equals("nuget.org"));
            if (idx >= 0)
            {
                sortedRepositories.Add(tempRepos[idx]);
                tempRepos.RemoveAt(idx);
            }
            idx = tempRepos.FindIndex(a => a.PackageSource.IsOfficial);
            if (idx >= 0)
            {
                sortedRepositories.Add(tempRepos[idx]);
                tempRepos.RemoveAt(idx);
            }
            foreach (var r in tempRepos)
            {
                sortedRepositories.Add(r);
            }
            return sortedRepositories;
        }
        public async Task GetPackageDependencies(PackageIdentity package, SourceCacheContext cacheContext, ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(package)) return;
            var repositories = GetSortedRepositories();

            foreach (var sourceRepository in repositories)
            {
                SourcePackageDependencyInfo dependencyInfo = null;
                try
                {
                    var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                    dependencyInfo = await dependencyInfoResource.ResolvePackage(
                        package, NuGetFramework, cacheContext, NullLogger.Instance, CancellationToken.None);
                    if (dependencyInfo == null) continue;
                    availablePackages.Add(dependencyInfo);
                }
                catch (Exception ex)
                {
                }
                foreach (var dependency in dependencyInfo.Dependencies)
                {
                    var identity = new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion);
                    await GetPackageDependencies(identity, cacheContext, availablePackages);
                }
                break;
            }
        }
        public async Task<List<IPackageSearchMetadata>> DownloadAndInstall(Project project, PackageIdentity identity, bool LoadDlls)
        {
            var result = new List<IPackageSearchMetadata>();

            using (var cacheContext = new SourceCacheContext())
            {
                var repositories = DefaultSourceRepositoryProvider.GetRepositories();
                var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                await GetPackageDependencies(identity, cacheContext, availablePackages);

                var resolverContext = new PackageResolverContext(
                    DependencyBehavior.Lowest,
                    new[] { identity.Id },
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<NuGet.Packaging.PackageReference>(),
                    Enumerable.Empty<PackageIdentity>(),
                    availablePackages,
                    DefaultSourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
                    NullLogger.Instance);

                var resolver = new PackageResolver();
                // resolverContext.IncludeUnlisted = true;
                var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                    .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));
                var packagePathResolver = new NuGet.Packaging.PackagePathResolver(PackagesInstallFolder);
                var clientPolicyContext = NuGet.Packaging.Signing.ClientPolicyContext.GetClientPolicy(Settings, NullLogger.Instance);
                var packageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.None, clientPolicyContext, NullLogger.Instance);
                var frameworkReducer = new FrameworkReducer();

                foreach (var packageToInstall in packagesToInstall)
                {
                    var installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                    if (installedPath == null)
                    {
                        var downloadResource = await packageToInstall.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);
                        var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                            packageToInstall,
                            new PackageDownloadContext(cacheContext),
                            NuGet.Configuration.SettingsUtility.GetGlobalPackagesFolder(Settings),
                            NullLogger.Instance, CancellationToken.None);

                        await PackageExtractor.ExtractPackageAsync(PackagesInstallFolder,
                            downloadResult.PackageStream,
                            packagePathResolver,
                            packageExtractionContext,
                            CancellationToken.None);
                    }
                    // per project or joined ?
                    // string TargetFolder = System.IO.Path.Combine(project.Path, "extensions");
                    string TargetFolder = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "extensions");
                   
                    InstallPackage(TargetFolder, packageToInstall, LoadDlls);
                }
            }
            return result;
        }
        public LocalPackageInfo getLocal(string identity)
        {
            FindLocalPackagesResourceV2 findLocalPackagev2 = new FindLocalPackagesResourceV2(PackagesInstallFolder);
            var packages = findLocalPackagev2.GetPackages(NullLogger.Instance, CancellationToken.None).ToList();
            packages = packages.Where(p => p.Identity.Id == identity).ToList();
            LocalPackageInfo res = null;
            foreach (var p in packages)
            {
                if (res == null) res = p;
                if (res.Identity.Version < p.Identity.Version) res = p;
            }
            return res;
        }
        public bool InstallPackage(string TargetFolder, PackageIdentity identity, bool LoadDlls)
        {
            bool ret = true;

            var packagePathResolver = new NuGet.Packaging.PackagePathResolver(PackagesInstallFolder);
            var installedPath = packagePathResolver.GetInstalledPath(identity);

            PackageReaderBase packageReader;
            packageReader = new PackageFolderReader(installedPath);
            var libItems = packageReader.GetLibItems();
            var frameworkReducer = new FrameworkReducer();
            var nearest = frameworkReducer.GetNearest(NuGetFramework, libItems.Select(x => x.TargetFramework));
            var files = libItems
                .Where(x => x.TargetFramework.Equals(nearest))
                .SelectMany(x => x.Items).ToList();
            foreach (var f in files)
            {
                InstallFile(TargetFolder, installedPath, f, LoadDlls);
            }

            var cont = packageReader.GetContentItems();
            nearest = frameworkReducer.GetNearest(NuGetFramework, cont.Select(x => x.TargetFramework));
            files = cont
                .Where(x => x.TargetFramework.Equals(nearest))
                .SelectMany(x => x.Items).ToList();
            foreach (var f in files)
            {
                InstallFile(TargetFolder, installedPath, f, LoadDlls);
            }

            try
            {
                var dependencies = packageReader.GetPackageDependencies();
                nearest = frameworkReducer.GetNearest(NuGetFramework, dependencies.Select(x => x.TargetFramework));
                foreach (var dep in dependencies.Where(x => x.TargetFramework.Equals(nearest)))
                {
                    foreach (var p in dep.Packages)
                    {
                        var local = getLocal(p.Id);
                        InstallPackage(TargetFolder, local.Identity, LoadDlls);
                    }
                }
            }
            catch (Exception ex)
            {
                ret = false;
                Log.Error(ex.ToString());
            }

            if (System.IO.Directory.Exists(installedPath + @"\build"))
            {
                if (System.IO.Directory.Exists(installedPath + @"\build\x64"))
                {
                    foreach (var f in System.IO.Directory.GetFiles(installedPath + @"\build\x64"))
                    {
                        var filename = System.IO.Path.GetFileName(f);
                        var target = System.IO.Path.Combine(TargetFolder, filename);
                        CopyIfNewer(f, target);
                    }
                }
            }

            return ret;
        }
        private void InstallFile(string TargetFolder, string installedPath, string f, bool LoadDlls)
        {
            string source;
            string f2;
            string filename;
            string dir;
            string target;
            try
            {
                source = System.IO.Path.Combine(installedPath, f);
                var arr = f.Split('/');
                if (arr[0] == "lib")
                {
                    if (arr.Length == 2)
                    {
                        f2 = f.Substring(f.IndexOf("/", 3) + 1);
                    }
                    else
                    {
                        f2 = f.Substring(f.IndexOf("/", 4) + 1);
                    }
                }
                else
                {
                    f2 = f.Substring(f.IndexOf("/", 0) + 1);
                }

                filename = System.IO.Path.GetFileName(f2);
                dir = System.IO.Path.GetDirectoryName(f2);
                target = System.IO.Path.Combine(TargetFolder, dir, filename);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(TargetFolder, dir)))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TargetFolder, dir));
                }
                CopyIfNewer(source, target);
                if(System.IO.Path.GetExtension(target) == ".dll" && LoadDlls)
                {
                    try
                    {
                        Log.Output("Loading " + target);
                        var an = System.Reflection.AssemblyName.GetAssemblyName(target);
                        var assembly = System.Reflection.Assembly.Load(an);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public static List<string> PendingDeletion = new List<string>();
        private void UninstallFile(string TargetFolder, string installedPath, string f)
        {
            string source;
            string f2;
            string filename;
            string dir;
            string target;
            try
            {
                source = System.IO.Path.Combine(installedPath, f);
                var arr = f.Split('/');
                if (arr[0] == "lib")
                {
                    if (arr.Length == 2)
                    {
                        f2 = f.Substring(f.IndexOf("/", 3) + 1);
                    }
                    else
                    {
                        f2 = f.Substring(f.IndexOf("/", 4) + 1);
                    }
                }
                else
                {
                    f2 = f.Substring(f.IndexOf("/", 0) + 1);
                }

                filename = System.IO.Path.GetFileName(f2);
                dir = System.IO.Path.GetDirectoryName(f2);
                target = System.IO.Path.Combine(TargetFolder, dir, filename);
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(TargetFolder, dir)))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TargetFolder, dir));
                }
                if (System.IO.File.Exists(target))
                {
                    try
                    {
                        System.IO.File.Delete(target);
                    }
                    catch (Exception)
                    {
                        PendingDeletion.Add(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        private void CopyIfNewer(string source, string target)
        {
            var infoOld = new System.IO.FileInfo(source);
            var infoNew = new System.IO.FileInfo(target);
            if (infoNew.LastWriteTime != infoOld.LastWriteTime)
            {
                try
                {
                    System.IO.File.Copy(source, target, true);
                    return;
                }
                catch (Exception)
                {

                }
            }
        }
        public void UninstallPackage(string TargetFolder, PackageIdentity identity)
        {

            var packagePathResolver = new PackagePathResolver(PackagesInstallFolder);
            var installedPath = packagePathResolver.GetInstalledPath(identity);

            PackageReaderBase packageReader;
            packageReader = new PackageFolderReader(installedPath);
            var libItems = packageReader.GetLibItems();
            var frameworkReducer = new FrameworkReducer();
            var nearest = frameworkReducer.GetNearest(NuGetFramework, libItems.Select(x => x.TargetFramework));
            var files = libItems
                .Where(x => x.TargetFramework.Equals(nearest))
                .SelectMany(x => x.Items).ToList();
            foreach (var f in files)
            {
                UninstallFile(TargetFolder, installedPath, f);
            }

            var cont = packageReader.GetContentItems();
            nearest = frameworkReducer.GetNearest(NuGetFramework, cont.Select(x => x.TargetFramework));
            files = cont
                .Where(x => x.TargetFramework.Equals(nearest))
                .SelectMany(x => x.Items).ToList();
            foreach (var f in files)
            {
                UninstallFile(TargetFolder, installedPath, f);
            }

            try
            {
                var dependencies = packageReader.GetPackageDependencies();
                nearest = frameworkReducer.GetNearest(NuGetFramework, dependencies.Select(x => x.TargetFramework));
                foreach (var dep in dependencies.Where(x => x.TargetFramework.Equals(nearest)))
                {
                    foreach (var p in dep.Packages)
                    {
                        var local = getLocal(p.Id);
                        UninstallPackage(TargetFolder, local.Identity);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            if (System.IO.Directory.Exists(installedPath + @"\build"))
            {
                if (System.IO.Directory.Exists(installedPath + @"\build\x64"))
                {
                    foreach (var f in System.IO.Directory.GetFiles(installedPath + @"\build\x64"))
                    {
                        var filename = System.IO.Path.GetFileName(f);
                        var target = System.IO.Path.Combine(TargetFolder, filename);
                        if (System.IO.File.Exists(target))
                        {
                            try
                            {
                                System.IO.File.Delete(target);
                            }
                            catch (Exception)
                            {
                                PendingDeletion.Add(target);
                            }
                        }
                    }
                }
            }
        }
    }
}
