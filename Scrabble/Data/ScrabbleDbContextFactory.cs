using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using SQLitePCL;

namespace Scrabble.Server.Data
{
    public class ScrabbleDbContextFactory : IDesignTimeDbContextFactory<ScrabbleDbContext>
    {
        public ScrabbleDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).AddEnvironmentVariables().Build();
            var databaseProvider = configuration["DatabaseProvider"] ?? DatabaseServiceCollectionExtensions.SqlServerProvider;
            var connectionString = configuration.GetConnectionString("ScrabbleDbConnection")
                ?? throw new InvalidOperationException("Connection string 'ScrabbleDbConnection' not found.");

            var options = new DbContextOptionsBuilder<ScrabbleDbContext>();
            if (databaseProvider.Equals(DatabaseServiceCollectionExtensions.SQLiteProvider, StringComparison.OrdinalIgnoreCase))
            {
                var sqliteConnectionString = configuration.GetConnectionString("ScrabbleDbSQLiteConnection")
                    ?? throw new InvalidOperationException("Connection string 'ScrabbleDbSQLiteConnection' not found.");
                options.UseSqlite(sqliteConnectionString,
                    x => x.MigrationsAssembly("Scrabble.SQLiteMigrations")
                );
            }
            else
            {
                options.UseSqlServer(connectionString,
                    x => x.MigrationsAssembly("Scrabble.SqlServerMigrations")
                );
            }


            return new ScrabbleDbContext(options.Options);
        }
    }

}




