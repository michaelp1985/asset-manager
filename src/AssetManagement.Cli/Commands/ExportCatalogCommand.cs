using AssetManagement.Catalog.Services;
using AssetManagement.Catalog.Settings;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AssetManagement.Cli.Commands;

public sealed class ExportCatalogCommand(CatalogExporter exporter, LibrarySettings settings)
    : AsyncCommand<ExportCatalogCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings _, CancellationToken cancellationToken)
    {
        await exporter.RegenerateAsync(cancellationToken);
        AnsiConsole.MarkupLine($"[green]Catalog regenerated:[/] [yellow]{settings.CatalogJsonPath}[/]");
        return 0;
    }
}
