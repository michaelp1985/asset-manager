using AssetManagement.Catalog.Extensions;
using AssetManagement.Catalog.Settings;
using AssetManagement.Cli.Commands;
using AssetManagement.Cli.Commands.Collection;
using AssetManagement.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

var libraryRoot = LibraryDiscovery.FindLibraryRoot();
if (libraryRoot is null)
{
    AnsiConsole.MarkupLine("[red]Error:[/] Could not locate asset library. Set ASSET_LIBRARY_ROOT or add a [yellow].assetlibrary[/] marker file at the library root.");
    return 1;
}

var services = new ServiceCollection();
services.AddAssetLibrary(libraryRoot);

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("asset");
    config.AddCommand<ImportAssetCommand>("import")
        .WithDescription("Import an asset file into the library");
    config.AddCommand<ExportAssetCommand>("export")
        .WithDescription("Copy an asset into a game project directory and log the usage");
    config.AddCommand<TagAssetCommand>("tag")
        .WithDescription("Add or remove tags on an existing asset");
    config.AddCommand<UpdateAssetCommand>("update")
        .WithDescription("Update an asset's name, description, or metadata");
    config.AddCommand<SearchAssetCommand>("search")
        .WithDescription("Search assets by tag, type, or name (at least one filter required)");
    config.AddCommand<ShowAssetCommand>("show")
        .WithDescription("Show full detail for a single asset including usage history");
    config.AddBranch("collection", branch =>
    {
        branch.AddCommand<CreateCollectionCommand>("create")
            .WithDescription("Create a new collection");
        branch.AddCommand<AddToCollectionCommand>("add")
            .WithDescription("Add an asset to a collection");
        branch.AddCommand<RemoveFromCollectionCommand>("remove")
            .WithDescription("Remove an asset from a collection");
        branch.AddCommand<ShowCollectionsCommand>("show")
            .WithDescription("List all collections with asset counts");
    });
    config.AddBranch("catalog", branch =>
    {
        branch.AddCommand<ExportCatalogCommand>("export")
            .WithDescription("Regenerate catalog.json from the current database state");
    });
});

return await app.RunAsync(args);
