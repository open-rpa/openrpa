using OpenRPA.Interfaces;
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
using System.Windows.Shapes;

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for InsertText.xaml
    /// </summary>
    public partial class InsertSelect : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class pitem
            {
            public string Name { get; set; }
            public IElement Item { get; set; }
        }
        private IElement item;
        private pitem[] _items;
        public InsertSelect(IElement item)
        {
            this.item = item;
            var temp = new List<pitem>();
            foreach (var i in item.Items) temp.Add(new pitem { Item = i, Name = i.Name });
            _items = temp.ToArray();
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(Window_Loaded));
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            DataContext = this;
            Activate();
            Focus();
            Topmost = true;
            Topmost = false;
            Focus();
            // textbox.Focus();
            Topmost = true;

        }
        public IElement SelectedItem { get; set; }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            var window = e.Source as Window;
            System.Threading.Thread.Sleep(100);
            window.Dispatcher.Invoke(
            new Action(() =>
            {
                search.Focus();
                search.ItemsSource = _items;
                search.PopulateComplete();
            }));
            okButton.IsEnabled = false;
            NotifyPropertyChanged("Items");
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                Close();
            }
        }
        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
            Topmost = true;
            Topmost = false;
            Focus();
            // textbox.Focus();
        }
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoCompleteBox acb = (AutoCompleteBox)sender;
            var p = (pitem)acb.SelectedItem;
            if (p != null) SelectedItem = p.Item;
            if (SelectedItem == null) return;
            search.Text = SelectedItem.Name;
            okButton.IsEnabled = true;

        }
        private void OnPopulatingAsynchronous(object sender, PopulatingEventArgs e)
        {
            AutoCompleteBox source = (AutoCompleteBox)sender;
            e.Cancel = true;
            _ = Dispatcher.BeginInvoke(
                new System.Action(delegate ()
                {
                    if(!string.IsNullOrEmpty(source.Text))
                    {
                        source.ItemsSource = _items.Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.ToLower().Contains(source.Text.ToLower())).ToArray();
                    } else
                    {
                        source.ItemsSource = _items;

                    }
                    source.PopulateComplete();
                }));
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            var p = (pitem)search.SelectedItem;
            if (p != null) SelectedItem = p.Item;
            if (SelectedItem == null) return;
            DialogResult = true;
            Close();
        }
    }
}
