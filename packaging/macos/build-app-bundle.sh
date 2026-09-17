#!/usr/bin/env bash
set -euo pipefail

# Assembles a self-contained `dotnet publish` output into a macOS .app bundle.
# Usage: build-app-bundle.sh <publish-dir> <output-app-dir> [version]

PUBLISH_DIR="${1:?Usage: build-app-bundle.sh <publish-dir> <output-app-dir> [version]}"
OUTPUT_APP="${2:?Usage: build-app-bundle.sh <publish-dir> <output-app-dir> [version]}"
VERSION="${3:-0.0.0}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

rm -rf "$OUTPUT_APP"
mkdir -p "$OUTPUT_APP/Contents/MacOS" "$OUTPUT_APP/Contents/Resources"

cp -r "$PUBLISH_DIR"/. "$OUTPUT_APP/Contents/MacOS/"
chmod +x "$OUTPUT_APP/Contents/MacOS/AzureKeyVaultDesktop"

sed "s/__VERSION__/$VERSION/g" "$SCRIPT_DIR/Info.plist.template" > "$OUTPUT_APP/Contents/Info.plist"

# Build a .icns from the source PNG using macOS-only tools (sips/iconutil), so the app
# bundle shows a real icon in Finder/Dock instead of the generic default.
if command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1 && [ -f "$SCRIPT_DIR/icon.png" ]; then
    ICONSET_DIR="$(mktemp -d)/AzureKeyVaultDesktop.iconset"
    mkdir -p "$ICONSET_DIR"
    for size in 16 32 128 256 512; do
        sips -z "$size" "$size" "$SCRIPT_DIR/icon.png" --out "$ICONSET_DIR/icon_${size}x${size}.png" >/dev/null
        double=$((size * 2))
        sips -z "$double" "$double" "$SCRIPT_DIR/icon.png" --out "$ICONSET_DIR/icon_${size}x${size}@2x.png" >/dev/null
    done
    iconutil -c icns "$ICONSET_DIR" -o "$OUTPUT_APP/Contents/Resources/AzureKeyVaultDesktop.icns"
    rm -rf "$(dirname "$ICONSET_DIR")"
else
    echo "Skipping .icns generation (sips/iconutil not available, or icon.png missing) — only relevant on macOS runners."
fi

echo "Built unsigned app bundle at $OUTPUT_APP"
echo "Note: unsigned - users must right-click > Open the first time (or run: xattr -cr '$OUTPUT_APP')"
