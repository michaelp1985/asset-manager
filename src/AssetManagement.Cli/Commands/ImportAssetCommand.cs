using AssetManagement.Catalog.Models;
using AssetManagement.Catalog.Services;
using AssetManagement.Core.Enums;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands;

public class ImportAssetCommand(AssetImportService importService) : AsyncCommand<ImportAssetCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<file>")]
        [Description("Path to the asset file to import")]
        public string FilePath { get; set; } = string.Empty;

        [CommandOption("--name <name>")]
        [Description("Human-readable name for the asset")]
        public required string Name { get; set; }

        [CommandOption("--type <type>")]
        [Description("Asset type: Spritesheet | Image | Audio | Tileset | Font | Video | Data")]
        public required string Type { get; set; }

        [CommandOption("--desc <description>")]
        [Description("Description written for LLM catalog consumption")]
        public required string Description { get; set; }

        [CommandOption("--tags <tags>")]
        [Description("Comma-separated name:Category pairs, e.g. space:Theme,character:Content")]
        public string? Tags { get; set; }

        [CommandOption("--meta <json>")]
        [Description("Optional JSON metadata blob (frames, dimensions, duration, etc.)")]
        public string? MetaJson { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AssetType>(settings.Type, ignoreCase: true, out var assetType))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Unknown asset type '{settings.Type}'. Valid values: {string.Join(", ", Enum.GetNames<AssetType>())}");
            return 1;
        }

        List<TagInput> tags = [];
        if (!string.IsNullOrWhiteSpace(settings.Tags))
        {
            foreach (var part in settings.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var split = part.Split(':', 2);
                if (split.Length != 2 || !Enum.TryParse<TagCategory>(split[1], ignoreCase: true, out var category))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Invalid tag format '{part}'. Expected name:Category (e.g. space:Theme).");
                    return 1;
                }
                tags.Add(new TagInput(split[0].Trim(), category));
            }
        }

        var request = new AssetImportRequest(
            FilePath: Path.GetFullPath(settings.FilePath),
            Name: settings.Name,
            Type: assetType,
            Description: settings.Description,
            Tags: tags,
            MetaJson: settings.MetaJson);

        AssetImportResult result;
        try
        {
            result = await AnsiConsole.Status()
                .StartAsync("Importing asset...", _ => importService.ImportAsync(request, cancellationToken));
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        var table = new Table()
            .AddColumn("ID")
            .AddColumn("Name")
            .AddColumn("Filename")
            .AddColumn("Tags Applied");

        table.AddRow(result.Id.ToString(), result.Name, result.Filename, result.TagsApplied.ToString());
        AnsiConsole.Write(table);

        return 0;
    }
}
