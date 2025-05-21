namespace SqlServerToPlantUML;

/// <summary>
/// Application configuration loaded from <c>appsettings.json</c>.
/// </summary>
public class AppConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableQuery { get; set; } = string.Empty;
}
