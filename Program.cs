using System;
using Microsoft.Extensions.Configuration;
using SyncDb.DataAccess;
using SyncDb.Services;

namespace SyncDb
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🗄️ Select database to sync:");
            Console.WriteLine("1. ERM");
            Console.WriteLine("2. OCC");
            Console.Write("Enter number (1 or 2): ");

            var inputDB = Console.ReadLine();
            string? selectedDb = inputDB == "1" ? "ERM" : inputDB == "2" ? "OCC" : null;
            string tableSection = selectedDb == "ERM" ? "TablesERM" : "TablesOCC";

            Console.WriteLine("🧹 Do you want to drop and recreate the tables?");
            Console.WriteLine("1. Yes");
            Console.WriteLine("2. No");
            Console.Write("Enter number (1 or 2): ");
            var inputEnsureTableExists = Console.ReadLine();
            int ensureFlag = inputEnsureTableExists == "1" ? 1 : 0;

            if (selectedDb == null)
            {
                Console.WriteLine("❌ Invalid selection. Exiting...");
                return;
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("json/erm_table.json", optional: true)
                .AddJsonFile("json/occ_table.json", optional: true)
                .Build();

            string? sourceConnectionString = configuration.GetConnectionString($"SourceConnectionString{selectedDb}");
            string? destConnectionString = configuration.GetConnectionString($"DestConnectionString{selectedDb}");

            if (string.IsNullOrEmpty(sourceConnectionString))
            {
                Console.WriteLine("❌ Source connection string is null or empty. Exiting...");
                return;
            }
            if (string.IsNullOrEmpty(destConnectionString))
            {
                Console.WriteLine("❌ Destination connection string is null or empty. Exiting...");
                return;
            }
            var postgreSqlDataAccess = new PostgreSqlDataAccess(sourceConnectionString);
            var sqlServerDataAccess = new SqlServerDataAccess(destConnectionString);

            TestConnectionAndLog(postgreSqlDataAccess, sqlServerDataAccess);

            var dataMigrator = new DataMigrator(postgreSqlDataAccess, sqlServerDataAccess, configuration, tableSection, ensureFlag);
            dataMigrator.MigrateData();
        }

        static void TestConnectionAndLog(PostgreSqlDataAccess postgreSqlDataAccess, SqlServerDataAccess sqlServerDataAccess)
        {
            try
            {
                postgreSqlDataAccess.OpenConnection();
                Console.WriteLine("✅ Connected to PostgreSQL!");

                sqlServerDataAccess.OpenConnection();
                Console.WriteLine("✅ Connected to SQL Server!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error connecting to databases: {ex.Message}");
            }
            finally
            {
                postgreSqlDataAccess.CloseConnection();
                sqlServerDataAccess.CloseConnection();
            }
        }
    }
}
