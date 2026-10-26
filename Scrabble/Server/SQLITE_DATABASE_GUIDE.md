# SQLite Database Support Guide

## Overview
The Scrabble.Server project now supports both SQL Server and SQLite databases. This allows developers to easily switch between database providers without changing code.

## Configuration

### Switching Database Providers
Edit `appsettings.json` and set the `DatabaseProvider` value:

```json
{
  "DatabaseProvider": "SqlServer",  // Use "SqlServer" or "SQLite"
  "ConnectionStrings": {
	"ScrabbleDbConnection": "Server=(localdb)\\mssqllocaldb;Database=Scrabble.Server-65BBC78E-AC48-4FCD-93BD-E18E0DFE7DA3;Trusted_Connection=True;MultipleActiveResultSets=true",
	"ScrabbleDbSQLiteConnection": "Data Source=scrabble.db"
  }
}
```

**Options:**
- `"SqlServer"` (default) - Uses the SQL Server connection specified in `ScrabbleDbConnection`
- `"SQLite"` - Uses the SQLite database file specified in `ScrabbleDbSQLiteConnection`

### Connection Strings
- **SQL Server**: `ScrabbleDbConnection` - Connection to your SQL Server instance
- **SQLite**: `ScrabbleDbSQLiteConnection` - Path to SQLite database file (relative to the application root)

## Database Contexts

The project now has separate contexts for each database provider:

### Application Contexts
- `ScrabbleDbContext` - For game data (Chat, Game, Player, PlayerGame entities)
- `ApplicationDbContext` - For ASP.NET Core Identity data
- `MyKeysContext` - For data protection keys

## Migrations

### Existing SQL Server Migrations
All existing SQL Server migrations in `Scrabble/SqlServerMigrations` continue to work without modification.

### Creating Migrations
To create migrations, use the Package Manager Console or .NET CLI with the context name:

**Option 1: Package Manager Console**
```powershell, SQLite
Add-Migration InitialCreateSQLite -Context ScrabbleDbContext -Project Scrabble.SQLiteMigrations
Add-Migration InitialCreateSQLiteAppDb -Context ApplicationDbContext -Project Scrabble.SQLiteMigrations
```
```powershell, SQL Server
Add-Migration NewMigration -Context ScrabbleDbContext -Project Scrabble.SqlServerMigrations
Add-Migration NewMigration -Context ApplicationDbContext -Project Scrabble.SqlServerMigrations
```

**Option 2: .NET CLI**
```bash
dotnet ef migrations add InitialCreateSQLite -c ScrabbleDbContextSQLite
dotnet ef migrations add InitialCreateSQLiteAppDb -c ApplicationDbContextSQLite
```

### Applying Migrations
Migrations are applied automatically on application startup via the ASP.NET Core migration infrastructure.

## Architecture Notes

### Database-Agnostic Design
- Entity models are shared between SQL Server and SQLite contexts
- SQL Server-specific column types (e.g., `ntext`, `datetime`) are mapped to SQLite equivalents (TEXT)
- Constraint names used in SQL Server are omitted for SQLite (not strictly necessary)

### Service Configuration
The `DatabaseServiceCollectionExtensions` class provides extension methods that:
- Read the `DatabaseProvider` setting from configuration
- Register the appropriate DbContext based on the provider
- Handle connection string resolution for each provider

## Development Workflow

### To use SQL Server (default)
1. No changes needed - SQL Server is the default provider
2. Run the application normally

### To use SQLite
1. Update `DatabaseProvider` to `"SQLite"` in `appsettings.json`
2. Ensure SQLite database file path in `ScrabbleDbSQLiteConnection` is valid
3. Run migrations for SQLite contexts (if not already created)
4. Start the application

## Troubleshooting

### Error: "Connection string 'ScrabbleDbConnection' not found"
- Verify `ConnectionStrings` section exists in `appsettings.json`
- Ensure the connection string key matches the configuration

### Error: "UseSqlite" not found
- Verify `Microsoft.EntityFrameworkCore.Sqlite` NuGet package is installed
- If not: `dotnet add package Microsoft.EntityFrameworkCore.Sqlite`

### SQLite database file not created
- Ensure the directory where the database file should be created exists
- Check file permissions
- SQLite will create the file automatically on first access

## Future Enhancements

Potential improvements for this implementation:
- Add environment-specific appsettings (e.g., `appsettings.Development.json` with SQLite for development)
- Create a configuration UI for switching providers at runtime
