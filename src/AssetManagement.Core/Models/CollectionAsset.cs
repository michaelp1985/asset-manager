namespace AssetManagement.Core.Models;

public class CollectionAsset
{
    public int CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
}
