namespace AssetManagement.Catalog.Models;

public record AssetUpdateRequest(Guid AssetId, string? Name, string? Description, string? MetaJson);
