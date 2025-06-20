using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DB2ERD.Controller;
using DB2ERD.Model;
using Xunit;

namespace DB2ERD.Tests;

public class ERDGenerationTests
{
    private class FakeGenerator : ITableGenerator
    {
        public List<SqlTable> Tables { get; set; } = new();
        public string LastQuery { get; private set; }
        public List<SqlTable> Execute(string tableQuery, List<string> include = null, List<string> exclude = null)
        {
            LastQuery = tableQuery;
            return Tables;
        }
    }

    [Fact]
    public void Should_ReturnErrorCode_When_ConfigFileIsMissing()
    {
        var cmd = new ErdGeneration();
        var result = cmd.Execute(null!, new ErdGeneration.Settings{ Config="nonexistent.json" });
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Should_WritePlantUMLToFile_When_ExecuteIsCalled()
    {
        var fake = new FakeGenerator();
        var parentTable = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Parent",
            full_name = "dbo.Parent",
            columnList = [new SqlColumn { column_name = "Id", data_type = "int", is_primary_key = true }]
        };
        var childTable = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Child",
            full_name = "dbo.Child",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true },
                new() { column_name = "ParentId", data_type = "int", is_foreign_key = true }
            },
            foreign_key_list =
            [
                new()
                {
                    fk_schema_name = "dbo",
                    fk_table_name = "Child",
                    pk_schema_name = "dbo",
                    pk_table_name = "Parent",
                    foreign_key_name = "FK_Child_Parent",
                    object_id = 1,
                    parent_object_id = 2
                }
            ]
        };
        fake.Tables.Add(parentTable);
        fake.Tables.Add(childTable);

        var file = Path.GetTempFileName();
        try
        {
            var cmd = new ErdGeneration { TableGenerator = fake };
            var settings = new ErdGeneration.Settings
            {
                ConnectionString = "fake",
                TableQuery = "SELECT",
                Output = file,
                Config = "nonexistent.json"
            };
            var code = cmd.Execute(null!, settings);
            Assert.Equal(0, code);
            Assert.True(File.Exists(file));
            var fileContent = File.ReadAllText(file);
            Assert.Contains("table( dbo.Parent )", fileContent);
            Assert.Contains("table( dbo.Child )", fileContent);
            Assert.Contains("dbo.Child }|--|| dbo.Parent", fileContent);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public void Should_ReturnErrorCode_When_ConnectionStringIsEmpty()
    {
        var cmd = new ErdGeneration();
        var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var settings = new ErdGeneration.Settings
            {
                ConnectionString = "",
                TableQuery = "SELECT",
                Output = file,
                Config = "nonexistent.json"
            };
            var code = cmd.Execute(null!, settings);
            Assert.Equal(-1, code);
            Assert.False(File.Exists(file));
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public void Should_ReturnErrorCode_When_DatabaseTypeIsInvalid()
    {
        var fake = new FakeGenerator();
        var file = Path.GetTempFileName();
        try
        {
            var cmd = new ErdGeneration { TableGenerator = fake };
            var settings = new ErdGeneration.Settings
            {
                ConnectionString = "fake",
                DatabaseType = "BadType",
                TableQuery = "SELECT",
                Output = file,
                Config = "nonexistent.json"
            };
            var code = cmd.Execute(null!, settings);
            Assert.Equal(-1, code);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public void Should_ReturnErrorCode_When_NoTablesAreReturned()
    {
        var fake = new FakeGenerator();
        var file = Path.GetTempFileName();
        try
        {
            var cmd = new ErdGeneration { TableGenerator = fake };
            var settings = new ErdGeneration.Settings
            {
                ConnectionString = "fake",
                TableQuery = "SELECT",
                Output = file,
                Config = "nonexistent.json"
            };

            var code = cmd.Execute(null!, settings);

            Assert.Equal(-1, code);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public void Should_ReadQueryFromConfig_AndFallbackToDefault()
    {
        // Arrange a fake generator with one table so Execute succeeds
        var fake = new FakeGenerator();
        fake.Tables.Add(new SqlTable
        {
            schema_name = "dbo",
            table_name = "Only",
            full_name = "dbo.Only",
            columnList = [ new SqlColumn { column_name = "Id", data_type = "int" } ]
        });

        var configPath = Path.GetTempFileName();
        const string queryFromConfig = "SELECT * FROM MyTables";
        var configWithQuery = new AppConfig { ConnectionString = "fake", TableQuery = queryFromConfig };
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "output.txt");

        try
        {
            // Write config that includes the custom query
            File.WriteAllText(configPath, JsonSerializer.Serialize(configWithQuery));

            var cmd = new ErdGeneration { TableGenerator = fake };
            var settings = new ErdGeneration.Settings { Config = configPath };

            // Act
            var code = cmd.Execute(null!, settings);

            // Assert query from config is used
            Assert.Equal(0, code);
            Assert.Equal(queryFromConfig, fake.LastQuery);

            if (File.Exists(outputFile))
                File.Delete(outputFile);

            // Now remove the TableQuery from the config to test default query usage
            File.WriteAllText(configPath, JsonSerializer.Serialize(new { ConnectionString = "fake" }));

            var fake2 = new FakeGenerator();
            fake2.Tables.Add(fake.Tables[0]);

            cmd = new ErdGeneration { TableGenerator = fake2 };
            code = cmd.Execute(null!, settings);

            Assert.Equal(0, code);
            Assert.Contains("sys.tables", fake2.LastQuery);
        }
        finally
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
        }
    }
}
