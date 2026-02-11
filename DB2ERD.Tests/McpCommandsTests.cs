using DB2ERD.Commands;
using DB2ERD.Controller;
using DB2ERD.Model;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DB2ERD.Tests;

public class McpCommandsTests
{
    private static readonly object ConsoleLock = new();

    private sealed class FakeGenerator : ITableGenerator
    {
        public List<SqlTable> Tables { get; set; } = new();
        public string LastQuery { get; private set; }

        public List<SqlTable> Execute(string tableQuery, List<string> tablesToInclude = null, List<string> tablesToExclude = null)
        {
            LastQuery = tableQuery;
            return Tables;
        }
    }

    [Fact]
    public void ListSupportedDatabases_Should_ReturnJsonPayload()
    {
        var command = new ListSupportedDatabasesCommand();
        var settings = new ListSupportedDatabasesCommand.Settings();

        var (exitCode, output) = ExecuteWithOutput(command, settings);
        using var json = JsonDocument.Parse(output);

        Assert.Equal(0, exitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        var databases = json.RootElement
            .GetProperty("data")
            .GetProperty("databases")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("SqlServer", databases);
        Assert.Contains("Oracle", databases);
        Assert.Contains("PostgreSql", databases);
        Assert.Contains("MySql", databases);
    }

    [Fact]
    public void GetSchemaMetadata_Should_BlockCustomQuery_WithoutExplicitOptIn()
    {
        var command = new GetSchemaMetadataCommand();
        var settings = new GetSchemaMetadataCommand.Settings
        {
            Config = "nonexistent.json",
            ConnectionString = "fake-connection",
            TableQuery = "SELECT * FROM sys.tables"
        };

        var (exitCode, output) = ExecuteWithOutput(command, settings);
        using var json = JsonDocument.Parse(output);

        Assert.Equal(-1, exitCode);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("CUSTOM_QUERY_NOT_ALLOWED", json.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public void GetSchemaMetadata_Should_ReturnBoundedTableList()
    {
        var fake = new FakeGenerator
        {
            Tables =
            [
                new SqlTable
                {
                    schema_name = "dbo",
                    table_name = "First",
                    full_name = "dbo.First",
                    columnList = [ new SqlColumn { column_name = "Id", data_type = "int" } ]
                },
                new SqlTable
                {
                    schema_name = "dbo",
                    table_name = "Second",
                    full_name = "dbo.Second",
                    columnList = [ new SqlColumn { column_name = "Id", data_type = "int" } ]
                }
            ]
        };

        var command = new GetSchemaMetadataCommand { TableGenerator = fake };
        var settings = new GetSchemaMetadataCommand.Settings
        {
            Config = "nonexistent.json",
            ConnectionString = "fake-connection",
            MaxTables = 1
        };

        var (exitCode, output) = ExecuteWithOutput(command, settings);
        using var json = JsonDocument.Parse(output);

        Assert.Equal(0, exitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("truncated").GetBoolean());
        Assert.Equal(2, json.RootElement.GetProperty("data").GetProperty("totalTableCount").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("data").GetProperty("returnedTableCount").GetInt32());
        Assert.Contains("sys.tables", fake.LastQuery);
    }

    [Fact]
    public void GenerateErdPuml_Should_ReturnPlantUmlText_WithoutFileOutput()
    {
        var fake = new FakeGenerator
        {
            Tables =
            [
                new SqlTable
                {
                    schema_name = "dbo",
                    table_name = "Parent",
                    full_name = "dbo.Parent",
                    columnList = [new SqlColumn { column_name = "Id", data_type = "int", is_primary_key = true }]
                },
                new SqlTable
                {
                    schema_name = "dbo",
                    table_name = "Child",
                    full_name = "dbo.Child",
                    columnList =
                    [
                        new SqlColumn { column_name = "Id", data_type = "int", is_primary_key = true },
                        new SqlColumn { column_name = "ParentId", data_type = "int", is_foreign_key = true }
                    ],
                    foreign_key_list =
                    [
                        new ForeignKeyConstraint
                        {
                            fk_schema_name = "dbo",
                            fk_table_name = "Child",
                            pk_schema_name = "dbo",
                            pk_table_name = "Parent",
                            foreign_key_name = "FK_Child_Parent"
                        }
                    ]
                }
            ]
        };

        var command = new GenerateErdPumlCommand { TableGenerator = fake };
        var settings = new GenerateErdPumlCommand.Settings
        {
            Config = "nonexistent.json",
            ConnectionString = "fake-connection"
        };

        var (exitCode, output) = ExecuteWithOutput(command, settings);
        using var json = JsonDocument.Parse(output);

        Assert.Equal(0, exitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        var puml = json.RootElement.GetProperty("data").GetProperty("puml").GetString();
        Assert.Contains("@startuml", puml);
        Assert.Contains("table( dbo.Parent )", puml);
    }

    [Fact]
    public void GenerateErdPuml_Should_TruncateLargeTextInJsonResponse()
    {
        var fake = new FakeGenerator
        {
            Tables =
            [
                new SqlTable
                {
                    schema_name = "dbo",
                    table_name = "Only",
                    full_name = "dbo.Only",
                    columnList =
                    [
                        new SqlColumn { column_name = "Id", data_type = "int", is_primary_key = true },
                        new SqlColumn { column_name = "Name", data_type = "nvarchar" }
                    ]
                }
            ]
        };

        var command = new GenerateErdPumlCommand { TableGenerator = fake };
        var settings = new GenerateErdPumlCommand.Settings
        {
            Config = "nonexistent.json",
            ConnectionString = "fake-connection",
            MaxOutputChars = 40,
            Mode = "all-tables"
        };

        var (exitCode, output) = ExecuteWithOutput(command, settings);
        using var json = JsonDocument.Parse(output);

        Assert.Equal(0, exitCode);
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.True(json.RootElement.GetProperty("data").GetProperty("pumlTruncated").GetBoolean());
        Assert.Equal(40, json.RootElement.GetProperty("data").GetProperty("puml").GetString().Length);
    }

    private static (int ExitCode, string Output) ExecuteWithOutput<TSettings>(Command<TSettings> command, TSettings settings)
        where TSettings : CommandSettings
    {
        lock (ConsoleLock)
        {
            var originalOut = Console.Out;
            using var capture = new StringWriter();

            try
            {
                Console.SetOut(capture);
                var exitCode = command.Execute(null!, settings);
                return (exitCode, capture.ToString().Trim());
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
