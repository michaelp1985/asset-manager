using AssetManagement.Catalog.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands;

public sealed class SearchAssetCommand(AssetQueryService queryService) : AsyncCommand<SearchAssetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--tag <name>")]
        [Description("Filter by tag name (partial match)")]
        public string? Tag { get; set; }

        [CommandOption("--type <type>")]
        [Description("Filter by asset type: Spritesheet | Image | Audio | Tileset | Font | Video | Data")]
        public string? Type { get; set; }

        [CommandOption("--name <name>")]
        [Description("Filter by asset name (partial match)")]
        public string? Name { get; set; }

        [CommandOption("--page <number>")]
        [Description("Page number (default: 1)")]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;

        [CommandOption("--size <number>")]
        [Description("Results per page (default: 20)")]
        [DefaultValue(20)]
        public int Size { get; set; } = 20;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Tag is null && settings.Type is null && settings.Name is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Specify at least one of --tag, --type, or --name.");
            return 1;
        }

        try
        {
            var result = await queryService.SearchAsync(
                settings.Tag, settings.Type, settings.Name,
                settings.Page, settings.Size, cancellationToken);

            if (result.TotalCount == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No assets found matching the given filters.[/]");
                return 0;
            }

            var table = new Table()
                .AddColumn("ID")
                .AddColumn("Name")
                .AddColumn("Type")
                .AddColumn("Ver")
                .AddColumn("Tags");

            foreach (var asset in result.Items)
                table.AddRow(
                    asset.Id.ToString(),
                    asset.Name,
                    asset.Type.ToString(),
                    $"v{asset.Version}",
                    string.Join(", ", asset.Tags));

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[grey]Page {result.Page} of {result.TotalPages} ({result.TotalCount} total)[/]");

            if (result.HasMore)
                AnsiConsole.MarkupLine($"[grey]Use --page {result.Page + 1} to see more.[/]");

            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
