# Frends.Community.SQL

FRENDS Community Task for SQL query results to CSV-file

[![Actions Status](https://github.com/CommunityHiQ/Frends.Community.SQL/workflows/PackAndPushAfterMerge/badge.svg)](https://github.com/CommunityHiQ/Frends.Community.SQL/actions) ![MyGet](https://img.shields.io/myget/frends-community/v/Frends.Community.SQL) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) 

- [Installing](#installing)
- [Tasks](#tasks)
     - [SaveQueryToCSV](#SaveQueryToCSV)
     - [BulkInsertDataTable](#BulkInsertDataTable)
- [License](#license)
- [Building](#building)
- [Contributing](#contributing)
- [Change Log](#change-log)

# Installing

You can install the task via FRENDS UI Task View or you can find the NuGet package from the following NuGet feed
https://www.myget.org/F/frends-community/api/v3/index.json and in Gallery view in MyGet https://www.myget.org/feed/frends-community/package/nuget/Frends.Community.SQL

# Tasks

## SaveQueryToCSV

### Parameters

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| Query | string | SQL query to execute | `SELECT id FROM foo` |
| ConnectionString | string | Database connection string | `Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword` |
| TimeoutSeconds | int | Timeout in seconds | 30 |
| OutputFilePath | string | CSV file path for output | `C:\output\path.csv` |
| QueryParameters | SQLParameter[] | Query parameters | `[ { "@Param1", "Value1" }, { "@Param2", "Value2" }]`

### Options

Settings for included attachments

| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| ColumnsToInclude | string[] | Columns to include in the output CSV. If no columns defined here then all columns will be written to output CSV. | `[id, name, value]` |
| FieldDelimiter | enum { Comma, Semicolon, Pipe } | Field delimeter to use in output CSV | Comma |
| LineBreak | enum { CR, LF, CRLF } | Line break style to use in output CSV. | CRLF |
| FileEncoding | enum | Encoding for the read content. By selecting 'Other' you can use any encoding. |
| EncodingInString | string | The name of encoding to use. Required if the FileEncoding choice is 'Other'. A partial list of supported encoding names: https://msdn.microsoft.com/en-us/library/system.text.encoding.getencodings(v=vs.110).aspx | `iso-8859-1` |
| IncludeHeadersInOutput | bool | Wherther to include headers in output CSV. | `true` |
| SanitizeColumnHeaders | bool | Whether to sanitize headers in output: (1) Strip any chars that are not 0-9, a-z or _ (2) Make sure that column does not start with a number or underscore (3) Force lower case | `true` |
| AddQuotesToDates | bool | Whether to add quotes around DATE and DATETIME fields | `true` |
| AddQuotesToStrings | bool | Whether to add quotes around string typed fields. | `true` |
| DateFormat | string | Date format to use for formatting DATE columns, use .NET formatting tokens. Note that formatting is done using invariant culture. | `yyyy-MM-dd` |
| DateTimeFormat | string | Date format to use for formatting DATETIME columns, use .NET formatting tokens. Note that formatting is done using invariant culture. | `yyyy-MM-dd HH:mm:ss` |

### Notes
Newlines in text fields are replaced with spaces.

Binary datatypes are converted using BitConverter.ToString() method. The method returns the byte array as string representation of the byte array. The output can be changed back to byte array using Split method: `output.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray()`

### Returns

Result object with properties:

| Property             | Type                 | Description                          | Example     |
| ---------------------| ---------------------| ------------------------------------ | ----------- |
| EntriesWritten       | int                  | Amount of entries written.           | 1           |
| Path                 | string               | Path to the file.                    | C:\test.csv |
| FileName             | string               | Name of the file                     | test.csv    |

## BulkInsertDataTable

### Parameters
| Property             | Type                 | Description                          | Example |
| ---------------------| ---------------------| ------------------------------------ | ----- |
| InputData | [DataTable](https://learn.microsoft.com/en-us/dotnet/api/system.data.datatable) | Data to insert into table. | See [DataTable examples](https://learn.microsoft.com/en-us/dotnet/api/system.data.datatable#examples). |
| TableName | string | Destination table name. | MyTable |
| Connection String | string | Connection String to be used to connect to the database. | Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword; |

### Options
| Property                         | Type                        | Description                                                                                                                                                                                                                                                                                                                                                       |
|----------------------------------|-----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Command Timeout Seconds          | int                         | Timeout in seconds to be used for the query. Default is 60 seconds,                                                                                                                                                                                                                                                                                               |
| Fire Triggers                    | bool                        | When specified, cause the server to fire the insert triggers for the rows being inserted into the database.                                                                                                                                                                                                                                                       |
| Keep Identity                    | bool                        | Preserve source identity values. When not specified, identity values are assigned by the destination.                                                                                                                                                                                                                                                             |
| Sql Transaction Isolation Level  | SqlTransationIsolationLevel | Transactions specify an isolation level that defines the degree to which one transaction must be isolated from resource or data modifications made by other transactions. Possible values are: Default, None, Serializable, ReadUncommitted, ReadCommitted, RepeatableRead, Snapshot. Additional documentation https://msdn.microsoft.com/en-us/library/ms378149(v=sql.110).aspx |

### Notes
This is a wrapper around [SqlBulkCopy](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy), similar to [Frends.Sql.BulkInsert](https://github.com/FrendsPlatform/Frends.Sql#sqlbulkinsert).

Unlike Frends.Sql.BulkInsert, this task takes the data as [DataTable](https://learn.microsoft.com/en-us/dotnet/api/system.data.datatable), which allows specifying column types explicitly.
In contrast, Frends.Sql.BulkInsert infers the types from the first inserted row, which can cause errors with non-string nullable columns.

### Returns
Integer - Number of copied rows.

# License

This project is licensed under the MIT License - see the LICENSE file for details

# Building

Clone a copy of the repo

`git clone https://github.com/CommunityHiQ/Frends.Community.SQL.git`

Build the project

`dotnet build`

Build docker container

`docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Salakala123!" -p 1433:1433 --name sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04`

Run Tests

`dotnet test`

Create a nuget package

`dotnet pack --configuration Release`

# Contributing
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!

# Change Log

| Version             | Changes                 |
| ---------------------| ---------------------|
| 1.1.1 | Migration from Frends.Community.SQL.QueryToFile. Converted to support .Net Framework 4.7.1 and .Net Standard 2.0. Renamed task. |
| 1.1.2 | Updated README and changed CI to create release in GitHub. |
| 1.1.3 | Updated dependencies System.ComponentModel.Annotations to 5.0.0 and CsvHelper to 27.1.1, also replaced MSTest.TestAdapter and MSTest.TestFramework with NUnit.Framework. |
| 1.2.0 | Added BulkInsertDataTable and related test. |
| 1.3.0 | Added parameter AddQuotesToStrings to SaveQueryToCSVOptions which if disabled will not add quotes to string typed fields. |
| 1.4.0 | Added result object SaveQueryToCSVResult with EntriesWritten, Path and FileName properties. Added option to pass custom field delimiter. Added support for binary datatypes. |
| 2.0.0 | Added targeting to .NET6 and .NET8. Updated the following packages: CsvHelper to v33.0.1 and System.Configuration.ConfigurationManager to 9.0.0. Replaced System.Data.SqlClient with Microsoft.Data.SqlClient, as the former is deprecated. Made small adjustments as required after package updates. |
