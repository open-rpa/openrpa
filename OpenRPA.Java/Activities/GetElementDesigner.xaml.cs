using Microsoft.VisualBasic.Activities;
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

namespace OpenRPA.Java
{
    public partial class GetElementDesigner : INotifyPropertyChanged
    {
        public GetElementDesigner()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            string SelectorString = ModelItem.GetValue<string>("Selector");

            var selector = new JavaSelector(SelectorString);
            var selectors = new Interfaces.Selector.SelectorWindow("Java", selector);
            selectors.ShowDialog();
        }
    }
}