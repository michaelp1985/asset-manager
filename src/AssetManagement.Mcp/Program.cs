using AssetManagement.Catalog.Extensions;
using AssetManagement.Catalog.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var libraryRoot = LibraryDiscovery.FindLibraryRoot();
if (libraryRoot is null)
{
    Console.Error.WriteLine("Error: Could not locate asset library. Set ASSET_LIBRARY_ROOT or add a .assetlibrary marker file at the library root.");
    return 1;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAssetLibrary(libraryRoot);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
return 0;
