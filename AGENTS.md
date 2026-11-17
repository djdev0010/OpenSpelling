# Instructions for AI coding agents

These rules apply to every agent working in this repository (Claude Code, Codex,
Copilot, Cursor, or any other).

1. **Read [docs/CODE_STANDARDS.md](docs/CODE_STANDARDS.md) before writing code** and follow
   it: architecture and dependency direction, code structure, size limits, patterns,
   and descriptive names.
2. **Run the checker before you finish:**
   `dotnet run scripts/CheckStandards.cs` must print `Code standards: OK`.
   Claude Code runs it automatically after each `.cs` edit (see `.claude/settings.json`).
3. **Run the tests:** `dotnet test` must pass.
4. Read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) to know where code goes. Domain and
   Application never do I/O or reference UI or native libraries. Every interface goes in
   `src/SpellingBee.Application/Ports/`, and platform code goes in an Infrastructure project.
   Screens call only `XxxUseCase` classes.
5. No network calls at runtime, no telemetry, open-source dependencies only.
6. If a rule gets in the way of readable code, don't silently break it. Propose a change
   to docs/CODE_STANDARDS.md.
