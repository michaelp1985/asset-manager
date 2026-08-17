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

# 6. Make sure ~/.local/bin is on PATH, and ASSET_LIBRARY_ROOT is set
# (the CLI only finds the library via that env var or by walking up from the
# current directory looking for .assetlibrary - without this it only works
# when you happen to be inside the library folder).
add_path_line() {
    local rc_file="$1" line="$2"
    if [[ -f "$rc_file" ]] && grep -qF "$line" "$rc_file"; then
        return
    fi
    printf '\n# Added by asset-management installer\n%s\n' "$line" >>"$rc_file"
    echo "Updated $rc_file"
}

# Replaces any previous ASSET_LIBRARY_ROOT line from an earlier install (in
# case the library path changed) rather than appending a stale duplicate.
upsert_line() {
    local rc_file="$1" pattern="$2" line="$3"
    [[ -f "$rc_file" ]] || return
    if grep -qF "$line" "$rc_file"; then
        return
    fi
    if grep -qE "$pattern" "$rc_file"; then
        local tmp
        tmp="$(mktemp)"
        grep -vE "$pattern" "$rc_file" >"$tmp"
        mv "$tmp" "$rc_file"
    fi
    printf '\n# Added by asset-management installer\n%s\n' "$line" >>"$rc_file"
    echo "Updated $rc_file"
}

if command -v fish >/dev/null 2>&1; then
    mkdir -p "$HOME/.config/fish"
    touch "$HOME/.config/fish/config.fish"
fi

case ":${PATH}:" in
    *":$BIN_DIR:"*)
        ;;
    *)
        echo "Adding $BIN_DIR to PATH..."
        [[ -f "$HOME/.bashrc" ]] && add_path_line "$HOME/.bashrc" 'export PATH="$HOME/.local/bin:$PATH"'
        [[ -f "$HOME/.zshrc" ]] && add_path_line "$HOME/.zshrc" 'export PATH="$HOME/.local/bin:$PATH"'
        [[ -f "$HOME/.config/fish/config.fish" ]] && add_path_line "$HOME/.config/fish/config.fish" 'set -gx PATH $HOME/.local/bin $PATH'
        echo "Restart your shell (or source your rc file) for 'asset' to be available directly."
        ;;
esac

echo "Setting ASSET_LIBRARY_ROOT..."
[[ -f "$HOME/.bashrc" ]] && upsert_line "$HOME/.bashrc" '^export ASSET_LIBRARY_ROOT=' "export ASSET_LIBRARY_ROOT=\"$library_path\""
[[ -f "$HOME/.zshrc" ]] && upsert_line "$HOME/.zshrc" '^export ASSET_LIBRARY_ROOT=' "export ASSET_LIBRARY_ROOT=\"$library_path\""
[[ -f "$HOME/.config/fish/config.fish" ]] && upsert_line "$HOME/.config/fish/config.fish" '^set -gx ASSET_LIBRARY_ROOT ' "set -gx ASSET_LIBRARY_ROOT \"$library_path\""

# 7. Initialize the library (also prints the MCP config snippet - step 8)
echo ""
echo "Initializing asset library at $library_path ..."
"$install_dir/asset" init --path "$library_path" --non-interactive

echo ""
echo "Install complete. Binaries installed to: $install_dir"
echo "Open a new shell (or source your rc file) so PATH and ASSET_LIBRARY_ROOT take effect, then run 'asset --help'."
