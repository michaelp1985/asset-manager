using AssetManagement.Core.Enums;

namespace AssetManagement.Catalog.Models;

public record AssetImportRequest(
    string FilePath,
    string Name,
    AssetType Type,
    string Description,
    IReadOnlyList<TagInput> Tags,
    string? MetaJson = null);
