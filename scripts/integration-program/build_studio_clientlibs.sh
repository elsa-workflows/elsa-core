#!/usr/bin/env bash
# Build the two Studio browser bundles from a consolidated elsa-core checkout.
set -euo pipefail

root="${1:-.}"
root="$(cd "$root" && pwd -P)"
designer="$root/src/studio/modules/Elsa.Studio.Workflows.Designer"
dom="$root/src/studio/framework/Elsa.Studio.DomInterop"

for package in "$designer/ClientLib/package.json" "$dom/ClientLib/package.json"; do
    if [[ ! -f "$package" ]]; then
        echo "Missing consolidated Studio source: $package" >&2
        exit 1
    fi
done

node_major="$(node -p 'process.versions.node.split(".")[0]')"
if [[ "$node_major" != 22 ]]; then
    echo "Studio ClientLib proof requires Node 22; found $(node --version)" >&2
    exit 1
fi

# --package-lock=false avoids creating untracked lockfiles absent from the pinned
# Studio source. The standalone Studio PR build likewise installs with --force.
dotnet restore "$designer/Elsa.Studio.Workflows.Designer.csproj"
(
    cd "$designer/ClientLib"
    npm install --force --package-lock=false --no-save
    npm run check:generated
    npm test
    npm run build
)
(
    cd "$dom/ClientLib"
    npm install --force --package-lock=false --no-save
    npm run build
)

for asset in \
    "$designer/wwwroot/designer.entry.js" \
    "$designer/wwwroot/react-designer.entry.js" \
    "$designer/wwwroot/designer.css" \
    "$dom/wwwroot/dom.entry.js" \
    "$dom/wwwroot/clipboard.entry.js" \
    "$dom/wwwroot/files.entry.js"; do
    if [[ ! -s "$asset" ]]; then
        echo "Studio ClientLib build omitted a required browser asset: $asset" >&2
        exit 1
    fi
done

echo "Studio ClientLib proof passed: generated BPMN types, Designer tests, both bundles, six required assets."
