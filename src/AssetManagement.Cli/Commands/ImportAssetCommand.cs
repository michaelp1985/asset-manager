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
        [Description("Asset type: Spritesheet | Image | Audio | Tileset | Font | Video | Data. Omit to detect from the file extension.")]
        public string? Type { get; set; }

        [CommandOption("--desc <description>")]
        [Description("Description written for LLM catalog consumption")]
        public string? Description { get; set; }

        [CommandOption("--tags <tags>")]
        [Description("Comma-separated name:Category pairs, e.g. space:Theme,character:Content")]
        public string? Tags { get; set; }

        [CommandOption("--meta <json>")]
        [Description("Optional JSON metadata blob (frames, dimensions, duration, etc.)")]
        public string? MetaJson { get; set; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return ValidationResult.Error("--name is required.");
            return ValidationResult.Success();
        }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AssetType assetType;
        if (!string.IsNullOrWhiteSpace(settings.Type))
        {
            if (!Enum.TryParse(settings.Type, ignoreCase: true, out assetType))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Unknown asset type '{settings.Type}'. Valid values: {string.Join(", ", Enum.GetNames<AssetType>())}");
                return 1;
            }
        }
        else
        {
            var detected = DetectTypeFromExtension(settings.FilePath);
            if (detected is not { } detectedType)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Could not detect asset type from the file extension. Pass --type explicitly (see --help for valid values).");
                return 1;
            }

            if (!AnsiConsole.Confirm($"Detected type: [yellow]{detectedType}[/]. Use this?"))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Re-run with --type explicitly (see --help for valid values).");
                return 1;
            }

            assetType = detectedType;
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
            Description: settings.Description ?? string.Empty,
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
        catch (InvalidOperationException ex)
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

    private static AssetType? DetectTypeFromExtension(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" or ".tga" or ".tiff" => AssetType.Image,
            ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" => AssetType.Audio,
            ".mp4" or ".webm" or ".mov" or ".avi" or ".mkv" => AssetType.Video,
            ".ttf" or ".otf" or ".woff" or ".woff2" => AssetType.Font,
            ".json" or ".xml" or ".csv" or ".yaml" or ".yml" => AssetType.Data,
            _ => null
        };
}
