using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.IO;
using NUnit.Framework;

namespace Frends.Community.SQL.Tests
{
    /*
    docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
    with Git bash add winpty to the start of
    winpty docker exec -it sql1 "bash"
    /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Salakala123!"
        
    Check rows before CleanUp:
    SELECT * FROM TestTable
    GO
    
    Optional queries:
    SELECT Name FROM sys.Databases;
    GO
    SELECT * FROM INFORMATION_SCHEMA.TABLES;
    GO
    */

    [TestFixture]
    class SaveQueryToCSVTests
    {
        private static readonly string _connString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!";
        private static readonly string _tableName = "TestTable";
        private static readonly string _destination = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestData/");

        [SetUp]
        public void Init()
        {
            using (var connection = new SqlConnection(_connString))
            {
                connection.Open();
                var createTable = connection.CreateCommand();
                createTable.CommandText = $@"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN CREATE TABLE {_tableName} ( Id int, LastName varchar(255), FirstName varchar(255), Salary decimal(6,2) ); END";
                createTable.ExecuteNonQuery();
                connection.Close();
            }

            // Create destination file path.
            Directory.CreateDirectory(_destination);
        }

        [TearDown]
        public void CleanUp()
        {
            using (var connection = new SqlConnection(_connString))
            {
                connection.Open();
                var createTable = connection.CreateCommand();
                createTable.CommandText = $@"IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{_tableName}') BEGIN DROP TABLE IF EXISTS {_tableName}; END";
                createTable.ExecuteNonQuery();
                connection.Close();
            }


            // Clean and remove destination directory
            Directory.Delete(_destination, true);
        }

        [Test]
        public async Task SaveQueryToCSV_StringWithApostrophe()
        {
            InsertTestData($"Insert into {_tableName} values (1,'Meikalainen','Matti',1523.25);");

            var query = new SaveQueryToCSVParameters
            {
                Query = $"Select Id, LastName, FirstName, REPLACE(Salary, '.', ',') AS 'Salary' from {_tableName}",
                QueryParameters = new SQLParameter[0],
                ConnectionString = _connString,
                TimeoutSeconds = 30,
                OutputFilePath = Path.Combine(_destination, "test.csv")
            };

            var options = new SaveQueryToCSVOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                LineBreak = CsvLineBreak.CRLF,
                FileEncoding = FileEncoding.UTF8,
                EnableBom = false,
                IncludeHeadersInOutput = true,
                SanitizeColumnHeaders = false,
                AddQuotesToDates = false,
                AddQuotesToStrings = true,
                DateFormat = "yyyy-MM-dd",
                DateTimeFormat = "yyyy-MM-ddTHH:mm:ss"
            };

            await SQL.SaveQueryToCSV(query, options, default);
            var output = File.ReadAllText(Path.Combine(_destination, "test.csv"));

            Assert.AreEqual("Id;LastName;FirstName;Salary\r\n1;\"Meikalainen\";\"Matti\";\"1523,25\"\r\n", output);
        }

        [Test]
        public async Task SaveQueryToCSV_StringWithoutApostrophe()
        {
            InsertTestData($"Insert into {_tableName} values (1,'Meikalainen','Matti',1523.25);");

            var query = new SaveQueryToCSVParameters
            {
                Query = $"Select Id, LastName, FirstName, REPLACE(Salary, '.', ',') AS 'Salary' from {_tableName}",
                QueryParameters = new SQLParameter[0],
                ConnectionString = _connString,
                TimeoutSeconds = 30,
                OutputFilePath = Path.Combine(_destination, "test.csv")
            };

            var options = new SaveQueryToCSVOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                LineBreak = CsvLineBreak.CRLF,
                FileEncoding = FileEncoding.UTF8,
                EnableBom = false,
                IncludeHeadersInOutput = true,
                SanitizeColumnHeaders = false,
                AddQuotesToDates = false,
                AddQuotesToStrings = false,
                DateFormat = "yyyy-MM-dd",
                DateTimeFormat = "yyyy-MM-ddTHH:mm:ss"
            };

            await SQL.SaveQueryToCSV(query, options, default);

            var output = File.ReadAllText(Path.Combine(_destination, "test.csv"));

            Assert.AreEqual("Id;LastName;FirstName;Salary\r\n1;Meikalainen;Matti;1523,25\r\n", output);
        }

        private static void InsertTestData(string commandText)
        {
            using (var sqlConnection = new SqlConnection(_connString))
            {
                sqlConnection.Open();
                using (var command = new SqlCommand())
                {
                    command.CommandText = commandText;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 30;
                    command.Connection = sqlConnection;

                    command.ExecuteReader();
                }

                sqlConnection.Close();
            }
        }
    }
}
