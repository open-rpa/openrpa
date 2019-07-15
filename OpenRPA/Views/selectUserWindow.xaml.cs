using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
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

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for selectUserWindow.xaml
    /// </summary>
    public partial class selectUserWindow : Window
    {
        public selectUserWindow()
        {
            InitializeComponent();
            okButton.IsEnabled = false;
        }



        public apiuser result { get; set; }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoCompleteBox acb = (AutoCompleteBox)sender;
            result = (apiuser)acb.SelectedItem;
            if (result == null) return;
            okButton.IsEnabled = true;
        }
        private apiuser[] usergroups;
        private async void OnPopulatingAsynchronous(object sender, PopulatingEventArgs e)
        {
            AutoCompleteBox source = (AutoCompleteBox)sender;

            // Cancel the populating value: this will allow us to call 
            // PopulateComplete as necessary.
            e.Cancel = true;

            usergroups = await global.webSocketClient.Query<apiuser>("users", @"{ name: {$regex: '" + source.Text.Replace("\\", "\\\\") + "', $options: 'i'} }");


            //TestItems.Clear();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            search.Focus();
        }
    }
}
