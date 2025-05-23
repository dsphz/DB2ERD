using System.Collections.Generic;
using System.IO;
using DB2ERD.Controller;
using DB2ERD.Model;
using Xunit;

namespace DB2ERD.Tests.Controller;

public class GeneratePlantUMLDiagramTests
{
    [Fact]
    public void Should_WriteExpectedUml_When_GeneratingAllRelationships()
    {
        var table1 = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Table1",
            full_name = "dbo.Table1",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true },
                new() { column_name = "Name", data_type = "nvarchar" }
            }
        };
        var table2 = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Table2",
            full_name = "dbo.Table2",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true }
            }
        };
        table1.foreign_key_list.Add(new ForeignKeyConstraint
        {
            fk_schema_name = "dbo",
            fk_table_name = "Table1",
            pk_schema_name = "dbo",
            pk_table_name = "Table2",
            foreign_key_name = "FK_Table1_Table2"
        });

        var tables = new List<SqlTable> { table1, table2 };
        var file = Path.GetTempFileName();
        try
        {
            var uml = GeneratePlantUMLDiagram.GenerateAllRelationships(tables, "ERD", file);
            var fileContent = File.ReadAllText(file);
            Assert.Equal(uml, fileContent);
            Assert.Contains("table( dbo.Table1 )", uml);
            Assert.Contains("table( dbo.Table2 )", uml);
            Assert.Contains("dbo.Table1 }|--|| dbo.Table2", uml);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }
}
