#!/usr/bin/env bash
# Prints the quality-loop size metrics as a Markdown table. Run from the repo root.
# Counts tracked files only (git ls-files), so bin/, obj/, node_modules/ and worktrees are excluded.
# A test project is any project under <product>/test or named *Test.csproj / *Tests.csproj
# (Studio keeps its test projects under studio/src).
set -euo pipefail

# Project directories of a product, as pathspecs: kind is "src" or "test".
project_dirs() {
  git ls-files "$1/*.csproj" | awk -v p="$1" -v kind="$2" '{
    is_test = ($0 ~ "^" p "/test/") || ($0 ~ /Tests?\.csproj$/)
    if ((kind == "test") == is_test && (is_test || $0 ~ "^" p "/src/")) { sub(/\/[^\/]+$/, ""); print $0 "/*" }
  }'
}
in_dirs() { local ext=$1; shift; local specs=(); for d in "$@"; do specs+=("${d%\*}*.$ext"); done; [ ${#specs[@]} -gt 0 ] && git ls-files -z -- "${specs[@]}"; }
loc() { xargs -0 cat 2>/dev/null | wc -l | tr -d ' '; }
public_types() { xargs -0 grep -hE '^\s*public (sealed |static |abstract |partial |readonly |record |ref )*(class|interface|record|struct|enum|delegate) ' 2>/dev/null | wc -l | tr -d ' '; }
test_methods() { xargs -0 grep -hoE '\[(Fact|Theory)' 2>/dev/null | wc -l | tr -d ' '; }
count() { git ls-files -z -- "$@" | tr -cd '\0' | wc -c | tr -d ' '; }
loc_of() { git ls-files -z -- "$@" | loc; }

echo "| Metric | Value |"
echo "|---|---|"
echo "| Commit | \`$(git rev-parse --short HEAD)\` |"
for p in core extensions studio; do
  src=(); while IFS= read -r d; do src+=("$d"); done < <(project_dirs "$p" src)
  tst=(); while IFS= read -r d; do tst+=("$d"); done < <(project_dirs "$p" test)
  echo "| **$p** src: projects / C# LOC / public types / Razor LOC | ${#src[@]} / $(in_dirs cs "${src[@]}" | loc) / $(in_dirs cs "${src[@]}" | public_types) / $(in_dirs razor "${src[@]}" | loc) |"
  echo "| **$p** test: projects / C# LOC / [Fact]+[Theory] | ${#tst[@]} / $(in_dirs cs "${tst[@]}" | loc) / $(in_dirs cs "${tst[@]}" | test_methods) |"
done
echo "| Projects in Elsa.sln | $(grep -c '\.csproj"' Elsa.sln) |"
echo "| Tracked .csproj not in Elsa.sln | $(git ls-files '*.csproj' | while read -r f; do grep -qF "$(basename "$f")" Elsa.sln || echo "$f"; done | wc -l | tr -d ' ') |"
echo "| Solution filters (.slnf) | $(count '*.slnf') |"
echo "| GitHub workflows: files / lines | $(count '.github/workflows/*.yml') / $(loc_of '.github/workflows/*.yml') |"
echo "| Tracked files at repo root | $(git ls-files | grep -vc /) |"
echo "| ADRs | $(count 'docs/adr/[0-9]*.md') |"
echo "| Markdown lines: specs / other | $(loc_of '*/specs/*.md') / $(( $(loc_of '*.md') - $(loc_of '*/specs/*.md') )) |"
