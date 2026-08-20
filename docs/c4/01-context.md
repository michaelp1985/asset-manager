# C4: System Context

## Diagram
```mermaid
C4Context
    Person(developer, "Developer", "Curates the asset library by hand")
    Person(agent, "Game-Dev AI Agent", "e.g. an opencode/Claude session, searches and pulls assets while building a game")

    System(assetMgmt, "Asset Management", "Imports, catalogs, tags, and exports game assets from a single local library")

    System_Ext(gameProject, "Game Project", "A downstream game codebase that receives exported asset copies")
    System_Ext(sourceFiles, "Source Asset Files", "Arbitrary files on disk the developer imports from (art tool exports, downloaded packs, etc.)")

    Rel(developer, assetMgmt, "Imports, tags, updates, exports via the asset CLI")
    Rel(agent, assetMgmt, "Searches, imports, exports via MCP tools")
    Rel(assetMgmt, sourceFiles, "Reads on import")
    Rel(assetMgmt, gameProject, "Writes asset copies on export")
```

## Description

| Actor / System | Type | Relationship to Asset Management |
|---|---|---|
| Developer | Person | Drives curation by hand through the `asset` CLI — import, tag, update, search, export, collection management. |
| Game-Dev AI Agent | Person (via software) | Connects over MCP (stdio) to search the catalog and pull assets into a game project on request; can also import/export/tag, but the intended split is human-drives-curation, agent-drives-consumption. |
| Game Project | External system (black box) | Receives asset copies on export. Asset Management has no live reference back into it — an exported asset is a one-way, disconnected snapshot. Its internals are never modeled here. |
| Source Asset Files | External system (black box) | Arbitrary filesystem locations (art tool output, downloaded asset packs, etc.) that files are imported *from*. Not owned or tracked by Asset Management prior to import. |

Asset Management itself is a single local system — no network services, no multi-tenant concerns. Everything below the System box in this diagram is internal and is broken down in [02-container.md](02-container.md).
