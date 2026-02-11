# Changelog

All notable changes to DB2ERD will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Security
- Fixed SQL injection vulnerabilities in all database generators by using parameterized queries
- All table and schema names are now properly parameterized in SQL queries

### Changed
- Removed Hungarian notation from field names for better code readability
- Improved code consistency across all database generators
- Updated field naming to use modern C# conventions (`_connectionString` instead of `m_connStr`)
- Refactored PlantUML generation code to eliminate duplication (reduced from ~230 lines to ~160 lines)

### Added
- Added comprehensive XML documentation to all public APIs
- Added CONTRIBUTING.md with guidelines for contributors
- Added CHANGELOG.md to track project changes
- Added .editorconfig for consistent code style
- Added CI badge to README
- Added security and contributing sections to README
- Added MCP-friendly CLI commands:
  - `list-supported-databases` for capability discovery
  - `get-schema-metadata` for structured schema JSON
  - `generate-erd-puml` for structured PlantUML JSON with optional file output

### MCP Readiness
- Added structured JSON success/error envelopes for machine callers
- Added bounded response controls with `--max-tables` and `--max-output-chars`
- Added explicit custom SQL opt-in with `--allow-custom-query` for safer defaults
- Added generator flags for silent mode and fail-fast error propagation in machine workflows

### Note on Naming Conventions
Model classes (SqlTable, SqlColumn, ForeignKeyConstraint) intentionally use snake_case for public properties to maintain compatibility with database metadata column names and existing code. While this differs from standard C# conventions, it simplifies data mapping and reduces the need for additional annotations. Future major versions may consider a migration to PascalCase with appropriate mapping attributes.

## Previous Releases

### .NET 8 Update
- Updated project to target .NET 8
- Updated all package dependencies
- Improved CI/CD workflows

### Initial Release
- Command-line tool to generate PlantUML ERD diagrams from database schemas
- Support for Microsoft SQL Server
- Support for Oracle
- Support for PostgreSQL
- Support for MySQL
- Configurable via appsettings.json or command-line arguments
- Automatic detection of tables, columns, primary keys, and foreign key relationships
