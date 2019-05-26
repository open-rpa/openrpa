using OpenRPA.Input;
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
using System.Windows.Shapes;

namespace OpenRPA.Views
{
    /// <summary>
    /// Interaction logic for InsertText.xaml
    /// </summary>
    public partial class KeyboardSeqWindow : Window, System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public string Text { get; set; }
        public KeyboardSeqWindow()
        {
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(Window_Loaded));
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            DataContext = this;
            Activate();
            Focus();
            Topmost = true;
            Topmost = false;
            Focus();
            textbox.Focus();
            Topmost = true;
            typeText = new Activities.TypeText();
        }
        private Activities.TypeText typeText = null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //rpaactivities.Generichook.enablecancel = false;
            var window = e.Source as Window;
            System.Threading.Thread.Sleep(100);
            window.Dispatcher.Invoke(
            new Action(() =>
            {
                //window.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                textbox.Focus();
            }));
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            typeText.AddKey(new Activities.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, false), null);
            Text = typeText.result;
            NotifyPropertyChanged("Text");
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            typeText.AddKey(new Activities.vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, true), null);
            Text = typeText.result;
            NotifyPropertyChanged("Text");
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            //rpaactivities.Generichook.enablecancel = true;
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
            Topmost = true;
            Topmost = false;
            Focus();
            textbox.Focus();
        }
    }
}
