namespace AssetManagement.Catalog.Models;

public record AssetExportResult(Guid AssetId, string Name, string SourceFilename, string DestinationPath, string GameName, int Version);
