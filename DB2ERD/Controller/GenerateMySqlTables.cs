using Dapper;
using DB2ERD.Model;
using MySql.Data.MySqlClient;
using System.Linq;
using Spectre.Console;

namespace DB2ERD.Controller
{
    public class GenerateMySqlTables : ITableGenerator
    {
        private readonly string _connStr;
        private List<SqlTable> _tableList = new();

        public GenerateMySqlTables(string connStr)
        {
            _connStr = connStr;
        }

        /// <inheritdoc />
        public List<SqlTable> Execute(string tableQuery, List<string> tablesToInclude = null, List<string> tablesToExclude = null)
        {
            _tableList = new List<SqlTable>();
            try
            {
                using var conn = new MySqlConnection(_connStr);
                var list = conn.Query<dynamic>(tableQuery);
                foreach (var row in list)
                {
                    var fullName = $"{row.schema_name}.{row.table_name}";
                    if (tablesToExclude != null && tablesToExclude.Contains(fullName))
                        continue;
                    if (tablesToInclude != null && !tablesToInclude.Contains(fullName))
                        continue;

                    AnsiConsole.MarkupLine($"[{row.schema_name}].[{row.table_name}]");
                    var table = new SqlTable
                    {
                        schema_name = row.schema_name,
                        table_name = row.table_name,
                        full_name = row.full_name
                    };
                    GetTableColumns(conn, table);
                    GetTablePrimaryKeys(conn, table);
                    GetTableForeignKeys(conn, table);
                    GetForeignKeyConstraint(conn, table);
                    _tableList.Add(table);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to execute table query: {ex.Message}[/]");
            }
            return _tableList;
        }

        private void GetTableColumns(MySqlConnection conn, SqlTable table)
        {
            var sql = $@"SELECT column_name, is_nullable, data_type
                FROM information_schema.columns
                WHERE table_schema = '{table.schema_name}' AND table_name = '{table.table_name}'
                ORDER BY ordinal_position";
            var list = conn.Query<dynamic>(sql);
            foreach (var row in list)
            {
                table.columnList.Add(new SqlColumn
                {
                    column_name = row.column_name,
                    is_nullable = row.is_nullable,
                    data_type = row.data_type
                });
            }
        }

        private void GetTablePrimaryKeys(MySqlConnection conn, SqlTable table)
        {
            var sql = $@"SELECT k.column_name AS primary_key_column
                FROM information_schema.table_constraints t
                JOIN information_schema.key_column_usage k ON t.constraint_name = k.constraint_name AND t.table_schema = k.table_schema
                WHERE t.table_schema = '{table.schema_name}' AND t.table_name = '{table.table_name}' AND t.constraint_type = 'PRIMARY KEY'";
            var list = conn.Query<dynamic>(sql);
            foreach (var row in list)
            {
                var col = table.columnList.FirstOrDefault(c => c.column_name == row.primary_key_column);
                if (col != null)
                    col.is_primary_key = true;
            }
        }

        private void GetTableForeignKeys(MySqlConnection conn, SqlTable table)
        {
            var sql = $@"SELECT k.column_name AS foreign_key_column
                FROM information_schema.table_constraints t
                JOIN information_schema.key_column_usage k ON t.constraint_name = k.constraint_name AND t.table_schema = k.table_schema
                WHERE t.table_schema = '{table.schema_name}' AND t.table_name = '{table.table_name}' AND t.constraint_type = 'FOREIGN KEY'";
            var list = conn.Query<dynamic>(sql);
            foreach (var row in list)
            {
                var col = table.columnList.FirstOrDefault(c => c.column_name == row.foreign_key_column);
                if (col != null)
                    col.is_foreign_key = true;
            }
        }

        private void GetForeignKeyConstraint(MySqlConnection conn, SqlTable table)
        {
            var sql = $@"SELECT 0 AS object_id,
                    0 AS parent_object_id,
                    k.table_schema AS fk_schema_name,
                    k.table_name AS fk_table_name,
                    k.constraint_name AS foreign_key_name,
                    k.referenced_table_schema AS pk_schema_name,
                    k.referenced_table_name AS pk_table_name
                FROM information_schema.key_column_usage k
                WHERE k.table_schema = '{table.schema_name}' AND k.table_name = '{table.table_name}' AND k.referenced_table_name IS NOT NULL";
            table.foreign_key_list = conn.Query<ForeignKeyConstraint>(sql).ToList();
        }
    }
}
