using AssetManagement.Core.Enums;

namespace AssetManagement.Catalog.Models;

public record AssetDetail(
    Guid Id,
    string Name,
    AssetType Type,
    int Version,
    string Description,
    string Filename,
    IReadOnlyList<string> Tags,
    string? MetaJson,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<UsageLogSummary> UsageHistory);

public record UsageLogSummary(string GameName, int VersionAtExport, string DestinationPath, DateTime ExportedAt);
