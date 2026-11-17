# models/

The voice and speech models are not committed (they are about 780 MB).
Download them with:

```bash
scripts/get-models.sh
```

After that, this folder holds:

| Folder | What it is |
|---|---|
| `kokoro-v1_0-int8/` | Kokoro voice: `model.int8.onnx`, `voices.bin`, `tokens.txt`, `lexicon-us-en.txt` |
| `parakeet-tdt-0.6b-v2-int8/` | Parakeet recognizer: `encoder/decoder/joiner.int8.onnx`, `tokens.txt` |
| `vad/silero_vad.onnx` | Voice-activity detection |

The app looks for this folder in this order:
1. the `SPELLINGBEE_MODELS` environment variable;
2. `Contents/Resources/models` inside the .app;
3. a `models/` folder above the running program (this one, when running from source).

Licenses are listed in `docs/THIRD_PARTY.md`.
