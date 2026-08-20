using AssetManagement.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Data.Contexts;

public class AssetDbContext(DbContextOptions<AssetDbContext> options) : DbContext(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<AssetTag> AssetTags => Set<AssetTag>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<CollectionAsset> CollectionAssets => Set<CollectionAsset>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetTag>()
            .HasKey(at => new { at.AssetId, at.TagId });

        modelBuilder.Entity<AssetTag>()
            .HasOne(at => at.Asset)
            .WithMany(a => a.AssetTags)
            .HasForeignKey(at => at.AssetId);

        modelBuilder.Entity<AssetTag>()
            .HasOne(at => at.Tag)
            .WithMany(t => t.AssetTags)
            .HasForeignKey(at => at.TagId);

        modelBuilder.Entity<CollectionAsset>()
            .HasKey(ca => new { ca.CollectionId, ca.AssetId });

        modelBuilder.Entity<CollectionAsset>()
            .HasOne(ca => ca.Collection)
            .WithMany(c => c.CollectionAssets)
            .HasForeignKey(ca => ca.CollectionId);

        modelBuilder.Entity<CollectionAsset>()
            .HasOne(ca => ca.Asset)
            .WithMany(a => a.CollectionAssets)
            .HasForeignKey(ca => ca.AssetId);

        modelBuilder.Entity<UsageLog>()
            .HasOne(ul => ul.Asset)
            .WithMany(a => a.UsageLogs)
            .HasForeignKey(ul => ul.AssetId);

        modelBuilder.Entity<Tag>()
            .HasIndex(t => new { t.Name, t.Category })
            .IsUnique();

        modelBuilder.Entity<Asset>()
            .HasIndex(a => a.ContentHash)
            .IsUnique()
            .HasFilter("\"ContentHash\" IS NOT NULL");

        modelBuilder.Entity<Asset>()
            .Property(a => a.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Tag>()
            .Property(t => t.Category)
            .HasConversion<string>();
    }
}
