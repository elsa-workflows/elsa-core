#!/usr/bin/env bash
set -euo pipefail
spike_root=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
cd "$spike_root"
mkdir -p artifacts/feed

# Use a fresh package cache for each proof. Repacking an unchanged prerelease
# version must not silently reuse the previous run's installed module assemblies.
export NUGET_PACKAGES
NUGET_PACKAGES=$(mktemp -d "$spike_root/artifacts/packages.XXXXXX")

cd client
CI=true pnpm install --frozen-lockfile
pnpm typecheck
pnpm build
cd ..
dotnet pack Runtime/Elsa.ReactStudio.Spike.csproj -o artifacts/feed -m:2
dotnet pack Labels/Elsa.ReactStudio.Spike.Labels.csproj -o artifacts/feed -m:2
dotnet pack ThirdParty/Example.ReactStudio.Spike.Weather.csproj -o artifacts/feed -m:2
dotnet build Host/Host.csproj -m:2

cd client
pnpm test
