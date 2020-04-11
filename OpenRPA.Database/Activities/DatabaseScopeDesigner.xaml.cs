using Microsoft.VisualBasic.Activities;
using OpenRPA.Interfaces;
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

namespace OpenRPA.Database
{
    public partial class DatabaseScopeDesigner : INotifyPropertyChanged
    {
        public DatabaseScopeDesigner()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void Open_Selector(object sender, RoutedEventArgs e)
        {
            string connectionString = ModelItem.GetValue<string>("ConnectionString");
            connectionString = GetConnectionString(connectionString);
            if(!string.IsNullOrEmpty(connectionString))
            {
                ModelItem.Properties["ConnectionString"].SetValue(new InArgument<string>() { Expression = new Literal<string>(connectionString) });
                NotifyPropertyChanged("ConnectionString");
            }
        }
        public static string GetConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) return GetConnectionString();
            try
            {
                // C:\Program Files\Common Files\System\Ole DB\OLEDB32.DLL
                MSDASC.DataLinks dataLinks = new MSDASC.DataLinks();

                // C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\PublicAssemblies\adodb.dll
                ADODB._Connection connection;

                connection = new ADODB.Connection();
                connection.ConnectionString = connectionString;

                object oConnection = connection;

                if (dataLinks.PromptEdit(ref oConnection))
                {
                    return connection.ConnectionString;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
        public static string GetConnectionString()
        {
            try
            {
                dynamic _con = null;
                MSDASC.DataLinks _link = new MSDASC.DataLinks();
                _con = _link.PromptNew();
                if (_con != null)
                {
                    // sourceConnectionString = ((ADODB.Connection)_con).ConnectionString;
                    return _con.ConnectionString;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }
    }
}