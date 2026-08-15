using AssetManagement.Catalog.Settings;
using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssetManagement.Catalog.Services;

public class CatalogExporter(AssetDbContext db, LibrarySettings settings)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task RegenerateAsync(CancellationToken ct = default)
    {
        var assets = await db.Assets
            .Include(a => a.AssetTags).ThenInclude(at => at.Tag)
            .Include(a => a.CollectionAssets)
            .OrderBy(a => a.Name)
            .ToListAsync(ct);

        var collections = await db.Collections
            .Include(c => c.CollectionAssets)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var tags = await db.Tags.OrderBy(t => t.Name).ToListAsync(ct);

        var taxonomy = tags
            .GroupBy(t => t.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Select(t => t.Name).ToList());

        var catalog = new
        {
            generated_at = DateTime.UtcNow.ToString("O"),
            tag_taxonomy = taxonomy,
            assets = assets.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                type = a.Type.ToString(),
                version = a.Version,
                tags = a.AssetTags.Select(at => at.Tag.Name).OrderBy(n => n).ToList(),
                description = a.Description,
                filename = a.Filename,
                meta = ParseMeta(a.MetaJson)
            }),
            collections = collections.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                asset_ids = c.CollectionAssets.Select(ca => ca.AssetId).ToList()
            })
        };

        Directory.CreateDirectory(Path.GetDirectoryName(settings.CatalogJsonPath)!);
        var json = JsonSerializer.Serialize(catalog, JsonOptions);
        await File.WriteAllTextAsync(settings.CatalogJsonPath, json, ct);
    }

    private static object? ParseMeta(string? metaJson)
    {
        if (string.IsNullOrWhiteSpace(metaJson)) return null;
        try { return JsonSerializer.Deserialize<object>(metaJson); }
        catch { return null; }
    }
}
