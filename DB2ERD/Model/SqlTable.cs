using System.Diagnostics;

namespace DB2ERD.Model
{
    /// <summary>
    /// Represents a database table with its columns and relationships.
    /// </summary>
    [DebuggerDisplay("Schema = {schema_name}, TableName = {table_name}")]
    public class SqlTable
    {
        /// <summary>
        /// Gets or sets the schema ID (database-specific identifier).
        /// </summary>
        public int schema_id { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the schema (or owner) containing this table.
        /// </summary>
        public string schema_name { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string table_name { get; set; }
        
        /// <summary>
        /// Gets or sets the object ID (database-specific identifier).
        /// </summary>
        public int object_id { get; set; }
        
        /// <summary>
        /// Gets or sets the fully qualified name of the table (schema.table).
        /// </summary>
        public string full_name { get; set; }
        
        /// <summary>
        /// Gets or sets the list of columns in this table.
        /// </summary>
        public List<SqlColumn> columnList { get; set; } = new List<SqlColumn>();
        
        /// <summary>
        /// Gets or sets the list of foreign key constraints originating from this table.
        /// </summary>
        public List<ForeignKeyConstraint> foreign_key_list { get; set; } = new List<ForeignKeyConstraint>();
    }
}
