using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using SyncDb.DataAccess;
using System.Data.SqlTypes;


namespace SyncDb.Services
{
    public class DataMigrator
    {
        private readonly PostgreSqlDataAccess _postgreSqlDataAccess;
        private readonly SqlServerDataAccess _sqlServerDataAccess;
        private readonly IConfiguration _configuration;
        private const int BatchSize = 10000; // จำนวนแถวต่อชุดที่ต้องการอ่านจาก PostgreSQL

        private readonly string _selectedDb;

        private readonly int _ensureTableExists;

        public DataMigrator(PostgreSqlDataAccess postgreSqlDataAccess, SqlServerDataAccess sqlServerDataAccess, IConfiguration configuration, string tableSection, int ensureTableExists)
        {
            _postgreSqlDataAccess = postgreSqlDataAccess;
            _sqlServerDataAccess = sqlServerDataAccess;
            _configuration = configuration;
            _selectedDb =  tableSection;
            _ensureTableExists = ensureTableExists;
        }

        public void MigrateData()
        {   
            var tableNames = _configuration.GetSection(_selectedDb).Get<List<string>>();

            if (tableNames == null)
            {
                Console.WriteLine("No tables found to sync.");
                return;
            }

            foreach (var tableName in tableNames)
                try
                {
                    Console.WriteLine($"Sync {tableName} Start!");
                    // Open connections
                    _postgreSqlDataAccess.OpenConnection();
                    _sqlServerDataAccess.OpenConnection();
                    string selectQuery = $"SELECT * FROM public.\"{tableName}\"";
                    string truncateQuery = $"TRUNCATE TABLE [dbo].[{tableName}]";
                    // Truncate destination target table
                    if (_ensureTableExists == 1)
                    {
                        string schemaOnlyQuery = $"SELECT * FROM public.\"{tableName}\" LIMIT 0";
                        EnsureTableExists(tableName, _postgreSqlDataAccess.GetData(schemaOnlyQuery).Tables[0]); 
                    }
                    // EnsureTableExists(tableName, _postgreSqlDataAccess.GetData(selectQuery).Tables[0]); //สำหรับการลบ table ก่อน แล้วให้สร้างมาใหม่ เพื่ออัพเดทตารางให้ปัจจุบันที่สุด
                    _sqlServerDataAccess.ExecuteQuery(truncateQuery);
                    int offset = 0;
                    int totalRowsCopied = 0;
                    bool hasMoreData = true;

                    while (hasMoreData)
                    {
                        // Read data in batches from PostgreSQL
                        string batchQuery = $"{selectQuery} LIMIT {BatchSize} OFFSET {offset}";
                        var data = _postgreSqlDataAccess.GetData(batchQuery);
                        // Convert data types
                        var convertedData = ConvertTypes(data);

                        // Migrate data to destination
                        if (convertedData != null && convertedData.Tables.Count > 0 && convertedData.Tables[0].Rows.Count > 0)
                        {
                            using (var bulkCopy = new SqlBulkCopy(_sqlServerDataAccess.Connection))
                            {
                                bulkCopy.BulkCopyTimeout = 30000;
                                bulkCopy.DestinationTableName = $"[dbo].[{tableName}]";
                                bulkCopy.BatchSize = BatchSize;

                                foreach (DataColumn column in convertedData.Tables[0].Columns)
                                {
                                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                                }

                                bulkCopy.WriteToServer(convertedData.Tables[0]);
                            }

                            offset += BatchSize; // Increase the offset for the next batch
                            totalRowsCopied += convertedData.Tables[0].Rows.Count;
                            Console.WriteLine($"Rows copied: {totalRowsCopied}");
                        }
                        else
                        {
                            hasMoreData = false; // No more data to read
                        }
                    }

                    Console.WriteLine($"Sync {tableName} Success! Total rows copied: {totalRowsCopied}");
                }
                catch (Exception err)
                {
                    Console.WriteLine($"Sync {tableName} failed: {err.Message}");
                }
                finally
                {
                    // Close connections
                    _postgreSqlDataAccess.CloseConnection();
                    _sqlServerDataAccess.CloseConnection();
                }
        }


        // ฟังก์ชัน EnsureTableExists ใช้ create table ก่อน inseart data
        public void EnsureTableExists(string tableName, DataTable dataTable)
        {   // สร้าง query เพื่อลบ table หากมีอยู่
            string dropTableQuery = $"IF OBJECT_ID('[dbo].[{tableName}]', 'U') IS NOT NULL DROP TABLE [dbo].[{tableName}];";
            using (var dropCommand = new SqlCommand(dropTableQuery, _sqlServerDataAccess.Connection))
            {
                // ทำการลบ table หากมีอยู่
                Console.WriteLine($"Dropping table: {tableName}");
                dropCommand.ExecuteNonQuery();
            }
            string checkTableQuery = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}') BEGIN CREATE TABLE [dbo].[{tableName}] (";
            foreach (DataColumn column in dataTable.Columns)
            {
                checkTableQuery += $"{column.ColumnName} {GetSqlDataType(column.DataType)}, ";
            }
            // Console.WriteLine($"Create table query: {checkTableQuery}");
            checkTableQuery = checkTableQuery.TrimEnd(',', ' ') + "); END";
            using (var command = new SqlCommand(checkTableQuery, _sqlServerDataAccess.Connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private string GetSqlDataType(Type dataType)
        {
            if (dataType == typeof(int))
                return "INT";
            if (dataType == typeof(bool))
                return "BIT";
            if (dataType == typeof(DateTime))
                return "DATETIME";
            if (dataType == typeof(string))
                return "NVARCHAR(MAX)";
            if (dataType == typeof(byte[]))
                return "VARBINARY(MAX)";
            return "NVARCHAR(MAX)";
        }


        private DataSet ConvertTypes(DataSet data)
        {
            DataSet convertedData = new DataSet();

            foreach (DataTable table in data.Tables)
            {
                DataTable newTable = new DataTable(table.TableName);

                // Define columns with the correct types
                foreach (DataColumn column in table.Columns)
                {
                    // Console.WriteLine("column.DataType===> " + column.DataType);
                    if (column.DataType == typeof(bool))
                    {
                        newTable.Columns.Add(column.ColumnName, typeof(int)); // Convert bool to int (bit)
                    }
                    else if (column.DataType == typeof(Array))
                    {
                        newTable.Columns.Add(column.ColumnName, typeof(string)); // Convert string[] to string
                    }
                    else if (column.DataType == typeof(DateTime))
                    {
                        newTable.Columns.Add(column.ColumnName, typeof(DateTime)); // Keep DateTime type
                    }
                    else
                    {
                        newTable.Columns.Add(column.ColumnName, column.DataType); // Keep original type
                    }
                }

                // Copy data with necessary conversions
                foreach (DataRow row in table.Rows)
                {
                    DataRow newRow = newTable.NewRow();
                    foreach (DataColumn column in table.Columns)
                    {
                        if (column.DataType == typeof(bool))
                        {
                            newRow[column.ColumnName] = row[column] != DBNull.Value ? ((bool)row[column] ? 1 : 0) : 0;
                            // newRow[column.ColumnName] = (bool)row[column] ? 1 : 0;
                        }
                        else if (column.DataType == typeof(Array) || column.DataType == typeof(string[]))
                        {
                            newRow[column.ColumnName] = row[column] != DBNull.Value ? string.Join(", ", ((Array)row[column]).Cast<object>()) : string.Empty;
                            // newTable.Columns.Add(column.ColumnName, typeof(string)); // Convert string[] to string
                        }
                        else if (column.DataType == typeof(DateTime))
                        {
                            if (row[column] != DBNull.Value)
                            {
                                DateTime dateValue = (DateTime)row[column];
                                if (dateValue < (DateTime)SqlDateTime.MinValue || dateValue > (DateTime)SqlDateTime.MaxValue)
                                {
                                    newRow[column.ColumnName] = DBNull.Value; // Set to DBNull if out of range
                                }
                                else
                                {
                                    newRow[column.ColumnName] = dateValue;
                                }
                            }
                            else
                            {
                                newRow[column.ColumnName] = DBNull.Value;
                            }
                        }
                        else
                        {
                            newRow[column.ColumnName] = row[column] != DBNull.Value ? row[column] : DBNull.Value;
                            // newRow[column.ColumnName] = row[column];
                        }
                    }
                    newTable.Rows.Add(newRow);
                }

                convertedData.Tables.Add(newTable);
            }

            return convertedData;
        }
    }
}
