using AssetManagement.Core.Enums;

namespace AssetManagement.Core.Models;

public class Asset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? MetaJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<AssetTag> AssetTags { get; set; } = [];
    public ICollection<CollectionAsset> CollectionAssets { get; set; } = [];
    public ICollection<UsageLog> UsageLogs { get; set; } = [];
}
