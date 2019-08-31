using NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenRPA.Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        private string logs;
        public string Logs
        {
            get
            {
                return logs;
            }
            set
            {
                logs = value;
                NotifyPropertyChanged("Logs");
            }
        }
        public IPackageRepository repo;
        public PackageManager packageManager;
#if DEBUG
        public IPackageRepository publicrepo;
        public PackageManager PublicpackageManager;
#endif
        public ProjectManager projectManager;
        public IFileSystem FileSystem;
        public DefaultPackagePathResolver resolver;
        public string RepositoryPath;
        public string InstallPath;
        public bool FirstRun = true;
        // public IPackagePathResolver resolver;
        System.Runtime.Versioning.FrameworkName TargetFramework;
        // System.Runtime.Versioning.FrameworkName TargetFramework20;
        Logger logger = new Logger();

        public MainWindow()
        {
            InitializeComponent();
            ButtonUpdateAll.IsEnabled = false;
            DataContext = this;
#if DEBUG
            // repo = PackageRepositoryFactory.Default.CreateRepository(@"C:\code\OpenRPA\packages");
            repo = PackageRepositoryFactory.Default.CreateRepository(@"C:\code\OpenRPA\packages");
            publicrepo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");
#else
            repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");
#endif
            logger.Updated += () =>
            {
                Logs = logger.Logs;
            };
            RepositoryPath = Environment.CurrentDirectory + @"\Packages";
            InstallPath = Environment.CurrentDirectory + @"\OpenRPA";
            // TargetFramework = new System.Runtime.Versioning.FrameworkName(".NETFramework, Version=4.0");
            TargetFramework = new System.Runtime.Versioning.FrameworkName(".NETFramework", new Version("4.6.2"));
            // TargetFramework20 = new System.Runtime.Versioning.FrameworkName(".NETStandard", new Version("2.0"));

            LoadPackages();
        }
        private void ReloadPackageManager()
        {
            if (packageManager != null) packageManager.PackageUninstalled -= PackageUninstalled;
            resolver = new DefaultPackagePathResolver(RepositoryPath);
            FileSystem = new PhysicalFileSystem(RepositoryPath);
            packageManager = new PackageManager(repo, resolver, FileSystem) { Logger = logger };
#if DEBUG
            PublicpackageManager = new PackageManager(publicrepo, resolver, FileSystem) { Logger = logger };
#endif

            // Lets do this EVERYTIME not just when unpacking !
            // packageManager.PackageInstalled += PackageInstalled;
            packageManager.PackageUninstalled += PackageUninstalled;
        }
        private static bool IsOfficeInstalled()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Winword.exe");
            if (key != null)
            {
                key.Close();
            }
            return key != null;
        }
        public void LoadPackages()
        {
            ReloadPackageManager();
            Task.Run(async () =>
            {
#if DEBUG
                List<IPackage> packages = repo.GetPackages().ToList();
#else
                List<IPackage> packages = repo.Search("OpenRPA.*", false).ToList();
#endif

                // IPackageRepository localRepository = PackageRepositoryFactory.Default.CreateRepository(RepositoryPath);
                PackageModel m = null;
                foreach (var p in packageManager.LocalRepository.GetPackages())
                {
                    if (p.Id.ToLower() == "openrpa" || p.Id.ToLower().StartsWith("openrpa."))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            m = Packages.Where(x => x.Package.Id == p.Id).FirstOrDefault();
                            if (m == null)
                            {
                                m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false, Version = p.Version };
                                m.LatestVersion = p.Version.ToString();
                                Packages.Add(m);
                            }
                            else
                            {
                                if (new Version(p.Version.ToString()) > new Version(m.Version.ToString())) { m.Version = p.Version; m.LatestVersion = p.Version.ToString(); m.Package = p; }
                            }
                        });
                        m.isDownloaded = true;
                        if (!m.isInstalled) m.isInstalled = isPackageInstalled(p);
                    }
                }
                foreach (var p in Packages)
                {
                    var id = p.Package.Id.ToString();
                    var exists = packages.Where(x => x.Id == p.Package.Id && new Version(x.Version.ToString()) > new Version(p.Version.ToString())).FirstOrDefault();
                    if (exists != null)
                    {
                        if (new Version(exists.Version.ToString()) > new Version(p.Version.ToString()))
                        {
                            p.canUpgrade = true;
                            p.LatestVersion = exists.Version.ToString();
                        }

                    }
                }
                foreach (var p in packages)
                {
                    if (p.Id.ToLower().Contains("openrpa.interfaces") || p.Id.ToLower().Contains("openrpa.namedpipewrapper")) continue;
                    this.Dispatcher.Invoke(() =>
                    {
                        m = Packages.Where(x => x.Package.Id == p.Id).FirstOrDefault();
                        if (m == null)
                        {
                            m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false, Version = p.Version };
                            m.LatestVersion = p.Version.ToString();
                            Packages.Add(m);
                        }
                        var exists = packageManager.LocalRepository.FindPackage(p.Id);
                        m.isDownloaded = (exists != null);
                        if (m.isDownloaded && !m.isInstalled) m.isInstalled = isPackageInstalled(p);
                    });
                }
                foreach (var p in packageManager.LocalRepository.GetPackages())
                {
                    // if (p.Id.ToLower().Contains("openrpa.interfaces") || p.Id.ToLower().Contains("openrpa.namedpipewrapper")) continue;

                    this.Dispatcher.Invoke(() =>
                    {
                        m = Packages.Where(x => x.Package.Id == p.Id).FirstOrDefault();
                        if (m == null)
                        {
                            m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false, Version = p.Version };
                            m.LatestVersion = p.Version.ToString();
                            Packages.Add(m);
                        }
                    });

                    m.isDownloaded = true;
                    if (!m.isInstalled) m.isInstalled = isPackageInstalled(p);
                }
                this.Dispatcher.Invoke(() =>
                {
                    ButtonUpdateAll.IsEnabled = false;
                });
                //if (!System.IO.Directory.Exists(RepositoryPath) && !System.IO.Directory.Exists(InstallPath))
                if (!System.IO.Directory.Exists(InstallPath) || !System.IO.File.Exists(InstallPath + @"\OpenRPA.exe"))
                {
                    var dialogResult = MessageBox.Show("Install OpenRPA and most common packages?", "First run", MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            listPackages.IsEnabled = false;
                            listPackages.SelectedValue = null;
                        });
                        try
                        {
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA").FirstOrDefault());
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.IE").FirstOrDefault());
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.NM").FirstOrDefault());
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Java").FirstOrDefault());
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Forms").FirstOrDefault());
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Script").FirstOrDefault());
                            if (IsOfficeInstalled())
                            {
                                await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Office").FirstOrDefault());
                            }
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.AviRecorder").FirstOrDefault());
                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.FileWatcher").FirstOrDefault());
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                        this.Dispatcher.Invoke(() =>
                        {
                            listPackages.IsEnabled = true;
                            ButtonLaunch(null, null);
                        });
                    }
                } else if(FirstRun) {
                    FirstRun = false;
                    int UpgradeCount = Packages.Where(x => x.canUpgrade).Count();
                    bool hasUpgrades = (UpgradeCount > 0);
                    if (hasUpgrades)
                    {
                        var dialogResult = MessageBox.Show(UpgradeCount + " packages has updates, update all ?", "Upgrades available", MessageBoxButton.YesNo);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            ButtonUpdateAllClick(null, null);
                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                ButtonUpdateAll.IsEnabled = true;
                            });

                        }
                    }

                }
            });
        }
        private bool isPackageInstalled(IPackage Package)
        {
            var PackageInstallPath = RepositoryPath + @"\" + Package.Id;
            List<IPackageAssemblyReference> assemblyReferences = GetCompatibleItems(TargetFramework, Package.AssemblyReferences).ToList();
            foreach (var f in assemblyReferences)
            {
                var source = System.IO.Path.Combine(PackageInstallPath, f.Path);
                var filename = System.IO.Path.GetFileName(source);
                var target = System.IO.Path.Combine(InstallPath, filename);
                if (!System.IO.File.Exists(target))
                {
                    return false;
                }
            }
            return true;
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
                    KillOpenRPA();
                    System.Threading.Thread.Sleep(1000);
                }
                System.IO.File.Copy(source, target, true);
            }
        }
        private void PackageInstalled(object sender, PackageOperationEventArgs eventArgs)
        {
            List<IPackageAssemblyReference> assemblyReferences = GetCompatibleItems(TargetFramework, eventArgs.Package.AssemblyReferences).ToList();
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
        public void PackageUninstalled(object sender, PackageOperationEventArgs eventArgs)
        {
            var fileRoot = System.IO.Path.GetDirectoryName(System.IO.Path.Combine(eventArgs.InstallPath, eventArgs.Package.AssemblyReferences.First().Path));
            System.Diagnostics.Debug.WriteLine(fileRoot);
            foreach (var f in eventArgs.Package.GetLibFiles())
            {
                var source = System.IO.Path.Combine(eventArgs.InstallPath, f.Path);
                var filename = System.IO.Path.GetFileName(source);
                var target = System.IO.Path.Combine(InstallPath, filename);
                if (System.IO.File.Exists(target))
                {
                    System.Diagnostics.Debug.WriteLine("deleteing " + filename);
                    bool hasError = false;
                    try
                    {
                        System.IO.File.Delete(target);
                    }
                    catch (Exception)
                    {
                        hasError = true;
                    }
                    if (hasError)
                    {
                        KillOpenRPA();
                        try
                        {
                            System.IO.File.Delete(target);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            if (!eventArgs.Package.Id.ToLower().Contains("openrpa."))
            {
                this.Dispatcher.Invoke(() =>
                {
                    foreach (var p in Packages.Where(x => x.Package.Id == eventArgs.Package.Id).ToList()) Packages.Remove(p);
                });
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
        public void KillOpenRPA()
        {
            Run("", "taskkill /f /fi \"pid gt 0\" /im OpenRPA.JavaBridge.exe");
            Run("", "taskkill /f /fi \"pid gt 0\" /im OpenRPA.exe");
            Run("", "taskkill /f /fi \"pid gt 0\" /im OpenRPA.NativeMessagingHost.exe");
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
        public System.Collections.ObjectModel.ObservableCollection<PackageModel> Packages { get; } = new System.Collections.ObjectModel.ObservableCollection<PackageModel>();
        void _InstallPackage(string packageId, SemanticVersion version)
        {
#if DEBUG
            var haslocal = repo.FindPackage(packageId, version);
            if (haslocal != null)
            {
                packageManager.InstallPackage(packageId, version: version, ignoreDependencies: true, allowPrereleaseVersions: false);
            }
            else
            {
                PublicpackageManager.InstallPackage(packageId, version: version, ignoreDependencies: true, allowPrereleaseVersions: false);
            }

#else
                    packageManager.InstallPackage(packageId, version: version, ignoreDependencies: true, allowPrereleaseVersions: false);
#endif

        }
        void InstallPackageDependencies(System.Runtime.Versioning.FrameworkName TargetFramework, IPackage Package)
        {
            if (Package == null) throw new ArgumentNullException("Package", "Package cannot be null");
            if (Package.Id.ToLower() == "netstandard.library") return;
            // if (Package.Id.ToLower() == "system.net.websockets.client.managed") return;
            if (Package.Id.ToLower() == "system.buffers") return;
            if (Package.Id.ToLower() == "system.reflection.emit") return;

            //if (Package.Id.ToLower() == "system.net.websockets.client.managed")
            //{
            //    var b = true;
            //}
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
                    _InstallPackage(d.Id, d.VersionSpec.MinVersion);
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
        async Task asyncInstallPackageDependencies(System.Runtime.Versioning.FrameworkName TargetFramework, IPackage Package)
        {
            if (Package == null) throw new ArgumentNullException("Package", "Package cannot be null");
            await Task.Factory.StartNew(() =>
            {
                InstallPackageDependencies(TargetFramework, Package);
            });
        }
        void InstallPackage(IPackage Package)
        {
            if (Package == null) throw new ArgumentNullException("Package", "Package cannot be null");
            try
            {
                if (!System.IO.Directory.Exists(InstallPath)) System.IO.Directory.CreateDirectory(InstallPath);
                InstallPackageDependencies(TargetFramework, Package);
                //packageManager.InstallPackage(Package, true, false, true);
                //packageManager.InstallPackage(SelectedValue.Package, false, false, false);

                _InstallPackage(Package.Id, null);
                PackageInstalled(null, new PackageOperationEventArgs(Package, FileSystem, RepositoryPath + @"\" + Package.Id + "." + Package.Version.ToString()));
            }
            catch (Exception ex)
            {
                logger.Log(MessageLevel.Error, ex.ToString());
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                LoadPackages();
            }
        }
        async Task InstallPackageAsync(IPackage Package)
        {
            if (Package == null) return;
            if (Package == null) throw new ArgumentNullException("Package", "Package cannot be null");
            await Task.Factory.StartNew(() => InstallPackage(Package));
        }
        private async void ButtonInstall(object sender, RoutedEventArgs e)
        {
            PackageModel SelectedValue = listPackages.SelectedValue as PackageModel;
            if (SelectedValue == null) return;
            listPackages.SelectedValue = null;
            listPackages.IsEnabled = false;
            await InstallPackageAsync(SelectedValue.Package);
            listPackages.IsEnabled = true;
            listPackages.SelectedValue = SelectedValue;
        }
        private void ButtonUpgrade(object sender, RoutedEventArgs e)
        {
            PackageModel SelectedValue = listPackages.SelectedValue as PackageModel;
            if (SelectedValue == null) return;
            _ = UpgradePackageAsync(SelectedValue);
        }
        private void ButtonUninstall(object sender, RoutedEventArgs e)
        {
            PackageModel SelectedValue = listPackages.SelectedValue as PackageModel;
            if (SelectedValue == null) return;
            var exists = packageManager.LocalRepository.FindPackage(SelectedValue.Package.Id);
            if (exists != null)
            {
                listPackages.SelectedValue = null;
                listPackages.IsEnabled = false;
                Task.Run(() =>
                {
                    try
                    {
                        packageManager.UninstallPackage(exists);
                    }
                    catch (Exception ex)
                    {
                        logger.Log(MessageLevel.Error, ex.ToString());
                        logger.Logs = ex.ToString() + Environment.NewLine + logger.Logs;
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            listPackages.IsEnabled = true;
                            listPackages.SelectedValue = SelectedValue;
                        });
                        LoadPackages();
                    }
                });
            }

        }
        private void ButtonLaunch(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(InstallPath + @"\OpenRPA.exe"))
            {
                Run(InstallPath, InstallPath + @"\OpenRPA.exe");
            }
            else
            {
                logger.Log(MessageLevel.Error, "Failed locating " + InstallPath + @"\OpenRPA.exe");
            }

        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        private void ButtonReinstall(object sender, RoutedEventArgs e)
        {
            ButtonInstall(null, null);
        }
        private async Task UpgradePackageAsync(PackageModel SelectedValue)
        {
            if (SelectedValue == null) return;
            var exists = packageManager.LocalRepository.FindPackage(SelectedValue.Package.Id);
            if (exists != null)
            {
                listPackages.SelectedValue = null;
                listPackages.IsEnabled = false;
                await Task.Run(() =>
                {
                    try
                    {
                        //packageManager.UpdatePackage(exists, true, false);
                        packageManager.UpdatePackage(exists.Id, new SemanticVersion(SelectedValue.LatestVersion), false, false);
                        exists = packageManager.LocalRepository.FindPackage(SelectedValue.Package.Id);
                        SelectedValue.canUpgrade = false;
                        PackageInstalled(null, new PackageOperationEventArgs(exists, FileSystem, RepositoryPath + @"\" + exists.Id + "." + exists.Version.ToString()));
                        InstallPackageDependencies(TargetFramework, exists);
                    }
                    catch (Exception ex)
                    {
                        logger.Log(MessageLevel.Error, ex.ToString());
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            listPackages.IsEnabled = true;
                            listPackages.SelectedValue = SelectedValue;
                        });
                        LoadPackages();
                    }
                });
            }
        }
        private async void ButtonUpdateAllClick(object sender, RoutedEventArgs e)
        {
            await Dispatcher.Invoke(async () =>
            {
                foreach (var p in Packages.ToList())
                {
                    if(p.canUpgrade)
                    {
                        await UpgradePackageAsync(p);
                    }
                }

                listPackages.IsEnabled = true;
                ButtonLaunch(null, null);
            });
        }
    }



}
