using AssetManagement.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

namespace AssetManagement.Cli.Commands;

public static class InitCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        string? path = null;
        bool nonInteractive = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--path" && i + 1 < args.Length)
                path = args[++i];
            else if (args[i] == "--non-interactive")
                nonInteractive = true;
        }

        if (path is null && nonInteractive)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] --path is required in non-interactive mode.");
            return 1;
        }

        if (path is null)
        {
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AssetLibrary");
            path = AnsiConsole.Ask("Where should your asset library live?", defaultPath);
        }

        path = Path.GetFullPath(path);

        try
        {
            Directory.CreateDirectory(Path.Combine(path, "assets"));
            Directory.CreateDirectory(Path.Combine(path, "catalog"));

            await File.WriteAllTextAsync(Path.Combine(path, ".assetlibrary"), """{"version":1}""");

            var options = new DbContextOptionsBuilder<AssetDbContext>()
                .UseSqlite($"Data Source={Path.Combine(path, "catalog", "catalog.db")}")
                .Options;

            await using var db = new AssetDbContext(options);
            await db.Database.MigrateAsync();

            var installDir = Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;
            var mcpPath = Path.Combine(installDir, "AssetManagement.Mcp");

            AnsiConsole.MarkupLine($"\n[green]Library initialized:[/] [yellow]{path}[/]");
            AnsiConsole.MarkupLine("\n[bold]Add to your MCP server config:[/]");
            AnsiConsole.WriteLine($$"""
{
  "asset-management": {
    "command": "{{mcpPath}}",
    "env": {
      "ASSET_LIBRARY_ROOT": "{{path}}"
    }
  }
}
""");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
