using Dapper;
using DB2ERD.Model;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using Spectre.Console;

namespace DB2ERD.Controller
{
    public class GenerateOracleTables : ITableGenerator
    {
        private readonly string _connStr;
        private List<SqlTable> _tableList = new();

        public GenerateOracleTables(string dbConnString)
        {
            _connStr = dbConnString;
        }

        /// <inheritdoc />
        public List<SqlTable> Execute(string tableQuery, List<string> tablesToInclude = null, List<string> tablesToExclude = null)
        {
            _tableList = new List<SqlTable>();
            try
            {
                using var conn = new OracleConnection(_connStr);
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

        private void GetTableColumns(OracleConnection conn, SqlTable table)
        {
            var sql = $"SELECT COLUMN_NAME, NULLABLE AS IS_NULLABLE, DATA_TYPE FROM ALL_TAB_COLUMNS WHERE OWNER = '{table.schema_name}' AND TABLE_NAME = '{table.table_name}' ORDER BY COLUMN_ID";
            var list = conn.Query<dynamic>(sql);
            foreach (var row in list)
            {
                table.columnList.Add(new SqlColumn
                {
                    column_name = row.COLUMN_NAME,
                    is_nullable = row.IS_NULLABLE,
                    data_type = row.DATA_TYPE
                });
            }
        }

        private void GetTablePrimaryKeys(OracleConnection conn, SqlTable table)
        {
            var sql = $@"SELECT cols.column_name AS primary_key_column
                FROM all_constraints cons
                JOIN all_cons_columns cols ON cons.constraint_name = cols.constraint_name AND cons.owner = cols.owner
                WHERE cons.constraint_type = 'P'
                    AND cons.owner = '{table.schema_name}'
                    AND cons.table_name = '{table.table_name}'";
            var list = conn.Query<dynamic>(sql);
            foreach (var row in list)
            {
                var col = table.columnList.FirstOrDefault(c => c.column_name == row.PRIMARY_KEY_COLUMN);
                if (col != null)
                    col.is_primary_key = true;
            }
        }

        private void GetTableForeignKeys(OracleConnection conn, SqlTable table)
        {
            var sql = $@"SELECT cols.column_name AS foreign_key_column
                FROM all_constraints cons
                JOIN all_cons_columns cols ON cons.constraint_name = cols.constraint_name AND cons.owner = cols.owner
                WHERE cons.constraint_type = 'R'
                    AND cons.owner = '{table.schema_name}'
                    AND cons.table_name = '{table.table_name}'";
            var list = conn.Query<dynamic>(sql);
            foreach (var row in list)
            {
                var col = table.columnList.FirstOrDefault(c => c.column_name == row.FOREIGN_KEY_COLUMN);
                if (col != null)
                    col.is_foreign_key = true;
            }
        }

        private void GetForeignKeyConstraint(OracleConnection conn, SqlTable table)
        {
            var sql = $@"SELECT
                    0 AS object_id,
                    0 AS parent_object_id,
                    fk.owner AS fk_schema_name,
                    fk.table_name AS fk_table_name,
                    fk.constraint_name AS foreign_key_name,
                    pk.owner AS pk_schema_name,
                    pk.table_name AS pk_table_name
                FROM all_constraints fk
                JOIN all_constraints pk ON fk.r_constraint_name = pk.constraint_name AND fk.r_owner = pk.owner
                WHERE fk.constraint_type = 'R'
                    AND fk.owner = '{table.schema_name}'
                    AND fk.table_name = '{table.table_name}'";
            table.foreign_key_list = conn.Query<ForeignKeyConstraint>(sql).ToList();
        }
    }
}
