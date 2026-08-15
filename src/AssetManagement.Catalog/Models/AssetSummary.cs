using AssetManagement.Core.Enums;

namespace AssetManagement.Catalog.Models;

public record AssetSummary(Guid Id, string Name, AssetType Type, int Version, IReadOnlyList<string> Tags);
