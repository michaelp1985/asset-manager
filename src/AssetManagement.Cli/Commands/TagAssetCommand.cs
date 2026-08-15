using AssetManagement.Catalog.Models;
using AssetManagement.Catalog.Services;
using AssetManagement.Core.Enums;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands;

public sealed class TagAssetCommand(AssetEditService editService) : AsyncCommand<TagAssetCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        [Description("Asset ID (GUID)")]
        public string Id { get; set; } = string.Empty;

        [CommandOption("--add <tags>")]
        [Description("Comma-separated name:Category pairs to add, e.g. looping:Attribute,fantasy:Theme")]
        public string? Add { get; set; }

        [CommandOption("--remove <names>")]
        [Description("Comma-separated tag names to remove, e.g. wizard,fantasy")]
        public string? Remove { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(settings.Id, out var assetId))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] '{settings.Id}' is not a valid asset ID.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.Add) && string.IsNullOrWhiteSpace(settings.Remove))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Specify at least one of --add or --remove.");
            return 1;
        }

        List<TagInput> add = [];
        if (!string.IsNullOrWhiteSpace(settings.Add))
        {
            foreach (var part in settings.Add.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var split = part.Split(':', 2);
                if (split.Length != 2 || !Enum.TryParse<TagCategory>(split[1], ignoreCase: true, out var category))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Invalid tag format '{part}'. Expected name:Category (e.g. looping:Attribute).");
                    return 1;
                }
                add.Add(new TagInput(split[0].Trim(), category));
            }
        }

        var remove = string.IsNullOrWhiteSpace(settings.Remove)
            ? []
            : settings.Remove.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var request = new AssetTagRequest(assetId, add, remove);

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Updating tags...", _ => editService.ApplyTagsAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Tags updated.[/] catalog.json regenerated.");
        return 0;
    }
}
