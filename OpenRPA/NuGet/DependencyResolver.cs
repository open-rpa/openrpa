using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using OpenRPA.Interfaces;
using NuGet.Common;
using Newtonsoft.Json;

namespace OpenRPA
{

    public class DependencyResolver
    {
        public List<PackageDependency> GetProjectDependencies(IProject project, NuGetFramework framework)
        {
            var result = new List<PackageDependency>();
            if (project.dependencies == null || project.dependencies.Count == 0)
            {
                return result;
            }
            foreach (var dependency in project.dependencies)
            {
                result.Add(new PackageDependency(dependency.Key, dependency.Value, project, framework));
            }

            return result;
        }

        public async Task<List<PackageDependency>> GetAllDependenciesAsync(string packageId, NuGetVersion version, IProject project, NuGetFramework targetFramework, SourceRepositoryProvider sourceRepositoryProvider, string dependencyPath)
        {
            var dependencies = new List<PackageDependency>();
            bool packageFound = false;
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            var frameworkReducer = new FrameworkReducer();
            foreach (var sourceRepository in sourceRepositoryProvider.GetRepositories())
            {
                if (packageFound) break;
                var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
                var packageMetadata = await metadataResource.GetMetadataAsync(packageId, includePrerelease: false, includeUnlisted: false, sourceCacheContext: new SourceCacheContext(), log: NullLogger.Instance, token: CancellationToken.None);

                var package = packageMetadata.FirstOrDefault(p => p.Identity.Version == version);
                if (package != null)
                {
                    var nearestFramework = frameworkReducer.GetNearest(targetFramework, package.DependencySets.Select(ds => ds.TargetFramework));
                    foreach (var dependencyGroup in package.DependencySets)
                    {
                        if (packageFound) break;
                        
                        if (dependencyGroup.TargetFramework == nearestFramework || dependencyGroup.TargetFramework == NuGetFramework.AnyFramework)
                        {
                            packageFound = true;
                            foreach (var dependency in dependencyGroup.Packages)
                            {
                                var newDependency = new PackageDependency(dependency.Id, dependency.VersionRange.MinVersion, project, targetFramework);
                                newDependency.AddToDependencyPath(packageId + " (" + version.ToNormalizedString() + ")");
                                dependencies.Add(newDependency);

                                // Recursively resolve dependencies
                                var subDependencies = await GetAllDependenciesAsync(dependency.Id, dependency.VersionRange.MinVersion, project, targetFramework, sourceRepositoryProvider, newDependency.DependencyPath);
                                dependencies.AddRange(subDependencies);
                            }
                        }
                    }
                }
            }
            return dependencies;
        }

        public List<PackageDependency> FlattenDependencies(List<List<PackageDependency>> allDependencies)
        {
            return allDependencies.SelectMany(dependencies => dependencies).ToList();
        }

        public Dictionary<string, Dictionary<NuGetFramework, PackageDependency>> GetHighestVersionDependencies(List<PackageDependency> dependencies)
        {
            var highestVersionDict = dependencies
                .GroupBy(d => d.Id)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(d => d.TargetFramework)
                          .ToDictionary(
                              gf => gf.Key,
                              gf => gf.OrderByDescending(d => d.Version).First()
                          )
                );

            return highestVersionDict;
        }

        public async Task ResolveAllDependencies(IEnumerable<IProject> projects, NuGetFramework framework, SourceRepositoryProvider sourceRepositoryProvider)
        {
            var allDependencies = new List<List<PackageDependency>>();

            foreach (var project in projects)
            {
                var initialDependencies = GetProjectDependencies(project, framework);

                foreach (var dependency in initialDependencies)
                {
                    dependency.AddToDependencyPath(project.name);
                    var resolvedDependencies = await GetAllDependenciesAsync(dependency.Id, dependency.Version, project, framework, sourceRepositoryProvider, dependency.DependencyPath);
                    resolvedDependencies.Add(dependency); // Include the initial dependency
                    allDependencies.Add(resolvedDependencies);
                }
            }

            var flatDependencies = FlattenDependencies(allDependencies);
            var outputString = JsonConvert.SerializeObject(flatDependencies, Formatting.Indented);
            Log.Output(outputString);
            var highestVersionDependencies = GetHighestVersionDependencies(flatDependencies);
            outputString = JsonConvert.SerializeObject(highestVersionDependencies, Formatting.Indented);
            Log.Output(outputString);
            // TODO: continue here
        }

    }
}
