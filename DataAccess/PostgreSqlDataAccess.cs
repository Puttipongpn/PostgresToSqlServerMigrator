using System;
using System.Data;
using Npgsql;

namespace SyncDb.DataAccess
{
    public class PostgreSqlDataAccess
    {
        private readonly string _connectionString;
        public NpgsqlConnection Connection { get; private set; }
        public string? ConnectionString { get; internal set; }

        public PostgreSqlDataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void OpenConnection()
        {
            Connection = new NpgsqlConnection(_connectionString);
            Connection.Open();
        }

        public void CloseConnection()
        {
            Connection.Close();
        }

        public DataSet GetData(string query)
        {
            var ds = new DataSet();
            using (var cmd = new NpgsqlCommand(query, Connection))
            {
                var da = new NpgsqlDataAdapter(cmd);
                da.Fill(ds);
            }
            return ds;
        }
    }
}
