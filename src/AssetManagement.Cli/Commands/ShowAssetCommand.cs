using AssetManagement.Catalog.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands;

public sealed class ShowAssetCommand(AssetQueryService queryService) : AsyncCommand<ShowAssetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Asset ID (GUID)")]
        public string Id { get; set; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settings.Id, out var assetId))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] '{settings.Id}' is not a valid asset ID.");
            return 1;
        }

        var asset = await queryService.GetByIdAsync(assetId, cancellationToken);
        if (asset is null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Asset '{assetId}' not found.");
            return 1;
        }

        // Core fields
        var details = new Table().HideHeaders().AddColumn("Field").AddColumn("Value");
        details.AddRow("[grey]ID[/]", asset.Id.ToString());
        details.AddRow("[grey]Name[/]", asset.Name);
        details.AddRow("[grey]Type[/]", asset.Type.ToString());
        details.AddRow("[grey]Version[/]", $"v{asset.Version}");
        details.AddRow("[grey]Filename[/]", asset.Filename);
        details.AddRow("[grey]Tags[/]", string.Join(", ", asset.Tags));
        details.AddRow("[grey]Description[/]", asset.Description);
        if (asset.MetaJson is not null)
            details.AddRow("[grey]Meta[/]", asset.MetaJson);
        details.AddRow("[grey]Created[/]", asset.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        details.AddRow("[grey]Updated[/]", asset.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        AnsiConsole.Write(details);

        // Usage history
        if (asset.UsageHistory.Count == 0)
        {
            AnsiConsole.MarkupLine("\n[grey]No usage history.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("\n[grey]Usage History[/]");
            var usage = new Table()
                .AddColumn("Game")
                .AddColumn("Ver")
                .AddColumn("Destination")
                .AddColumn("Exported At");

            foreach (var log in asset.UsageHistory)
                usage.AddRow(
                    log.GameName,
                    $"v{log.VersionAtExport}",
                    log.DestinationPath,
                    log.ExportedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));

            AnsiConsole.Write(usage);
        }

        return 0;
    }
}
