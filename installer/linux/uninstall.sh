#!/usr/bin/env bash
# Asset Management - Linux uninstaller
#
# Removes the binaries installed by install.sh and the PATH / ASSET_LIBRARY_ROOT
# lines it added to your shell rc files. Your asset library (the data directory
# you chose during install) is left untouched.

set -euo pipefail

BIN_DIR="$HOME/.local/bin"
ASSET_LINK="$BIN_DIR/asset"

read -r -p "This removes the asset-management binaries and the PATH/ASSET_LIBRARY_ROOT lines added by the installer. Your library data is not touched. Continue? [y/N] " confirm
case "$confirm" in
    [yY]|[yY][eE][sS]) ;;
    *) echo "Aborted."; exit 0 ;;
esac

if [[ -L "$ASSET_LINK" ]]; then
    install_dir="$(dirname "$(readlink -f "$ASSET_LINK")")"
else
    install_dir="$HOME/.local/share/asset-management"
    echo "Warning: no symlink found at $ASSET_LINK, assuming default install dir $install_dir" >&2
fi

echo "Removing binaries from $install_dir ..."
rm -f "$install_dir/asset" "$install_dir/AssetManagement.Mcp"
rmdir "$install_dir" 2>/dev/null || true

echo "Removing symlink $ASSET_LINK ..."
rm -f "$ASSET_LINK"

# Strips the "# Added by asset-management installer" comment line and the
# single line that follows it (install.sh always writes them as a pair).
remove_block() {
    local rc_file="$1"
    [[ -f "$rc_file" ]] || return
    grep -qF '# Added by asset-management installer' "$rc_file" || return
    local tmp
    tmp="$(mktemp)"
    awk '
        /^# Added by asset-management installer$/ { skip=1; next }
        skip > 0 { skip--; next }
        { print }
    ' "$rc_file" >"$tmp"
    mv "$tmp" "$rc_file"
    echo "Cleaned up $rc_file"
}

remove_block "$HOME/.bashrc"
remove_block "$HOME/.zshrc"
remove_block "$HOME/.config/fish/config.fish"

echo ""
echo "Uninstall complete. Your asset library was left untouched."
echo "Open a new shell for the PATH/ASSET_LIBRARY_ROOT changes to take effect."
