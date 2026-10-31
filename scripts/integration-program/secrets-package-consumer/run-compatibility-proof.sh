#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
temp_dir="$(mktemp -d "${TMPDIR:-/tmp}/elsa-secrets-package-consumer.XXXXXX")"
trap 'rm -rf "$temp_dir"' EXIT

frameworks=(net8.0 net9.0 net10.0)
PYTHONDONTWRITEBYTECODE=1 python3 "$script_dir/test_verify_expected_break.py"
projects=(
  "$script_dir/legacy-baseline/LegacyBaseline.csproj"
  "$script_dir/core-consumer/CoreConsumer.csproj"
)

for project in "${projects[@]}"; do
  dotnet restore "$project" \
    --configfile "$script_dir/NuGet.Config" \
    --locked-mode \
    --packages "$temp_dir/packages"
done

for framework in "${frameworks[@]}"; do
  dotnet build "${projects[0]}" --no-restore --configuration Release --framework "$framework" --verbosity minimal
  printf 'PASS: Extensions 3.8.1 legacy API consumer compiles on %s\n' "$framework"

  dotnet build "${projects[1]}" --no-restore --configuration Release --framework "$framework" --verbosity minimal
  printf 'PASS: Core 3.8.4 consumer API compiles on %s\n' "$framework"

  log_file="$temp_dir/core-legacy-${framework}.log"
  if dotnet build "${projects[1]}" --no-restore --configuration Release --framework "$framework" --verbosity minimal --no-incremental -p:IncludeLegacyConsumer=true >"$log_file" 2>&1; then
    cat "$log_file"
    printf 'FAIL: legacy API unexpectedly compiled against Core 3.8.4 on %s\n' "$framework" >&2
    exit 1
  fi
  if ! PYTHONDONTWRITEBYTECODE=1 python3 "$script_dir/verify_expected_break.py" "$log_file"
  then
    cat "$log_file"
    printf 'FAIL: Core 3.8.4 failed for a reason other than the absent legacy SecretsDbContext API on %s\n' "$framework" >&2
    exit 1
  fi
  printf 'EXPECTED BREAK: Core 3.8.4 does not expose the legacy SecretsDbContext API on %s\n' "$framework"
done
