using AssetManagement.Core.Enums;

namespace AssetManagement.Core.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TagCategory Category { get; set; }

    public ICollection<AssetTag> AssetTags { get; set; } = [];
}
