using AssetManagement.Catalog.Models;
using AssetManagement.Catalog.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands;

public sealed class ExportAssetCommand(AssetExportService exportService) : AsyncCommand<ExportAssetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Asset ID (GUID) to export")]
        public string Id { get; set; } = string.Empty;

        [CommandArgument(1, "<destination>")]
        [Description("Destination directory to copy the asset into")]
        public string DestinationDir { get; set; } = string.Empty;

        [CommandOption("--game <name>")]
        [Description("Name of the game project receiving the asset")]
        public required string GameName { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settings.Id, out var assetId))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] '{settings.Id}' is not a valid asset ID.");
            return 1;
        }

        var request = new AssetExportRequest(assetId, Path.GetFullPath(settings.DestinationDir), settings.GameName);

        AssetExportResult result;
        try
        {
            result = await AnsiConsole.Status()
                .StartAsync("Exporting asset...", _ => exportService.ExportAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        var table = new Table()
            .AddColumn("ID")
            .AddColumn("Name")
            .AddColumn("Version")
            .AddColumn("Destination");

        table.AddRow(result.AssetId.ToString(), result.Name, $"v{result.Version}", result.DestinationPath);
        AnsiConsole.Write(table);

        return 0;
    }
}
