using OpenRPA.Interfaces;
using OpenRPA.Interfaces.entity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for PermissionsWindow.xaml
    /// </summary>
    public partial class PermissionsWindow : Window
    {
        public PermissionsWindow()
        {
            InitializeComponent();
        }

        public wfPermissionsModel vm;
        public PermissionsWindow(apibase item)
        {
            Owner = App.Current.MainWindow;
            InitializeComponent();
            vm = new wfPermissionsModel(this, item);
            reload();
            DataContext = vm;
        }
        private void reload()
        {
            vm.Source.Clear();
            foreach (var ace in vm.item._acl)
            {
                vm.Source.Add(ace);
            }
        }
        private void listDetectors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
        private void cmdNew(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            try
            {
                var view = new selectUserWindow();
                // Hide();
                view.ShowDialog();

                var acl = vm.item._acl.ToList();
                acl.Add(new ace()
                {
                    _id = view.result._id,
                    name = view.result.name,
                    deny = false,
                    Delete = true,
                    Read = true,
                    Invoke = true,
                    Update = true
                });
                vm.item._acl = acl.ToArray();
                reload();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                MessageBox.Show("CmdTest: " + ex.Message);
            }
            finally
            {
                // Show();
            }



        }
        private void cmdDelete(object sender, ExecutedRoutedEventArgs e)
        {
            if (vm.SelectedItem == null) return;
            var acl = vm.item._acl.ToList();
            foreach (var ace in acl.ToList())
            {
                if (ace._id == vm.SelectedItem._id) acl.Remove(ace);
            }
            vm.item._acl = acl.ToArray();
            reload();
        }

    }

    public class BitConverter : DependencyObject, IValueConverter
    {
        public static DependencyProperty SourceValueProperty =
       DependencyProperty.Register("SourceValue",
                                   typeof(ace_right),
                                   typeof(BitConverter));

        public ace_right SourceValue
        {
            get { return (ace_right)GetValue(SourceValueProperty); }
            set { SetValue(SourceValueProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var a = value as ace;
            var t = parameter as string;
            if (a == null || string.IsNullOrEmpty(t)) return false;

            ace_right bit;
            Enum.TryParse(t, true, out bit);
            return a.getBit((decimal)bit);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object sourceValue = SourceValue;
            return null;
        }
    }

    public class wfPermissionsModel
    {
        public PermissionsWindow window;
        public ObservableCollection<ace> Source { get; set; }

        public ace SelectedItem { get; set; }
        public apibase item { get; set; }

        public wfPermissionsModel(PermissionsWindow window, apibase item)
        {
            this.window = window;
            this.item = item;
            Source = new ObservableCollection<ace>();
        }
    }
    
}
