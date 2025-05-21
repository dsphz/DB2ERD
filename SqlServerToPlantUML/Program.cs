using SqlServerToPlantUML.Controller;
using System.CommandLine;
using System.Text.Json;

namespace SqlServerToPlantUML;

public class AppConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableQuery { get; set; } = string.Empty;
}

internal class Program
{
    static int Main(string[] args)
    {
        var configOption = new Option<FileInfo?>("--config", () => null, "Path to configuration file");
        var connectionOption = new Option<string?>("--connection-string", "Database connection string");
        var queryOption = new Option<string?>("--table-query", "SQL used to list tables");
        var outputOption = new Option<string>("--output", () => "output.txt", "Output file path");

        var rootCommand = new RootCommand("SQL Server to PlantUML ERD generator")
        {
            configOption,
            connectionOption,
            queryOption,
            outputOption
        };

        rootCommand.SetHandler((FileInfo? configFile, string? connStr, string? tableQuery, string output) =>
        {
            var configPath = configFile?.FullName ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var config = new AppConfig();

            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                    if (loaded != null)
                        config = loaded;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read configuration: {ex.Message}");
                }
            }

            if (!string.IsNullOrWhiteSpace(connStr))
            {
                config.ConnectionString = connStr;
            }

            if (!string.IsNullOrWhiteSpace(tableQuery))
            {
                config.TableQuery = tableQuery;
            }

            if (string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                Console.WriteLine("Connection string is required.");
                return;
            }

            var query = string.IsNullOrWhiteSpace(config.TableQuery)
                ? "SELECT schema_id, SCHEMA_NAME(schema_id) as [schema_name], name as table_name, object_id, '['+SCHEMA_NAME(schema_id)+'].['+name+']' AS full_name FROM sys.tables where is_ms_shipped = 0"
                : config.TableQuery;

            var generator = new GenerateSqlServerTables(config.ConnectionString);
            var tables = generator.Execute(query);

            GeneratePlantUMLDiagram.GenerateAllRelationships(tables, "ERD", output);
            Console.WriteLine($"Diagram written to {output}");
        }, configOption, connectionOption, queryOption, outputOption);

        return rootCommand.Invoke(args);
    }
}
