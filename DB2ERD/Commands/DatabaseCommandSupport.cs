using DB2ERD.Controller;
using DB2ERD.Model;
using System.Text.Json;

namespace DB2ERD.Commands;

internal static class DatabaseCommandSupport
{
    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static bool TryResolveRequest(
        DatabaseAccessSettings settings,
        bool safeQueryMode,
        out ResolvedDatabaseRequest request,
        out string errorCode,
        out string errorMessage)
    {
        request = null;
        errorCode = string.Empty;
        errorMessage = string.Empty;

        if (!TryReadConfig(settings.Config, out var config, out errorCode, out errorMessage))
        {
            return false;
        }

        var connectionString = settings.ConnectionString ?? config?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errorCode = "MISSING_CONNECTION_STRING";
            errorMessage = "Connection string is required.";
            return false;
        }

        var dbTypeString = settings.DatabaseType ?? config?.DatabaseType ?? "SqlServer";
        if (!Enum.TryParse<DatabaseType>(dbTypeString, true, out var dbType))
        {
            errorCode = "UNKNOWN_DATABASE_TYPE";
            errorMessage = $"Unknown database type: {dbTypeString}.";
            return false;
        }

        if (safeQueryMode && !string.IsNullOrWhiteSpace(settings.TableQuery) && !settings.AllowCustomQuery)
        {
            errorCode = "CUSTOM_QUERY_NOT_ALLOWED";
            errorMessage = "Custom table queries are disabled by default. Pass --allow-custom-query to enable them explicitly.";
            return false;
        }

        var defaultQuery = GetDefaultTableQuery(dbType);
        var query = defaultQuery;
        if (!safeQueryMode || settings.AllowCustomQuery)
        {
            query = !string.IsNullOrWhiteSpace(settings.TableQuery)
                ? settings.TableQuery
                : !string.IsNullOrWhiteSpace(config?.TableQuery)
                    ? config.TableQuery
                    : defaultQuery;
        }

        request = new ResolvedDatabaseRequest
        {
            ConnectionString = connectionString,
            DatabaseType = dbType,
            Query = query
        };

        return true;
    }

    public static ITableGenerator CreateTableGenerator(
        DatabaseType dbType,
        string connectionString,
        bool verbose,
        bool throwOnError)
    {
        return dbType switch
        {
            DatabaseType.Oracle => new GenerateOracleTables(connectionString, verbose, throwOnError),
            DatabaseType.PostgreSql => new GeneratePostgreSqlTables(connectionString, verbose, throwOnError),
            DatabaseType.MySql => new GenerateMySqlTables(connectionString, verbose, throwOnError),
            _ => new GenerateSqlServerTables(connectionString, verbose, throwOnError)
        };
    }

    public static List<string> ParseTableList(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return null;
        }

        var values = csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return values.Count == 0 ? null : values;
    }

    public static List<SqlTable> ApplyTableLimit(List<SqlTable> tables, int maxTables, out bool truncated, out int totalTableCount)
    {
        tables ??= new List<SqlTable>();
        totalTableCount = tables.Count;
        truncated = false;

        if (maxTables <= 0 || tables.Count <= maxTables)
        {
            return tables;
        }

        truncated = true;
        return tables.Take(maxTables).ToList();
    }

    public static int CountRelationships(List<SqlTable> tables)
    {
        if (tables == null || tables.Count == 0)
        {
            return 0;
        }

        return tables.Sum(t => t.foreign_key_list?.Count ?? 0);
    }

    public static string GetDefaultTableQuery(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.SqlServer => "SELECT schema_id, SCHEMA_NAME(schema_id) as [schema_name], name as table_name, object_id, '['+SCHEMA_NAME(schema_id)+'].['+name+']' AS full_name FROM sys.tables where is_ms_shipped = 0",
            DatabaseType.Oracle => "SELECT owner AS schema_name, table_name, owner||'.'||table_name AS full_name FROM all_tables WHERE owner NOT IN ('SYS','SYSTEM')",
            DatabaseType.PostgreSql => "SELECT table_schema AS schema_name, table_name, table_schema||'.'||table_name AS full_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema NOT IN ('pg_catalog','information_schema')",
            DatabaseType.MySql => "SELECT table_schema AS schema_name, table_name, CONCAT(table_schema,'.',table_name) AS full_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE()",
            _ => string.Empty
        };
    }

    public static void WriteSuccess<T>(T data, bool prettyJson)
    {
        var envelope = new JsonEnvelope<T>
        {
            Success = true,
            ErrorCode = string.Empty,
            Message = string.Empty,
            Data = data
        };
        WriteEnvelope(envelope, prettyJson);
    }

    public static void WriteError(string errorCode, string message, bool prettyJson)
    {
        var envelope = new JsonEnvelope<object>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Data = null
        };
        WriteEnvelope(envelope, prettyJson);
    }

    private static void WriteEnvelope<T>(JsonEnvelope<T> envelope, bool prettyJson)
    {
        var options = prettyJson ? PrettyJsonOptions : CompactJsonOptions;
        Console.Out.WriteLine(JsonSerializer.Serialize(envelope, options));
    }

    private static bool TryReadConfig(string configPath, out AppConfig config, out string errorCode, out string errorMessage)
    {
        config = null;
        errorCode = string.Empty;
        errorMessage = string.Empty;

        if (!File.Exists(configPath))
        {
            return true;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<AppConfig>(json);
            return true;
        }
        catch (Exception ex)
        {
            errorCode = "CONFIG_READ_FAILED";
            errorMessage = $"Failed to read configuration: {ex.Message}";
            return false;
        }
    }
}

internal sealed class JsonEnvelope<T>
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}

internal abstract class DatabaseAccessSettings : Spectre.Console.Cli.CommandSettings
{
    [Spectre.Console.Cli.CommandOption("-c|--config <FILE>")]
    [System.ComponentModel.Description("Path to configuration JSON file")]
    public string Config { get; set; } = "appsettings.json";

    [Spectre.Console.Cli.CommandOption("--connection-string <STRING>")]
    [System.ComponentModel.Description("Database connection string")]
    public string ConnectionString { get; set; }

    [Spectre.Console.Cli.CommandOption("--table-query <SQL>")]
    [System.ComponentModel.Description("SQL query used to list tables")]
    public string TableQuery { get; set; }

    [Spectre.Console.Cli.CommandOption("--allow-custom-query")]
    [System.ComponentModel.Description("Allow an explicitly provided custom --table-query")]
    public bool AllowCustomQuery { get; set; }

    [Spectre.Console.Cli.CommandOption("--dbtype <TYPE>")]
    [System.ComponentModel.Description("Database type: SqlServer, Oracle, PostgreSql, MySql")]
    public string DatabaseType { get; set; }

    [Spectre.Console.Cli.CommandOption("--include-tables <CSV>")]
    [System.ComponentModel.Description("Comma-separated fully qualified tables to include (schema.table)")]
    public string IncludeTables { get; set; }

    [Spectre.Console.Cli.CommandOption("--exclude-tables <CSV>")]
    [System.ComponentModel.Description("Comma-separated fully qualified tables to exclude (schema.table)")]
    public string ExcludeTables { get; set; }

    [Spectre.Console.Cli.CommandOption("--max-tables <COUNT>")]
    [System.ComponentModel.Description("Maximum number of tables returned in JSON payload")]
    public int MaxTables { get; set; } = 200;

    [Spectre.Console.Cli.CommandOption("--pretty-json")]
    [System.ComponentModel.Description("Render JSON output with indentation")]
    public bool PrettyJson { get; set; }

    [Spectre.Console.Cli.CommandOption("--verbose")]
    [System.ComponentModel.Description("Enable verbose database table logging")]
    public bool Verbose { get; set; }
}

internal sealed class ResolvedDatabaseRequest
{
    public string ConnectionString { get; set; }
    public DatabaseType DatabaseType { get; set; }
    public string Query { get; set; }
}
