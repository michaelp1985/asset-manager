using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssetManagement.Data.Contexts;

public class AssetDbContextFactory : IDesignTimeDbContextFactory<AssetDbContext>
{
    public AssetDbContext CreateDbContext(string[] args)
    {
        var catalogDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "catalog");
        var dbPath = Path.GetFullPath(Path.Combine(catalogDir, "catalog.db"));

        var options = new DbContextOptionsBuilder<AssetDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new AssetDbContext(options);
    }
}
