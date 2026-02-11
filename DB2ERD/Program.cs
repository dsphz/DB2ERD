using Spectre.Console;
using Spectre.Console.Cli;
using DB2ERD.Commands;
using DB2ERD.Controller;
using System.ComponentModel;
using System.Text.Json;

namespace DB2ERD;

internal class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.SetApplicationName("db2erd");
            config.SetDefaultCommand<ErdGeneration>();

            config.AddCommand<ErdGeneration>("generate-file")
                .WithDescription("Legacy file-oriented ERD generation command.");
            config.AddCommand<ListSupportedDatabasesCommand>("list-supported-databases")
                .WithDescription("List supported databases and default introspection queries.");
            config.AddCommand<GetSchemaMetadataCommand>("get-schema-metadata")
                .WithDescription("Return schema metadata as structured JSON.");
            config.AddCommand<GenerateErdPumlCommand>("generate-erd-puml")
                .WithDescription("Generate PlantUML text and return it as structured JSON.");
        });

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

        var defaultQuery = DatabaseCommandSupport.GetDefaultTableQuery(dbType);

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
