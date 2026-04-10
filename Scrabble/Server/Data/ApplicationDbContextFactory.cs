using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Scrabble.Server.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // pass your design time connection string here
            optionsBuilder.UseSqlServer("Server=(LOCAL)\\SQLSERVER14;Database=Scrabble;Trusted_Connection=True;Trust Server Certificate=true;Encrypt=false");
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}




