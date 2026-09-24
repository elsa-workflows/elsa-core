#!/usr/bin/env bash
# Build the two Studio browser bundles from mapped or pinned standalone source.
set -euo pipefail

root="${1:-.}"
root="$(cd "$root" && pwd -P)"
if [[ -f "$root/src/studio/modules/Elsa.Studio.Workflows.Designer/ClientLib/package.json" ]]; then
    studio="$root/src/studio"
else
    studio="$root/src"
fi
designer="$studio/modules/Elsa.Studio.Workflows.Designer"
dom="$studio/framework/Elsa.Studio.DomInterop"
locks="$(cd "$(dirname "${BASH_SOURCE[0]}")/consolidated-build/studio-clientlib-lockfiles" && pwd -P)"

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

for entry in "$designer/ClientLib:$locks/designer.package-lock.json" \
             "$dom/ClientLib:$locks/dom-interop.package-lock.json"; do
    clientlib="${entry%%:*}"
    reviewed_lock="${entry#*:}"
    checkout_lock="$clientlib/package-lock.json"
    if [[ -L "$checkout_lock" ]]; then
        echo "Studio package lockfile cannot be a symlink: $checkout_lock" >&2
        exit 1
    fi
    if [[ -e "$checkout_lock" ]]; then
        if ! cmp -s "$reviewed_lock" "$checkout_lock"; then
            echo "Studio package lockfile differs from the reviewed lock: $checkout_lock" >&2
            exit 1
        fi
    else
        cp "$reviewed_lock" "$checkout_lock"
    fi
done

assets=(
    "$designer/wwwroot/designer.entry.js"
    "$designer/wwwroot/react-designer.entry.js"
    "$designer/wwwroot/designer.css"
    "$dom/wwwroot/dom.entry.js"
    "$dom/wwwroot/clipboard.entry.js"
    "$dom/wwwroot/files.entry.js"
)
for asset in "${assets[@]}"; do
    if [[ -L "$asset" ]]; then
        echo "Studio browser asset cannot be a symlink: $asset" >&2
        exit 1
    fi
    if git -C "$root" ls-files --error-unmatch -- "${asset#"$root"/}" >/dev/null 2>&1; then
        echo "Refusing to remove tracked Studio browser asset: $asset" >&2
        exit 1
    fi
    if [[ -e "$asset" ]]; then
        rm -- "$asset"
    fi
done

dotnet restore "$designer/Elsa.Studio.Workflows.Designer.csproj"
(
    cd "$designer/ClientLib"
    npm ci --force
    npm run check:generated
    npm test
    npm run build
)
(
    cd "$dom/ClientLib"
    npm ci --force
    npm run build
)

for asset in "${assets[@]}"; do
    if [[ ! -s "$asset" ]]; then
        echo "Studio ClientLib build omitted a required browser asset: $asset" >&2
        exit 1
    fi
done

echo "Studio ClientLib proof passed: generated BPMN types, Designer tests, both bundles, six required assets."
