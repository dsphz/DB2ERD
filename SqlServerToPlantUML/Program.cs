using SqlServerToPlantUML.Controller;
using System.Text.Json;

namespace SqlServerToPlantUML;

internal class Program
{
    static void Main(string[] args)
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Configuration file not found: {configPath}");
            return;
        }

        AppConfig? config = null;
        try
        {
            var json = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<AppConfig>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read configuration: {ex.Message}");
            return;
        }

        if (config == null || string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            Console.WriteLine("Invalid configuration. ConnectionString is required.");
            return;
        }

        // Use the provided table query if set; otherwise fall back to a basic
        // query that lists all non-system tables.
        var query = string.IsNullOrWhiteSpace(config.TableQuery)
            ? "SELECT schema_id, SCHEMA_NAME(schema_id) as [schema_name], name as table_name, object_id, '['+SCHEMA_NAME(schema_id)+'].['+name+']' AS full_name FROM sys.tables where is_ms_shipped = 0"
            : config.TableQuery;

        var generator = new GenerateSqlServerTables(config.ConnectionString);
        var tables = generator.Execute(query);

        const string output = "output.txt";
        GeneratePlantUMLDiagram.GenerateAllRelationships(tables, "ERD", output);
        Console.WriteLine($"Diagram written to {output}");
    }
}
