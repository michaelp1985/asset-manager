namespace AssetManagement.Catalog.Settings;

public class LibrarySettings
{
    public string LibraryRoot { get; init; } = string.Empty;
    public string AssetsPath => Path.Combine(LibraryRoot, "assets");
    public string DbPath => Path.Combine(LibraryRoot, "catalog", "catalog.db");
    public string CatalogJsonPath => Path.Combine(LibraryRoot, "catalog", "catalog.json");
}
