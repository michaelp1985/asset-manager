using AssetManagement.Catalog.Models;
using AssetManagement.Catalog.Settings;
using AssetManagement.Core.Models;
using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AssetManagement.Catalog.Services;

public class AssetImportService(AssetDbContext db, LibrarySettings settings, CatalogExporter catalogExporter)
{
    public async Task<AssetImportResult> ImportAsync(AssetImportRequest request, CancellationToken ct = default)
    {
        if (!File.Exists(request.FilePath))
            throw new FileNotFoundException($"Asset file not found: {request.FilePath}");

        Directory.CreateDirectory(settings.AssetsPath);

        var id = Guid.NewGuid();
        var slug = Slugify(request.Name);
        var ext = Path.GetExtension(request.FilePath);
        var filename = $"{id}_{slug}{ext}";
        var destination = Path.Combine(settings.AssetsPath, filename);

        string contentHash;
        await using (var sourceStream = File.OpenRead(request.FilePath))
        await using (var destStream = File.Create(destination))
        using (var sha256 = SHA256.Create())
        await using (var cryptoStream = new CryptoStream(destStream, sha256, CryptoStreamMode.Write))
        {
            await sourceStream.CopyToAsync(cryptoStream, ct);
            await cryptoStream.FlushFinalBlockAsync(ct);
            contentHash = Convert.ToHexString(sha256.Hash!);
        }

        try
        {
            var existing = await db.Assets.FirstOrDefaultAsync(a => a.ContentHash == contentHash, ct);
            if (existing is not null)
                throw new InvalidOperationException(
                    $"This file is already in the library as '{existing.Name}' (Id: {existing.Id}).");

            var now = DateTime.UtcNow;
            var asset = new Asset
            {
                Id = id,
                Name = request.Name,
                Type = request.Type,
                Description = request.Description,
                Filename = filename,
                Version = 1,
                MetaJson = request.MetaJson,
                ContentHash = contentHash,
                CreatedAt = now,
                UpdatedAt = now
            };

            await db.Assets.AddAsync(asset, ct);

            foreach (var tagInput in request.Tags)
            {
                var tag = await db.Tags.FirstOrDefaultAsync(
                    t => t.Name == tagInput.Name && t.Category == tagInput.Category, ct)
                    ?? await CreateTagAsync(tagInput, ct);

                await db.AssetTags.AddAsync(new AssetTag { AssetId = id, TagId = tag.Id }, ct);
            }

            await db.SaveChangesAsync(ct);
        }
        catch
        {
            File.Delete(destination);
            throw;
        }

        await catalogExporter.RegenerateAsync(ct);

        return new AssetImportResult(id, request.Name, filename, request.Tags.Count);
    }

    private async Task<Tag> CreateTagAsync(TagInput input, CancellationToken ct)
    {
        var tag = new Tag { Name = input.Name, Category = input.Category };
        await db.Tags.AddAsync(tag, ct);
        await db.SaveChangesAsync(ct);
        return tag;
    }

    private static string Slugify(string name)
    {
        var slug = name.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}
