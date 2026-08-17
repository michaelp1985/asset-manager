# Installing Asset Management (Linux)

1. Extract the tarball:
   ```bash
   tar xzf asset-management-linux-x64.tar.gz
   cd asset-management-linux-x64
   ```
2. Run the installer:
   ```bash
   ./install.sh
   ```
   You'll be prompted for an install directory (default `~/.local/share/asset-management`)
   and a library location (default `~/AssetLibrary`). The script installs both
   binaries, symlinks `asset` onto your `PATH` via `~/.local/bin`, sets
   `ASSET_LIBRARY_ROOT`, and initializes the library.
3. Open a new shell (or `source` your rc file) so `PATH` and
   `ASSET_LIBRARY_ROOT` take effect, then confirm:
   ```bash
   asset --help
   ```

## MCP server

The installer prints an MCP config snippet after `asset init` runs — point your
MCP client at `<install-dir>/AssetManagement.Mcp` with `ASSET_LIBRARY_ROOT` set
in its environment.

## Uninstalling

```bash
rm -rf ~/.local/share/asset-management ~/.local/bin/asset
```

Then remove the `# Added by asset-management installer` block from
`~/.bashrc` / `~/.zshrc` / `~/.config/fish/config.fish`. Your asset library
(the directory you chose during install) is left untouched.

## Requirements

- Linux x86_64
- No .NET runtime needed — the binaries are self-contained
