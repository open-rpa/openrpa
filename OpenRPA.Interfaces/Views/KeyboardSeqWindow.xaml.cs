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

namespace OpenRPA.Interfaces.Views
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
        internal List<vKey> _keys;
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
            InputDriver.Instance.OnKeyDown += _OnKeyDown;
            InputDriver.Instance.OnKeyUp += _OnKeyUp;
        }
        private void _OnKeyDown(Input.InputEventArgs e)
        {
            AddKey(new vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, false));
            NotifyPropertyChanged("Text");
        }
        private void _OnKeyUp(Input.InputEventArgs e)
        {
            AddKey(new vKey((FlaUI.Core.WindowsAPI.VirtualKeyShort)e.Key, true));
            NotifyPropertyChanged("Text");
        }

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
        private void Window_Closed(object sender, EventArgs e)
        {
            //rpaactivities.Generichook.enablecancel = true;
            InputDriver.Instance.OnKeyDown -= _OnKeyDown;
            InputDriver.Instance.OnKeyUp -= _OnKeyUp;
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
        public void AddKey(vKey _key)
        {
            if (_keys == null) _keys = new List<vKey>();
            _keys.Add(_key);
            Text = "";
            for (var i = 0; i < _keys.Count; i++)
            {
                string val = "";
                var key = _keys[i];
                if (key.up == false && (i + 1) < _keys.Count)
                {
                    if (key.KeyCode == _keys[i + 1].KeyCode && _keys[i + 1].up)
                    {
                        i++;
                        val = "{" + key.KeyCode.ToString() + "}";
                        if (key.KeyCode.ToString().StartsWith("KEY_"))
                        {
                            val = key.KeyCode.ToString().Substring(4).ToLower();
                        }
                        if (key.KeyCode == FlaUI.Core.WindowsAPI.VirtualKeyShort.SPACE)
                        {
                            val = " ";
                        }
                    }
                }
                if (string.IsNullOrEmpty(val))
                {
                    if (key.up == false)
                    {
                        val = "{" + key.KeyCode.ToString() + " down}";
                    }
                    else
                    {
                        val = "{" + key.KeyCode.ToString() + " up}";
                    }

                }
                Text += val;
            }
            //Text = result;
            if (Text == null) Text = "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
