using NuGet.Versioning;
using NuGet.Frameworks;
using OpenRPA.Interfaces;

namespace OpenRPA
{
    public class PackageDependency
    {
        public PackageDependency(string id, string version, IProject project, NuGetFramework targetFramework)
            : this(id, NuGetVersion.Parse(version), project, targetFramework)
        { }

        public PackageDependency(string id, NuGetVersion version, IProject project, NuGetFramework targetFramework)
        {
            Id = id;
            Version = version;
            Project = project;
            TargetFramework = targetFramework;
        }

        public string Id { get; set; }
        public NuGetVersion Version { get; set; }
        public IProject Project { get; set; }
        public NuGetFramework TargetFramework { get; set; }

        public override string ToString()
        {
            return $"{Id} {Version} from Project '{Project.name}' ({Project._id})";
        }
    }
}
