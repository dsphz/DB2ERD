using System.Diagnostics;

namespace DB2ERD.Model
{
    /// <summary>
    /// Represents a foreign key constraint relationship between two tables.
    /// </summary>
    [DebuggerDisplay("FK = {foreign_key_name}")]
    public class ForeignKeyConstraint
    {
        /// <summary>
        /// Gets or sets the database-specific object ID for this foreign key.
        /// </summary>
        public int object_id { get; set; }
        
        /// <summary>
        /// Gets or sets the database-specific parent object ID.
        /// </summary>
        public int parent_object_id { get; set; }
        
        /// <summary>
        /// Gets or sets the schema name of the table that contains the foreign key (child table).
        /// </summary>
        public string fk_schema_name { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the table that contains the foreign key (child table).
        /// </summary>
        public string fk_table_name { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the foreign key constraint.
        /// </summary>
        public string foreign_key_name { get; set; }
        
        /// <summary>
        /// Gets or sets the schema name of the referenced table (parent table).
        /// </summary>
        public string pk_schema_name { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the referenced table (parent table).
        /// </summary>
        public string pk_table_name { get; set; }
    }
}
