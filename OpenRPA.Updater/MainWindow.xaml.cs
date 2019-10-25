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
        // readonly System.Runtime.Versioning.FrameworkName TargetFramework;
        public MainWindow()
        {
            InitializeComponent();
            ButtonUpdateAll.IsEnabled = false;
            DataContext = this;
            OpenRPAPackageManagerLogger.Instance.Updated += () =>
            {
                Logs = OpenRPAPackageManagerLogger.Instance.Logs;
            };
            RepositoryPath = Environment.CurrentDirectory + @"\Packages";
            InstallPath = Environment.CurrentDirectory + @"\OpenRPA";
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
            var result = new List<PackageModel>();
            foreach (var m in Packages) result.Add(m);
            Task.Run(async () =>
            {
                var packagesearch = await OpenRPAPackageManager.Instance.Search("OpenRPA");
                foreach (var p in packagesearch)
                {
                    var exists = result.Where(x => x.Package.Identity.Id == p.Identity.Id).FirstOrDefault();
                    if (exists == null)
                    {
                        PackageModel m = new PackageModel() { Package = p, canUpgrade = false, isDownloaded = false };
                        m.LocalPackage = OpenRPAPackageManager.Instance.getLocal(p.Identity.Id);
                        result.Add(m);
                    } else
                    {
                        exists.LocalPackage = OpenRPAPackageManager.Instance.getLocal(p.Identity.Id);
                    }
                }
                foreach(var m in result)
                    if (m.LocalPackage != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            m.isDownloaded = true;
                            m.isInstalled = OpenRPAPackageManager.Instance.IsPackageInstalled(m.LocalPackage);
                            m.canUpgrade = m.Version > m.LocalPackage.Identity.Version;
                        });
                    }
                Dispatcher.Invoke(() =>
                {
                    foreach (var p in result)
                    {
                        var exists = Packages.Where(x => x.Package.Identity.Id == p.Package.Identity.Id).FirstOrDefault();
                        if (exists == null) Packages.Add(p);
                        if (exists != null)
                        {
                            p.NotifyPropertyChanged("Image");
                            p.NotifyPropertyChanged("Name");
                            p.NotifyPropertyChanged("IsNotDownloaded");
                            p.NotifyPropertyChanged("isDownloaded");
                            p.NotifyPropertyChanged("canUpgrade");
                            p.NotifyPropertyChanged("isDownloaded");
                            p.NotifyPropertyChanged("Name");
                            p.NotifyPropertyChanged("InstalledVersionString");
                            p.NotifyPropertyChanged("LatestVersion");
                            p.NotifyPropertyChanged("LatestVersion");
                        }
                    }
                    ButtonUpdateAll.IsEnabled = result.Where(x => x.canUpgrade == true).Count() > 0;
                });

                if (!System.IO.Directory.Exists(InstallPath) || !System.IO.File.Exists(InstallPath + @"\OpenRPA.exe"))
                {
                    FirstRun = false;
                    var dialogResult = MessageBox.Show("Install OpenRPA and most common packages?", "First run", MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA").First().Package.Identity);
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA.IE").First().Package.Identity);
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA.NM").First().Package.Identity);
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA.Forms").First().Package.Identity);
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA.Script").First().Package.Identity);
                        if (IsOfficeInstalled())
                        {
                            await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA.Office").First().Package.Identity);
                        }
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA.AviRecorder").First().Package.Identity);
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(result.Where(x => x.Package.Identity.Id == "OpenRPA.FileWatcher").First().Package.Identity);
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
        public System.Collections.ObjectModel.ObservableCollection<PackageModel> Packages { get; } = new System.Collections.ObjectModel.ObservableCollection<PackageModel>();
        private async void ButtonInstall(object sender, RoutedEventArgs e)
        {
            PackageModel SelectedValue = listPackages.SelectedValue as PackageModel;
            if (SelectedValue == null) return;
            BtnInstall.IsEnabled = false;
            BtnReinstall.IsEnabled = false;
            BtnUpgrade.IsEnabled = false;
            BtnUninstall.IsEnabled = false;

            listPackages.SelectedValue = null;
            listPackages.IsEnabled = false;
            listPackages.IsEnabled = true;
            listPackages.SelectedValue = SelectedValue;
            await OpenRPAPackageManager.Instance.DownloadAndInstall(SelectedValue.Package.Identity);
            LoadPackages();
            BtnInstall.IsEnabled = SelectedValue.IsNotDownloaded;
            BtnReinstall.IsEnabled = SelectedValue.isDownloaded;
            BtnUpgrade.IsEnabled = SelectedValue.canUpgrade;
            BtnUninstall.IsEnabled = SelectedValue.isDownloaded;
        }
        private void ButtonUpgrade(object sender, RoutedEventArgs e)
        {
            //PackageModel SelectedValue = listPackages.SelectedValue as PackageModel;
            //if (SelectedValue == null) return;
            //_ = UpgradePackageAsync(SelectedValue);
            ButtonInstall(null, null);
        }
        private void ButtonUninstall(object sender, RoutedEventArgs e)
        {
            PackageModel SelectedValue = listPackages.SelectedValue as PackageModel;
            if (SelectedValue == null) return;
            BtnInstall.IsEnabled = false;
            BtnReinstall.IsEnabled = false;
            BtnUpgrade.IsEnabled = false;
            BtnUninstall.IsEnabled = false;

            listPackages.SelectedValue = null;
            listPackages.IsEnabled = false;
            listPackages.IsEnabled = true;
            listPackages.SelectedValue = SelectedValue;
            OpenRPAPackageManager.Instance.UninstallPackage(SelectedValue.Package.Identity);
            LoadPackages();
            BtnInstall.IsEnabled = SelectedValue.IsNotDownloaded;
            BtnReinstall.IsEnabled = SelectedValue.isDownloaded;
            BtnUpgrade.IsEnabled = SelectedValue.canUpgrade;
            BtnUninstall.IsEnabled = SelectedValue.isDownloaded;


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
        private async void ButtonUpdateAllClick(object sender, RoutedEventArgs e)
        {
            await Dispatcher.Invoke(async () =>
            {
                foreach (var p in Packages.ToList())
                {
                    if (p.canUpgrade)
                    {
                        await OpenRPAPackageManager.Instance.DownloadAndInstall(p.Package.Identity);
                        // await UpgradePackageAsync(p);
                    }
                }
                listPackages.IsEnabled = true;
                ButtonLaunch(null, null);
                LoadPackages();
            });
        }
    }



}
