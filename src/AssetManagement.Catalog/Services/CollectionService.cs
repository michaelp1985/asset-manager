using AssetManagement.Catalog.Models;
using AssetManagement.Core.Models;
using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Catalog.Services;

public class CollectionService(AssetDbContext db, CatalogExporter exporter)
{
    public async Task<CollectionSummary> CreateAsync(string name, string? description, CancellationToken ct = default)
    {
        var collection = new Collection { Name = name, Description = description };
        db.Collections.Add(collection);
        await db.SaveChangesAsync(ct);
        await exporter.RegenerateAsync(ct);
        return new CollectionSummary(collection.Id, collection.Name, collection.Description, 0);
    }

    public async Task AddAssetAsync(int collectionId, Guid assetId, CancellationToken ct = default)
    {
        await RequireCollectionAsync(collectionId, ct);
        await RequireAssetAsync(assetId, ct);

        var exists = await db.CollectionAssets.AnyAsync(
            ca => ca.CollectionId == collectionId && ca.AssetId == assetId, ct);

        if (!exists)
        {
            db.CollectionAssets.Add(new CollectionAsset { CollectionId = collectionId, AssetId = assetId });
            await db.SaveChangesAsync(ct);
            await exporter.RegenerateAsync(ct);
        }
    }

    public async Task RemoveAssetAsync(int collectionId, Guid assetId, CancellationToken ct = default)
    {
        var link = await db.CollectionAssets.FirstOrDefaultAsync(
            ca => ca.CollectionId == collectionId && ca.AssetId == assetId, ct);

        if (link is not null)
        {
            db.CollectionAssets.Remove(link);
            await db.SaveChangesAsync(ct);
            await exporter.RegenerateAsync(ct);
        }
    }

    public async Task<IReadOnlyList<CollectionSummary>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Collections
            .Include(c => c.CollectionAssets)
            .OrderBy(c => c.Name)
            .Select(c => new CollectionSummary(
                c.Id, c.Name, c.Description,
                c.CollectionAssets.Count))
            .ToListAsync(ct);
    }

    private async Task RequireCollectionAsync(int id, CancellationToken ct)
    {
        if (!await db.Collections.AnyAsync(c => c.Id == id, ct))
            throw new KeyNotFoundException($"Collection '{id}' not found.");
    }

    private async Task RequireAssetAsync(Guid id, CancellationToken ct)
    {
        if (!await db.Assets.AnyAsync(a => a.Id == id, ct))
            throw new KeyNotFoundException($"Asset '{id}' not found.");
    }
}
