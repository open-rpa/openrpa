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
        public string RepositoryPath;
        public string InstallPath;
        public bool FirstRun = true;
        readonly System.Runtime.Versioning.FrameworkName TargetFramework;
        public MainWindow()
        {
            InitializeComponent();
            ButtonUpdateAll.IsEnabled = false;
            DataContext = this;
            // https://api.nuget.org/v3/index.json
            // https://www.nuget.org/api/v2/
            // https://packages.nuget.org/api/v2
            // https://nuget.pkg.github.com/open-rpa/index.json
#if DEBUG
            // repo = PackageRepositoryFactory.Default.CreateRepository(@"C:\code\OpenRPA\packages");
            // repo = PackageRepositoryFactory.Default.CreateRepository(@"C:\code\OpenRPA\packages");
#else
#endif
            OpenRPAPackageManagerLogger.Instance.Updated += () =>
            {
                Logs = OpenRPAPackageManagerLogger.Instance.Logs;
            };
            RepositoryPath = Environment.CurrentDirectory + @"\Packages";
            InstallPath = Environment.CurrentDirectory + @"\OpenRPA";
            // TargetFramework = new System.Runtime.Versioning.FrameworkName(".NETFramework, Version=4.0");
            TargetFramework = new System.Runtime.Versioning.FrameworkName(".NETFramework", new Version("4.6.2"));
            // TargetFramework20 = new System.Runtime.Versioning.FrameworkName(".NETStandard", new Version("2.0"));

            LoadPackages();
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
            Task.Run(async () =>
            {

                var packages = await OpenRPAPackageManager.Instance.Search("OpenRPA");
                foreach (var p in packages)
                {
                    PackageModel m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false, Version = p.Identity.Version };
                    m.LocalPackage = OpenRPAPackageManager.Instance.getLocal(p.Identity.Id);
                    if(m.LocalPackage != null) { 
                        m.InstalledVersion = m.LocalPackage.Identity.Version;
                        m.isDownloaded = true;
                    }

                }



                //                // IPackageRepository localRepository = PackageRepositoryFactory.Default.CreateRepository(RepositoryPath);
                //                PackageModel m = null;
                //                foreach (var p in packageManager.LocalRepository.GetPackages())
                //                {
                //                    if (p.Id.ToLower() == "openrpa" || p.Id.ToLower().StartsWith("openrpa."))
                //                    {
                //                        this.Dispatcher.Invoke(() =>
                //                        {
                //                            m = Packages.Where(x => x.Package.Id == p.Id).FirstOrDefault();
                //                            if (m == null)
                //                            {
                //                                m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false, Version = p.Version };
                //                                m.LatestVersion = p.Version.ToString();
                //                                Packages.Add(m);
                //                            }
                //                            else
                //                            {
                //                                if (new Version(p.Version.ToString()) > new Version(m.Version.ToString())) { m.Version = p.Version; m.LatestVersion = p.Version.ToString(); m.Package = p; }
                //                            }
                //                        });
                //                        m.isDownloaded = true;
                //                        if (!m.isInstalled) m.isInstalled = IsPackageInstalled(p);
                //                    }
                //                }
                //                foreach (var p in Packages)
                //                {
                //                    var id = p.Package.Id.ToString();
                //                    var exists = packages.Where(x => x.Id == p.Package.Id && new Version(x.Version.ToString()) > new Version(p.Version.ToString())).FirstOrDefault();
                //                    if (exists != null)
                //                    {
                //                        if (new Version(exists.Version.ToString()) > new Version(p.Version.ToString()))
                //                        {
                //                            p.canUpgrade = true;
                //                            p.LatestVersion = exists.Version.ToString();
                //                        }

                //                    }
                //                }
                //                foreach (var p in packages)
                //                {
                //                    if (p.Id.ToLower().Contains("openrpa.interfaces") || p.Id.ToLower().Contains("openrpa.namedpipewrapper")) continue;
                //                    this.Dispatcher.Invoke(() =>
                //                    {
                //                        m = Packages.Where(x => x.Package.Id == p.Id).FirstOrDefault();
                //                        if (m == null)
                //                        {
                //                            m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false, Version = p.Version };
                //                            m.LatestVersion = p.Version.ToString();
                //                            Packages.Add(m);
                //                        }
                //                        var exists = packageManager.LocalRepository.FindPackage(p.Id);
                //                        m.isDownloaded = (exists != null);
                //                        if (m.isDownloaded && !m.isInstalled) m.isInstalled = IsPackageInstalled(p);
                //                    });
                //                }
                //                foreach (var p in packageManager.LocalRepository.GetPackages())
                //                {
                //                    // if (p.Id.ToLower().Contains("openrpa.interfaces") || p.Id.ToLower().Contains("openrpa.namedpipewrapper")) continue;

                //                    this.Dispatcher.Invoke(() =>
                //                    {
                //                        m = Packages.Where(x => x.Package.Id == p.Id).FirstOrDefault();
                //                        if (m == null)
                //                        {
                //                            m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false, Version = p.Version };
                //                            m.LatestVersion = p.Version.ToString();
                //                            Packages.Add(m);
                //                        }
                //                    });

                //                    m.isDownloaded = true;
                //                    if (!m.isInstalled) m.isInstalled = IsPackageInstalled(p);
                //                }
                //                this.Dispatcher.Invoke(() =>
                //                {
                //                    ButtonUpdateAll.IsEnabled = false;
                //                });
                //                //if (!System.IO.Directory.Exists(RepositoryPath) && !System.IO.Directory.Exists(InstallPath))
                //                if (!System.IO.Directory.Exists(InstallPath) || !System.IO.File.Exists(InstallPath + @"\OpenRPA.exe"))
                //                {
                //                    FirstRun = false;
                //                    var dialogResult = MessageBox.Show("Install OpenRPA and most common packages?", "First run", MessageBoxButton.YesNo);
                //                    if (dialogResult == MessageBoxResult.Yes)
                //                    {
                //                        this.Dispatcher.Invoke(() =>
                //                        {
                //                            listPackages.IsEnabled = false;
                //                            listPackages.SelectedValue = null;
                //                        });
                //                        try
                //                        {
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA").FirstOrDefault());
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.IE").FirstOrDefault());
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.NM").FirstOrDefault());
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Java").FirstOrDefault());
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Forms").FirstOrDefault());
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Script").FirstOrDefault());
                //                            if (IsOfficeInstalled())
                //                            {
                //                                await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.Office").FirstOrDefault());
                //                            }
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.AviRecorder").FirstOrDefault());
                //                            await InstallPackageAsync(packages.Where(x => x.Id == "OpenRPA.FileWatcher").FirstOrDefault());
                //                        }
                //                        catch (Exception ex)
                //                        {
                //                            MessageBox.Show(ex.ToString());
                //                        }
                //                        this.Dispatcher.Invoke(() =>
                //                        {
                //                            listPackages.IsEnabled = true;
                //                            ButtonLaunch(null, null);
                //                        });
                //                    }
                //                } else if(FirstRun) {
                //                    FirstRun = false;
                //                    int UpgradeCount = Packages.Where(x => x.canUpgrade).Count();
                //                    bool hasUpgrades = (UpgradeCount > 0);
                //                    if (hasUpgrades)
                //                    {
                //                        var dialogResult = MessageBox.Show(UpgradeCount + " packages has updates, update all ?", "Upgrades available", MessageBoxButton.YesNo);
                //                        if (dialogResult == MessageBoxResult.Yes)
                //                        {
                //                            ButtonUpdateAllClick(null, null);
                //                        }
                //                        else
                //                        {
                //                            this.Dispatcher.Invoke(() =>
                //                            {
                //                                ButtonUpdateAll.IsEnabled = true;
                //                            });

                //                        }
                //                    }

                //                }
            });
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

        public void Run(string WorkingDirectory, string command)
        {
            using (System.Diagnostics.Process p = new System.Diagnostics.Process())
            {
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.Arguments = "/c \"" + command + "\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.WorkingDirectory = WorkingDirectory;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }
        }
        public void KillOpenRPA()
        {
            Run("", "taskkill /f /fi \"pid gt 0\" /im OpenRPA.JavaBridge.exe");
            Run("", "taskkill /f /fi \"pid gt 0\" /im OpenRPA.exe");
            Run("", "taskkill /f /fi \"pid gt 0\" /im OpenRPA.NativeMessagingHost.exe");
        }
        public System.Collections.ObjectModel.ObservableCollection<PackageModel> Packages { get; } = new System.Collections.ObjectModel.ObservableCollection<PackageModel>();
        private async void ButtonInstall(object sender, RoutedEventArgs e)
        {
            PackageModel SelectedValue = listPackages.SelectedValue as PackageModel;
            if (SelectedValue == null) return;
            listPackages.SelectedValue = null;
            listPackages.IsEnabled = false;
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
            //var exists = packageManager.LocalRepository.FindPackage(SelectedValue.Package.Id);
            //if (exists != null)
            //{
            //    listPackages.SelectedValue = null;
            //    listPackages.IsEnabled = false;
            //    Task.Run(() =>
            //    {
            //        try
            //        {
            //            packageManager.UninstallPackage(exists);
            //        }
            //        catch (Exception ex)
            //        {
            //            logger.Log(MessageLevel.Error, ex.ToString());
            //            logger.Logs = ex.ToString() + Environment.NewLine + logger.Logs;
            //            System.Diagnostics.Debug.WriteLine(ex.ToString());
            //        }
            //        finally
            //        {
            //            this.Dispatcher.Invoke(() =>
            //            {
            //                listPackages.IsEnabled = true;
            //                listPackages.SelectedValue = SelectedValue;
            //            });
            //            LoadPackages();
            //        }
            //    });
            //}

        }
        private void ButtonLaunch(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(InstallPath + @"\OpenRPA.exe"))
            {
                Run(InstallPath, InstallPath + @"\OpenRPA.exe");
            }
            else
            {
                OpenRPAPackageManagerLogger.Instance.Log(NuGet.Common.LogLevel.Error, "Failed locating " + InstallPath + @"\OpenRPA.exe");
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
            //if (SelectedValue == null) return;
            //var exists = packageManager.LocalRepository.FindPackage(SelectedValue.Package.Id);
            //if (exists != null)
            //{
            //    listPackages.SelectedValue = null;
            //    listPackages.IsEnabled = false;
            //    await Task.Run(() =>
            //    {
            //        try
            //        {
            //            //packageManager.UpdatePackage(exists, true, false);
            //            packageManager.UpdatePackage(exists.Id, new SemanticVersion(SelectedValue.LatestVersion), false, false);
            //            exists = packageManager.LocalRepository.FindPackage(SelectedValue.Package.Id);
            //            SelectedValue.canUpgrade = false;
            //            PackageInstalled(null, new PackageOperationEventArgs(exists, FileSystem, RepositoryPath + @"\" + exists.Id + "." + exists.Version.ToString()));
            //            InstallPackageDependencies(TargetFramework, exists);
            //        }
            //        catch (Exception ex)
            //        {
            //            OpenRPAPackageManagerLogger.Instance.Log(NuGet.Common.LogLevel.Error, ex.ToString());
            //            System.Diagnostics.Debug.WriteLine(ex.ToString());
            //        }
            //        finally
            //        {
            //            this.Dispatcher.Invoke(() =>
            //            {
            //                listPackages.IsEnabled = true;
            //                listPackages.SelectedValue = SelectedValue;
            //            });
            //            LoadPackages();
            //        }
            //    });
            //}
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
