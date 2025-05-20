using System;
using System.Data;
using System.Data.SqlClient;

namespace SyncDb.DataAccess
{
    public class SqlServerDataAccess
    {
        private readonly string _connectionString;
        public SqlConnection Connection { get; private set; }

        public SqlServerDataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void OpenConnection()
        {
            Connection = new SqlConnection(_connectionString);
            Connection.Open();
        }

        public void CloseConnection()
        {
            Connection.Close();
        }

        public void ExecuteQuery(string query)
        {
            using (var cmd = new SqlCommand(query, Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
