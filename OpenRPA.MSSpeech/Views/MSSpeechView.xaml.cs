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

namespace OpenRPA.MSSpeech.Views
{
    public partial class MSSpeechView : UserControl, INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public MSSpeechView(MSSpeechPlugin plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
            DataContext = this;
        }
        private MSSpeechPlugin plugin;
        public Detector Entity
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
                NotifyPropertyChanged("Entity");
            }
        }
        public string Commands
        {
            get
            {
                return plugin.Commands;
            }
            set
            {
                plugin.Commands = value;
                plugin.Stop();
                plugin.Start();
                NotifyPropertyChanged("Commands");
            }
        }
        // 
        public bool IncludeCommonWords
        {
            get
            {
                return plugin.IncludeCommonWords;
            }
            set
            {
                plugin.IncludeCommonWords = value;
                plugin.Stop();
                plugin.Start();
                NotifyPropertyChanged("Entity");
            }
        }

    }
}
