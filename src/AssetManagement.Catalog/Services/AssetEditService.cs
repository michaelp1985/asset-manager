using AssetManagement.Catalog.Models;
using AssetManagement.Core.Models;
using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Catalog.Services;

public class AssetEditService(AssetDbContext db, CatalogExporter exporter)
{
    public async Task<int> ApplyTagsAsync(AssetTagRequest request, CancellationToken ct = default)
    {
        var asset = await db.Assets
            .Include(a => a.AssetTags).ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == request.AssetId, ct)
            ?? throw new KeyNotFoundException($"Asset '{request.AssetId}' not found.");

        // Remove by name (case-insensitive, removes all categories with that name)
        if (request.Remove.Count > 0)
        {
            var removeSet = request.Remove.Select(n => n.ToLowerInvariant()).ToHashSet();
            var toRemove = asset.AssetTags
                .Where(at => removeSet.Contains(at.Tag.Name.ToLowerInvariant()))
                .ToList();
            db.AssetTags.RemoveRange(toRemove);
        }

        // Add: find-or-create each tag, skip if already linked
        var existingTagIds = asset.AssetTags.Select(at => at.TagId).ToHashSet();
        int added = 0;

        foreach (var input in request.Add)
        {
            var tag = await db.Tags.FirstOrDefaultAsync(
                t => t.Name == input.Name && t.Category == input.Category, ct);

            if (tag is null)
            {
                tag = new Tag { Name = input.Name, Category = input.Category };
                db.Tags.Add(tag);
                await db.SaveChangesAsync(ct);
            }

            if (!existingTagIds.Contains(tag.Id))
            {
                db.AssetTags.Add(new AssetTag { AssetId = asset.Id, TagId = tag.Id });
                existingTagIds.Add(tag.Id);
                added++;
            }
        }

        asset.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await exporter.RegenerateAsync(ct);

        return added;
    }

    public async Task UpdateAsync(AssetUpdateRequest request, CancellationToken ct = default)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, ct)
            ?? throw new KeyNotFoundException($"Asset '{request.AssetId}' not found.");

        if (request.Name is not null) asset.Name = request.Name;
        if (request.Description is not null) asset.Description = request.Description;
        if (request.MetaJson is not null) asset.MetaJson = request.MetaJson;
        asset.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await exporter.RegenerateAsync(ct);
    }
}
