namespace AssetManagement.Catalog.Models;

public record AssetExportRequest(Guid AssetId, string DestinationDir, string GameName);
