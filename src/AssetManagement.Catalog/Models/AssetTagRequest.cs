namespace AssetManagement.Catalog.Models;

public record AssetTagRequest(Guid AssetId, IReadOnlyList<TagInput> Add, IReadOnlyList<string> Remove);
