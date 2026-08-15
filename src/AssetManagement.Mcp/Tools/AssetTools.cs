using AssetManagement.Catalog.Models;
using AssetManagement.Catalog.Services;
using AssetManagement.Core.Enums;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace AssetManagement.Mcp.Tools;

[McpServerToolType]
public class AssetTools(AssetImportService importService, AssetExportService exportService, CollectionService collectionService, AssetQueryService queryService)
{
    [McpServerTool, Description("Import an asset file into the library. Returns the created asset record as JSON.")]
    public async Task<string> ImportAsset(
        [Description("Absolute path to the asset file")] string filePath,
        [Description("Human-readable name for the asset")] string name,
        [Description("Asset type: Spritesheet | Image | Audio | Tileset | Font | Video | Data")] string type,
        [Description("Description written for LLM catalog consumption")] string description,
        [Description("Comma-separated tags in name:Category format, e.g. space:Theme,character:Content")] string? tags = null,
        [Description("Optional JSON metadata blob (frames, dimensions, duration, etc.)")] string? metaJson = null)
    {
        if (!Enum.TryParse<AssetType>(type, ignoreCase: true, out var assetType))
            throw new ArgumentException($"Unknown asset type '{type}'. Valid values: {string.Join(", ", Enum.GetNames<AssetType>())}");

        var tagInputs = ParseTags(tags);

        var request = new AssetImportRequest(
            FilePath: filePath,
            Name: name,
            Type: assetType,
            Description: description,
            Tags: tagInputs,
            MetaJson: metaJson);

        var result = await importService.ImportAsync(request);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Export an asset from the library into a game project directory. Returns the export record as JSON.")]
    public async Task<string> ExportAsset(
        [Description("Asset ID (GUID) to export")] string assetId,
        [Description("Absolute path to the destination directory in the game project")] string destinationDir,
        [Description("Name of the game project receiving the asset")] string gameName)
    {
        if (!Guid.TryParse(assetId, out var guid))
            throw new ArgumentException($"'{assetId}' is not a valid asset ID.");

        var request = new AssetExportRequest(guid, destinationDir, gameName);
        var result = await exportService.ExportAsync(request);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Create a new asset collection. Returns the created collection as JSON.")]
    public async Task<string> CreateCollection(
        [Description("Collection name")] string name,
        [Description("Optional description")] string? description = null)
    {
        var result = await collectionService.CreateAsync(name, description);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Add an asset to a collection.")]
    public async Task AddAssetToCollection(
        [Description("Collection ID")] int collectionId,
        [Description("Asset ID (GUID)")] string assetId)
    {
        if (!Guid.TryParse(assetId, out var guid))
            throw new ArgumentException($"'{assetId}' is not a valid asset ID.");
        await collectionService.AddAssetAsync(collectionId, guid);
    }

    [McpServerTool, Description("Remove an asset from a collection.")]
    public async Task RemoveAssetFromCollection(
        [Description("Collection ID")] int collectionId,
        [Description("Asset ID (GUID)")] string assetId)
    {
        if (!Guid.TryParse(assetId, out var guid))
            throw new ArgumentException($"'{assetId}' is not a valid asset ID.");
        await collectionService.RemoveAssetAsync(collectionId, guid);
    }

    [McpServerTool, Description("List all collections with their asset counts. Returns JSON array.")]
    public async Task<string> GetCollections()
    {
        var result = await collectionService.GetAllAsync();
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Search assets by tag, type, or name. At least one filter required. Returns paginated JSON.")]
    public async Task<string> SearchAssets(
        [Description("Filter by tag name (partial match)")] string? tag = null,
        [Description("Filter by asset type: Spritesheet | Image | Audio | Tileset | Font | Video | Data")] string? type = null,
        [Description("Filter by asset name (partial match)")] string? name = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Results per page (default: 20)")] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(tag) && string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Specify at least one of: tag, type, or name.");

        var result = await queryService.SearchAsync(tag, type, name, page, pageSize);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get full detail for a single asset including tags, metadata, and usage history. Returns JSON, or null if not found.")]
    public async Task<string?> GetAsset(
        [Description("Asset ID (GUID)")] string assetId)
    {
        if (!Guid.TryParse(assetId, out var guid))
            throw new ArgumentException($"'{assetId}' is not a valid asset ID.");

        var result = await queryService.GetByIdAsync(guid);
        return result is null ? null : JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static List<TagInput> ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return [];

        return tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part =>
            {
                var split = part.Split(':', 2);
                if (split.Length != 2 || !Enum.TryParse<TagCategory>(split[1], ignoreCase: true, out var category))
                    throw new ArgumentException($"Invalid tag format '{part}'. Expected name:Category (e.g. space:Theme).");
                return new TagInput(split[0].Trim(), category);
            })
            .ToList();
    }
}
