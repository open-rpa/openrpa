using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Database
{
    public class Connection : IDisposable
    {
        private string ConnectionString;
        private OleDbConnection conn;
        public Connection(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
            conn = new OleDbConnection(this.ConnectionString);
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
        public int ExecuteNonQuery(string Query, System.Data.CommandType commandType)
        {
            using (OleDbCommand cmd = new OleDbCommand(Query, conn))
            {
                cmd.CommandType = commandType;
                // if (Params != null) cmd.Parameters.AddRange(Params);
                return cmd.ExecuteNonQuery();
            }
        }
        internal DataTable ExecuteQuery(string Query, CommandType commandType)
        {
            DataTable result = new DataTable();
            using (OleDbCommand cmd = new OleDbCommand(Query, conn))
            {
                cmd.CommandType = commandType;
                // if (Params != null) cmd.Parameters.AddRange(Params);
                OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
                adapter.Fill(result);
            }
            return result;
        }
        public object ExecuteScalar(string Query, System.Data.CommandType commandType)
        {
            using (OleDbCommand cmd = new OleDbCommand(Query, conn))
            {
                cmd.CommandType = commandType;
                // if (Params != null) cmd.Parameters.AddRange(Params);
                return cmd.ExecuteScalar();
            }
        }
        internal int UpdateDataTable(string tablename, DataTable datatable)
        {
            using (OleDbCommand cmd = new OleDbCommand("SELECT * FROM [" + tablename + "]", conn))
            {
                using (OleDbDataAdapter oledbDataAdapter = new OleDbDataAdapter(cmd))
                {
                    using (OleDbCommandBuilder oledbCommandBuilder = new OleDbCommandBuilder(oledbDataAdapter))
                    {
                        oledbCommandBuilder.QuotePrefix = " [";
                        oledbCommandBuilder.QuoteSuffix = "] ";
                        oledbDataAdapter.DeleteCommand = oledbCommandBuilder.GetDeleteCommand(true);
                        oledbDataAdapter.UpdateCommand = oledbCommandBuilder.GetUpdateCommand(true);
                        oledbDataAdapter.InsertCommand = oledbCommandBuilder.GetInsertCommand(true);
                        return oledbDataAdapter.Update(datatable);
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
