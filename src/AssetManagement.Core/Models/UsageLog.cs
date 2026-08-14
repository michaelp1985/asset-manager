namespace AssetManagement.Core.Models;

public class UsageLog
{
    public int Id { get; set; }
    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public string GameName { get; set; } = string.Empty;
    public int VersionAtExport { get; set; }
    public string DestinationPath { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
}
