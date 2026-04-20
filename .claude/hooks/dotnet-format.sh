#!/usr/bin/env bash
# Post-edit hook : auto-format C# files after Edit/Write/MultiEdit.
# Reçoit le payload JSON du hook Claude Code sur stdin.
# Silencieux et non bloquant : une erreur de format ne fait jamais échouer le tool call.

set -uo pipefail

payload="$(cat)"

# Extraction de tool_input.file_path depuis le JSON payload.
if command -v jq >/dev/null 2>&1; then
  file_path="$(printf '%s' "$payload" | jq -r '.tool_input.file_path // empty')"
else
  # Fallback sans jq : grep du premier file_path
  file_path="$(printf '%s' "$payload" \
    | grep -oE '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' \
    | head -n1 \
    | sed -E 's/.*"([^"]*)"$/\1/')"
fi

# Sort si pas de fichier, ou si pas du C#
[[ -n "${file_path:-}" ]] || exit 0
[[ "$file_path" == *.cs ]] || exit 0
[[ -f "$file_path" ]] || exit 0

# Format ciblé, silencieux, jamais bloquant
dotnet format --include "$file_path" >/dev/null 2>&1 || true
exit 0
