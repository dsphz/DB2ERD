using System.Diagnostics;

namespace DB2ERD.Model
{
    /// <summary>
    /// Represents a database column with its metadata.
    /// </summary>
    [DebuggerDisplay("ColumnName = {column_name}, DataType = {data_type}")]
    public class SqlColumn
    {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string column_name { get; set; }
        
        /// <summary>
        /// Gets or sets whether the column allows NULL values.
        /// </summary>
        public string is_nullable { get; set; }
        
        /// <summary>
        /// Gets or sets the data type of the column.
        /// </summary>
        public string data_type { get; set; }
        
        /// <summary>
        /// Gets or sets whether this column is part of the primary key.
        /// </summary>
        public bool is_primary_key { get; set; }
        
        /// <summary>
        /// Gets or sets whether this column is a foreign key reference.
        /// </summary>
        public bool is_foreign_key { get; set; }
    }
}
