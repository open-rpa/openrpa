using OpenRPA.Selenium.Activities;
using System;
using System.Activities;
using System.Activities.Presentation.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace OpenRPA.Selenium
{
    public partial class SelectElementDesigner : INotifyPropertyChanged
    {
        public SelectElementDesigner()
        {
            InitializeComponent();
            var elementActions = new ElementActions();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
       
        public bool ShowLoopExpanded { get; set; }
      
        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            Activity loopaction = ModelItem.GetValue<Activity>("LoopAction");
            if (loopaction != null)
            {
                ShowLoopExpanded = true;
                NotifyPropertyChanged("ShowLoopExpanded");
            }
        }
       
        public ObservableCollection<string> Options
        {
            get
            {
                return new ObservableCollection<string> {
                    "Text",
                    "Value",
                    "Index",
                };
            }
        }
    }
}