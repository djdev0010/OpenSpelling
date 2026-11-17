#!/usr/bin/env bash
# Claude Code PostToolUse hook: after an agent writes or edits a .cs file, check it
# against docs/CODE_STANDARDS.md. Exit code 2 sends the problems back to the agent.
set -u
file_path=$(python3 -c 'import sys,json; print(json.load(sys.stdin).get("tool_input",{}).get("file_path",""))' 2>/dev/null)
[[ "$file_path" == *.cs ]] || exit 0
[[ "$file_path" == */scripts/* ]] && exit 0

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo_root" || exit 0
if ! output=$(dotnet run scripts/CheckStandards.cs -- "$file_path" 2>&1); then
  echo "$output" >&2
  echo "Fix these to follow docs/CODE_STANDARDS.md before moving on." >&2
  exit 2
fi
exit 0
