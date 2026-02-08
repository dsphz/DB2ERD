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

### Documentation
- Added CONTRIBUTING.md with guidelines for contributors
- Added CHANGELOG.md to track project changes

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
