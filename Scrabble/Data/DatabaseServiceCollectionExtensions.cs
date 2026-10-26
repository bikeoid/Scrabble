using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Scrabble.Server.Data;

/// <summary>
/// Extension methods to simplify database configuration based on the DatabaseProvider setting.
/// </summary>
public static class DatabaseServiceCollectionExtensions
{
    public const string SqlServerProvider = "SqlServer";
    public const string SQLiteProvider = "SQLite";

    /// <summary>
    /// Adds the appropriate ScrabbleDbContext (SQL Server or SQLite) to the service collection.
    /// </summary>
    public static IServiceCollection AddScrabbleDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = GetDatabaseProvider(configuration);
        Console.WriteLine($"Using database provider: {databaseProvider}");

        var connectionString = configuration.GetConnectionString("ScrabbleDbConnection") 
            ?? throw new InvalidOperationException("Connection string 'ScrabbleDbConnection' not found.");

        if (databaseProvider.Equals(SQLiteProvider, StringComparison.OrdinalIgnoreCase))
        {
            var sqliteConnectionString = configuration.GetConnectionString("ScrabbleDbSQLiteConnection")
                ?? throw new InvalidOperationException("Connection string 'ScrabbleDbSQLiteConnection' not found.");
            services.AddDbContext<ScrabbleDbContext>(options =>
                options.UseSqlite(sqliteConnectionString,
            x => x.MigrationsAssembly("Scrabble.SQLiteMigrations")
            ));
        }
        else
        {
            services.AddDbContext<ScrabbleDbContext>(options =>
                options.UseSqlServer(connectionString,
            x => x.MigrationsAssembly("Scrabble.SqlServerMigrations")
            ));
        }

        return services;
    }

    /// <summary>
    /// Adds the appropriate ApplicationDbContext (SQL Server or SQLite) to the service collection.
    /// </summary>
    public static IServiceCollection AddApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = GetDatabaseProvider(configuration);
        var connectionString = configuration.GetConnectionString("ScrabbleDbConnection")
            ?? throw new InvalidOperationException("Connection string 'ScrabbleDbConnection' not found.");

        if (databaseProvider.Equals(SQLiteProvider, StringComparison.OrdinalIgnoreCase))
        {
            var sqliteConnectionString = configuration.GetConnectionString("ScrabbleDbSQLiteConnection")
                ?? throw new InvalidOperationException("Connection string 'ScrabbleDbSQLiteConnection' not found.");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(sqliteConnectionString,
                    x => x.MigrationsAssembly("Scrabble.SQLiteMigrations")
                ));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString,
                    x => x.MigrationsAssembly("Scrabble.SqlServerMigrations")
                ));
        }

        return services;
    }

    /// <summary>
    /// Adds MyKeysContext for data protection keys.
    /// </summary>
    public static IServiceCollection AddMyKeysContext(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = GetDatabaseProvider(configuration);
        var connectionString = configuration.GetConnectionString("ScrabbleDbConnection")
            ?? throw new InvalidOperationException("Connection string 'ScrabbleDbConnection' not found.");

        if (databaseProvider.Equals(SQLiteProvider, StringComparison.OrdinalIgnoreCase))
        {
            var sqliteConnectionString = configuration.GetConnectionString("ScrabbleDbSQLiteConnection")
                ?? throw new InvalidOperationException("Connection string 'ScrabbleDbSQLiteConnection' not found.");
            services.AddDbContext<MyKeysContext>(options =>
                options.UseSqlite(sqliteConnectionString,
                    x => x.MigrationsAssembly("Scrabble.SQLiteMigrations")
                ));
        }
        else
        {
            services.AddDbContext<MyKeysContext>(options =>
                options.UseSqlServer(connectionString,
                    x => x.MigrationsAssembly("Scrabble.SqlServerMigrations")
                ));
        }

        return services;
    }

    /// <summary>
    /// Gets the current database provider setting.
    /// </summary>
    public static string GetDatabaseProvider(this IConfiguration configuration)
    {
        return configuration["DatabaseProvider"] ?? SqlServerProvider;
    }
}
