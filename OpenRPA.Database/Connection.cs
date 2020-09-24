using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Database
{
    public class Connection : IDisposable
    {
        public string DataSource { get; set; }
        public string DataProvider { get; set; }
        public string ConnectionString { get; set; }
        private DbConnection conn;
        private DbProviderFactory factory;
        public Connection(string DataProvider, string DataSource, string ConnectionString)
        {
            this.DataProvider = DataProvider;
            this.DataSource = DataSource;
            this.ConnectionString = ConnectionString;
            if (string.IsNullOrEmpty(DataProvider)) DataProvider = "System.Data.OleDb";
            factory = DbProviderFactories.GetFactory(DataProvider);
            conn = factory.CreateConnection();
            conn.ConnectionString = ConnectionString;
        }
        public void Close()
        {
            if(conn!=null)
            {
                try
                {
                    conn.Close();
                }
                catch (Exception)
                {
                }
                conn.Dispose();
            }
            conn = null;
        }
        public void Open()
        {
            conn.Open();
        }
        public int ExecuteNonQuery(string Query, CommandType commandType)
        {
            using (var cmd = factory.CreateCommand())
            {
                cmd.CommandText = Query;
                cmd.Connection = conn;
                cmd.CommandType = commandType;
                // if (Params != null) cmd.Parameters.AddRange(Params);
                return cmd.ExecuteNonQuery();
            }
        }
        internal DataTable ExecuteQuery(string Query, CommandType commandType)
        {
            DataTable result = new DataTable();
            using (var cmd = factory.CreateCommand())
            {
                cmd.CommandText = Query;
                cmd.Connection = conn;
                cmd.CommandType = commandType;
                // if (Params != null) cmd.Parameters.AddRange(Params);
                var adapter = factory.CreateDataAdapter();
                adapter.SelectCommand = cmd;
                adapter.Fill(result);
            }
            return result;
        }
        public object ExecuteScalar(string Query, CommandType commandType)
        {
            using (var cmd = factory.CreateCommand())
            {
                cmd.CommandText = Query;
                cmd.Connection = conn;
                cmd.CommandType = commandType;
                // if (Params != null) cmd.Parameters.AddRange(Params);
                return cmd.ExecuteScalar();
            }
        }
        internal int UpdateDataTable(string tablename, DataTable datatable)
        {
            using (var cmd = factory.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM " + tablename;
                cmd.Connection = conn;
                using (var adapter = factory.CreateDataAdapter())
                {
                    adapter.SelectCommand = cmd;
                    using (var builder = factory.CreateCommandBuilder())
                    {
                        builder.DataAdapter = adapter;
                        //builder.QuotePrefix = " [";
                        //builder.QuoteSuffix = "] ";
                        adapter.DeleteCommand = builder.GetDeleteCommand(true);
                        adapter.UpdateCommand = builder.GetUpdateCommand(true);
                        adapter.InsertCommand = builder.GetInsertCommand(true);
                        return adapter.Update(datatable);
                    }
                }
            }
        }

        bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            Close();
            disposed = true;
        }
    }
}
