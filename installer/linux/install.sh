#!/usr/bin/env bash
# Asset Management - Linux installer
#
# Expects to run from inside the extracted release tarball, alongside the
# self-contained single-file binaries `asset` and `AssetManagement.Mcp`
# (see PLAN-PUBLISH.md, section 4).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

DEFAULT_INSTALL_DIR="$HOME/.local/share/asset-management"
DEFAULT_LIBRARY_DIR="$HOME/AssetLibrary"
BIN_DIR="$HOME/.local/bin"

# 1. Detect architecture
arch="$(uname -m)"
if [[ "$arch" != "x86_64" ]]; then
    echo "Error: unsupported architecture '$arch'. Only x86_64 is supported." >&2
    exit 1
fi

if [[ ! -f "$SCRIPT_DIR/asset" || ! -f "$SCRIPT_DIR/AssetManagement.Mcp" ]]; then
    echo "Error: 'asset' and 'AssetManagement.Mcp' must sit next to this script." >&2
    echo "Extract the full release tarball rather than running install.sh on its own." >&2
    exit 1
fi

# 2. Prompt for install dir
read -r -p "Install binaries to [$DEFAULT_INSTALL_DIR]? " install_dir_input
install_dir="${install_dir_input:-$DEFAULT_INSTALL_DIR}"

# 3. Prompt for library path
read -r -p "Where should your asset library live? [$DEFAULT_LIBRARY_DIR] " library_path_input
library_path="${library_path_input:-$DEFAULT_LIBRARY_DIR}"

# 4. Install binaries
mkdir -p "$install_dir"
install -m 755 "$SCRIPT_DIR/asset" "$install_dir/asset"
install -m 755 "$SCRIPT_DIR/AssetManagement.Mcp" "$install_dir/AssetManagement.Mcp"

# 5. Symlink asset -> ~/.local/bin/asset
mkdir -p "$BIN_DIR"
ln -sf "$install_dir/asset" "$BIN_DIR/asset"

# 6. Make sure ~/.local/bin is on PATH
add_path_line() {
    local rc_file="$1" line="$2"
    if [[ -f "$rc_file" ]] && grep -qF "$line" "$rc_file"; then
        return
    fi
    printf '\n# Added by asset-management installer\n%s\n' "$line" >>"$rc_file"
    echo "Updated $rc_file"
}

case ":${PATH}:" in
    *":$BIN_DIR:"*)
        ;;
    *)
        echo "Adding $BIN_DIR to PATH..."
        [[ -f "$HOME/.bashrc" ]] && add_path_line "$HOME/.bashrc" 'export PATH="$HOME/.local/bin:$PATH"'
        [[ -f "$HOME/.zshrc" ]] && add_path_line "$HOME/.zshrc" 'export PATH="$HOME/.local/bin:$PATH"'
        if command -v fish >/dev/null 2>&1; then
            mkdir -p "$HOME/.config/fish"
            add_path_line "$HOME/.config/fish/config.fish" 'set -gx PATH $HOME/.local/bin $PATH'
        fi
        echo "Restart your shell (or source your rc file) for 'asset' to be available directly."
        ;;
esac

# 7. Initialize the library (also prints the MCP config snippet - step 8)
echo ""
echo "Initializing asset library at $library_path ..."
"$install_dir/asset" init --path "$library_path" --non-interactive

echo ""
echo "Install complete. Binaries installed to: $install_dir"
echo "Run 'asset --help' to get started (open a new shell first if PATH was just updated)."
