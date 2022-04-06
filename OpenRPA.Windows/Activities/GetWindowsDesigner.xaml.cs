using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Windows
{
    public partial class GetWindowsDesigner : INotifyPropertyChanged
    {
        public GetWindowsDesigner()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public bool ShowLoopExpanded { get; set; }
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ShowLoopExpanded = !ShowLoopExpanded;
            NotifyPropertyChanged("ShowLoopExpanded");
        }
        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            Activity loopaction = ModelItem.GetValue<Activity>("LoopAction");
            if (loopaction != null)
            {
                ShowLoopExpanded = true;
                NotifyPropertyChanged("ShowLoopExpanded");
            }
        }
    }
}