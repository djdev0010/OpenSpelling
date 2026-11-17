#!/usr/bin/env bash
# Builds Spelling Bee for the App Store and uploads it to TestFlight, in one command.
# Run scripts/get-models.sh, scripts/build-sherpa-ios.sh and scripts/install-ipad.sh first
# (install-ipad.sh picks the bundle ID and makes the signing profile this script reuses).
#
# Usage: scripts/publish-testflight.sh [--archive-only]
#   --archive-only   build the archive and stop, without signing for the App Store or uploading
#
# Before the first upload, create the app in App Store Connect (My Apps → +) with the same
# bundle ID. The upload uses the Apple ID signed in to Xcode, which creates the App Store
# certificate and profile on first use. Nothing account-specific is stored in the repository.
#
# Set these to override:
#   SPELLINGBEE_BUNDLE_ID   bundle ID (default: the one in .ipad-bundle-id, which git ignores)
#   SPELLINGBEE_TEAM_ID     Apple developer team, when more than one is signed in to Xcode
#   SPELLINGBEE_API_KEY_PATH, SPELLINGBEE_API_KEY_ID, SPELLINGBEE_API_ISSUER_ID
#                           upload with an App Store Connect API key instead of the Xcode
#                           account. Keep the .p8 file outside this repository.
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$REPO/src/SpellingBee.App.iOS"
BUNDLE_ID_FILE="$REPO/.ipad-bundle-id"
PROFILES="$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"
OUTPUT="$REPO/dist/testflight"
ARCHIVE_ONLY=false
[[ "${1:-}" == "--archive-only" ]] && ARCHIVE_ONLY=true

fail() { echo "$*" >&2; exit 1; }

# Prints one field from every provisioning profile Xcode has saved.
read_profiles() {
  for profile in "$PROFILES"/*.mobileprovision; do
    [[ -f "$profile" ]] || continue
    security cms -D -i "$profile" 2>/dev/null | plutil -extract "$1" raw - 2>/dev/null || true
  done
}

# --- 1. Check what the build needs. -------------------------------------------------------
[[ -d "$REPO/native/ios/SherpaOnnxC.xcframework/ios-arm64" ]] ||
  fail "The speech engine isn't built for iPad yet. Run scripts/build-sherpa-ios.sh first."
[[ -f "$REPO/models/vad/silero_vad.onnx" ]] ||
  fail "The models are missing. Run scripts/get-models.sh first."

BUNDLE_ID="${SPELLINGBEE_BUNDLE_ID:-}"
[[ -z "$BUNDLE_ID" && -f "$BUNDLE_ID_FILE" ]] && BUNDLE_ID="$(tr -d '[:space:]' < "$BUNDLE_ID_FILE")"
[[ -n "$BUNDLE_ID" ]] ||
  fail "No bundle ID yet. Run scripts/install-ipad.sh once, or set SPELLINGBEE_BUNDLE_ID."
read_profiles Entitlements.application-identifier | grep -q "\.$BUNDLE_ID$" ||
  fail "No signing profile for $BUNDLE_ID. Run scripts/install-ipad.sh once with the iPad plugged in."

TEAM_ID="${SPELLINGBEE_TEAM_ID:-$(read_profiles TeamIdentifier.0 | sort -u)}"
[[ -n "$TEAM_ID" && "$(wc -l <<< "$TEAM_ID")" -eq 1 ]] ||
  fail "Can't tell which Apple developer team to use. Set SPELLINGBEE_TEAM_ID (it's in Xcode → Settings → Accounts)."

# --- 2. Build the archive. ----------------------------------------------------------------
# Every upload needs a higher build number than the last, so use the date and time.
BUILD_NUMBER="$(date +%Y%m%d).$(date +%H%M)"
ARCHIVE="$OUTPUT/SpellingBee-$BUILD_NUMBER.xcarchive"
rm -rf "$OUTPUT" && mkdir -p "$OUTPUT"
touch "$OUTPUT/.build-started"

echo "Building $BUNDLE_ID build $BUILD_NUMBER (Release)…"
dotnet publish "$PROJECT" -c Release -r ios-arm64 -v:quiet -nologo \
  -p:ApplicationId="$BUNDLE_ID" -p:ApplicationVersion="$BUILD_NUMBER" \
  -p:CodesignKey="Apple Development" -p:ArchiveOnBuild=true

# .NET always saves the archive in Xcode's archive folder; move this run's archive here.
NEW_ARCHIVE="$(find "$HOME/Library/Developer/Xcode/Archives" -maxdepth 2 -name '*.xcarchive' \
  -newer "$OUTPUT/.build-started" 2>/dev/null | head -1)"
[[ -n "$NEW_ARCHIVE" ]] || fail "The build finished but made no archive."
mv "$NEW_ARCHIVE" "$ARCHIVE"

if $ARCHIVE_ONLY; then
  echo "Archive ready: $ARCHIVE"
  exit 0
fi

# --- 3. Sign for the App Store and upload. ------------------------------------------------
cat > "$OUTPUT/ExportOptions.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>method</key><string>app-store-connect</string>
  <key>destination</key><string>upload</string>
  <key>teamID</key><string>$TEAM_ID</string>
  <key>signingStyle</key><string>automatic</string>
  <key>manageAppVersionAndBuildNumber</key><false/>
  <key>uploadSymbols</key><true/>
</dict>
</plist>
PLIST

API_KEY_ARGS=()
if [[ -n "${SPELLINGBEE_API_KEY_PATH:-}" ]]; then
  API_KEY_ARGS=(-authenticationKeyPath "$SPELLINGBEE_API_KEY_PATH"
                -authenticationKeyID "${SPELLINGBEE_API_KEY_ID:?Set SPELLINGBEE_API_KEY_ID too}"
                -authenticationKeyIssuerID "${SPELLINGBEE_API_ISSUER_ID:?Set SPELLINGBEE_API_ISSUER_ID too}")
fi

echo "Signing for the App Store and uploading (about 900 MB)…"
xcodebuild -exportArchive -archivePath "$ARCHIVE" -exportOptionsPlist "$OUTPUT/ExportOptions.plist" \
  -exportPath "$OUTPUT" -allowProvisioningUpdates ${API_KEY_ARGS[@]+"${API_KEY_ARGS[@]}"} ||
  fail "Upload failed. Check that the app exists in App Store Connect with bundle ID $BUNDLE_ID."

echo "Uploaded build $BUILD_NUMBER. It appears in App Store Connect → TestFlight after processing (10–30 minutes)."
