# Asset Management

A standalone game asset library. Assets are imported once, tagged and cataloged in
SQLite, and copied into individual game projects on export — games hold their own
snapshot with no live reference back to the library, so the library can evolve
without breaking anything downstream.

Two front ends share the same catalog:

- **`asset` CLI** — the write path. Import, tag, update, export, and manage
  collections by hand.
- **MCP server** (`AssetManagement.Mcp`) — a read-mostly surface for a game-dev
  AI agent. It can import/export/tag on request, but the intent is that a human
  drives curation through the CLI while an agent searches and pulls assets into
  a game project via MCP.

## How it's organized

```
<library-root>/
  .assetlibrary          marker file that identifies the library root
  assets/                flat file storage, named {id}_{slug}.ext
  catalog/
    catalog.db            SQLite — source of truth
    catalog.json           regenerated after every mutation; what the MCP/agent reads
```

`catalog.json` includes a `tag_taxonomy` block up front, so an LLM has the tag
vocabulary before it sees the asset list.

## Installation

### Windows / Linux installer (recommended)

Installers for both platforms live in `installer/`, but there's no published
release yet — the GitHub Actions packaging job is still on the list (see
`PLAN-PUBLISH.md`). Until then, build them locally.

**Windows** — needs the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [NSIS 3](https://nsis.sourceforge.io/) (`makensis` on `PATH`):

```bash
dotnet publish src/AssetManagement.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win/cli
dotnet publish src/AssetManagement.Mcp -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/win/mcp
makensis installer/windows/setup.nsi
```

Run the resulting `dist/win/AssetManagementSetup.exe`. It installs both
binaries, adds them to `PATH`, and runs `asset init` for you — the finish page
offers the MCP config snippet to paste into your client.

**Linux** — needs the [.NET 10 SDK](https://dotnet.microsoft.com/download):

```bash
dotnet publish src/AssetManagement.Cli -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o dist/linux/cli
dotnet publish src/AssetManagement.Mcp -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o dist/linux/mcp
cp dist/linux/cli/asset dist/linux/mcp/AssetManagement.Mcp installer/linux/install.sh dist/linux/
cd dist/linux && ./install.sh
```

The script installs both binaries, symlinks `asset` onto your `PATH`, and runs
`asset init` for you, printing the MCP config snippet at the end.

### Build from source

For development, or if you'd rather skip the installers:

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
git clone <repo-url>
cd AssetManagement
dotnet build
```

### Set up a library

Run `asset init` to create a new library. It creates `assets/` and `catalog/`,
writes the `.assetlibrary` marker, and runs the database migrations.

```bash
dotnet run --project src/AssetManagement.Cli -- init
```

Follow the prompt for a library path, or run non-interactively:

```bash
dotnet run --project src/AssetManagement.Cli -- init --path /path/to/library --non-interactive
```

### Run the CLI

The CLI finds its library root one of two ways:

1. `ASSET_LIBRARY_ROOT` environment variable, checked first
2. Walking up from the current directory looking for a `.assetlibrary` marker

```bash
export ASSET_LIBRARY_ROOT=/path/to/library
dotnet run --project src/AssetManagement.Cli -- search --type Image
```

For everyday use without building an installer, publish a binary and put it
on your `PATH`:

```bash
dotnet publish src/AssetManagement.Cli -c Release -o ~/.local/share/asset-management/cli
ln -s ~/.local/share/asset-management/cli/asset ~/.local/bin/asset
```

### Run the MCP server

```bash
dotnet publish src/AssetManagement.Mcp -c Release -o ~/.local/share/asset-management/mcp
```

Point your MCP client at the published binary, with the library root in its env:

```json
{
  "asset-management": {
    "command": "/home/you/.local/share/asset-management/mcp/AssetManagement.Mcp",
    "env": {
      "ASSET_LIBRARY_ROOT": "/path/to/library"
    }
  }
}
```

## CLI reference

| Command | Description |
|---|---|
| `asset init [--path <dir>] [--non-interactive]` | Create a new library (dirs, marker file, DB) |
| `asset import <file> --name <name> --type <type> --desc <desc> [--tags <tags>] [--meta <json>]` | Import an asset file |
| `asset export <id> <destination> --game <name>` | Copy an asset into a game project and log the usage |
| `asset tag <id> [--add <tags>] [--remove <names>]` | Add or remove tags on an existing asset |
| `asset update <id> [--name <name>] [--desc <desc>] [--meta <json>]` | Update name, description, or metadata |
| `asset search [--tag <name>] [--type <type>] [--name <name>] [--page <n>] [--size <n>]` | Search the catalog (at least one filter required) |
| `asset show <id>` | Full detail for one asset, including usage history |
| `asset collection create --name <name> [--desc <desc>]` | Create a collection |
| `asset collection add <collection-id> <asset-id>` | Add an asset to a collection |
| `asset collection remove <collection-id> <asset-id>` | Remove an asset from a collection |
| `asset collection show` | List all collections with asset counts |
| `asset catalog export` | Regenerate `catalog.json` from the current database state |

**Asset types:** `Spritesheet`, `Image`, `Audio`, `Tileset`, `Font`, `Video`, `Data`

**Tag format:** comma-separated `name:Category` pairs, e.g. `space:Theme,character:Content`.
**Tag categories:** `Theme`, `Content`, `Attribute`

## MCP tools

`ImportAsset`, `ExportAsset`, `CreateCollection`, `AddAssetToCollection`,
`RemoveAssetFromCollection`, `GetCollections`, `SearchAssets`, `GetAsset` — same
underlying operations as the CLI commands above, exposed for a game-dev agent.

## Project layout

- `src/AssetManagement.Core` — domain models, enums
- `src/AssetManagement.Data` — EF Core `DbContext`, migrations
- `src/AssetManagement.Catalog` — services (import, export, query, edit, collections, catalog.json export), library discovery
- `src/AssetManagement.Cli` — Spectre.Console CLI (`asset`)
- `src/AssetManagement.Mcp` — MCP server exposing the same operations to agents
- `tests/` — `AssetManagement.Catalog.Tests`, `AssetManagement.Data.Tests` (scaffolded, BDD coverage still to be written — see `PLAN.md`)
