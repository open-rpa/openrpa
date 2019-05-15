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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        
        public string fqdn
        {
            get { return txtFQDN.Text; }
            set { txtFQDN.Text = value; }
        }
        public string username
        {
            get { return TxtUsername.Text; }
            set { TxtUsername.Text = value; }
        }
        public string password
        {
            get { return txtPassword.Password; }
            set { txtPassword.Password = value; }
        }
        public string errormessage
        {
            get { return txtErrormessage.Text; }
            set { txtErrormessage.Text = value; }
        }
        
        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void buttonRegister_Click(object sender, RoutedEventArgs e)
        {
            var registration = new RegistrationWindow();
            registration.ShowDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtUsername.Text)) TxtUsername.Focus();
            if (!string.IsNullOrEmpty(TxtUsername.Text)) txtPassword.Focus();
        }

        private void TxtUsername_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter) { e.Handled = true; txtPassword.Focus(); }
        }

        private void TxtPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { e.Handled = true; OKButton_Click(null,null); }
        }
    }
}
