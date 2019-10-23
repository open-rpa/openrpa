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
        public NuGetVersion Version { get; set; }
        public NuGetVersion InstalledVersion { get; set; }
        public string Name
        {
            get
            {
                return "unknown";
                //if (Package == null) return "unknown";
                //if (!string.IsNullOrEmpty(Package.Title)) return Package.Title;
                //return Package.Id;
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
        //public string _LatestVersion = "";
        //public string LatestVersion
        //{
        //    get
        //    {

        //        return _LatestVersion;
        //    }
        //    set
        //    {
        //        _LatestVersion = value;
        //        NotifyPropertyChanged("LatestVersion");
        //    }
        //}
        public string LatestVersion
        {
            get
            {
                return Version.ToString();
            }
        }
        public override string ToString()
        {
            return "unknown";
            //if (Package == null) return "unknown";
            //if (!string.IsNullOrEmpty(Package.Title)) return Package.Title;
            //return Package.Id;
        }

    }
}
