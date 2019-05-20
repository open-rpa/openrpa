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

namespace OpenRPA.NM
{
    public partial class OpenURLDesigner : INotifyPropertyChanged
    {
        public OpenURLDesigner()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var browser = Browser.GetBrowser();
            //if (browser == null) return;
            //ModelItem.Properties["Url"].SetValue(new InArgument<string>(browser.wBrowser.LocationURL));

        }
    }
}