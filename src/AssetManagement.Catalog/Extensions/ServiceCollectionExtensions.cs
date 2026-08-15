using AssetManagement.Catalog.Services;
using AssetManagement.Catalog.Settings;
using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManagement.Catalog.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssetLibrary(this IServiceCollection services, string libraryRoot)
    {
        var librarySettings = new LibrarySettings { LibraryRoot = libraryRoot };

        services.AddSingleton(librarySettings);

        services.AddDbContext<AssetDbContext>(options =>
            options.UseSqlite($"Data Source={librarySettings.DbPath}"));

        services.AddScoped<CatalogExporter>();
        services.AddScoped<AssetImportService>();
        services.AddScoped<AssetExportService>();
        services.AddScoped<AssetEditService>();

        return services;
    }
}
