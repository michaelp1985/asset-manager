using AssetManagement.Catalog.Models;
using AssetManagement.Catalog.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands;

public sealed class UpdateAssetCommand(AssetEditService editService) : AsyncCommand<UpdateAssetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Asset ID (GUID)")]
        public string Id { get; set; } = string.Empty;

        [CommandOption("--name <name>")]
        [Description("New name for the asset")]
        public string? Name { get; set; }

        [CommandOption("--desc <description>")]
        [Description("New description")]
        public string? Description { get; set; }

        [CommandOption("--meta <json>")]
        [Description("Replacement JSON metadata blob")]
        public string? MetaJson { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settings.Id, out var assetId))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] '{settings.Id}' is not a valid asset ID.");
            return 1;
        }

        if (settings.Name is null && settings.Description is null && settings.MetaJson is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Specify at least one of --name, --desc, or --meta.");
            return 1;
        }

        var request = new AssetUpdateRequest(assetId, settings.Name, settings.Description, settings.MetaJson);

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Updating asset...", _ => editService.UpdateAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Asset updated.[/] catalog.json regenerated.");
        return 0;
    }
}
