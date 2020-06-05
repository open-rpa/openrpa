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
            if(NMHook.connected)
            {
                NMHook.enumwindowandtabs();
                if (NMHook.chromeconnected)
                {
                    var tab = NMHook.CurrentChromeTab;
                    if (tab != null)
                    {
                        ModelItem.Properties["Browser"].SetValue(new InArgument<string>("chrome"));
                        ModelItem.Properties["Url"].SetValue(new InArgument<string>(tab.url));
                    }
                }
                if (NMHook.ffconnected)
                {
                    var tab = NMHook.CurrentFFTab;
                    if (tab != null)
                    {
                        ModelItem.Properties["Browser"].SetValue(new InArgument<string>("ff"));
                        ModelItem.Properties["Url"].SetValue(new InArgument<string>(tab.url));
                    }
                }
                if (NMHook.edgeconnected)
                {
                    var tab = NMHook.CurrentEdgeTab;
                    if (tab != null)
                    {
                        ModelItem.Properties["Browser"].SetValue(new InArgument<string>("edge"));
                        ModelItem.Properties["Url"].SetValue(new InArgument<string>(tab.url));
                    }
                }
            }

        }
    }
}