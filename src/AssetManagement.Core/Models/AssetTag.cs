namespace AssetManagement.Core.Models;

public class AssetTag
{
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
