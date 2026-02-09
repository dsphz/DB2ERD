using Dapper;
using DB2ERD.Model;
using Microsoft.Data.SqlClient;
using System.Linq;
using Spectre.Console;

namespace DB2ERD.Controller
{
    public class GenerateSqlServerTables : ITableGenerator
    {
        private readonly string _connectionString;
        private List<SqlTable> _tableList = new List<SqlTable>();

        public GenerateSqlServerTables(string dbConnString)
        {
            _connectionString = dbConnString;
        }

        /// <inheritdoc />
        public List<SqlTable> Execute(string tableQuery,
            List<string> tablesToInclude = null,
            List<string> tablesToExclude = null)
        {
            _tableList = new List<SqlTable>();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    dynamic list = conn.Query<dynamic>(tableQuery);

                    foreach (var row in list)
                    {
                        try
                        {
                            var fullName = $"{row.schema_name}.{row.table_name}";
                            if (tablesToExclude != null && tablesToExclude.Contains(fullName))
                                continue;

                            if (tablesToInclude != null && !tablesToInclude.Contains(fullName))
                                continue;

                            AnsiConsole.MarkupLine($"[{row.schema_name}].[{row.table_name}]");

                            var table = new SqlTable
                            {
                                schema_id = row.schema_id,
                                schema_name = row.schema_name,
                                table_name = row.table_name,
                                object_id = row.object_id,
                                full_name = row.full_name
                            };

                            GetTableColumns(conn, table);
                            GetTablePrimaryKeys(conn, table);
                            GetTableForeignKeys(conn, table);
                            GetForeignKeyConstraint(conn, table);

                            _tableList.Add(table);
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Failed to process table {row.schema_name}.{row.table_name}: {ex.Message}[/]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to execute table query: {ex.Message}[/]");
            }

            return _tableList;
        }


        private void GetForeignKeyConstraint(SqlConnection conn, SqlTable table)
        {
            var sql = @"SELECT 
                object_id,parent_object_id,
                  OBJECT_SCHEMA_NAME(parent_object_id) as [fk_schema_name],
                  OBJECT_NAME(parent_object_id) AS [fk_table_name],
                  name AS [foreign_key_name],
                  OBJECT_SCHEMA_NAME(referenced_object_id) as [pk_schema_name],
                  OBJECT_NAME(referenced_object_id) AS [pk_table_name]
                FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID(@fullName)";

            table.foreign_key_list = conn.Query<ForeignKeyConstraint>(sql, new { fullName = table.full_name }).ToList();
        }

        private void GetTableForeignKeys(SqlConnection conn, SqlTable table)
        {
            var sql = @"SELECT KU.table_name as table_name
                    ,column_name as foreign_key_column
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                    ON TC.CONSTRAINT_TYPE = 'FOREIGN KEY' 
                    AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                    AND KU.table_name=@tableName
	                AND KU.TABLE_SCHEMA = @schemaName
                ORDER BY 
                     KU.TABLE_NAME
                    ,KU.ORDINAL_POSITION";

            dynamic foreignKeyList = conn.Query<dynamic>(sql, new { tableName = table.table_name, schemaName = table.schema_name });

            foreach (var row in foreignKeyList)
            {
                var col = table.columnList.Where(x => x.column_name == row.foreign_key_column).FirstOrDefault();
                if (col != null)
                {
                    col.is_foreign_key = true;
                }
            }
        }

        private void GetTablePrimaryKeys(SqlConnection conn, SqlTable table)
        {
            var sql = @"SELECT KU.table_name as table_name
                    ,column_name as primary_key_column
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                    ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                    AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                    AND KU.table_name=@tableName
	                AND KU.TABLE_SCHEMA = @schemaName
                ORDER BY 
                     KU.TABLE_NAME
                    ,KU.ORDINAL_POSITION";

            dynamic primaryKeyList = conn.Query<dynamic>(sql, new { tableName = table.table_name, schemaName = table.schema_name });

            foreach (var row in primaryKeyList)
            {
                var col = table.columnList.Where(x => x.column_name == row.primary_key_column).FirstOrDefault();
                if (col != null)
                {
                    col.is_primary_key = true;
                }
            }
        }

        private void GetTableColumns(SqlConnection conn, SqlTable table)
        {
            var sql = "select COLUMN_NAME, IS_NULLABLE,DATA_TYPE from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = @schemaName and TABLE_NAME = @tableName order by ORDINAL_POSITION";

            dynamic columnList = conn.Query<dynamic>(sql, new { schemaName = table.schema_name, tableName = table.table_name });

            foreach (var row in columnList)
            {
                var c = new SqlColumn
                {
                    column_name = row.COLUMN_NAME,
                    is_nullable = row.IS_NULLABLE,
                    data_type = row.DATA_TYPE
                };

                table.columnList.Add(c);
            }
        }
    }
}
