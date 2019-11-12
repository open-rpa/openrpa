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

namespace OpenRPA.IE
{
    public partial class OpenURLDesigner
    {
        public OpenURLDesigner()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var browser = Browser.GetBrowser();
            if (browser == null) return;
            ModelItem.Properties["Url"].SetValue(new InArgument<string>(browser.wBrowser.LocationURL));
        }
    }
}