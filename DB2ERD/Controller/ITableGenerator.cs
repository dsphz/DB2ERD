namespace DB2ERD.Controller;

using DB2ERD.Model;
using System.Collections.Generic;

public interface ITableGenerator
{
    List<SqlTable> Execute(string tableQuery,
        List<string>? tablesToInclude = null,
        List<string>? tablesToExclude = null);
}
