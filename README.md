# SqlServerToPlantUML
Generate PlantUML Entity Relationship Diagram From SQL Server Tables

**Updated for .NET 8**

This command line utility parses the tables in SQL server to generate PlantUML syntax to create diagrams of SQL server tables and thier foreign key relationships 

Example Output:

![alt tag](https://raw.githubusercontent.com/OceanAirdrop/SqlServerToPlantUML/main/SqlServerToPlantUML/ExampleDiagrams/AdventureWorks2014%20-%20HumanResources%20Schema.png)

More Examples Here: https://github.com/OceanAirdrop/SqlServerToPlantUML/tree/main/SqlServerToPlantUML/ExampleDiagrams



PlantUML is an open-source tool allowing users to create diagrams from a plain text language.  You can find more about it here: https://plantuml.com

The PlantUML website also has an online server which you can find [here](https://www.plantuml.com/plantuml/uml/hLLVJzim47_Ff_0qQQDe3ErbGgYg41HMDiOGhVOwNU9hSyAnkxCDpGRVVKxAm43CMA5zY9hVdzxlVBQp7Uk0dQzKzahYb3IAELC5NFyumtfjqHFzVD0lZ3Ay_JhVslhu8Vny4w11VxDzEn2B_cO-k55F3IFDz8twMWhjvu7qHXPU-4h72fmstw2eK19iC1Q6PPVKGQwmKQ9sv6gn4Fs-M9ky4nSbEUo9mI3BbXmnyXNeKxJ6Su7s_RYagHEzx7Scn9tOeaKEXwAtBHpfchGBfYTVWfKGVsiLnr1Xmz3dm4K976FZfgiJMX9SUCRWlfOfwAkHD2MKGt0jBiEpvLmbCWnn3kmgrDr1Rk5b6bk1KlyVvnCRFqyJLgXxlvehKoDsLhthBMcqThzTqMNbgQ8T6GtgS7ZignmvLT-HcP4EHWdu6yz0qw_MxDZ8Da99o0sp9fcY7k4vghYjCV6poIVaLB9bkyIkZFjrfv_0v42D7JXlMuS7Iu9Q8dT28s9C6Oz11MkK1mfc3PwJY-7lKBJ9HiQpJIKRKt5sX72EZxBsF3Dgb_WZDXSpJx91lAQV7aa6SZD3BpDNjuC_8kiCroiCsYgxwV5Z9CrqmJWE6H6ZOoDcl4YRJg1-mVwSWiEz4lGCxnVI4UVqcb8PZNKCX9eE6CD8wAAEMlUil64pxe6Mr10xayQ4ZfpHlVPeMmxkXqJhbwVceb9FJF1MhA-lY-7oTfiqSOADQhUbBMbkioUF_DNbniRbPVhAU9PJn7-kJxjFilI8hbP5lAJ_akwWbhvIsHy0).


Inspiration For this project was from this blogpost: https://raphael-leger.medium.com/automatically-generating-up-to-date-database-diagrams-with-typeorm-d1279a20545e

## Building

Use the [.NET 8 SDK](https://dotnet.microsoft.com/) and run:

```bash
dotnet build
```

## Configuration

The application reads its settings from an `appsettings.json` file placed next to the executable. The file must define a connection string and the SQL query used to list the tables that will be diagrammed.

Example `SqlServerToPlantUML/appsettings.json`:

```json
{
  "ConnectionString": "Data Source=MACHINENAME;Initial Catalog=AdventureWorks2014;User ID=sa;Password=MYPASSWORD;Connect Timeout=30;",
  "TableQuery": "SELECT schema_id, SCHEMA_NAME(schema_id) as [schema_name], name as table_name, object_id, '['+SCHEMA_NAME(schema_id)+'].['+name+']' AS full_name FROM sys.tables where is_ms_shipped = 0"
}
```

Update these values to match your environment before running the program.

## Usage

Run the executable with options to override values from the configuration file:

```bash
dotnet SqlServerToPlantUML.dll \
    --config appsettings.json \
    --connection-string "Server=.;Database=MyDb;Trusted_Connection=True;" \
    --table-query "SELECT ..." \
    --output diagram.puml
```

If an option is omitted, the value from the specified configuration file is used.
