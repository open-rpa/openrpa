using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
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

namespace OpenRPA.FileWatcher.Views
{
    public partial class FileWatcherView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public FileWatcherView(FileWatcherDetectorPlugin plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            DataContext = this;
        }
        private FileWatcherDetectorPlugin plugin;
        public IDetector Entity
        {
            get
            {
                return plugin.Entity;
            }
        }
        public string EntityName
        {
            get
            {
                if (Entity == null) return string.Empty;
                return Entity.name;
            }
            set
            {
                Entity.name = value;
                Entity.isDirty = true;
                NotifyPropertyChanged("Entity");
            }
        }
        public string EntityPath
        {
            get
            {
                return plugin.Watchpath;
            }
            set
            {
                plugin.Watchpath = value;
                Entity.isDirty = true;
                plugin.Stop();
                plugin.Start();
                NotifyPropertyChanged("Entity");
            }
        }
        public string EntityFilter
        {
            get
            {
                return plugin.WatchFilter;
            }
            set
            {
                plugin.WatchFilter = value;
                Entity.isDirty = true;
                plugin.Stop();
                plugin.Start();
                NotifyPropertyChanged("Entity");
            }
        }
        public bool IncludeSubdirectories
        {
            get
            {
                return plugin.IncludeSubdirectories;
            }
            set
            {
                plugin.IncludeSubdirectories = value;
                Entity.isDirty = true;
                plugin.Stop();
                plugin.Start();
                NotifyPropertyChanged("Entity");
            }
        }        
        private void Open_Selector_Click(object sender, RoutedEventArgs e)
        {
        }
        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
        }
        private void Select_Click(object sender, RoutedEventArgs e)
        {
        }
        private void StartRecordPlugins()
        {
        }
        private void StopRecordPlugins()
        {
        }
        public void OnUserAction(Interfaces.IRecordPlugin sender, Interfaces.IRecordEvent e)
        {
        }
    }
}
