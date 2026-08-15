using AssetManagement.Catalog.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands.Collection;

public sealed class AddToCollectionCommand(CollectionService collectionService) : AsyncCommand<AddToCollectionCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<collection-id>")]
        [Description("Collection ID")]
        public int CollectionId { get; set; }

        [CommandArgument(1, "<asset-id>")]
        [Description("Asset ID (GUID)")]
        public string AssetId { get; set; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settings.AssetId, out var assetId))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] '{settings.AssetId}' is not a valid asset ID.");
            return 1;
        }

        try
        {
            await collectionService.AddAssetAsync(settings.CollectionId, assetId, cancellationToken);
            AnsiConsole.MarkupLine($"[green]Asset added to collection {settings.CollectionId}.[/]");
        }
        catch (KeyNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        return 0;
    }
}
