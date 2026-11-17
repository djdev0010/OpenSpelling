#!/usr/bin/env bash
# Downloads the offline voice and speech models into models/ (about 600 MB to download,
# about 780 MB on disk). Run once after cloning. Safe to run again: models already
# present are skipped.
set -euo pipefail

RELEASES="https://github.com/k2-fsa/sherpa-onnx/releases/download"
MODELS="$(cd "$(dirname "$0")/.." && pwd)/models"
DOWNLOADS="$(mktemp -d)"
trap 'rm -rf "$DOWNLOADS"' EXIT

mkdir -p "$MODELS"

download_and_extract() {
  local archive_path="$1"
  echo "Downloading $(basename "$archive_path")…"
  curl -fL --progress-bar -o "$DOWNLOADS/archive.tar.bz2" "$RELEASES/$archive_path"
  tar -xjf "$DOWNLOADS/archive.tar.bz2" -C "$DOWNLOADS"
  rm "$DOWNLOADS/archive.tar.bz2"
}

# Kokoro voice (text to speech). Only the US English lexicon files are kept, so no espeak-ng (GPL).
KOKORO="$MODELS/kokoro-v1_0-int8"
if [[ ! -f "$KOKORO/model.int8.onnx" ]]; then
  download_and_extract "tts-models/kokoro-int8-multi-lang-v1_0.tar.bz2"
  mkdir -p "$KOKORO"
  for file in model.int8.onnx voices.bin tokens.txt lexicon-us-en.txt LICENSE README.md; do
    [[ -f "$DOWNLOADS/kokoro-int8-multi-lang-v1_0/$file" ]] && mv "$DOWNLOADS/kokoro-int8-multi-lang-v1_0/$file" "$KOKORO/"
  done
else
  echo "Kokoro voice already present."
fi

# Parakeet TDT 0.6B v2 (speech to text). Hears spoken letter names much better than the 110M model.
PARAKEET="$MODELS/parakeet-tdt-0.6b-v2-int8"
if [[ ! -f "$PARAKEET/encoder.int8.onnx" ]]; then
  download_and_extract "asr-models/sherpa-onnx-nemo-parakeet-tdt-0.6b-v2-int8.tar.bz2"
  mkdir -p "$PARAKEET"
  for file in encoder.int8.onnx decoder.int8.onnx joiner.int8.onnx tokens.txt README.md; do
    mv "$DOWNLOADS/sherpa-onnx-nemo-parakeet-tdt-0.6b-v2-int8/$file" "$PARAKEET/"
  done
else
  echo "Parakeet recognizer already present."
fi

# Silero voice-activity detection (finds where speech starts and stops).
if [[ ! -f "$MODELS/vad/silero_vad.onnx" ]]; then
  mkdir -p "$MODELS/vad"
  echo "Downloading silero_vad.onnx…"
  curl -fL --progress-bar -o "$MODELS/vad/silero_vad.onnx" "$RELEASES/asr-models/silero_vad.onnx"
else
  echo "Voice-activity model already present."
fi

echo "Models are ready in $MODELS"
