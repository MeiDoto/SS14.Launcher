#!/usr/bin/env bash
# ==============================================================================
# Space Station 14 Launcher - AppImage Packaging Script
# Packages the standalone Linux build into a single portable .AppImage file
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
APPDIR="${ROOT_DIR}/bin/AppDir"
OUT_DIR="${ROOT_DIR}/publish"

echo "=== Building Space Station 14 Launcher AppImage ==="
cd "${ROOT_DIR}"

# 1. Ensure Linux standalone build exists
if [ ! -d "bin/publish/Linux/bin_x64" ]; then
    echo "Publishing Linux standalone build..."
    python3 publish.py linux --x64-only
fi

# 2. Prepare AppDir structure
rm -rf "${APPDIR}"
mkdir -p "${APPDIR}/usr/bin"
mkdir -p "${APPDIR}/usr/share/applications"
mkdir -p "${APPDIR}/usr/share/icons/hicolor/256x256/apps"
mkdir -p "${APPDIR}/usr/share/metainfo"

# 3. Copy binaries and assets
cp -r bin/publish/Linux/* "${APPDIR}/usr/bin/"
cp PublishFiles/SS14.desktop "${APPDIR}/usr/share/applications/SS14.Launcher.desktop"
cp PublishFiles/SS14.desktop "${APPDIR}/SS14.Launcher.desktop"
cp PublishFiles/SS14.png "${APPDIR}/usr/share/icons/hicolor/256x256/apps/SS14.png"
cp PublishFiles/SS14.png "${APPDIR}/SS14.png"
cp PublishFiles/SS14.png "${APPDIR}/.DirIcon"
cp packaging/appstream/org.spacestation14.launcher.metainfo.xml "${APPDIR}/usr/share/metainfo/"

# 4. Create AppRun entrypoint
cat << 'EOF' > "${APPDIR}/AppRun"
#!/usr/bin/env bash
HERE="$(dirname "$(readlink -f "${0}")")"
export PATH="${HERE}/usr/bin:${PATH}"
export LD_LIBRARY_PATH="${HERE}/usr/bin:${HERE}/usr/bin/bin_x64:${LD_LIBRARY_PATH:-}"
exec "${HERE}/usr/bin/SS14.Launcher" "$@"
EOF
chmod +x "${APPDIR}/AppRun"

# 5. Build AppImage using appimagetool if installed
mkdir -p "${OUT_DIR}"
OUTPUT_APPIMAGE="${OUT_DIR}/Space_Station_14_Launcher-x86_64.AppImage"

if command -v appimagetool &> /dev/null; then
    echo "Running appimagetool..."
    ARCH=x86_64 appimagetool "${APPDIR}" "${OUTPUT_APPIMAGE}"
    echo "AppImage created successfully at ${OUTPUT_APPIMAGE}"
else
    echo "appimagetool is not installed on host. Created AppDir at ${APPDIR} ready for packaging."
    echo "To produce AppImage: ARCH=x86_64 appimagetool ${APPDIR} ${OUTPUT_APPIMAGE}"
fi

echo "Done!"
