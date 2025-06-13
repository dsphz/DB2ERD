namespace DB2ERD;

/// <summary>
/// Application configuration loaded from <c>appsettings.json</c>.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Database connection string used to read schema metadata.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// SQL statement that returns the list of tables to process.
    /// </summary>
    public string TableQuery { get; set; } = string.Empty;

    /// <summary>
    /// Name of the database type to use when generating the diagram.
    /// </summary>
    public string DatabaseType { get; set; } = "SqlServer";
}
