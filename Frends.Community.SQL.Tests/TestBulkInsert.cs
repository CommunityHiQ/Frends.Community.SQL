using NUnit.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Frends.Community.SQL.Tests
{
    [TestFixture]
    public class TestBulkInsert
    {
        /*
        docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04
        with Git bash add winpty to the start of
        docker exec -it sql1 "bash"
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

        private readonly string ConnectionString = "Server=127.0.0.1,1433;Database=Master;User Id=SA;Password=Salakala123!;TrustServerCertificate=True";
        // The test case creates and destroys an SQL table with this name.
        // Ensure that this table does not already exist in the DB! Test aborts if it does.
        private readonly string TableName = "FrendsTestTable";
        private readonly struct DbRow
        {
            public readonly int Id;
            public readonly string FirstName;
            public readonly string LastName;
            public DbRow(int Id, string FirstName, string LastName)
            {
                this.Id = Id;
                this.FirstName = FirstName;
                this.LastName = LastName;
            }
        }
        // Schema for the test table
        private readonly DataColumn[] Columns = typeof(DbRow).GetFields().Select(field => new DataColumn(field.Name, field.FieldType)).ToArray();

        [SetUp]
        public void SetUp()
        {
            // Map C# types to SQL types
            var typeMap = new Dictionary<Type, string>
            {
                [typeof(int)] = "int",
                [typeof(string)] = "varchar(255)"
            };
            // Create SQL table with the test harness schema
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    CREATE TABLE {TableName} ({string.Join(
                            ",",
                            Columns.Select(column => $"{column.ColumnName} {typeMap[column.DataType]}")
                        )});";
                cmd.ExecuteNonQuery();
            }
        }
        [TearDown]
        public void TearDown()
        {
            // Drop test table
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"DROP TABLE {TableName};";
                cmd.ExecuteNonQuery();
            }
        }

        [Test]
        public async Task TestBasicInsert()
        {
            var insertedData = new DataTable();
            insertedData.Columns.AddRange(Columns);
            foreach (var rowData in new[]
            {
                new DbRow(1, "Etu", "Suku" ),
                new DbRow(2, "First", "Last"),
                new DbRow(3, "Eka", "Name")
            })
            {
                var row = insertedData.NewRow();
                foreach (var (column, columnIndex) in Columns.Select((x, i) => (x, i)))
                {
                    row[column.ColumnName] = rowData.GetType().GetField(column.ColumnName).GetValue(rowData);
                }
                insertedData.Rows.Add(row);
            }
            var result =
                await
                    SQL.BulkInsertDataTable(
                        new BulkInsertInput()
                        {
                            ConnectionString = ConnectionString,
                            TableName = TableName,
                            InputData = insertedData
                        },
                        new BulkInsertOptions()
                        {
                            CommandTimeoutSeconds = 60,
                            FireTriggers = true,
                            KeepIdentity = false,
                            SqlTransactionIsolationLevel = SqlTransactionIsolationLevel.ReadCommitted
                        }, CancellationToken.None);
            Assert.AreEqual(3, result);

            var tableRows = GetTableRows().ToArray();
            Assert.AreEqual(3, tableRows.Length);
            Assert.AreEqual("Suku", tableRows[0].LastName);
        }

        private IEnumerable<DbRow> GetTableRows()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                return connection.Query<DbRow>($"SELECT * FROM {TableName}");
            }
        }
    }
}
