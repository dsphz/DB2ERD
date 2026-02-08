# Contributing to DB2ERD

Thank you for your interest in contributing to DB2ERD! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How to Contribute

### Reporting Issues

If you find a bug or have a suggestion for improvement:

1. Check the [Issues](https://github.com/dsphz/DB2ERD/issues) page to see if it's already been reported
2. If not, create a new issue with a clear title and description
3. Include steps to reproduce the issue (for bugs)
4. Include your environment details (.NET version, database type, OS)

### Submitting Changes

1. **Fork the repository** and create a new branch for your changes
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**
   - Write clear, readable code
   - Follow the existing code style
   - Add or update tests as needed
   - Update documentation if you're changing functionality

3. **Test your changes**
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit your changes**
   - Write clear, descriptive commit messages
   - Reference any related issues in your commit message

5. **Push to your fork** and submit a pull request

## Development Guidelines

### Code Style

- Use C# naming conventions (PascalCase for public members, camelCase for private fields with `_` prefix)
- Add XML documentation comments to public APIs
- Keep methods focused and concise
- Use meaningful variable and method names

### Security

- Always use parameterized queries to prevent SQL injection
- Never concatenate user input directly into SQL queries
- Validate and sanitize all external inputs

### Testing

- Write unit tests for new features
- Ensure all existing tests pass before submitting
- Use the existing test patterns as examples

### Adding Database Support

To add support for a new database type:

1. Create a new class implementing `ITableGenerator` in the `Controller` directory
2. Add the database type to the `DatabaseType` enum
3. Update the factory switch statement in `Program.cs`
4. Add a default query for the new database type
5. Add tests for the new generator
6. Update the README.md to document the new database support

## Building the Project

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/)
- A code editor (Visual Studio, VS Code, or Rider)

### Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project DB2ERD/DB2ERD.csproj -- --help
```

## Pull Request Process

1. Ensure your code builds and all tests pass
2. Update the README.md with details of changes (if applicable)
3. Your pull request will be reviewed by maintainers
4. Address any feedback from the review
5. Once approved, a maintainer will merge your pull request

## Questions?

If you have questions about contributing, feel free to open an issue with the "question" label.

## License

By contributing to DB2ERD, you agree that your contributions will be licensed under the same license as the project (see LICENSE file).
