using DB2ERD.Model;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;


namespace DB2ERD.Controller
{
    public static class GeneratePlantUMLDiagram
    {
        private const string PlantUmlHeader = @"@startuml
!define primary_key(x) <b><color:#b8861b><&key></color> x</b>
!define foreign_key(x) <color:#aaaaaa><&key></color> x
!define column(x) <color:#efefef><&media-record></color> x
!define table(x) entity x << (T, white) >>";

        public static string GenerateAllTables(List<SqlTable> tableList, string title, string fileName, bool excludeRelationshipsToTablesThatDontExist = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine(PlantUmlHeader);

            foreach (var table in tableList)
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

            sb.AppendLine("' *** Define Table Relationships");

            foreach (var table in tableList)
            {
                foreach (var item in table.foreign_key_list)
                {
                    if (excludeRelationshipsToTablesThatDontExist == true)
                    {
                        var count = tableList.Where(x => x.schema_name == item.pk_schema_name && x.table_name == item.pk_table_name).Count();
                        if (count == 0)
                            continue;
                    }

                    sb.AppendLine($"{item.fk_schema_name}.{item.fk_table_name} {OneToManyRelationship()} {item.pk_schema_name}.{item.pk_table_name}");
                }

            }

            sb.AppendLine("@enduml");

            var text = sb.ToString();

            File.WriteAllText(fileName, sb.ToString());

            return text;
        }

        public static string GenerateTablesWithNoRelationships(List<SqlTable> tableList, string title, string fileName)
        {
            var sb = new StringBuilder();

            var toProcessList = new List<SqlTable>();

            foreach (var table in tableList)
            {
                if (table.foreign_key_list.Count == 0)
                {
                    bool relationshipFound = false;

                    // Ok.. This table doesnt rely on other tables, but do other tables rely on this?
                    foreach (var t in tableList)
                    {
                        foreach (var key in t.foreign_key_list)
                        {
                            if (key.pk_schema_name == table.schema_name && key.pk_table_name == table.table_name)
                            {
                                // this table has a relationship with our table! 
                                relationshipFound = true;
                                break;
                            }
                        }

                        if (relationshipFound == true)
                            break;
                    }

                    if (relationshipFound == false)
                        toProcessList.Add(table);
                }
            }


            sb.AppendLine(PlantUmlHeader);

            foreach (var table in toProcessList)
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

            sb.AppendLine("' *** Define Table Relationships");

            foreach (var table in toProcessList)
            {
                foreach (var item in table.foreign_key_list)
                {
                    sb.AppendLine($"{item.fk_schema_name}.{item.fk_table_name} {OneToManyRelationship()} {item.pk_schema_name}.{item.pk_table_name}");
                }

            }

            sb.AppendLine("@enduml");

            var text = sb.ToString();

            File.WriteAllText(fileName, sb.ToString());

            return text;
        }

        public static string GenerateAllRelationships(List<SqlTable> tableList, string title, string fileName)
        {
            var sb = new StringBuilder();

            var toProcessList = new List<SqlTable>();

            foreach (var table in tableList)
            {
                if (table.foreign_key_list.Count == 0)
                {
                    bool relationshipFound = false;

                    // Ok.. This table doesnt rely on other tables, but do other tables rely on this?
                    foreach (var t in tableList)
                    {
                        foreach (var key in t.foreign_key_list)
                        {
                            if (key.pk_schema_name == table.schema_name && key.pk_table_name == table.table_name)
                            {
                                // this table has a relationship with our table! 
                                relationshipFound = true;
                                break;
                            }
                        }

                        if (relationshipFound == true)
                            break;
                    }

                    if (relationshipFound == true)
                        toProcessList.Add(table);
                }
                else
                {
                    toProcessList.Add(table);
                }
            }


            sb.AppendLine(PlantUmlHeader);

            foreach (var table in toProcessList)
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

            sb.AppendLine("' *** Define Table Relationships");

            foreach (var table in toProcessList)
            {
                foreach (var item in table.foreign_key_list)
                {
                    sb.AppendLine($"{item.fk_schema_name}.{item.fk_table_name} {OneToManyRelationship()} {item.pk_schema_name}.{item.pk_table_name}");
                }

            }

            sb.AppendLine("@enduml");

            var text = sb.ToString();

            File.WriteAllText(fileName, sb.ToString());

            return text;
        }

        private static string OneToManyRelationship()
        {
            // https://plantuml.com/ie-diagram
            return "}|--||";
        }

        private static string ZeroToOneRelationship()
        {
            return "|o--";
        }

        private static string ExactlyOneRelationship()
        {
            return "||--";
        }
    }
}
