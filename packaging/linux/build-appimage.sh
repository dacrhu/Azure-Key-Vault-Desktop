#!/usr/bin/env bash
set -euo pipefail

# Packages a self-contained `dotnet publish` output into a Linux AppImage.
# Usage: build-appimage.sh <publish-dir> <output-appimage-path>

PUBLISH_DIR="${1:?Usage: build-appimage.sh <publish-dir> <output-appimage-path>}"
OUTPUT_PATH="${2:?Usage: build-appimage.sh <publish-dir> <output-appimage-path>}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORK_DIR="$(mktemp -d)"
APPDIR="$WORK_DIR/AppDir"

mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"

cp -r "$PUBLISH_DIR"/. "$APPDIR/usr/bin/"
cp "$SCRIPT_DIR/AzureKeyVaultDesktop.desktop" "$APPDIR/usr/share/applications/"
cp "$SCRIPT_DIR/AzureKeyVaultDesktop.desktop" "$APPDIR/"
cp "$SCRIPT_DIR/icon.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/AzureKeyVaultDesktop.png"
cp "$SCRIPT_DIR/icon.png" "$APPDIR/AzureKeyVaultDesktop.png"

cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/bin/AzureKeyVaultDesktop" "$@"
EOF
chmod +x "$APPDIR/AppRun"
chmod +x "$APPDIR/usr/bin/AzureKeyVaultDesktop"

if ! command -v appimagetool >/dev/null 2>&1; then
    curl -L -o "$WORK_DIR/appimagetool" \
        "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
    chmod +x "$WORK_DIR/appimagetool"
    APPIMAGETOOL="$WORK_DIR/appimagetool"
else
    APPIMAGETOOL="$(command -v appimagetool)"
fi

# appimagetool is itself an AppImage, which normally needs FUSE to mount and run — not
# installed by default on GitHub's ubuntu-latest runners. APPIMAGE_EXTRACT_AND_RUN makes it
# self-extract to a temp dir and run from there instead, sidestepping FUSE entirely.
APPIMAGE_EXTRACT_AND_RUN=1 ARCH=x86_64 "$APPIMAGETOOL" "$APPDIR" "$OUTPUT_PATH"

rm -rf "$WORK_DIR"
