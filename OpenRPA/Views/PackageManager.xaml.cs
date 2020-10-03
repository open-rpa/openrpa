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
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public DelegateCommand DockAsDocumentCommand = new DelegateCommand((e) => { }, (e) => false);
        public DelegateCommand AutoHideCommand { get; set; } = new DelegateCommand((e) => { }, (e) => false);
        public bool CanClose { get; set; } = false;
        public bool CanHide { get; set; } = false;
        private Project project;
        public PackageManager(Project project) : base()
        {
            this.project = project;
            DataContext = this;
            InitializeComponent();
            _ = NuGetPackageManager.Instance.Initialize(this);
        }
        public bool IncludePrerelease { get; set; }
        public NuGet.Configuration.PackageSource SelectedPackageSource { get; set; }
        private NuGet.Protocol.Core.Types.IPackageSearchMetadata _SelectedPackageItem;
        public NuGet.Protocol.Core.Types.IPackageSearchMetadata SelectedPackageItem { get {
                return _SelectedPackageItem;
            }
            set
            {
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
        private System.Collections.ObjectModel.ObservableCollection<NuGet.Configuration.PackageSource> _PackageSources;
        public System.Collections.ObjectModel.ObservableCollection<NuGet.Configuration.PackageSource> PackageSources
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
        private System.Collections.ObjectModel.ObservableCollection<NuGet.Protocol.Core.Types.IPackageSearchMetadata> _PackageSourceItems;
        public System.Collections.ObjectModel.ObservableCollection<NuGet.Protocol.Core.Types.IPackageSearchMetadata> PackageSourceItems
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
                if (SelectedPackageSource == null) return;
                _ = NuGetPackageManager.Instance.Search(SelectedPackageSource, this, IncludePrerelease, _FilterText);
                NotifyPropertyChanged("FilterText");
            }
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _ = NuGetPackageManager.Instance.Initialize(this);
        }
        private void treePackageSources_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedPackageSource = treePackageSources.SelectedItem as NuGet.Configuration.PackageSource;
            FilterText = "";
        }
    }
}
