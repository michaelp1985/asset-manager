namespace AssetManagement.Catalog.Models;

public record AssetImportResult(
    Guid Id,
    string Name,
    string Filename,
    int TagsApplied);
