using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Updater
{
    using System.Threading;
    using NuGet.Common;
    using NuGet.Frameworks;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.Protocol;
    using NuGet.Protocol.Core.Types;
    using NuGet.Resolver;

    public class OpenRPAPackageManager
    {
        private static ILogger _logger = null;
        public static ILogger Logger
        {
            get
            {
                if (_logger == null) _logger = OpenRPAPackageManagerLogger.Instance;
                return _logger;
                // return NullLogger.Instance;
            }
        }
        private static OpenRPAPackageManager _instance = null;
        public static OpenRPAPackageManager Instance
        {
            get
            {
                if (_instance == null) _instance = new OpenRPAPackageManager();
                return _instance;
            }
        }
        public NuGet.Configuration.ISettings _settings = null;
        public NuGet.Configuration.ISettings Settings
        {
            get
            {
                if (_settings == null) _settings = NuGet.Configuration.Settings.LoadDefaultSettings(root: null);
                return _settings;
            }
        }
        public NuGetFramework _nuGetFramework = null;
        public NuGetFramework NuGetFramework
        {
            get
            {
                if (_nuGetFramework == null) _nuGetFramework = NuGetFramework.ParseFolder("net467");
                return _nuGetFramework;
            }
        }
        public SourceRepositoryProvider _sourceRepositoryProvider = null;
        public SourceRepositoryProvider SourceRepositoryProvider
        {
            get
            {
                if (_sourceRepositoryProvider == null)
                {
                    _sourceRepositoryProvider = new SourceRepositoryProvider(new NuGet.Configuration.PackageSourceProvider(Settings), Repository.Provider.GetCoreV3());
                }
                return _sourceRepositoryProvider;
            }
        }
        public string _packagesfolder = null;
        public string Packagesfolder
        {
            get
            {
                if (string.IsNullOrEmpty(_packagesfolder)) _packagesfolder = System.IO.Path.GetFullPath("packages");
                return _packagesfolder;
            }
        }
        public string _destinationfolder = null;
        public string Destinationfolder
        {
            get
            {
                if (string.IsNullOrEmpty(_destinationfolder)) _destinationfolder = System.IO.Path.GetFullPath("openrpa");
                return _destinationfolder;
            }
        }
        public async Task<List<IPackageSearchMetadata>> Search(string searchstring)
        {
            var result = new List<IPackageSearchMetadata>();
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            foreach (var sourceRepository in SourceRepositoryProvider.GetRepositories())
            {
                var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
                var supportedFramework = new[] { ".NETFramework,Version=v4.6" };
                var searchFilter = new SearchFilter(true)
                {
                    SupportedFrameworks = supportedFramework,
                    IncludeDelisted = false
                };

                var jsonNugetPackages = await searchResource
                            .SearchAsync(searchstring, searchFilter, 0, 50, OpenRPAPackageManagerLogger.Instance, CancellationToken.None);
                foreach (var p in jsonNugetPackages.Where(x => x.Identity.Id.Contains(searchstring)))
                {
                    var exists = result.Where(x => x.Identity == p.Identity).FirstOrDefault();
                    if (exists == null) result.Add(p);
                }
            }
            return result;
        }
        public async Task GetPackageDependencies(PackageIdentity package, SourceCacheContext cacheContext,
            ISet<SourcePackageDependencyInfo> availablePackages)
        {
            if (availablePackages.Contains(package)) return;
            var repositories = SourceRepositoryProvider.GetRepositories();
            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package, NuGetFramework, cacheContext, Logger, CancellationToken.None);
                if (dependencyInfo == null) continue;
                availablePackages.Add(dependencyInfo);
                foreach (var dependency in dependencyInfo.Dependencies)
                {
                    await GetPackageDependencies(
                        new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                        cacheContext, availablePackages);
                }
            }
        }

        // https://github.com/NuGet/Home/issues/5674
        public async Task<List<IPackageSearchMetadata>> DownloadAndInstall(PackageIdentity package)
        {
            var result = new List<IPackageSearchMetadata>();

            using (var cacheContext = new SourceCacheContext())
            {
                var repositories = SourceRepositoryProvider.GetRepositories();
                var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                await GetPackageDependencies(package, cacheContext, availablePackages);

                var resolverContext = new PackageResolverContext(
                    DependencyBehavior.Lowest,
                    new[] { package.Id },
                    Enumerable.Empty<string>(),
                    Enumerable.Empty<NuGet.Packaging.PackageReference>(),
                    Enumerable.Empty<PackageIdentity>(),
                    availablePackages,
                    SourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
                    NullLogger.Instance);

                var resolver = new PackageResolver();
                var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                    .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));
                var packagePathResolver = new NuGet.Packaging.PackagePathResolver(Packagesfolder);
                var clientPolicyContext = NuGet.Packaging.Signing.ClientPolicyContext.GetClientPolicy(Settings, OpenRPAPackageManagerLogger.Instance);
                var packageExtractionContext = new PackageExtractionContext(PackageSaveMode.Defaultv3, XmlDocFileSaveMode.None, clientPolicyContext, OpenRPAPackageManagerLogger.Instance);
                var frameworkReducer = new FrameworkReducer();

                foreach (var packageToInstall in packagesToInstall)
                {
                    PackageReaderBase packageReader;
                    var installedPath = packagePathResolver.GetInstalledPath(packageToInstall);
                    if (installedPath == null)
                    {
                        var downloadResource = await packageToInstall.Source.GetResourceAsync<DownloadResource>(CancellationToken.None);
                        var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                            packageToInstall,
                            new PackageDownloadContext(cacheContext),
                            NuGet.Configuration.SettingsUtility.GetGlobalPackagesFolder(Settings),
                            NullLogger.Instance, CancellationToken.None);

                        await PackageExtractor.ExtractPackageAsync(
                            downloadResult.PackageSource,
                            downloadResult.PackageStream,
                            packagePathResolver,
                            packageExtractionContext,
                            CancellationToken.None);

                        packageReader = downloadResult.PackageReader;
                    }
                    else
                    {
#pragma warning disable IDE0067 // Dispose objects before losing scope
                        packageReader = new PackageFolderReader(installedPath);
#pragma warning restore IDE0067 // Dispose objects before losing scope
                    }

                    var libItems = packageReader.GetLibItems();
                    var nearest = frameworkReducer.GetNearest(NuGetFramework, libItems.Select(x => x.TargetFramework));
                    //Console.WriteLine(string.Join("\n", libItems
                    //    .Where(x => x.TargetFramework.Equals(nearest))
                    //    .SelectMany(x => x.Items)));
                    var files = libItems
                        .Where(x => x.TargetFramework.Equals(nearest))
                        .SelectMany(x => x.Items).ToList();

                    var frameworkItems = packageReader.GetFrameworkItems();
                    var nearest2 = frameworkReducer.GetNearest(NuGetFramework, frameworkItems.Select(x => x.TargetFramework));


                    var refs = packageReader.GetReferenceItems();
                    var libs = packageReader.GetLibItems();
                    var contents = packageReader.GetContentItems();

                    foreach (var f in files)
                    {
                        string source = "";
                        string f2 = "";
                        string filename = "";
                        string dir = "";
                        string target = "";
                        try
                        {
                            source = System.IO.Path.Combine(installedPath, f);
                            f2 = f.Substring(f.IndexOf("/", 4) + 1);
                            filename = System.IO.Path.GetFileName(f2);
                            dir = System.IO.Path.GetDirectoryName(f2);
                            target = System.IO.Path.Combine(Destinationfolder, dir, filename);
                            if (!System.IO.Directory.Exists(System.IO.Path.Combine(Destinationfolder, dir)))
                            {
                                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Destinationfolder, dir));
                            }
                            // if (!System.IO.File.Exists(target)) System.IO.File.Copy(source, target);
                            if (System.IO.File.Exists(target)) System.IO.File.Delete(target);
                            System.IO.File.Copy(source, target);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    // packageReader.CopyFiles(destinationfolder, files, ExtractFile, logger, CancellationToken.None);
                }
            }
            return result;
        }
        //private string ExtractFile(string sourcePath, string targetPath, System.IO.Stream sourceStream)
        //{
        //    using (var targetStream = System.IO.File.OpenWrite(targetPath))
        //    {
        //        sourceStream.CopyTo(targetStream);
        //    }
        //    return targetPath;
        //}
        public LocalPackageInfo getLocal(string identity)
        {
            List<Lazy<INuGetResourceProvider>> providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());

            FindLocalPackagesResourceV2 findLocalPackagev2 = new FindLocalPackagesResourceV2(Packagesfolder);
            var packages = findLocalPackagev2.GetPackages(Logger, CancellationToken.None).ToList();
            packages = packages.Where(p => p.Identity.Id == identity).ToList();
            LocalPackageInfo res = null;
            foreach (var p in packages)
            {
                if (res == null) res = p;
                if (res.Identity.Version < p.Identity.Version) res = p;
            }
            return res;
            //found, but missing a lot of informations...
            //var supportedFramework = new[] { ".NETFramework,Version=v4.6" };
            //var searchFilter = new SearchFilter(true)
            //{
            //    SupportedFrameworks = supportedFramework,
            //    IncludeDelisted = false
            //};
            //var localSource = new NuGet.Configuration.PackageSource(Packagesfolder);
            //SourceRepository localRepository = new SourceRepository(localSource, providers);
            //PackageSearchResource searchLocalResource = await localRepository
            //    .GetResourceAsync<PackageSearchResource>();

            //var packageFound3 = await searchLocalResource
            //    .SearchAsync("Newtonsoft.Json", searchFilter, 0, 10, Logger, CancellationToken.None);
            //var thePackage = packageFound3.FirstOrDefault();
            //// found but missing the assemblies property
        }

        public bool IsPackageInstalled(string identity)
        {
            var p = getLocal(identity);



            return false;
        }
    }
}
