using NuGet;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA
{
    using System.Reflection;
    using System.Diagnostics;
    public class Updates
    {
        public IPackageRepository repo;
        public PackageManager packageManager;
        public DefaultPackagePathResolver resolver;
        public IFileSystem FileSystem;
        public string RepositoryPath;
        public string InstallPath;
        System.Runtime.Versioning.FrameworkName TargetFramework;
        Logger logger = new Logger();
        public Updates()
        {
            TargetFramework = new System.Runtime.Versioning.FrameworkName(".NETFramework", new Version("4.6.2"));
            var cur = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
            RepositoryPath = cur.Parent.FullName + @"\Packages";
            // InstallPath = cur.Parent.FullName + @"\OpenRPA";
            InstallPath = cur.Parent.FullName;
            logger.Updated += (string message) =>
            {
                Log.Information(message);
            };
            repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");
            resolver = new DefaultPackagePathResolver(RepositoryPath);
            FileSystem = new PhysicalFileSystem(RepositoryPath);
            // packageManager = new PackageManager(repo, InstallPath) { Logger = logger };
            packageManager = new PackageManager(repo, resolver, FileSystem) { Logger = logger };
        }
        public bool UpdaterNeedsUpdate()
        {
            string OpenRPAUpdaterexe = System.IO.Path.Combine(InstallPath, "OpenRPA.Updater.exe");
            if (!System.IO.File.Exists(OpenRPAUpdaterexe)) return false;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(OpenRPAUpdaterexe);
            string version = fileVersionInfo.ProductVersion;

            var onlinepackage = repo.FindPackage("OpenRPA.Updater");
            if (onlinepackage == null) return false;
            // var localpackage = packageManager.LocalRepository.FindPackage("OpenRPA.Updater");
            //if (onlinepackage == null || localpackage == null) return true;
            if (new Version(onlinepackage.Version.ToString()) > new Version(version))
            {
                return true;
            }
            return false;
        }
        public string OpenRPANeedsUpdate()
        {
            var onlinepackage = repo.FindPackage("OpenRPA");
            //var localpackage = packageManager.LocalRepository.FindPackage("OpenRPA");
            //if (onlinepackage == null || localpackage == null) return true;

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductVersion;

            if (new Version(onlinepackage.Version.ToString()) > new Version(version))
            {
                if(!string.IsNullOrEmpty(onlinepackage.ReleaseNotes)) return onlinepackage.ReleaseNotes;
                return "A new version " + onlinepackage.Version.ToString() + " is ready for download, current version is " + version;
            }
            return null;
        }
        public void UpdateUpdater()
        {
            var onlinepackage = repo.FindPackage("OpenRPA.Updater");
            if(onlinepackage!=null)
            {
                InstallPackage(TargetFramework, onlinepackage);
            }
        }
        static IEnumerable<T> GetCompatibleItems<T>(System.Runtime.Versioning.FrameworkName targetFramework, IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;
            if (VersionUtility.TryGetCompatibleItems(targetFramework, items, out compatibleItems))
            {
                return compatibleItems;
            }
            return Enumerable.Empty<T>();
        }
        private void CopyIfNewer(string source, string target)
        {
            var infoOld = new System.IO.FileInfo(source);
            var infoNew = new System.IO.FileInfo(target);
            //if (infoNew.LastWriteTime > infoOld.LastWriteTime)
            if (infoNew.LastWriteTime != infoOld.LastWriteTime)
            {
                try
                {
                    System.IO.File.Copy(source, target, true);
                    return;
                }
                catch (Exception)
                {
                    KillOpenRPAUpdater();
                    System.Threading.Thread.Sleep(1000);
                }
                System.IO.File.Copy(source, target, true);
            }
        }
        public void Run(string WorkingDirectory, string command)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c " + command;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WorkingDirectory = WorkingDirectory;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }
        public void KillOpenRPAUpdater()
        {
            Run("", "taskkill /f /fi \"pid gt 0\" /im OpenRPA.Updater.exe");
        }
        private void PackageInstalled(object sender, PackageOperationEventArgs eventArgs)
        {
            //if (eventArgs.Package.Id.ToLower().Contains("humanizer"))
            //{
            //    var b = true;
            //}
            // List<IPackageAssemblyReference> assemblyReferences = GetCompatibleItems(TargetFramework, eventArgs.Package.AssemblyReferences).ToList();
            List<IPackageAssemblyReference> assemblyReferences = GetCompatibleItems(TargetFramework, eventArgs.Package.AssemblyReferences).ToList();
            if (assemblyReferences.Count == 0)
            {
                // assemblyReferences = GetCompatibleItems(TargetFramework20, eventArgs.Package.AssemblyReferences).ToList();
            }
            var skipFiles = new List<string>();
            foreach (var f in assemblyReferences)
            {
                var source = System.IO.Path.Combine(eventArgs.InstallPath, f.Path);
                var filename = System.IO.Path.GetFileName(source);
                skipFiles.Add(filename);
                var target = System.IO.Path.Combine(InstallPath, filename);
                if (!System.IO.File.Exists(source))
                {
                    eventArgs.Package.ExtractContents(FileSystem, eventArgs.InstallPath);
                    System.Diagnostics.Debug.WriteLine("Where is " + source);
                }
                CopyIfNewer(source, target);
            }
            foreach (var f in eventArgs.Package.GetLibFiles())
            {
                var source = System.IO.Path.Combine(eventArgs.InstallPath, f.Path);
                var filename = System.IO.Path.GetFileName(source);
                if (skipFiles.Contains(filename)) continue;
                var target = System.IO.Path.Combine(InstallPath, filename);
                if (!System.IO.File.Exists(source))
                {
                    eventArgs.Package.ExtractContents(FileSystem, eventArgs.InstallPath);
                    System.Diagnostics.Debug.WriteLine("Where is " + source);
                }
                CopyIfNewer(source, target);
            }
            foreach (var f in eventArgs.Package.GetContentFiles())
            {
                var source = System.IO.Path.Combine(eventArgs.InstallPath, f.Path);
                var filename = System.IO.Path.GetFileName(source);
                if (skipFiles.Contains(filename)) continue;
                var target = System.IO.Path.Combine(InstallPath, filename);
                if (!System.IO.File.Exists(source))
                {
                    eventArgs.Package.ExtractContents(FileSystem, eventArgs.InstallPath);
                    System.Diagnostics.Debug.WriteLine("Where is " + source);
                }
                CopyIfNewer(source, target);
            }

            if (System.IO.Directory.Exists(eventArgs.InstallPath + @"\build"))
            {
                if (System.IO.Directory.Exists(eventArgs.InstallPath + @"\build\x64"))
                {
                    foreach (var f in System.IO.Directory.GetFiles(eventArgs.InstallPath + @"\build\x64"))
                    {
                        var filename = System.IO.Path.GetFileName(f);
                        var target = System.IO.Path.Combine(InstallPath, filename);
                        CopyIfNewer(f, target);
                    }
                }
            }
        }
        void InstallPackageDependencies(System.Runtime.Versioning.FrameworkName TargetFramework, IPackage Package)
        {
            if (Package == null) throw new ArgumentNullException("Package", "Package cannot be null");
            if (Package.Id.ToLower() == "netstandard.library") return;
            if (Package.Id.ToLower() == "system.net.websockets.client.managed") return;
            if (Package.Id.ToLower() == "system.reflection.emit") return;

            try
            {
                IPackage exists = null;
                System.Runtime.Versioning.FrameworkName framework = TargetFramework;
                if (Package == null) return;
                var deps = Package.GetCompatiblePackageDependencies(framework);
                if (deps.Count() == 0)
                {
                    var dsets = Package.DependencySets.Where(x => x.TargetFramework.Version.Major == 4).OrderByDescending(x => x.TargetFramework.Version.Minor).ToList();
                    if (dsets.Count() == 0 && Package.DependencySets.Count() == 1)
                    {
                        dsets = Package.DependencySets.ToList();
                    }
                    if (dsets.Count() > 0)
                    {
                        if (dsets.First().Dependencies.Count() > 0)
                        {
                            framework = dsets.First().TargetFramework;
                            System.Diagnostics.Debug.WriteLine("******** " + Package.Id + " " + framework.ToString());
                            deps = dsets.First().Dependencies;
                        }

                    }
                    //// .NETFramework4.5
                    //framework = new System.Runtime.Versioning.FrameworkName(".NETStandard", new Version("2.0"));
                    //deps = Package.GetCompatiblePackageDependencies(framework);
                    //if(deps.Count() > 0)
                    //{
                    //    System.Diagnostics.Debug.WriteLine("******** " + Package.Id + " " + framework.ToString());
                    //}
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("******** " + Package.Id + " " + TargetFramework.ToString());
                    foreach (var d in deps) System.Diagnostics.Debug.WriteLine(d.Id);
                }
                foreach (var d in deps)
                {
                    exists = packageManager.LocalRepository.FindPackage(d.Id);
                    // d.Id.ToLower().Contains("fastmember") ||
                    // if (exists != null) continue;
                    //if (d.Id.ToLower().Contains("controlzex") || d.Id.ToLower().Contains("humanizer"))
                    //{
                    //    packageManager.InstallPackage(d.Id, version: d.VersionSpec.MinVersion, ignoreDependencies: true, allowPrereleaseVersions: false);
                    //}
                    //else
                    //{
                    //    packageManager.InstallPackage(d.Id, null, true, false);
                    //}
                    //packageManager.InstallPackage(d.Id, null, true, false);
                    packageManager.InstallPackage(d.Id, version: d.VersionSpec.MinVersion, ignoreDependencies: true, allowPrereleaseVersions: false);
                    // Whats wrong with FastMember.1.5.0 ??
                    exists = packageManager.LocalRepository.FindPackage(d.Id);
                    // exists = packageManager.LocalRepository.FindPackage(d.Id, d.VersionSpec.MinVersion);
                    if (exists != null)
                    {
                        PackageInstalled(null, new PackageOperationEventArgs(exists, FileSystem, RepositoryPath + @"\" + exists.Id + "." + exists.Version.ToString()));
                        InstallPackageDependencies(framework, exists);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(MessageLevel.Error, ex.ToString());
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        }
        void InstallPackage(System.Runtime.Versioning.FrameworkName TargetFramework, IPackage Package)
        {
            if (Package == null) throw new ArgumentNullException("Package", "Package cannot be null");
            try
            {
                if (!System.IO.Directory.Exists(InstallPath)) System.IO.Directory.CreateDirectory(InstallPath);
                InstallPackageDependencies(TargetFramework, Package);
                packageManager.InstallPackage(Package, true, false, true);
                //packageManager.InstallPackage(SelectedValue.Package, false, false, false);
                PackageInstalled(null, new PackageOperationEventArgs(Package, FileSystem, RepositoryPath + @"\" + Package.Id + "." + Package.Version.ToString()));
            }
            catch (Exception ex)
            {
                logger.Log(MessageLevel.Error, ex.ToString());
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
            }
        }
    }
}
