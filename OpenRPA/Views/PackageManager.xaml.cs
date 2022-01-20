using NuGet.Packaging.Core;
using NuGet.Versioning;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for OpenProject.xaml
    /// </summary>
    public partial class PackageManager : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            GenericTools.RunUI(() =>
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            });            
        }
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; } = new DelegateCommand((e) => { }, (e) => false);
        public bool NeedsReload { get; set; }
        private bool _CanClose = true;
        public bool CanClose { get { return _CanClose; } set { _CanClose = value; NotifyPropertyChanged("CanClose"); } }
        private bool _CanHide = true;
        public bool CanHide { get { return _CanHide; } set { _CanHide = value; NotifyPropertyChanged("CanHide"); } }
        private bool _IsBusy = true;
        public bool IsBusy { get { return _IsBusy; } set { _IsBusy = value; NotifyPropertyChanged("IsBusy"); } }
        private string _BusyContent = "Loading NuGet feeds and packages ...";
        public string BusyContent { get { return _BusyContent; } set { _BusyContent = value; NotifyPropertyChanged("BusyContent"); } }
        private bool _IncludePrerelease = false;
        public bool IncludePrerelease { get { return _IncludePrerelease; } set { _IncludePrerelease = value; NotifyPropertyChanged("IncludePrerelease"); FilterText = FilterText; } }
        private Project project;
        public PackageManager(Project project) : base()
        {
            this.project = project;
            DataContext = this;
            InitializeComponent();
            NuGetPackageManager.Instance.Initialize(this);
            this.Title = "NuGet Package Manager for " + project.name;
        }
        public PackageSourceWrapper SelectedPackageSource { get; set; }
        private PackageSearchItem _SelectedPackageItem;
        public PackageSearchItem SelectedPackageItem { get {
                return _SelectedPackageItem;
            }
            set
            {
                if(value!=null) _ = value.LoadVersions(IncludePrerelease);
                _SelectedPackageItem = value;
                NotifyPropertyChanged("SelectedPackageItem");
                NotifyPropertyChanged("IsPackageItemSelected");
                // ((NuGet.Protocol.Core.Types.IPackageSearchMetadata)listPackageSourceItems.SelectedValue).Identity;
            }
        }
        public bool IsPackageItemSelected
        {
            get
            {
                return (SelectedPackageItem != null);
            }
            set
            {

            }
        }
        private System.Collections.ObjectModel.ObservableCollection<PackageSourceWrapper> _PackageSources;
        public System.Collections.ObjectModel.ObservableCollection<PackageSourceWrapper> PackageSources
        {
            get
            {
                return _PackageSources;
            }
            set
            {
                if (_PackageSources == value)
                {
                    return;
                }
                _PackageSources = value;
                NotifyPropertyChanged("PackageSources");
            }
        }
        private System.Collections.ObjectModel.ObservableCollection<PackageSearchItem> _PackageSourceItems;
        public System.Collections.ObjectModel.ObservableCollection<PackageSearchItem> PackageSourceItems
        {
            get
            {
                return _PackageSourceItems;
            }
            set
            {
                if (_PackageSourceItems == value)
                {
                    return;
                }
                _PackageSourceItems = value;
                NotifyPropertyChanged("PackageSourceItems");
            }
        }
        private string _FilterText = "";
        public string FilterText
        {
            get
            {
                return _FilterText;
            }
            set
            {
                _FilterText = value;
                if (SelectedPackageSource == null) { IsBusy = false; return; }
                _ = SelectedPackageSource.Search(project, this, IncludePrerelease, _FilterText);
                NotifyPropertyChanged("FilterText");
            }
        }
        //private async Task UpdateFilterText(string value)
        //{
        //    _FilterText = value;
        //    NotifyPropertyChanged("FilterText");
        //    if (SelectedPackageSource == null) { IsBusy = false; return; }
        //    async Task<bool> UserKeepsTyping()
        //    {
        //        string txt = value;   // remember text
        //        await Task.Delay(500);        // wait some
        //        return txt != value;  // return that text chaged or not
        //    }
        //    if (await UserKeepsTyping()) return;
        //    SelectedPackageSource.Search(project, this, IncludePrerelease, _FilterText);
        //}
        private void treePackageSources_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedPackageSource = treePackageSources.SelectedItem as PackageSourceWrapper;
            if (SelectedPackageSource.source == null)
            {
                SelectedPackageSource.ClearCache();
            }

            FilterText = "";
        }
        public ICommand InstallCommand { get { return new RelayCommand<object>(OnInstall, CanInstall); } }
        public ICommand UninstallCommand { get { return new RelayCommand<object>(OnUninstall, CanInstall); } }
        internal bool CanInstall(object _item)
        {
            return true;
        }
        internal void OnInstall(object _item)
        {
            IsBusy = true;
            Task.Run(async () =>
            {
                try
                {
                    if(!string.IsNullOrEmpty(SelectedPackageItem.InstalledVersion))
                    {
                        var _minver = VersionRange.Parse(SelectedPackageItem.InstalledVersion);
                        var _identity = new PackageIdentity(SelectedPackageItem.Id, _minver.MinVersion);

                        // per project or joined ?
                        // string TargetFolder = System.IO.Path.Combine(project.Path, "extensions");
                        string TargetFolder = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "extensions");

                        BusyContent = "Uninstalling " + _identity.ToString();
                        NuGetPackageManager.Instance.UninstallPackage(TargetFolder, _identity);
                        SelectedPackageItem.IsInstalled = false;

                    }
                    BusyContent = "Initializing";
                    var minver = VersionRange.Parse(SelectedPackageItem.SelectedVersion);
                    var identity = new PackageIdentity(SelectedPackageItem.Id, minver.MinVersion);
                    if (SelectedPackageItem.RequireLicenseAcceptance)
                    {
                        // Request accept
                    }
                    if (project.dependencies == null) project.dependencies = new Dictionary<string, string>();
                    var keys = project.dependencies.Keys.ToList();
                    foreach (var key in keys)
                    {
                        if (key.ToLower() == identity.Id.ToLower()) project.dependencies.Remove(key);
                    }
                    project.dependencies.Add(identity.Id, minver.MinVersion.ToString());
                    BusyContent = "Saving current project settings";
                    await project.Save<Project>(true);
                    BusyContent = "Installing NuGet Packages";
                    await project.InstallDependencies(false);
                    NeedsReload = true;
                    //BusyContent = "Reloading Activities Toolbox";
                    //GenericTools.RunUI(()=> WFToolbox.Instance.InitializeActivitiesToolbox());
                    SelectedPackageItem.InstalledVersion = SelectedPackageItem.SelectedVersion;
                    SelectedPackageItem.IsInstalled = true;
                    IsBusy = false;
                    if (SelectedPackageSource.source == null)
                    {
                        SelectedPackageSource.ClearCache();
                        GenericTools.RunUI(() =>
                        {
                            FilterText = FilterText;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }

            });
            
        }
        internal void OnUninstall(object _item)
        {
            IsBusy = true;
            Task.Run(async () =>
            {
                try
                {
                    BusyContent = "Initializing";
                    var minver = VersionRange.Parse(SelectedPackageItem.InstalledVersion);
                    var identity = new PackageIdentity(SelectedPackageItem.Id, minver.MinVersion);

                    if (project.dependencies == null) project.dependencies = new Dictionary<string, string>();
                    var keys = project.dependencies.Keys.ToList();
                    foreach (var key in keys)
                    {
                        if(key.ToLower() == identity.Id.ToLower()) project.dependencies.Remove(key);
                    }                    
                    // per project or joined ?
                    // string TargetFolder = System.IO.Path.Combine(project.Path, "extensions");
                    string TargetFolder = System.IO.Path.Combine(Interfaces.Extensions.ProjectsDirectory, "extensions");

                    BusyContent = "Uninstalling package";
                    NuGetPackageManager.Instance.UninstallPackage(TargetFolder, identity);
                    SelectedPackageItem.IsInstalled = false;
                    BusyContent = "Saving current project settings";
                    await project.Save<Project>(true);
                    //BusyContent = "Updating NuGet Packages";
                    //await project.InstallDependencies();
                    //BusyContent = "Reloading Activities Toolbox";
                    //GenericTools.RunUI(() => WFToolbox.Instance.InitializeActivitiesToolbox());
                    SelectedPackageItem.InstalledVersion = "";
                    SelectedPackageItem.IsInstalled = false;
                    if(NuGetPackageManager.PendingDeletion.Count > 0)
                    {
                        Config.local.files_pending_deletion = NuGetPackageManager.PendingDeletion.ToArray();
                        Config.Save();
                        Log.Output("Please restart the robot for the change to take fully effect");
                        Log.Warning("package files will be deleted next time you start the robot");
                        // MessageBox.Show("Please restart the robot for the change to take fully effect");
                    }
                    if(SelectedPackageSource.source == null) { 
                        GenericTools.RunUI(() =>
                        {
                            SelectedPackageSource.ClearCache();
                            FilterText = FilterText;
                        });                        
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                IsBusy = false;

            });

        }

    }
}
