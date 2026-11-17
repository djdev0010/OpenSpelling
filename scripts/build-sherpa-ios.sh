#!/usr/bin/env bash
# Builds sherpa-onnx for the iPad (device + simulator) as native/ios/SherpaOnnxC.xcframework.
# The sherpa-onnx NuGet package has no iOS library, so this builds the same version from source.
# Needs Xcode and cmake. Takes 20-40 minutes the first time; later runs reuse the build.
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
SHERPA_VERSION="2.0.1"          # keep equal to org.k2fsa.sherpa.onnx in Directory.Packages.props
ONNXRUNTIME_VERSION="1.28.2"    # the version sherpa-onnx's iOS script expects
# sherpa-onnx's build breaks on paths with spaces (like "Spelling app"), so build outside the repo.
WORK="$HOME/Library/Caches/SpellingBee/sherpa-onnx-$SHERPA_VERSION"
BUILD_DIR="build-ios-shared-sherpa-with-static-onnxruntime"
OUT="$REPO/native/ios"

if [ ! -d "$WORK" ]; then
  git clone --depth 1 --branch "v$SHERPA_VERSION" https://github.com/k2-fsa/sherpa-onnx.git "$WORK"
fi
cd "$WORK"

# The upstream script downloads onnxruntime with wget; fetch it with curl so wget isn't needed.
ORT_ROOT="$BUILD_DIR/ios-onnxruntime"
if [ ! -d "$ORT_ROOT/$ONNXRUNTIME_VERSION/onnxruntime.xcframework" ]; then
  mkdir -p "$ORT_ROOT/$ONNXRUNTIME_VERSION"
  curl -fL -o "$ORT_ROOT/$ONNXRUNTIME_VERSION/onnxruntime.zip" \
    "https://github.com/csukuangfj/onnxruntime-libs/releases/download/v$ONNXRUNTIME_VERSION/onnxruntime-ios-static-xcframework-$ONNXRUNTIME_VERSION.xcframework.zip"
  (cd "$ORT_ROOT/$ONNXRUNTIME_VERSION" && unzip -q onnxruntime.zip && rm onnxruntime.zip)
  ln -sfn "$ONNXRUNTIME_VERSION/onnxruntime.xcframework" "$ORT_ROOT/onnxruntime.xcframework"
fi

./build-ios-shared-sherpa-with-static-onnxruntime.sh

mkdir -p "$OUT"
rm -rf "$OUT/SherpaOnnxC.xcframework"
cp -R "$BUILD_DIR/SherpaOnnxC.xcframework" "$OUT/"
echo "Built $OUT/SherpaOnnxC.xcframework"
