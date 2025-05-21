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
        var app = new CommandApp<GenerateCommand>();
        return app.Run(args);
    }
}

public class GenerateCommand : Command<GenerateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-c|--config <FILE>")]
        [Description("Path to configuration JSON file")] 
        public string Config { get; set; } = "appsettings.json";

        [CommandOption("--connection-string <STRING>")]
        [Description("Database connection string")] 
        public string? ConnectionString { get; set; }

        [CommandOption("--table-query <SQL>")]
        [Description("SQL query used to list tables")]
        public string? TableQuery { get; set; }

        [CommandOption("-o|--output <FILE>")]
        [Description("Output PlantUML file")]
        public string Output { get; set; } = "output.txt";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        AppConfig? config = null;
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

        var query = settings.TableQuery ?? config?.TableQuery ??
            "SELECT schema_id, SCHEMA_NAME(schema_id) as [schema_name], name as table_name, object_id, '['+SCHEMA_NAME(schema_id)+'].['+name+']' AS full_name FROM sys.tables where is_ms_shipped = 0";

        var generator = new GenerateSqlServerTables(connectionString);
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
