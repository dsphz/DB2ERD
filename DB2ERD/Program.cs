using Spectre.Console;
using Spectre.Console.Cli;
using DB2ERD.Controller;
using System.ComponentModel;

using System.Text.Json;

namespace DB2ERD;

internal class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<ErdGeneration>();
        return app.Run(args);
    }
}

public class ErdGeneration : Command<ErdGeneration.Settings>
{
    /// <summary>
    /// Optional table generator used for testing. When not set, the command
    /// will create an instance of <see cref="GenerateSqlServerTables"/> at
    /// runtime.
    /// </summary>
    public ITableGenerator TableGenerator { get; set; }
    public class Settings : CommandSettings
    {
        [CommandOption("-c|--config <FILE>")]
        [Description("Path to configuration JSON file")] 
        public string Config { get; set; } = "appsettings.json";

        [CommandOption("--connection-string <STRING>")]
        [Description("Database connection string")] 
        public string ConnectionString { get; set; }

        [CommandOption("--table-query <SQL>")]
        [Description("SQL query used to list tables")]
        public string TableQuery { get; set; }

        [CommandOption("--dbtype <TYPE>")]
        [Description("Database type: SqlServer, Oracle, PostgreSql, MySql")]
        public string DatabaseType { get; set; }

        [CommandOption("-o|--output <FILE>")]
        [Description("Output PlantUML file")]
        public string Output { get; set; } = "output.txt";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        AppConfig config = null;
        if (File.Exists(settings.Config))
        {
            try
            {
                var json = File.ReadAllText(settings.Config);
                config = JsonSerializer.Deserialize<AppConfig>(json);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to read configuration: {ex.Message}[/]");
                return -1;
            }
        }

        var connectionString = settings.ConnectionString ?? config?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            AnsiConsole.MarkupLine("[red]Connection string is required.[/]");
            return -1;
        }

        var dbTypeString = settings.DatabaseType ?? config?.DatabaseType ?? "SqlServer";
        if (!Enum.TryParse<DatabaseType>(dbTypeString, true, out var dbType))
        {
            AnsiConsole.MarkupLine($"[red]Unknown database type: {dbTypeString}[/]");
            return -1;
        }

        var defaultQuery = dbType switch
        {
            DatabaseType.SqlServer => "SELECT schema_id, SCHEMA_NAME(schema_id) as [schema_name], name as table_name, object_id, '['+SCHEMA_NAME(schema_id)+'].['+name+']' AS full_name FROM sys.tables where is_ms_shipped = 0",
            DatabaseType.Oracle => "SELECT owner AS schema_name, table_name, owner||'.'||table_name AS full_name FROM all_tables WHERE owner NOT IN ('SYS','SYSTEM')",
            DatabaseType.PostgreSql => "SELECT table_schema AS schema_name, table_name, table_schema||'.'||table_name AS full_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema NOT IN ('pg_catalog','information_schema')",
            DatabaseType.MySql => "SELECT table_schema AS schema_name, table_name, CONCAT(table_schema,'.',table_name) AS full_name FROM information_schema.tables WHERE table_type='BASE TABLE' AND table_schema = DATABASE()",
            _ => string.Empty
        };

        // Prefer a query passed on the command line. If none is specified,
        // look for one in the configuration file. When both are missing or
        // blank, fall back to the built-in default for the selected database.
        var query = !string.IsNullOrWhiteSpace(settings.TableQuery)
            ? settings.TableQuery
            : !string.IsNullOrWhiteSpace(config?.TableQuery)
                ? config.TableQuery
                : defaultQuery;

        var generator = TableGenerator ?? dbType switch
        {
            DatabaseType.Oracle => new GenerateOracleTables(connectionString),
            DatabaseType.PostgreSql => new GeneratePostgreSqlTables(connectionString),
            DatabaseType.MySql => new GenerateMySqlTables(connectionString),
            _ => new GenerateSqlServerTables(connectionString)
        };
        var tables = generator.Execute(query);
        // add a check for empty tables
        if (tables == null || tables.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No tables found.[/]");
            return -1;
        }
        GeneratePlantUMLDiagram.GenerateAllRelationships(tables, "ERD", settings.Output);
        AnsiConsole.MarkupLine($"Output written to [green]{settings.Output}[/]");
        return 0;
    }
}
