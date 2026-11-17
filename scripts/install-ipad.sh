#!/usr/bin/env bash
# Builds Spell on Words for a real iPad, installs it, and starts it, in one command.
# Run scripts/get-models.sh and scripts/build-sherpa-ios.sh first, and plug in the iPad.
#
# Usage: scripts/install-ipad.sh [--debug]
#
# The first run picks a bundle ID (saved in .ipad-bundle-id, which git ignores) and has Xcode
# create a signing profile for it. Later runs reuse both. Set these to override:
#   SPELLINGBEE_BUNDLE_ID   bundle ID to sign with, for example com.yourname.spellingbee
#   SPELLINGBEE_TEAM_ID     Apple developer team, when more than one is signed in to Xcode
#   SPELLINGBEE_IPAD_ID     iPad to install on, when more than one is connected
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$REPO/src/SpellingBee.App.iOS"
BUNDLE_ID_FILE="$REPO/.ipad-bundle-id"
PROFILES="$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"
CONFIGURATION="Release"
[[ "${1:-}" == "--debug" ]] && CONFIGURATION="Debug"

fail() { echo "$*" >&2; exit 1; }

# --- 1. Check what the build needs. -------------------------------------------------------
[[ -d "$REPO/native/ios/SherpaOnnxC.xcframework/ios-arm64" ]] ||
  fail "The speech engine isn't built for iPad yet. Run scripts/build-sherpa-ios.sh first."
[[ -f "$REPO/models/vad/silero_vad.onnx" ]] ||
  fail "The models are missing. Run scripts/get-models.sh first."
security find-identity -v -p codesigning | grep -q "Apple Development" ||
  fail "No Apple Development certificate. In Xcode, open Settings → Accounts and sign in with your Apple ID."

# --- 2. Find the iPad: a real, paired iPad that is connected now. -------------------------
find_ipads() {
  xcrun devicectl list devices --json-output /dev/stdout 2>/dev/null | python3 -c '
import json, sys
for device in json.load(sys.stdin)["result"]["devices"]:
    hardware, connection = device["properties"]["hardware"], device["properties"]["connection"]
    if (hardware.get("deviceType") == "iPad" and hardware.get("reality") == "physical"
            and connection.get("pairingState") == "paired" and connection.get("state") != "unavailable"):
        print(hardware["udid"], device["properties"]["state"].get("name", ""), sep="\t")'
}

IPAD_ID="${SPELLINGBEE_IPAD_ID:-}"
if [[ -z "$IPAD_ID" ]]; then
  IPADS="$(find_ipads)"
  [[ -n "$IPADS" ]] ||
    fail "No iPad found. Plug it in, unlock it, and tap Trust. Then turn on Settings → Privacy & Security → Developer Mode."
  if [[ "$(wc -l <<< "$IPADS")" -gt 1 ]]; then
    echo "More than one iPad is connected. Choose one with SPELLINGBEE_IPAD_ID=<id>:" >&2
    fail "$IPADS"
  fi
  IPAD_ID="$(cut -f1 <<< "$IPADS")"
  echo "iPad: $(cut -f2 <<< "$IPADS")"
fi

# --- 3. Choose the bundle ID, and remember it so the same app is updated next time. ------
BUNDLE_ID="${SPELLINGBEE_BUNDLE_ID:-}"
[[ -z "$BUNDLE_ID" && -f "$BUNDLE_ID_FILE" ]] && BUNDLE_ID="$(tr -d '[:space:]' < "$BUNDLE_ID_FILE")"
if [[ -z "$BUNDLE_ID" ]]; then
  # Apple needs every app ID to be unique, so add a random suffix.
  BUNDLE_ID="com.spellingbee.ipad$(openssl rand -hex 3)"
  echo "$BUNDLE_ID" > "$BUNDLE_ID_FILE"
  echo "Chose bundle ID $BUNDLE_ID (saved in .ipad-bundle-id)."
fi

# --- 4. Make sure there is a signing profile for that bundle ID. --------------------------
# Prints one field from every provisioning profile Xcode has saved.
read_profiles() {
  for profile in "$PROFILES"/*.mobileprovision; do
    [[ -f "$profile" ]] || continue
    security cms -D -i "$profile" 2>/dev/null | plutil -extract "$1" raw - 2>/dev/null || true
  done
}

if ! read_profiles Entitlements.application-identifier | grep -q "\.$BUNDLE_ID$"; then
  command -v xcodegen > /dev/null ||
    fail "No signing profile for $BUNDLE_ID. Install xcodegen (brew install xcodegen) or follow the README's Xcode steps."

  TEAM_ID="${SPELLINGBEE_TEAM_ID:-$(read_profiles TeamIdentifier.0 | sort -u)}"
  [[ -n "$TEAM_ID" && "$(wc -l <<< "$TEAM_ID")" -eq 1 ]] ||
    fail "Can't tell which Apple developer team to use. Set SPELLINGBEE_TEAM_ID (it's in Xcode → Settings → Accounts)."

  echo "Asking Xcode for a signing profile for $BUNDLE_ID…"
  STUB="$(mktemp -d)"
  trap 'rm -rf "$STUB"' EXIT
  mkdir "$STUB/Stub"
  echo 'import UIKit; UIApplicationMain(CommandLine.argc, CommandLine.unsafeArgv, nil, nil)' > "$STUB/Stub/main.swift"
  cat > "$STUB/project.yml" <<YAML
name: SigningStub
targets:
  SigningStub:
    type: application
    platform: iOS
    deploymentTarget: "17.0"
    sources: [Stub]
    settings:
      PRODUCT_BUNDLE_IDENTIFIER: $BUNDLE_ID
      DEVELOPMENT_TEAM: $TEAM_ID
      CODE_SIGN_STYLE: Automatic
      TARGETED_DEVICE_FAMILY: "2"
      GENERATE_INFOPLIST_FILE: YES
YAML
  (cd "$STUB" && xcodegen generate --quiet &&
    xcodebuild -project SigningStub.xcodeproj -scheme SigningStub -destination "id=$IPAD_ID" \
      -allowProvisioningUpdates -derivedDataPath build -quiet build) ||
    fail "Xcode couldn't create a signing profile. Check that you're signed in under Xcode → Settings → Accounts."
fi

# --- 5. Build, install and start. ---------------------------------------------------------
echo "Building ($CONFIGURATION)…"
dotnet build "$PROJECT" -c "$CONFIGURATION" -r ios-arm64 \
  -p:ApplicationId="$BUNDLE_ID" -p:CodesignKey="Apple Development" -v:quiet -nologo

APP="$PROJECT/bin/$CONFIGURATION/net10.0-ios/ios-arm64/SpellingBee.App.iOS.app"
echo "Installing on the iPad (about 900 MB, so this takes a minute)…"
xcrun devicectl device install app --device "$IPAD_ID" "$APP" > /dev/null
if ! LAUNCH_ERROR="$(xcrun devicectl device process launch --device "$IPAD_ID" "$BUNDLE_ID" 2>&1)"; then
  if grep -q "unlocked" <<< "$LAUNCH_ERROR"; then
    fail "Installed, but the iPad is locked. Unlock it and tap Spell on Words."
  fi
  fail "Installed, but iPadOS wouldn't start it. On the iPad, open Settings → General → VPN & Device Management, trust your Apple ID, and open Spell on Words."
fi
echo "Spell on Words is running on the iPad."
