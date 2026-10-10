#!/usr/bin/env bash
# Prints the quality-loop size metrics as a Markdown table. Run from the repo root.
# Counts tracked files only (git ls-files), so bin/, obj/, node_modules/ and worktrees are excluded.
set -euo pipefail

files() { git ls-files -z -- "$@"; }
loc() { files "$@" | xargs -0 cat 2>/dev/null | wc -l | tr -d ' '; }
count() { files "$@" | tr -cd '\0' | wc -c | tr -d ' '; }
public_types() { files "$@" | xargs -0 grep -hE '^\s*public (sealed |static |abstract |partial |readonly |record |ref )*(class|interface|record|struct|enum|delegate) ' 2>/dev/null | wc -l | tr -d ' '; }
test_methods() { files "$@" | xargs -0 grep -hoE '\[(Fact|Theory)' 2>/dev/null | wc -l | tr -d ' '; }

echo "| Metric | Value |"
echo "|---|---|"
echo "| Commit | \`$(git rev-parse --short HEAD)\` |"
for p in core extensions studio; do
  echo "| **$p** src: projects / C# LOC / public types | $(count "$p/src/*.csproj") / $(loc "$p/src/*.cs") / $(public_types "$p/src/*.cs") |"
  echo "| **$p** Razor LOC | $(loc "$p/src/*.razor") |"
  echo "| **$p** test: projects / C# LOC / [Fact]+[Theory] | $(count "$p/test/*.csproj") / $(loc "$p/test/*.cs") / $(test_methods "$p/test/*.cs") |"
done
echo "| Projects in Elsa.sln | $(grep -c '\.csproj"' Elsa.sln) |"
echo "| Tracked .csproj not in Elsa.sln | $(files '*.csproj' | tr '\0' '\n' | while read -r f; do grep -qF "$(basename "$f")" Elsa.sln || echo "$f"; done | wc -l | tr -d ' ') |"
echo "| Solution filters (.slnf) | $(count '*.slnf') |"
echo "| GitHub workflows: files / lines | $(count '.github/workflows/*.yml') / $(loc '.github/workflows/*.yml') |"
echo "| Tracked files at repo root | $(git ls-files | grep -vc /) |"
echo "| ADRs | $(count 'docs/adr/[0-9]*.md') |"
echo "| Markdown lines: specs / other | $(loc '*/specs/*.md') / $(( $(loc '*.md') - $(loc '*/specs/*.md') )) |"
