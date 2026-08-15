using AssetManagement.Catalog.Models;
using AssetManagement.Catalog.Settings;
using AssetManagement.Core.Models;
using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Catalog.Services;

public class AssetExportService(AssetDbContext db, LibrarySettings settings)
{
    public async Task<AssetExportResult> ExportAsync(AssetExportRequest request, CancellationToken ct = default)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, ct)
            ?? throw new KeyNotFoundException($"Asset '{request.AssetId}' not found.");

        var sourcePath = Path.Combine(settings.AssetsPath, asset.Filename);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"Asset file not found in library: {asset.Filename}", sourcePath);

        Directory.CreateDirectory(request.DestinationDir);

        // Strip the leading GUID + underscore so the game project gets a clean filename
        var exportFilename = asset.Filename[37..];
        var destinationPath = Path.Combine(request.DestinationDir, exportFilename);

        File.Copy(sourcePath, destinationPath, overwrite: true);

        db.UsageLogs.Add(new UsageLog
        {
            AssetId = asset.Id,
            GameName = request.GameName,
            VersionAtExport = asset.Version,
            DestinationPath = destinationPath,
            ExportedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);

        return new AssetExportResult(asset.Id, asset.Name, asset.Filename, destinationPath, request.GameName, asset.Version);
    }
}
