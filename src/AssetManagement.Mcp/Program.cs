using AssetManagement.Catalog.Extensions;
using AssetManagement.Catalog.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

var libraryRoot = LibraryDiscovery.FindLibraryRoot();
if (libraryRoot is null)
{
    Console.Error.WriteLine("Error: Could not locate asset library. Set ASSET_LIBRARY_ROOT or add a .assetlibrary marker file at the library root.");
    return 1;
}

var builder = Host.CreateApplicationBuilder(args);

// stdio is the MCP transport — any log output on stdout corrupts the JSON-RPC stream.
// Host.CreateApplicationBuilder registers a default console provider that logs to stdout;
// replace it rather than stacking a second provider on top.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddAssetLibrary(libraryRoot);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
return 0;
