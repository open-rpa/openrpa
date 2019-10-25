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
        public string RepositoryPath;
        public string InstallPath;
        System.Runtime.Versioning.FrameworkName TargetFramework;
        public Updates()
        {
            TargetFramework = new System.Runtime.Versioning.FrameworkName(".NETFramework", new Version("4.6.2"));
            var cur = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
            RepositoryPath = cur.Parent.FullName + @"\Packages";
            // InstallPath = cur.Parent.FullName + @"\OpenRPA";
            InstallPath = cur.Parent.FullName;
            Updater.OpenRPAPackageManager.Instance.Destinationfolder = InstallPath;
        }
        public async Task<bool> UpdaterNeedsUpdate()
        {
            string OpenRPAUpdaterexe = System.IO.Path.Combine(InstallPath, "OpenRPA.Updater.exe");
            if (!System.IO.File.Exists(OpenRPAUpdaterexe)) return false;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(OpenRPAUpdaterexe);
            string version = fileVersionInfo.ProductVersion;

            var package = (await Updater.OpenRPAPackageManager.Instance.Search("OpenRPA.Updater")).Where(x=> x.Identity.Id == "OpenRPA.Updater").FirstOrDefault();
            if (package == null) return false;
            if (new Version(package.Identity.Version.ToString()) > new Version(version))
            {
                return true;
            }
            return false;
        }
        public async Task<string> OpenRPANeedsUpdate()
        {
            var package = (await Updater.OpenRPAPackageManager.Instance.Search("OpenRPA")).Where(x => x.Identity.Id == "OpenRPA").FirstOrDefault();
            if (package == null) return null;

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductVersion;

            if (new Version(package.Identity.Version.ToString()) > new Version(version))
            {
                // if(!string.IsNullOrEmpty(package.Summary)) return package.Summary;
                return "A new version " + package.Identity.Version.ToString() + " is ready for download, current version is " + version;
            }
            return null;
        }
        public async Task UpdateUpdater()
        {
            var package = (await Updater.OpenRPAPackageManager.Instance.Search("OpenRPA.Updater")).Where(x => x.Identity.Id == "OpenRPA.Updater").FirstOrDefault();
            if (package == null) return;
            Updater.OpenRPAPackageManager.Instance.InstallPackage(package.Identity);
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

    }
}
