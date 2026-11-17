# Spelling Bee

Offline spelling practice app for a 5-year-old. Avalonia (C#) desktop app, macOS first;
Windows and iOS later via adapter projects.

@AGENTS.md
@docs/CODE_STANDARDS.md
@docs/ARCHITECTURE.md

- Models are not committed; scripts/get-models.sh downloads them into models/.
- When delegating to a subagent, tell it to read AGENTS.md, docs/ARCHITECTURE.md and docs/CODE_STANDARDS.md first.
