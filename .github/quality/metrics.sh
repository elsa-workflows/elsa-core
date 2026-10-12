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
# Tracked files of a product with extension $3, NUL-separated, for kind "src" or "test". A file is test code
# when it lies under <product>/test, inside a test project, or in a shared testing/ folder linked into test projects.
# Every other file under <product>/src (including shared files outside any project) counts as source.
files_of() {
  { git ls-files "$1/*.csproj"; echo "--"; git ls-files "$1/*.$3"; } | awk -v p="$1" -v kind="$2" '
    /^--$/ { files = 1; next }
    !files { if ($0 ~ "^" p "/test/" || $0 ~ /Tests?\.csproj$/) { d = $0; sub(/\/[^\/]+$/, "/", d); dirs[++n] = d } next }
    { t = ($0 ~ "^" p "/test/") || ($0 ~ /\/testing\//); for (i = 1; i <= n && !t; i++) if (index($0, dirs[i]) == 1) t = 1
      if (kind == "test" ? t : (!t && $0 ~ "^" p "/src/")) printf "%s%c", $0, 0 }'
}
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
  echo "| **$p** src: projects / C# LOC / public types / Razor LOC | ${#src[@]} / $(files_of "$p" src cs | loc) / $(files_of "$p" src cs | public_types) / $(files_of "$p" src razor | loc) |"
  echo "| **$p** test: projects / C# LOC / [Fact]+[Theory] | ${#tst[@]} / $(files_of "$p" test cs | loc) / $(files_of "$p" test cs | test_methods) |"
done
echo "| Projects in Elsa.sln | $(grep -c '\.csproj"' Elsa.sln) |"
echo "| Tracked .csproj not in Elsa.sln | $(git ls-files '*.csproj' | while read -r f; do grep -qF "$(basename "$f")" Elsa.sln || echo "$f"; done | wc -l | tr -d ' ') |"
echo "| Solution filters (.slnf) | $(count '*.slnf') |"
echo "| GitHub workflows: files / lines | $(count '.github/workflows/*.yml') / $(loc_of '.github/workflows/*.yml') |"
echo "| Tracked files at repo root | $(git ls-files | grep -vc /) |"
echo "| ADRs | $(count 'docs/adr/[0-9]*.md') |"
echo "| Markdown lines: specs / other | $(loc_of '*/specs/*.md') / $(( $(loc_of '*.md') - $(loc_of '*/specs/*.md') )) |"
