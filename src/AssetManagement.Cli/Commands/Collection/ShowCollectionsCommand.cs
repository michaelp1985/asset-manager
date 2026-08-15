using AssetManagement.Catalog.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AssetManagement.Cli.Commands.Collection;

public sealed class ShowCollectionsCommand(CollectionService collectionService) : AsyncCommand<ShowCollectionsCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings _, CancellationToken cancellationToken)
    {
        var collections = await collectionService.GetAllAsync(cancellationToken);

        if (collections.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No collections found.[/]");
            return 0;
        }

        var table = new Table()
            .AddColumn("ID")
            .AddColumn("Name")
            .AddColumn("Description")
            .AddColumn("Assets");

        foreach (var c in collections)
            table.AddRow(c.Id.ToString(), c.Name, c.Description ?? string.Empty, c.AssetCount.ToString());

        AnsiConsole.Write(table);
        return 0;
    }
}
