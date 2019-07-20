using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenRPA.Activities
{
    public partial class RemovePermissionDesigner : INotifyPropertyChanged
    {
        public RemovePermissionDesigner()
        {
            InitializeComponent();
            TestItems = new List<string>();
            TestItems.Add("Allan");
            TestItems.Add("Bettina");
            TestItems.Add("Ziva");
            TestItems.Add("Kristian");
            TestItems.Add("Mette");

        }
        public List<string> TestItems { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoCompleteBox acb = (AutoCompleteBox)sender;

            // In these sample scenarios, the Tag is the name of the content 
            // presenter to use to display the value.
            //string contentPresenterName = (string)acb.Tag;
            //ContentPresenter cp = FindName(contentPresenterName) as ContentPresenter;
            //if (cp != null)
            //{
            //    cp.Content = acb.SelectedItem;
            //}
            //JObject item = (JObject)acb.SelectedItem;
            //if(item!=null) acb.Text = item.Value<string>("name");
            //e.Handled = true;

            //txtname.Text = "";
            var item = (apiuser)acb.SelectedItem;
            if (item == null) return;
            ModelItem.Properties["EntityId"].SetValue(new System.Activities.InArgument<string>(item._id));
            ModelItem.Properties["Name"].SetValue(new System.Activities.InArgument<string>(item.name));
            //txtname.Text = item.name;
        }


        private apiuser[] usergroups;
        private async void OnPopulatingAsynchronous(object sender, PopulatingEventArgs e)
        {
            AutoCompleteBox source = (AutoCompleteBox)sender;

            // Cancel the populating value: this will allow us to call 
            // PopulateComplete as necessary.
            e.Cancel = true;
            try
            {
                usergroups = await global.webSocketClient.Query<apiuser>("users", @"{ name: {$regex: '" + source.Text.Replace("\\", "\\\\") + "', $options: 'i'} }");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }

            TestItems.Clear();
            if (usergroups == null) return;
            // Use the dispatcher to simulate an asynchronous callback when 
            // data becomes available
            _ = Dispatcher.BeginInvoke(
                new System.Action(delegate ()
                {
                    //source.ItemsSource = new string[]
                    //{
                    //    e.Parameter + "1",
                    //    e.Parameter + "2",
                    //    e.Parameter + "3",
                    //};
                    //source.ItemsSource = TestItems.ToArray();
                    source.ItemsSource = usergroups;

                    // Population is complete
                    source.PopulateComplete();
                }));
        }



        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            search.Text = ModelItem.GetValue<string>("Name");
            //NotifyPropertyChanged("Name");
        }
    }
}