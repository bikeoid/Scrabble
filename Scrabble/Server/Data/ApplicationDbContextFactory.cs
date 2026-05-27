using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Scrabble.Server.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            //var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //// pass your design time connection string here
            //optionsBuilder.UseSqlServer("Server=(LOCAL)\\SQLSERVER14;Database=Scrabble;Trusted_Connection=True;Trust Server Certificate=true;Encrypt=false");
            //return new ApplicationDbContext(optionsBuilder.Options);
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).AddEnvironmentVariables().Build();
            var conn = config.GetConnectionString("ScrabbleDbConnection")
                       ?? throw new InvalidOperationException("Connection string 'ScrabbleDbConnection' not found.");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            options.UseSqlServer(conn);
            return new ApplicationDbContext(options.Options);
        }
    }

}




