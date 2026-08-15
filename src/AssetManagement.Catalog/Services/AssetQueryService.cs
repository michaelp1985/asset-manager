using AssetManagement.Catalog.Models;
using AssetManagement.Core.Enums;
using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Catalog.Services;

public class AssetQueryService(AssetDbContext db)
{
    public async Task<PagedResult<AssetSummary>> SearchAsync(
        string? tag, string? type, string? name,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        AssetType? assetType = null;
        if (type is not null)
        {
            if (!Enum.TryParse<AssetType>(type, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Unknown asset type '{type}'. Valid values: {string.Join(", ", Enum.GetNames<AssetType>())}");
            assetType = parsed;
        }

        var query = db.Assets
            .Include(a => a.AssetTags).ThenInclude(at => at.Tag)
            .AsQueryable();

        if (assetType.HasValue)
            query = query.Where(a => a.Type == assetType.Value);

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(a => EF.Functions.Like(a.Name, $"%{name}%"));

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(a => a.AssetTags.Any(at => EF.Functions.Like(at.Tag.Name, $"%{tag}%")));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var summaries = items.Select(a => new AssetSummary(
            a.Id, a.Name, a.Type, a.Version,
            a.AssetTags.Select(at => at.Tag.Name).OrderBy(n => n).ToList()
        )).ToList();

        return new PagedResult<AssetSummary>(summaries, page, pageSize, total);
    }

    public async Task<AssetDetail?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await db.Assets
            .Include(a => a.AssetTags).ThenInclude(at => at.Tag)
            .Include(a => a.UsageLogs)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (asset is null) return null;

        return new AssetDetail(
            asset.Id, asset.Name, asset.Type, asset.Version,
            asset.Description, asset.Filename,
            asset.AssetTags.Select(at => at.Tag.Name).OrderBy(n => n).ToList(),
            asset.MetaJson,
            asset.CreatedAt, asset.UpdatedAt,
            asset.UsageLogs
                .OrderByDescending(l => l.ExportedAt)
                .Select(l => new UsageLogSummary(l.GameName, l.VersionAtExport, l.DestinationPath, l.ExportedAt))
                .ToList()
        );
    }
}
