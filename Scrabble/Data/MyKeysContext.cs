using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace Scrabble.Server.Data;

public class MyKeysContext : DbContext, IDataProtectionKeyContext
{
    // A recommended constructor overload when using EF Core 
    // with dependency injection.
    public MyKeysContext(DbContextOptions<MyKeysContext> options)
        : base(options) {
        Database.Migrate();
    }

    // This maps to the table that stores keys.
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
}

