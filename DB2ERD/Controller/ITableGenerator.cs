namespace DB2ERD.Controller;

using DB2ERD.Model;
using System.Collections.Generic;

/// <summary>
/// Provides a mechanism for retrieving table metadata from a database.
/// </summary>
public interface ITableGenerator
{
    /// <summary>
    /// Executes the table query and returns a list of tables with column and key information.
    /// </summary>
    /// <param name="tableQuery">SQL statement used to enumerate the tables.</param>
    /// <param name="tablesToInclude">Optional list of fully qualified table names to include.</param>
    /// <param name="tablesToExclude">Optional list of tables to exclude.</param>
    /// <returns>The list of discovered tables.</returns>
    List<SqlTable> Execute(
        string tableQuery,
        List<string> tablesToInclude = null,
        List<string> tablesToExclude = null);
}
