using Dapper;
using DB2ERD.Model;
using Microsoft.Data.SqlClient;
using System.Linq;
using Spectre.Console;

namespace DB2ERD.Controller
{
    public class GenerateSqlServerTables : ITableGenerator
    {
        private string m_connStr;
        private List<SqlTable> m_tableList = new List<SqlTable>();

        public GenerateSqlServerTables(string dbConnString)
        {
            m_connStr = dbConnString;
        }

        public List<SqlTable> Execute(string tableQuery,
            List<string> tablesToInclude = null,
            List<string> tablesToExclude = null)
        {
            m_tableList = new List<SqlTable>();

            try
            {
                using (var conn = new SqlConnection(m_connStr))
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

                        var table = new SqlTable();
                        table.schema_id = row.schema_id;
                        table.schema_name = row.schema_name;
                        table.table_name = row.table_name;
                        table.object_id = row.object_id;
                        table.full_name = row.full_name;

                        GetTableColumns(table);
                        GetTablePrimaryKeys(table);
                        GetTableForeignKeys(table);
                        GetForeignKeyConstraint(table);

                        m_tableList.Add(table);
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

            return m_tableList;
        }


        public void GetForeignKeyConstraint(SqlTable table)
        {
            var sql = $@"SELECT 
                object_id,parent_object_id,
                  OBJECT_SCHEMA_NAME(parent_object_id) as [fk_schema_name],
                  OBJECT_NAME(parent_object_id) AS [fk_table_name],
                  name AS [foreign_key_name],
                  OBJECT_SCHEMA_NAME(referenced_object_id) as [pk_schema_name],
                  OBJECT_NAME(referenced_object_id) AS [pk_table_name]
                FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID('{table.full_name}')";

            using (var conn = new SqlConnection(m_connStr))
            {
                table.foreign_key_list = conn.Query<ForeignKeyConstraint>(sql).ToList();
            }
            //table.foreign_key_list = list;
            //foreach (var row in list)
            //{
            //    var obj = new ForeignKeyConstraint();
            //    obj.object_id = row.object_id;
            //    obj.parent_object_id = row.parent_object_id;
            //    obj.fk_schema_name = row.fk_schema;
            //    obj.fk_table_name = row.fk_Table;
            //    obj.foreign_key_name = row.foreign_key;
            //    obj.pk_schema_name = row.pk_schema;
            //    obj.pk_table_name = row.pk_table;

            //    table.foreign_key_list.Add(obj);
            //}
        }

        public void GetTableForeignKeys(SqlTable table)
        {
            var sql = $@"SELECT KU.table_name as table_name
                    ,column_name as foreign_key_column
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                    ON TC.CONSTRAINT_TYPE = 'FOREIGN KEY' 
                    AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                    AND KU.table_name='{table.table_name}'
	                AND KU.TABLE_SCHEMA = '{table.schema_name}'
                ORDER BY 
                     KU.TABLE_NAME
                    ,KU.ORDINAL_POSITION";

            using (var conn = new SqlConnection(m_connStr))
            {
                dynamic foreighnKeyList = conn.Query<dynamic>(sql);

                foreach (var row in foreighnKeyList)
                {
                    var col = table.columnList.Where(x => x.column_name == row.foreign_key_column).FirstOrDefault();
                    if (col != null)
                    {
                        col.is_foreign_key = true;
                    }
                }
            }
        }

        public void GetTablePrimaryKeys(SqlTable table)
        {
            var sql = $@"SELECT KU.table_name as table_name
                    ,column_name as primary_key_column
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                    ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                    AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
                    AND KU.table_name='{table.table_name}'
	                AND KU.TABLE_SCHEMA = '{table.schema_name}'
                ORDER BY 
                     KU.TABLE_NAME
                    ,KU.ORDINAL_POSITION";

            using (var conn = new SqlConnection(m_connStr))
            {
                dynamic primaryKeyList = conn.Query<dynamic>(sql);

                foreach (var row in primaryKeyList)
                {
                    var col = table.columnList.Where(x => x.column_name == row.primary_key_column).FirstOrDefault();
                    if (col != null)
                    {
                        col.is_primary_key = true;
                    }
                }
            }
        }

        public void GetTableColumns(SqlTable table)
        {
            var sql = $"select COLUMN_NAME, IS_NULLABLE,DATA_TYPE from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{table.schema_name}' and TABLE_NAME = '{table.table_name}' order by ORDINAL_POSITION";

            using (var conn = new SqlConnection(m_connStr))
            {
                dynamic columnList = conn.Query<dynamic>(sql);

                foreach (var row in columnList)
                {
                    var c = new SqlColumn();
                    c.column_name = row.COLUMN_NAME;
                    c.is_nullable = row.IS_NULLABLE;
                    c.data_type = row.DATA_TYPE;

                    table.columnList.Add(c);
                }
            }
        }
    }
}
