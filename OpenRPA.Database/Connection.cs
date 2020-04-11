using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Database
{
    public class Connection : IDisposable
    {
        private string ConnectionString;
        private SqlConnection conn;
        public Connection(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
            conn = new SqlConnection(this.ConnectionString);
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
        public int ExecuteNonQuery(string Query)
        {
            using (SqlCommand cmd = new SqlCommand(Query, conn))
            {
                // if (Params != null) cmd.Parameters.AddRange(Params);
                // cmd.CommandType = CommandType.StoredProcedure;
                return cmd.ExecuteNonQuery();
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
