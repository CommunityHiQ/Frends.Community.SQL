using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using NUnit.Framework;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Dapper;
using System.Collections;

namespace Frends.Community.SQL.Tests
{
    [TestFixture]
    public class SQLTest
    {
        [Test]
        public void DataReaderToCsvTest_AllColumns()
        {
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {
                new DataColumn("col_string", typeof(string)),
                new DataColumn("col_datetime", typeof(DateTime)),
                new DataColumn("col_float", typeof(float))
            });

            dt.Rows.Add("Hello\"semicolon1", DateTime.Parse("2018-12-31T11:22:33"), 3000.212);
            dt.Rows.Add("Hello\"semicolon2", DateTime.Parse("2018-12-31T11:22:34"), 3000.212);

            var options = new SaveQueryToCSVOptions
            {
                DateFormat = "MM-dd-yyyy",
                DateTimeFormat = "MM-dd-yyyy HH:mm:ss",
                ColumnsToInclude = new string[0],
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                AddQuotesToDates = false
            };

            using (var writer = new StringWriter())
            using (var csvFile = SQL.CreateCsvWriter(options.GetFieldDelimeterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                var entries = SQL.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split(new String[] { "\r\n" }, StringSplitOptions.None);

                // 4 lines = 1 header line + 2 data lines + 1 newline at end of file
                Assert.AreEqual(4, resultLines.Length);
                Assert.AreEqual(2, entries);
                Assert.AreEqual("col_string;col_datetime;col_float", resultLines[0]);
                Assert.AreEqual("\"Hello\\\"semicolon1\";12-31-2018 11:22:33;3000.212", resultLines[1]);
                Assert.AreEqual("\"Hello\\\"semicolon2\";12-31-2018 11:22:34;3000.212", resultLines[2]);
            }
        }

        [Test]
        public void DataReaderToCsvTest_ExcludeColumnHeaders()
        {
            var dt = new DataTable();
            dt.Columns.Add(new DataColumn("col_string", typeof(string)));
            dt.Rows.Add("test");

            var options = new SaveQueryToCSVOptions { IncludeHeadersInOutput = false };
            using (var writer = new StringWriter())
            using (var csvFile = SQL.CreateCsvWriter(options.GetFieldDelimeterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                SQL.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split(new String[] { "\r\n" }, StringSplitOptions.None);

                // 2 lines = 0 header lines + 1 data lines + 1 newline at end of file
                Assert.AreEqual(2, resultLines.Length);
                Assert.AreEqual("\"test\"", resultLines[0]);
            }
        }

        [Test]
        public void DataReaderToCsvTest_SanitizeColumnHeaders()
        {
            var dt = new DataTable();
            dt.Columns.Add(new DataColumn("COL_STRING", typeof(string)));
            dt.Rows.Add("test");

            var options = new SaveQueryToCSVOptions { SanitizeColumnHeaders = true };
            using (var writer = new StringWriter())
            using (var csvFile = SQL.CreateCsvWriter(options.GetFieldDelimeterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                SQL.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split(new String[] { "\r\n" }, StringSplitOptions.None);

                // 3 lines = 1 header lines + 1 data lines + 1 newline at end of file
                Assert.AreEqual(3, resultLines.Length);
                Assert.AreEqual("col_string", resultLines[0]);
            }
        }

        [Test]
        public void DataReaderToCsvTest_SelectedColumns()
        {
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {
                new DataColumn("COL_StrING", typeof(string)),
                new DataColumn("col_datetime", typeof(DateTime)),
                new DataColumn("123Col_float", typeof(float))
            });

            dt.Rows.Add("Hello\"semicolon1", DateTime.Parse("2018-12-31T11:22:33"), 3000.212);
            dt.Rows.Add("Hello\"semicolon2", DateTime.Parse("2018-12-31T11:22:34"), 3000.212);

            var options = new SaveQueryToCSVOptions
            {
                DateFormat = "MM-dd-yyyy HH:mm:ss",
                ColumnsToInclude = new[] { "COL_StrING", "123Col_float" },
                FieldDelimiter = CsvFieldDelimiter.Pipe,
                SanitizeColumnHeaders = true
            };

            using (var writer = new StringWriter())
            using (var csvFile = SQL.CreateCsvWriter(options.GetFieldDelimeterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                SQL.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                var result = writer.ToString();
                var resultLines = result.Split(new String[] { "\r\n" }, StringSplitOptions.None);

                // 4 lines = 1 header line + 2 data lines + 1 newline at end of file
                Assert.AreEqual(4, resultLines.Length);
                Assert.AreEqual("col_string|col_float", resultLines[0]);
                Assert.AreEqual("\"Hello\\\"semicolon1\"|3000.212", resultLines[1]);
                Assert.AreEqual("\"Hello\\\"semicolon2\"|3000.212", resultLines[2]);
            }
        }

        /// <summary>
        /// This test is basically to make sure that the dump to CSV is not horribly slow.
        /// My results are ok (not great though) - about 1m rows in under 2 seconds. But 
        /// this depends on the agent CPU of course.
        /// </summary>
        [Ignore("This test is for occasional performance testing only and depends on host CPU")]
        [Test]
        public void DataReaderToCsvTest_1mRows()
        {
            var rowAmount = 1000000;
            var processingMaxTimeSeconds = 2d;
            var dt = new DataTable();
            dt.Columns.AddRange(new[]
            {
                new DataColumn("col_string", typeof(string)),
                new DataColumn("col_datetime", typeof(DateTime)),
                new DataColumn("col_float", typeof(float)),
                new DataColumn("col_double", typeof(double)),
                new DataColumn("col_decimal", typeof(decimal)),
            });

            for (var i = 0; i < rowAmount; i++)
            {
                dt.Rows.Add($"Hello, mister {i}", DateTime.Now, i, i, i);
            }

            var options = new SaveQueryToCSVOptions
            {
                DateFormat = "MM-dd-yyyy HH:mm:ss",
                ColumnsToInclude = new[] { "col_string", "col_float" },
                FieldDelimiter = CsvFieldDelimiter.Pipe
            };

            using (var writer = new StringWriter())
            using (var csvFile = SQL.CreateCsvWriter(options.GetFieldDelimeterAsString(), writer))
            using (var reader = new DataTableReader(dt))
            {
                var sw = Stopwatch.StartNew();
                SQL.DataReaderToCsv(reader, csvFile, options, new System.Threading.CancellationToken());
                csvFile.Flush();
                sw.Stop();
                Console.WriteLine("Elapsed={0}", sw.Elapsed);
                var result = writer.ToString();
                var resultLines = result.Split(new String[] { "\r\n" }, StringSplitOptions.None);

                // rowAmout + 1 header row + 1 newline at end
                Assert.AreEqual(rowAmount + 2, resultLines.Length);

                // Check execution time
                Assert.IsTrue(
                    sw.Elapsed.TotalSeconds < processingMaxTimeSeconds,
                    $"DataReaderToCsv completed in {sw.Elapsed.TotalSeconds} seconds. Processing max time: {processingMaxTimeSeconds} seconds");
            }
        }

        [Test]
        public void FormatDbValue_String()
        {
            var options = new SaveQueryToCSVOptions { FieldDelimiter = CsvFieldDelimiter.Semicolon };
            // Basic case
            Assert.AreEqual(
                "\"hello, world\"",
                SQL.FormatDbValue("hello, world", null, typeof(string), options));

            // Quotes should be escaped
            Assert.AreEqual(
                "\"hello\\\" world\"",
                SQL.FormatDbValue("hello\" world", null, typeof(string), options));

            // Newlines should be replaced by spaces
            Assert.AreEqual(
                "\"hello world\"",
                SQL.FormatDbValue("hello\rworld", null, typeof(string), options));
            Assert.AreEqual(
                "\"hello world\"",
                SQL.FormatDbValue("hello\r\nworld", null, typeof(string), options));
            Assert.AreEqual(
                "\"hello world\"",
                SQL.FormatDbValue("hello\nworld", null, typeof(string), options));
        }

        [Test]
        public void FormatDbValue_DateTime()
        {
            var options = new SaveQueryToCSVOptions
            {
                FieldDelimiter = CsvFieldDelimiter.Semicolon,
                DateFormat = "dd-MM_yyyy",
                DateTimeFormat = "dd-MM_yyyy HH:mm:ss",
                AddQuotesToDates = false,
            };

            // Date
            Assert.AreEqual(
                "31-12_2018",
                SQL.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAte", typeof(DateTime), options));

            // Datetime
            Assert.AreEqual(
                "31-12_2018 11:22:33",
                SQL.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAteTIME", typeof(DateTime), options));

            options.AddQuotesToDates = true;

            // Date
            Assert.AreEqual(
                "\"31-12_2018\"",
                SQL.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAte", typeof(DateTime), options));

            // Datetime
            Assert.AreEqual(
                "\"31-12_2018 11:22:33\"",
                SQL.FormatDbValue(DateTime.Parse("2018-12-31T11:22:33"), "DAteTIME", typeof(DateTime), options));
        }

        [Test]
        public void FormatDbValue_Nulls()
        {
            var options = new SaveQueryToCSVOptions();

            Assert.AreEqual(
                "",
                SQL.FormatDbValue(null, "DOUBLE", typeof(double), options));

            // All string and date/datetime types should be quoted, including nulls
            Assert.AreEqual(
                "\"\"",
                SQL.FormatDbValue(DBNull.Value, "DATE", typeof(DateTime), options));

            Assert.AreEqual(
                "\"\"",
                SQL.FormatDbValue(DBNull.Value, "DATETIME", typeof(DateTime), options));
            Assert.AreEqual(
                "\"\"",
                SQL.FormatDbValue(DBNull.Value, "NVARCHAR", typeof(string), options));
        }

        [Test]
        public void FormatDbValue_FloatDoubleDecimal()
        {
            var options = new SaveQueryToCSVOptions();
            // Float
            Assert.AreEqual(
                "1234.543",
                SQL.FormatDbValue((float)1234.543, "FLOAT", typeof(float), options));
            // Double
            Assert.AreEqual(
                "1234.543",
                SQL.FormatDbValue((double)1234.543, "DOUBLE", typeof(double), options));
            // Float
            Assert.AreEqual(
                "1234.543",
                SQL.FormatDbValue((decimal)1234.543, "DECIMAL", typeof(decimal), options));
        }

        [Test]
        public void FormatDbHeader()
        {
            // Basic case
            Assert.AreEqual(
                "123_hello!!! THIS IS MADNESS",
                SQL.FormatDbHeader("123_hello!!! THIS IS MADNESS", false));
            // Sanitize it!
            Assert.AreEqual(
                "hellothisis5anitiz3d_madness",
                SQL.FormatDbHeader("123_hello!!! THIS IS 5aNiTiZ3D_MADNESS", true));
        }
    }

    [TestFixture]
    public class TestBulkInsert
    {
        private readonly string ConnectionString = "Put your test MSSQL connection string here";
        // The test case creates and destroys an SQL table with this name.
        // Ensure that this table does not already exist in the DB! Test aborts if it does.
        private readonly string TableName = "FrendsTestTable";
        private readonly struct DbRow {
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
                    CREATE TABLE {TableName} ({
                        string.Join(
                            ",",
                            Columns.Select(column => $"{column.ColumnName} {typeMap[column.DataType]}")
                        )
                    });";
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
                foreach (var (column, columnIndex) in Columns.Select((x,i) => (x,i)))
                {
                    row[column.ColumnName] = rowData.GetType().GetField(column.ColumnName).GetValue(rowData);
                }
                insertedData.Rows.Add(row);
            }
            var result =
                await
                    Frends.Community.SQL.SQL.BulkInsertDataTable(
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
            Assert.AreEqual(tableRows.Length, 3);
            Assert.AreEqual(tableRows[0].LastName, "Suku");
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
