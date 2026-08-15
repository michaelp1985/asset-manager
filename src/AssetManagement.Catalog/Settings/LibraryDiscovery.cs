namespace AssetManagement.Catalog.Settings;

public static class LibraryDiscovery
{
    private const string MarkerFile = ".assetlibrary";

    /// <summary>
    /// Resolves the library root via ASSET_LIBRARY_ROOT env var first,
    /// then by walking up the directory tree looking for a .assetlibrary marker file.
    /// </summary>
    public static string? FindLibraryRoot()
    {
        var envRoot = Environment.GetEnvironmentVariable("ASSET_LIBRARY_ROOT");
        if (!string.IsNullOrEmpty(envRoot))
            return envRoot;

        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, MarkerFile)))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }
}
