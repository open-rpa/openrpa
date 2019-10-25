using NuGet;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Updater
{
    public class PackageModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public IPackageSearchMetadata Package { get; set; }
        public NuGet.Protocol.LocalPackageInfo LocalPackage { get; set; }
        public bool canUpgrade { get; set; }
        public NuGetVersion Version
        {
            get
            {
                return Package.Identity.Version;
            }
        }
        public NuGetVersion InstalledVersion
        {
            get
            {
                if (LocalPackage == null) return null;
                return LocalPackage.Identity.Version;
            }
        }
        public string Name
        {
            get
            {
                if (Package == null) return "unknown";
                if (!string.IsNullOrEmpty(Package.Title)) return Package.Title;
                return Package.Identity.Id;
            }
            set
            {

            }
        }
        public string Image
        {
            get
            {
                if (canUpgrade) return "Resources/download.png";
                if (isInstalled) return "Resources/check.png";
                if (isDownloaded) return "Resources/Package.png";
                return "Resources/CloudDownload.png"; 
                //return "Resources/circle.png";
            }
            set
            {

            }
        }
        public bool _isDownloaded = false;
        public bool isDownloaded
        {
            get
            {

                return _isDownloaded;
            }
            set
            {
                _isDownloaded = value;
                NotifyPropertyChanged("isDownloaded");
                NotifyPropertyChanged("IsNotDownloaded");
                NotifyPropertyChanged("Image");
            }
        }
        public bool IsNotDownloaded
        {
            get
            {

                return !_isDownloaded;
            }
        }
        public bool _isInstalled = false;
        public bool isInstalled
        {
            get
            {

                return _isInstalled;
            }
            set
            {
                _isInstalled = value;
                NotifyPropertyChanged("isInstalled");
                NotifyPropertyChanged("Image");
            }
        }
        public string InstalledVersionString
        {
            get
            {
                if (LocalPackage == null) return "";
                return LocalPackage.Identity.Version.ToString();
            }
        }
        public string LatestVersion
        {
            get
            {
                return Version.ToString();
            }
        }
        public override string ToString()
        {
            if (Package == null) return "unknown";
            if (!string.IsNullOrEmpty(Package.Title)) return Package.Title;
            return Package.Identity.Id;
        }

    }
}
