using AssetManagement.Catalog.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AssetManagement.Cli.Commands.Collection;

public sealed class CreateCollectionCommand(CollectionService collectionService) : AsyncCommand<CreateCollectionCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--name <name>")]
        [Description("Collection name")]
        public required string Name { get; set; }

        [CommandOption("--desc <description>")]
        [Description("Optional description")]
        public string? Description { get; set; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return ValidationResult.Error("--name is required.");
            return ValidationResult.Success();
        }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var result = await collectionService.CreateAsync(settings.Name, settings.Description, cancellationToken);
        AnsiConsole.MarkupLine($"[green]Collection created.[/] ID: [yellow]{result.Id}[/] — {result.Name}");
        return 0;
    }
}
