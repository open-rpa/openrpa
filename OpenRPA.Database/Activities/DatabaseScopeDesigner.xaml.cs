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
using Microsoft.Data.ConnectionUI;

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
            string dataProvider = ModelItem.GetValue<string>("DataProvider");
            string dataSource = ModelItem.GetValue<string>("DataSource");
            string connectionString = ModelItem.GetValue<string>("ConnectionString");
            var config = GetConnectionString(dataProvider, dataSource, connectionString);
            if(config != null && !string.IsNullOrEmpty(config.ConnectionString))
            {
                ModelItem.Properties["DataProvider"].SetValue(new InArgument<string>() { Expression = new Literal<string>(config.DataProvider) });
                ModelItem.Properties["DataSource"].SetValue(new InArgument<string>() { Expression = new Literal<string>(config.DataSource) });
                ModelItem.Properties["ConnectionString"].SetValue(new InArgument<string>() { Expression = new Literal<string>(config.ConnectionString) });
                NotifyPropertyChanged("DataProvider");
                NotifyPropertyChanged("DataSource");
                NotifyPropertyChanged("ConnectionString");
            }
        }
        static Connection GetConnectionString(string DataProvider, string DataSource, string ConnectionString)
        {
            return GetConnectionString(new Connection(DataProvider, DataSource, ConnectionString));
        }
        static Connection GetConnectionString(Connection connection)
        {
            if (connection == null || string.IsNullOrEmpty(connection.ConnectionString)) return GetConnectionString();

            var dlg = new DataConnectionDialog();
            DataSource.AddStandardDataSources(dlg);
            DataSource dataSource = dlg.DataSources.Where(x => x.Name == connection.DataSource).FirstOrDefault();
            dlg.SelectedDataSource = dataSource;
            // dlg.SetSelectedDataProvider(dataSource, null);
            try
            {
                dlg.ConnectionString = connection.ConnectionString;
            }
            catch (Exception)
            {
            }

            if (DataConnectionDialog.Show(dlg) == System.Windows.Forms.DialogResult.OK)
            {
                var result = new Connection(dlg.SelectedDataProvider.Name, dlg.SelectedDataSource.Name, dlg.ConnectionString);
                return result;
            }
            return null;
        }
        static Connection GetConnectionString()
        {
            var dlg = new DataConnectionDialog();
            DataSource.AddStandardDataSources(dlg);
            if (DataConnectionDialog.Show(dlg) == System.Windows.Forms.DialogResult.OK)
            {
                var result = new Connection(dlg.SelectedDataProvider.Name, dlg.SelectedDataSource.Name, dlg.ConnectionString);
                return result;
            }
            return null;
        }

        //public static string GetConnectionString(string connectionString)
        //{
        //    if (string.IsNullOrEmpty(connectionString)) return GetConnectionString();
        //    try
        //    {
        //        // C:\Program Files\Common Files\System\Ole DB\OLEDB32.DLL
        //        MSDASC.DataLinks dataLinks = new MSDASC.DataLinks();

        //        // C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\PublicAssemblies\adodb.dll
        //        ADODB._Connection connection;

        //        connection = new ADODB.Connection();
        //        connection.ConnectionString = connectionString;

        //        object oConnection = connection;

        //        if (dataLinks.PromptEdit(ref oConnection))
        //        {
        //            return connection.ConnectionString;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    return null;
        //}
        //public static string GetConnectionString()
        //{
        //    try
        //    {
        //        dynamic _con = null;
        //        MSDASC.DataLinks _link = new MSDASC.DataLinks();
        //        _con = _link.PromptNew();
        //        if (_con != null)
        //        {
        //            // sourceConnectionString = ((ADODB.Connection)_con).ConnectionString;
        //            return _con.ConnectionString;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //    return null;
        //}
    }
}