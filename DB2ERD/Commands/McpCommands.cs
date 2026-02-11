using DB2ERD.Controller;
using DB2ERD.Model;
using Spectre.Console.Cli;

namespace DB2ERD.Commands;

public sealed class ListSupportedDatabasesCommand : Command<ListSupportedDatabasesCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--pretty-json")]
        [System.ComponentModel.Description("Render JSON output with indentation")]
        public bool PrettyJson { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var databases = Enum.GetValues<DatabaseType>()
            .Select(dbType => new SupportedDatabaseItem
            {
                Name = dbType.ToString(),
                DefaultIntrospectionQuery = DatabaseCommandSupport.GetDefaultTableQuery(dbType)
            })
            .ToList();

        var payload = new SupportedDatabasesPayload
        {
            Databases = databases
        };

        DatabaseCommandSupport.WriteSuccess(payload, settings.PrettyJson);
        return 0;
    }
}

public sealed class GetSchemaMetadataCommand : Command<GetSchemaMetadataCommand.Settings>
{
    /// <summary>
    /// Optional table generator used for testing. When not set, a generator is created
    /// based on the selected database type.
    /// </summary>
    public ITableGenerator TableGenerator { get; set; }

    public sealed class Settings : DatabaseAccessSettings
    {
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.MaxTables < 1)
        {
            DatabaseCommandSupport.WriteError(
                "INVALID_MAX_TABLES",
                "--max-tables must be greater than zero.",
                settings.PrettyJson);
            return -1;
        }

        if (!DatabaseCommandSupport.TryResolveRequest(
            settings,
            safeQueryMode: true,
            out var request,
            out var errorCode,
            out var errorMessage))
        {
            DatabaseCommandSupport.WriteError(errorCode, errorMessage, settings.PrettyJson);
            return -1;
        }

        try
        {
            var generator = TableGenerator ?? DatabaseCommandSupport.CreateTableGenerator(
                request.DatabaseType,
                request.ConnectionString,
                settings.Verbose,
                throwOnError: true);

            var includeTables = DatabaseCommandSupport.ParseTableList(settings.IncludeTables);
            var excludeTables = DatabaseCommandSupport.ParseTableList(settings.ExcludeTables);

            var allTables = generator.Execute(request.Query, includeTables, excludeTables) ?? new List<SqlTable>();
            var limitedTables = DatabaseCommandSupport.ApplyTableLimit(allTables, settings.MaxTables, out var truncated, out var totalTableCount);

            var payload = new SchemaMetadataPayload
            {
                DatabaseType = request.DatabaseType.ToString(),
                SafeQueryMode = !settings.AllowCustomQuery,
                Truncated = truncated,
                TotalTableCount = totalTableCount,
                ReturnedTableCount = limitedTables.Count,
                RelationshipCount = DatabaseCommandSupport.CountRelationships(limitedTables),
                Tables = limitedTables
            };

            DatabaseCommandSupport.WriteSuccess(payload, settings.PrettyJson);
            return 0;
        }
        catch (Exception ex)
        {
            DatabaseCommandSupport.WriteError("SCHEMA_READ_FAILED", ex.Message, settings.PrettyJson);
            return -1;
        }
    }
}

public sealed class GenerateErdPumlCommand : Command<GenerateErdPumlCommand.Settings>
{
    /// <summary>
    /// Optional table generator used for testing. When not set, a generator is created
    /// based on the selected database type.
    /// </summary>
    public ITableGenerator TableGenerator { get; set; }

    public sealed class Settings : DatabaseAccessSettings
    {
        [CommandOption("--mode <MODE>")]
        [System.ComponentModel.Description("Diagram mode: all-relationships, all-tables, isolated-tables")]
        public string Mode { get; set; } = "all-relationships";

        [CommandOption("--output-file <FILE>")]
        [System.ComponentModel.Description("Optional path to write the generated PlantUML diagram")]
        public string OutputFile { get; set; }

        [CommandOption("--max-output-chars <COUNT>")]
        [System.ComponentModel.Description("Maximum number of PlantUML characters included in JSON response")]
        public int MaxOutputChars { get; set; } = 200000;

        [CommandOption("--exclude-missing-relationships")]
        [System.ComponentModel.Description("Exclude relationships that point to missing tables (all-tables mode only)")]
        public bool ExcludeMissingRelationships { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.MaxTables < 1)
        {
            DatabaseCommandSupport.WriteError(
                "INVALID_MAX_TABLES",
                "--max-tables must be greater than zero.",
                settings.PrettyJson);
            return -1;
        }

        if (settings.MaxOutputChars < 1)
        {
            DatabaseCommandSupport.WriteError(
                "INVALID_MAX_OUTPUT_CHARS",
                "--max-output-chars must be greater than zero.",
                settings.PrettyJson);
            return -1;
        }

        if (!DatabaseCommandSupport.TryResolveRequest(
            settings,
            safeQueryMode: true,
            out var request,
            out var errorCode,
            out var errorMessage))
        {
            DatabaseCommandSupport.WriteError(errorCode, errorMessage, settings.PrettyJson);
            return -1;
        }

        try
        {
            var generator = TableGenerator ?? DatabaseCommandSupport.CreateTableGenerator(
                request.DatabaseType,
                request.ConnectionString,
                settings.Verbose,
                throwOnError: true);

            var includeTables = DatabaseCommandSupport.ParseTableList(settings.IncludeTables);
            var excludeTables = DatabaseCommandSupport.ParseTableList(settings.ExcludeTables);

            var allTables = generator.Execute(request.Query, includeTables, excludeTables) ?? new List<SqlTable>();
            var limitedTables = DatabaseCommandSupport.ApplyTableLimit(allTables, settings.MaxTables, out var tablesTruncated, out var totalTableCount);

            if (limitedTables.Count == 0)
            {
                DatabaseCommandSupport.WriteError("NO_TABLES_FOUND", "No tables found.", settings.PrettyJson);
                return -1;
            }

            var mode = settings.Mode?.Trim().ToLowerInvariant() ?? string.Empty;
            string puml;

            switch (mode)
            {
                case "all-relationships":
                    puml = GeneratePlantUMLDiagram.GenerateAllRelationships(limitedTables, "ERD", settings.OutputFile);
                    break;
                case "all-tables":
                    puml = GeneratePlantUMLDiagram.GenerateAllTables(
                        limitedTables,
                        "ERD",
                        settings.OutputFile,
                        settings.ExcludeMissingRelationships);
                    break;
                case "isolated-tables":
                    puml = GeneratePlantUMLDiagram.GenerateTablesWithNoRelationships(limitedTables, "ERD", settings.OutputFile);
                    break;
                default:
                    DatabaseCommandSupport.WriteError(
                        "INVALID_MODE",
                        "Unknown mode. Supported values: all-relationships, all-tables, isolated-tables.",
                        settings.PrettyJson);
                    return -1;
            }

            var outputPuml = puml;
            var pumlTruncated = false;
            if (outputPuml.Length > settings.MaxOutputChars)
            {
                outputPuml = outputPuml[..settings.MaxOutputChars];
                pumlTruncated = true;
            }

            var payload = new ErdPumlPayload
            {
                DatabaseType = request.DatabaseType.ToString(),
                Mode = mode,
                SafeQueryMode = !settings.AllowCustomQuery,
                TablesTruncated = tablesTruncated,
                TotalTableCount = totalTableCount,
                ReturnedTableCount = limitedTables.Count,
                RelationshipCount = DatabaseCommandSupport.CountRelationships(limitedTables),
                PumlTruncated = pumlTruncated,
                PumlLength = puml.Length,
                Puml = outputPuml,
                OutputFile = settings.OutputFile
            };

            DatabaseCommandSupport.WriteSuccess(payload, settings.PrettyJson);
            return 0;
        }
        catch (Exception ex)
        {
            DatabaseCommandSupport.WriteError("ERD_GENERATION_FAILED", ex.Message, settings.PrettyJson);
            return -1;
        }
    }
}

public sealed class SupportedDatabasesPayload
{
    public List<SupportedDatabaseItem> Databases { get; set; } = new();
}

public sealed class SupportedDatabaseItem
{
    public string Name { get; set; }
    public string DefaultIntrospectionQuery { get; set; }
}

public sealed class SchemaMetadataPayload
{
    public string DatabaseType { get; set; }
    public bool SafeQueryMode { get; set; }
    public bool Truncated { get; set; }
    public int TotalTableCount { get; set; }
    public int ReturnedTableCount { get; set; }
    public int RelationshipCount { get; set; }
    public List<SqlTable> Tables { get; set; }
}

public sealed class ErdPumlPayload
{
    public string DatabaseType { get; set; }
    public string Mode { get; set; }
    public bool SafeQueryMode { get; set; }
    public bool TablesTruncated { get; set; }
    public int TotalTableCount { get; set; }
    public int ReturnedTableCount { get; set; }
    public int RelationshipCount { get; set; }
    public bool PumlTruncated { get; set; }
    public int PumlLength { get; set; }
    public string Puml { get; set; }
    public string OutputFile { get; set; }
}
