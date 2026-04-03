using ContentDAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContentDAL.Connection;

public class ContentDbContextFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    public ContentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContentDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=contentdb;Username=postgres;Password=1234567890");

        return new ContentDbContext(optionsBuilder.Options);
    }
}