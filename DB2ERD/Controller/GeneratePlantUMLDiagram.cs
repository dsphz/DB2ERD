using DB2ERD.Model;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace DB2ERD.Controller
{
    /// <summary>
    /// Provides methods to generate PlantUML diagram descriptions from database table metadata.
    /// </summary>
    public static class GeneratePlantUMLDiagram
    {
        private const string PlantUmlHeader = @"@startuml
!define primary_key(x) <b><color:#b8861b><&key></color> x</b>
!define foreign_key(x) <color:#aaaaaa><&key></color> x
!define column(x) <color:#efefef><&media-record></color> x
!define table(x) entity x << (T, white) >>";

        /// <summary>
        /// Generates PlantUML diagram description for all specified tables.
        /// </summary>
        /// <param name="tableList">List of tables to include in the diagram.</param>
        /// <param name="title">Title for the diagram (not currently used in output).</param>
        /// <param name="fileName">Path to the output PlantUML file.</param>
        /// <param name="excludeRelationshipsToTablesThatDontExist">
        /// When true, excludes relationships to tables not in the tableList.
        /// </param>
        /// <returns>The generated PlantUML text.</returns>
        public static string GenerateAllTables(List<SqlTable> tableList, string title, string fileName = "", bool excludeRelationshipsToTablesThatDontExist = false)
        {
            return GenerateDiagram(tableList, fileName, excludeRelationshipsToTablesThatDontExist);
        }

        /// <summary>
        /// Generates PlantUML diagram description for tables that have no relationships.
        /// Excludes tables that have either foreign keys or are referenced by other tables.
        /// </summary>
        /// <param name="tableList">List of tables to process.</param>
        /// <param name="title">Title for the diagram (not currently used in output).</param>
        /// <param name="fileName">Path to the output PlantUML file.</param>
        /// <returns>The generated PlantUML text.</returns>
        public static string GenerateTablesWithNoRelationships(List<SqlTable> tableList, string title, string fileName = "")
        {
            var isolatedTables = FilterIsolatedTables(tableList);
            return GenerateDiagram(isolatedTables, fileName, false);
        }

        /// <summary>
        /// Generates PlantUML diagram description for all tables that have relationships.
        /// Only includes tables that either have foreign keys or are referenced by other tables.
        /// </summary>
        /// <param name="tableList">List of tables to process.</param>
        /// <param name="title">Title for the diagram (not currently used in output).</param>
        /// <param name="fileName">Path to the output PlantUML file.</param>
        /// <returns>The generated PlantUML text.</returns>
        public static string GenerateAllRelationships(List<SqlTable> tableList, string title, string fileName = "")
        {
            var relatedTables = FilterRelatedTables(tableList);
            return GenerateDiagram(relatedTables, fileName, false);
        }

        private static List<SqlTable> FilterIsolatedTables(List<SqlTable> tableList)
        {
            var isolatedTables = new List<SqlTable>();

            foreach (var table in tableList)
            {
                if (table.foreign_key_list.Count == 0 && !IsReferencedByOtherTables(table, tableList))
                {
                    isolatedTables.Add(table);
                }
            }

            return isolatedTables;
        }

        private static List<SqlTable> FilterRelatedTables(List<SqlTable> tableList)
        {
            var relatedTables = new List<SqlTable>();

            foreach (var table in tableList)
            {
                if (table.foreign_key_list.Count > 0 || IsReferencedByOtherTables(table, tableList))
                {
                    relatedTables.Add(table);
                }
            }

            return relatedTables;
        }

        private static bool IsReferencedByOtherTables(SqlTable table, List<SqlTable> allTables)
        {
            return allTables.Any(t => t.foreign_key_list.Any(fk =>
                fk.pk_schema_name == table.schema_name && fk.pk_table_name == table.table_name));
        }

        private static string GenerateDiagram(List<SqlTable> tableList, string fileName, bool excludeRelationshipsToTablesThatDontExist)
        {
            var sb = new StringBuilder();

            sb.AppendLine(PlantUmlHeader);

            // Generate table definitions
            foreach (var table in tableList)
            {
                AppendTableDefinition(sb, table);
            }

            sb.AppendLine("' *** Define Table Relationships");

            // Generate relationships
            foreach (var table in tableList)
            {
                AppendTableRelationships(sb, table, tableList, excludeRelationshipsToTablesThatDontExist);
            }

            sb.AppendLine("@enduml");

            var text = sb.ToString();
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                File.WriteAllText(fileName, text);
            }

            return text;
        }

        private static void AppendTableDefinition(StringBuilder sb, SqlTable table)
        {
            sb.AppendLine($"table( {table.schema_name}.{table.table_name} )");
            sb.AppendLine("{");
            
            foreach (var col in table.columnList)
            {
                if (col.is_primary_key)
                    sb.AppendLine($"   primary_key( {col.column_name} ): {col.data_type} <<PK>>");
                else if (col.is_foreign_key)
                    sb.AppendLine($"   foreign_key( {col.column_name} ): {col.data_type} <<FK>>");
                else
                    sb.AppendLine($"   column( {col.column_name} ): {col.data_type}");
            }
            
            sb.AppendLine("}");
        }

        private static void AppendTableRelationships(StringBuilder sb, SqlTable table, List<SqlTable> allTables, bool excludeRelationshipsToTablesThatDontExist)
        {
            foreach (var fk in table.foreign_key_list)
            {
                if (excludeRelationshipsToTablesThatDontExist)
                {
                    var targetExists = allTables.Any(t => 
                        t.schema_name == fk.pk_schema_name && t.table_name == fk.pk_table_name);
                    
                    if (!targetExists)
                        continue;
                }

                sb.AppendLine($"{fk.fk_schema_name}.{fk.fk_table_name} {OneToManyRelationship()} {fk.pk_schema_name}.{fk.pk_table_name}");
            }
        }

        private static string OneToManyRelationship()
        {
            // https://plantuml.com/ie-diagram
            return "}|--||";
        }
    }
}
