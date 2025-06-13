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

    [Fact]
    public void Should_WriteExpectedUml_When_GeneratingAllTables()
    {
        var parent = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Parent",
            full_name = "dbo.Parent",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true }
            }
        };

        var child = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Child",
            full_name = "dbo.Child",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true },
                new() { column_name = "ParentId", data_type = "int", is_foreign_key = true }
            }
        };

        child.foreign_key_list.Add(new ForeignKeyConstraint
        {
            fk_schema_name = "dbo",
            fk_table_name = "Child",
            pk_schema_name = "dbo",
            pk_table_name = "Parent",
            foreign_key_name = "FK_Child_Parent"
        });

        var grandChild = new SqlTable
        {
            schema_name = "dbo",
            table_name = "GrandChild",
            full_name = "dbo.GrandChild",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true },
                new() { column_name = "ChildId", data_type = "int", is_foreign_key = true }
            }
        };

        grandChild.foreign_key_list.Add(new ForeignKeyConstraint
        {
            fk_schema_name = "dbo",
            fk_table_name = "GrandChild",
            pk_schema_name = "dbo",
            pk_table_name = "Child",
            foreign_key_name = "FK_GrandChild_Child"
        });

        var standalone = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Standalone",
            full_name = "dbo.Standalone",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true }
            }
        };

        var tables = new List<SqlTable> { parent, child, grandChild, standalone };
        var file = Path.GetTempFileName();
        try
        {
            var uml = GeneratePlantUMLDiagram.GenerateAllTables(tables, "ERD", file);
            var fileContent = File.ReadAllText(file);
            Assert.Equal(uml, fileContent);
            Assert.Contains("table( dbo.Parent )", uml);
            Assert.Contains("table( dbo.Child )", uml);
            Assert.Contains("table( dbo.GrandChild )", uml);
            Assert.Contains("table( dbo.Standalone )", uml);
            Assert.Contains("dbo.Child }|--|| dbo.Parent", uml);
            Assert.Contains("dbo.GrandChild }|--|| dbo.Child", uml);
            Assert.DoesNotContain("dbo.Standalone }|--||", uml);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public void Should_GenerateOnlyIsolatedTables_When_GeneratingTablesWithNoRelationships()
    {
        var table1 = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Table1",
            full_name = "dbo.Table1",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true }
            }
        };

        var table2 = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Table2",
            full_name = "dbo.Table2",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true },
                new() { column_name = "Table1Id", data_type = "int", is_foreign_key = true }
            }
        };

        table2.foreign_key_list.Add(new ForeignKeyConstraint
        {
            fk_schema_name = "dbo",
            fk_table_name = "Table2",
            pk_schema_name = "dbo",
            pk_table_name = "Table1",
            foreign_key_name = "FK_Table2_Table1"
        });

        var isolated = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Isolated",
            full_name = "dbo.Isolated",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true }
            }
        };

        var tables = new List<SqlTable> { table1, table2, isolated };
        var file = Path.GetTempFileName();
        try
        {
            var uml = GeneratePlantUMLDiagram.GenerateTablesWithNoRelationships(tables, "ERD", file);
            var fileContent = File.ReadAllText(file);
            Assert.Equal(uml, fileContent);
            Assert.Contains("table( dbo.Isolated )", uml);
            Assert.DoesNotContain("table( dbo.Table1 )", uml);
            Assert.DoesNotContain("table( dbo.Table2 )", uml);
            Assert.DoesNotContain("dbo.Table2 }|--|| dbo.Table1", uml);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public void Should_HandleRelationshipsToMissingTables_BasedOnFlag()
    {
        var referencing = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Referencing",
            full_name = "dbo.Referencing",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true },
                new() { column_name = "MissingId", data_type = "int", is_foreign_key = true }
            }
        };

        referencing.foreign_key_list.Add(new ForeignKeyConstraint
        {
            fk_schema_name = "dbo",
            fk_table_name = "Referencing",
            pk_schema_name = "dbo",
            pk_table_name = "Missing",
            foreign_key_name = "FK_Referencing_Missing"
        });

        var other = new SqlTable
        {
            schema_name = "dbo",
            table_name = "Other",
            full_name = "dbo.Other",
            columnList = new List<SqlColumn>
            {
                new() { column_name = "Id", data_type = "int", is_primary_key = true }
            }
        };

        var tables = new List<SqlTable> { referencing, other };

        var file1 = Path.GetTempFileName();
        try
        {
            var uml = GeneratePlantUMLDiagram.GenerateAllTables(tables, "ERD", file1, excludeRelationshipsToTablesThatDontExist: true);
            var fileContent = File.ReadAllText(file1);
            Assert.Equal(uml, fileContent);
            Assert.DoesNotContain("dbo.Referencing }|--|| dbo.Missing", uml);
        }
        finally
        {
            if (File.Exists(file1))
                File.Delete(file1);
        }

        var file2 = Path.GetTempFileName();
        try
        {
            var uml2 = GeneratePlantUMLDiagram.GenerateAllTables(tables, "ERD", file2, excludeRelationshipsToTablesThatDontExist: false);
            var fileContent2 = File.ReadAllText(file2);
            Assert.Equal(uml2, fileContent2);
            Assert.Contains("dbo.Referencing }|--|| dbo.Missing", uml2);
        }
        finally
        {
            if (File.Exists(file2))
                File.Delete(file2);
        }
    }
}
