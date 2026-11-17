#!/usr/bin/env bash
# Builds "dist/Spelling Bee.app": a self-contained macOS app with the models inside,
# so it runs with no .NET install and no network. Run scripts/get-models.sh first.
# Usage: scripts/bundle-mac.sh [osx-arm64|osx-x64]   (default: this Mac's architecture)
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
RUNTIME="${1:-$([[ "$(uname -m)" == "arm64" ]] && echo osx-arm64 || echo osx-x64)}"
VERSION="0.1.0"
APP="$REPO/dist/Spelling Bee.app"
PUBLISH="$REPO/dist/publish-$RUNTIME"

for required in kokoro-v1_0-int8/model.int8.onnx parakeet-tdt-0.6b-v2-int8/encoder.int8.onnx vad/silero_vad.onnx; do
  if [[ ! -f "$REPO/models/$required" ]]; then
    echo "Missing models/$required. Run scripts/get-models.sh first." >&2
    exit 1
  fi
done

echo "Publishing for $RUNTIME…"
rm -rf "$PUBLISH" "$APP"
dotnet publish "$REPO/src/SpellingBee.App.Desktop" -c Release -r "$RUNTIME" --self-contained -o "$PUBLISH"

echo "Assembling $APP…"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "$PUBLISH/." "$APP/Contents/MacOS/"
# The icon, from the PNG drawn by scripts/draw-app-icon.swift.
ICONSET="$PUBLISH/AppIcon.iconset"
mkdir -p "$ICONSET"
for points in 16 32 128 256 512; do
  sips -z "$points" "$points" "$REPO/src/SpellingBee.App/Assets/AppIcon.png" --out "$ICONSET/icon_${points}x${points}.png" > /dev/null
  sips -z $((points * 2)) $((points * 2)) "$REPO/src/SpellingBee.App/Assets/AppIcon.png" --out "$ICONSET/icon_${points}x${points}@2x.png" > /dev/null
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/AppIcon.icns"
rm -rf "$ICONSET"
# SpeechModelFiles looks for the models in Contents/Resources/models.
ditto "$REPO/models" "$APP/Contents/Resources/models"
rm -f "$APP/Contents/Resources/models/README.md"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>                 <string>Spelling Bee</string>
  <key>CFBundleDisplayName</key>          <string>Spelling Bee</string>
  <key>CFBundleIdentifier</key>           <string>app.spellingbee.desktop</string>
  <key>CFBundleVersion</key>              <string>$VERSION</string>
  <key>CFBundleShortVersionString</key>   <string>$VERSION</string>
  <key>CFBundlePackageType</key>          <string>APPL</string>
  <key>CFBundleIconFile</key>             <string>AppIcon</string>
  <key>CFBundleExecutable</key>           <string>SpellingBee.App.Desktop</string>
  <key>LSMinimumSystemVersion</key>       <string>12.0</string>
  <key>LSApplicationCategoryType</key>    <string>public.app-category.education</string>
  <key>NSHighResolutionCapable</key>      <true/>
  <key>NSMicrophoneUsageDescription</key>
  <string>Spelling Bee listens while your child spells words out loud. Everything stays on this Mac.</string>
</dict>
</plist>
PLIST

# Ad-hoc signature: enough to run on this Mac. Sharing with other Macs needs a Developer ID and notarization.
codesign --force --deep --sign - "$APP"

echo "Done: $APP ($(du -sh "$APP" | cut -f1))"
