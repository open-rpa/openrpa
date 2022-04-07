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
            if (config != null && !string.IsNullOrEmpty(config.ConnectionString))
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
    }
}