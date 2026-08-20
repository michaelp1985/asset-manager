# 0001: Content-Hash Duplicate Detection on Asset Import

## Status
Accepted

## Context
`AssetImportService.ImportAsync` had no duplicate check at all. Every import generated a fresh GUID, copied the source file into the library under a new name, and created a new `Asset` row — so importing the same underlying file twice (even under a different name, from a different source path) silently produced two catalog rows and two on-disk copies. There was no protection against redundant imports.

Filename- or source-path-based checks were considered and rejected: they only catch accidental re-imports from the exact same location under the exact same name, and miss the actual case that matters — the same file content imported again under a different name or from a different path. The only signal that reliably identifies "the same asset" regardless of naming or location is the file's content itself.

## Decision
Compute a SHA-256 hash of each imported file's bytes and use it as the duplicate key:

- **`Asset.ContentHash`** (`string?`) — nullable, not backfilled for pre-existing rows. A plain non-nullable column with a unique index would have broken immediately on the assets already in the catalog before this feature existed, since they'd all collide on the same default value.
- **Filtered unique index**: `HasIndex(a => a.ContentHash).IsUnique().HasFilter("\"ContentHash\" IS NOT NULL")` (`AssetDbContext.OnModelCreating`). Legacy rows stay `null` and sit outside the constraint; every asset imported from this point forward always gets a real hash and is covered by it.
- **Hash computed in the same I/O pass as the file copy**, not a separate read — the source file is streamed once through a `CryptoStream` wrapping the destination write in `AssetImportService.ImportAsync`, so duplicate detection adds no extra file I/O over what the import already did.
- **Duplicate found → `InvalidOperationException`** naming the conflicting asset (name + Id), thrown from inside the existing `try` block so the established "delete the copied file, rethrow" cleanup handles it with no new cleanup code. No custom exception type was introduced — the codebase has no existing exception hierarchy; every service throws built-in exceptions directly (`FileNotFoundException`, `KeyNotFoundException`, `ArgumentException`), and `InvalidOperationException` fits that convention for "valid request, blocked by current state."
- **CLI catches it explicitly** (`ImportAssetCommand.cs`, printing `[red]Error:[/] {message}` and returning exit code 1, mirroring the existing `FileNotFoundException` handling). **MCP does not catch it** — `AssetTools.ImportAsset` lets all exceptions propagate to the MCP SDK today, matching how every other exception in that file is already handled.

## Consequences
- New imports are protected against redundant content regardless of filename or source path.
- The unique index is also a hard DB-level backstop against a race between the duplicate check and the insert (two imports of the same file completing at the same instant). This is not specially handled with a friendly error message — the app is a single-user desktop CLI/MCP tool, not a concurrent server, so the scenario isn't realistic enough to warrant it; if it ever happened, `SaveChangesAsync` would throw, the existing catch-all would clean up the copied file, and the import would fail safely.
- Rejecting a duplicate import currently reports only the first conflicting asset found; it does not report every prior duplicate if more than one somehow exists.

## References
- `src/AssetManagement.Core/Models/Asset.cs`
- `src/AssetManagement.Data/Contexts/AssetDbContext.cs`
- `src/AssetManagement.Data/Migrations/20260820165911_AddAssetContentHash.cs`
- `src/AssetManagement.Catalog/Services/AssetImportService.cs`
- `src/AssetManagement.Cli/Commands/ImportAssetCommand.cs`
