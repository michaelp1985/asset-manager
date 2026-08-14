namespace AssetManagement.Core.Models;

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<CollectionAsset> CollectionAssets { get; set; } = [];
}
