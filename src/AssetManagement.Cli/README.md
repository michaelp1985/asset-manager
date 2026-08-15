# AssetManagement CLI

Command-line interface for managing the AiGames asset library.

Run from the `AssetManagement/` directory (where `.assetlibrary` lives), or set `ASSET_LIBRARY_ROOT` to that path.

```
dotnet run --project src/AssetManagement.Cli -- <command> [options]
```

---

## Commands

### `import`

Imports an asset file into the library. Copies the file to `assets/`, writes the database record, and regenerates `catalog.json`.

```
asset import <file> --name <name> --type <type> --desc <description> [--tags <tags>] [--meta <json>]
```

| Argument / Option | Required | Description |
|---|---|---|
| `<file>` | Yes | Path to the source asset file |
| `--name` | Yes | Human-readable name |
| `--type` | Yes | `Spritesheet`, `Image`, `Audio`, `Tileset`, `Font`, `Video`, `Data` |
| `--desc` | Yes | Description written for LLM catalog consumption |
| `--tags` | No | Comma-separated `name:Category` pairs (categories: `Theme`, `Content`, `Attribute`) |
| `--meta` | No | Raw JSON blob for asset-specific metadata (frames, dimensions, etc.) |

**Examples**

Minimal import:
```bash
dotnet run --project src/AssetManagement.Cli -- import /path/to/hero.png \
  --name "Hero Idle" \
  --type Spritesheet \
  --desc "8-frame idle animation for the main hero character."
```

With tags:
```bash
dotnet run --project src/AssetManagement.Cli -- import /path/to/fireball.png \
  --name "Fireball Projectile" \
  --type Spritesheet \
  --desc "6-frame fireball projectile animation, loops seamlessly." \
  --tags "fantasy:Theme,projectile:Content,looping:Attribute"
```

With tags and metadata:
```bash
dotnet run --project src/AssetManagement.Cli -- import /path/to/Wizard_Move.png \
  --name "Wizard Move" \
  --type Spritesheet \
  --desc "24-frame movement spritesheet for a wizard character. 6-column 4-row grid, 4-directional walk cycle." \
  --tags "fantasy:Theme,character:Content,wizard:Content,4-directional:Attribute,movement:Attribute" \
  --meta '{"frames":24,"columns":6,"rows":4,"directions":4,"framesPerDirection":6}'
```

---

### `catalog export`

Regenerates `catalog.json` from the current database state without performing any other operation. Useful after direct database changes or to force a sync.

```
asset catalog export
```

**Example**

```bash
dotnet run --project src/AssetManagement.Cli -- catalog export
```

Output:
```
Catalog regenerated: /path/to/AssetManagement/catalog/catalog.json
```

---

## Tag Categories

| Category | Purpose | Examples |
|---|---|---|
| `Theme` | Art style or world setting | `fantasy`, `sci-fi`, `medieval`, `space` |
| `Content` | What the asset depicts | `character`, `wizard`, `terrain`, `projectile` |
| `Attribute` | Behavioral or technical traits | `looping`, `4-directional`, `movement`, `animated` |

Tag format: `name:Category` — case-insensitive for the category.
