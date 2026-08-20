# C4: Component

## Diagram
```mermaid
C4Component
    Container_Boundary(cli, "asset CLI") {
        Component(importCmd, "ImportAssetCommand")
        Component(exportCmd, "ExportAssetCommand")
        Component(tagCmd, "TagAssetCommand")
        Component(updateCmd, "UpdateAssetCommand")
        Component(searchCmd, "SearchAssetCommand")
        Component(showCmd, "ShowAssetCommand")
        Component(exportCatalogCmd, "ExportCatalogCommand")
        Component(initCmd, "InitCommand")
        Component(collectionCmds, "Collection/*", "AddToCollection, CreateCollection, RemoveFromCollection, ShowCollections")
    }

    Container_Boundary(mcp, "MCP Server") {
        Component(assetTools, "AssetTools", "import_asset, export_asset, search_assets, get_asset, create_collection, add/remove_asset_to/from_collection, get_collections")
    }

    Container_Boundary(catalog, "Catalog Service Layer") {
        Component(importSvc, "AssetImportService", "Hashes + copies the file, duplicate check, creates Asset + tags")
        Component(exportSvc, "AssetExportService", "Copies an asset out to a game project dir, writes UsageLog")
        Component(editSvc, "AssetEditService", "Tag/metadata updates on an existing asset")
        Component(querySvc, "AssetQueryService", "Search/get by id")
        Component(collectionSvc, "CollectionService", "Create/list collections, add/remove assets")
        Component(exporter, "CatalogExporter", "Regenerates catalog.json after any write")
        Component(discovery, "LibraryDiscovery / LibrarySettings", "Resolves library root: .assetlibrary marker or ASSET_LIBRARY_ROOT")
    }

    Rel(importCmd, importSvc, "calls")
    Rel(exportCmd, exportSvc, "calls")
    Rel(tagCmd, editSvc, "calls")
    Rel(updateCmd, editSvc, "calls")
    Rel(searchCmd, querySvc, "calls")
    Rel(showCmd, querySvc, "calls")
    Rel(exportCatalogCmd, exporter, "calls")
    Rel(collectionCmds, collectionSvc, "calls")
    Rel(initCmd, discovery, "bootstraps a new library, runs before discovery on every other command")

    Rel(assetTools, importSvc, "calls")
    Rel(assetTools, exportSvc, "calls")
    Rel(assetTools, querySvc, "calls")
    Rel(assetTools, collectionSvc, "calls")

    Rel(importSvc, exporter, "triggers regeneration")
    Rel(exportSvc, exporter, "triggers regeneration")
    Rel(editSvc, exporter, "triggers regeneration")
    Rel(collectionSvc, exporter, "triggers regeneration")
```

## Description

### `asset` CLI components
Thin command classes (Spectre.Console.Cli) — each parses its own arguments/options, calls exactly one Catalog service method, and formats the result as a table or `[red]Error:[/]` markup. No business logic lives here.

| Component | Calls into | Notes |
|---|---|---|
| `ImportAssetCommand` | `AssetImportService` | Catches `FileNotFoundException` and `InvalidOperationException` (duplicate content) explicitly. |
| `ExportAssetCommand` | `AssetExportService` | |
| `TagAssetCommand`, `UpdateAssetCommand` | `AssetEditService` | Catch `KeyNotFoundException` for an unknown asset id. |
| `SearchAssetCommand`, `ShowAssetCommand` | `AssetQueryService` | Read-only. |
| `ExportCatalogCommand` | `CatalogExporter` | Manual re-export trigger; normally runs automatically after writes. |
| `Collection/*` | `CollectionService` | Create/list/add-to/remove-from collections. |
| `InitCommand` | `LibraryDiscovery` / EF Core `Database.Migrate()` | The one command that runs *before* library-root discovery — bootstraps `.assetlibrary`, `assets/`, `catalog/`, and the schema for a brand-new library. |

### MCP Server components
A single `AssetTools` class (`[McpServerToolType]`) exposes one `[McpServerTool]` method per capability, each a thin wrapper calling straight into the same Catalog services the CLI uses — so behavior is identical between the human and agent front ends. Tool parameters use plain (non-nullable) types with empty-string/default sentinels rather than nullable unions, since some tool-calling models struggle to generate valid JSON against a `["string","null"]` schema.

### Catalog Service Layer components
The shared core, referenced by both front ends:

| Component | Responsibility |
|---|---|
| `AssetImportService` | Validates the source file exists, streams it into `assets/` while computing a SHA-256 content hash in the same pass, checks the hash against existing assets (rejecting with `InvalidOperationException` on a match — see [ADR 0001](../adr/0001-content-hash-duplicate-detection.md)), creates the `Asset` row and its tags, triggers a catalog re-export. |
| `AssetExportService` | Looks up an asset, copies its file into a game project directory, records a `UsageLog` entry. |
| `AssetEditService` | Applies tag changes or metadata updates to an existing asset without re-importing. |
| `AssetQueryService` | Search (by tag/type/name, paginated) and get-by-id, backing both CLI search/show and MCP `search_assets`/`get_asset`. |
| `CollectionService` | Create/list collections, add/remove assets from a collection. |
| `CatalogExporter` | Regenerates `catalog.json` from `catalog.db` — the one place that keeps the denormalized export in sync; called after every mutating operation across every service. |
| `LibraryDiscovery` / `LibrarySettings` | Resolves the active library root (`ASSET_LIBRARY_ROOT` env var, falling back to walking up from the working directory for a `.assetlibrary` marker) and derives `AssetsPath`/`DbPath` from it. |

Not broken out further here: `AssetManagement.Data` (just `AssetDbContext` + EF Core migrations — see [02-container.md](02-container.md)) and `AssetManagement.Core` (plain entity/enum definitions with no behavior).
