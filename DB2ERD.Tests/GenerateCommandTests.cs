using System.Collections.Generic;
using System.IO;
using DB2ERD;
using DB2ERD.Controller;
using DB2ERD.Model;
using Xunit;

namespace DB2ERD.Tests;

public class GenerateCommandTests
{
    private class FakeGenerator : ITableGenerator
    {
        public List<SqlTable> Tables { get; set; } = new();
        public string? LastQuery { get; private set; }
        public List<SqlTable> Execute(string tableQuery, List<string>? include = null, List<string>? exclude = null)
        {
            LastQuery = tableQuery;
            return Tables;
        }
    }

    [Fact]
    public void Execute_ReturnsErrorWhenMissingConnectionString()
    {
        var cmd = new GenerateCommand();
        var result = cmd.Execute(null!, new GenerateCommand.Settings{ Config="nonexistent.json" });
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Execute_WritesOutputFile()
    {
        var fake = new FakeGenerator();
        fake.Tables.Add(new SqlTable
        {
            schema_name = "dbo",
            table_name = "T",
            full_name = "dbo.T",
            columnList = new List<SqlColumn>{ new SqlColumn{ column_name="Id", data_type="int" } }
        });
        var file = Path.GetTempFileName();
        try
        {
            var cmd = new GenerateCommand { TableGenerator = fake };
            var settings = new GenerateCommand.Settings
            {
                ConnectionString = "fake",
                TableQuery = "SELECT",
                Output = file,
                Config = "nonexistent.json"
            };
            var code = cmd.Execute(null!, settings);
            Assert.Equal(0, code);
            Assert.True(File.Exists(file));
            Assert.Contains("table( dbo.T )", File.ReadAllText(file));
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }
}
