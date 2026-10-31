#!/usr/bin/env bash
# Build the two Studio browser bundles from mapped or pinned standalone source.
# Set STUDIO_CLIENTLIB_SKIP_TESTS=1 to build the bundles without running the ClientLib tests (image builds).
set -euo pipefail

skip_tests="${STUDIO_CLIENTLIB_SKIP_TESTS:-0}"

root="${1:-.}"
root="$(cd "$root" && pwd -P)"
if [[ -f "$root/studio/src/modules/Elsa.Studio.Workflows.Designer/ClientLib/package.json" ]]; then
    studio="$root/studio/src"
elif [[ -f "$root/src/studio/modules/Elsa.Studio.Workflows.Designer/ClientLib/package.json" ]]; then
    studio="$root/src/studio"
else
    studio="$root/src"
fi
designer="$studio/modules/Elsa.Studio.Workflows.Designer"
dom="$studio/framework/Elsa.Studio.DomInterop"
# The BPMN designer contract and shared rules. It builds no browser asset of its own, and older pinned Studio sources
# predate it, so it is tested only where it exists.
bpmn_shared="$studio/modules/Elsa.Studio.Bpmn/ClientLib"
# The bpmn-js and X6 designers bundle the shared rules, so each is built only where they and it exist.
bpmn_js="$studio/modules/Elsa.Studio.Bpmn.BpmnJs"
bpmn_x6="$studio/modules/Elsa.Studio.Bpmn.X6"
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

clientlibs=("$designer/ClientLib:$locks/designer.package-lock.json" \
            "$dom/ClientLib:$locks/dom-interop.package-lock.json")
if [[ -f "$bpmn_shared/package.json" ]]; then
    clientlibs+=("$bpmn_shared:$locks/bpmn-shared.package-lock.json")
fi
if [[ -f "$bpmn_shared/package.json" && -f "$bpmn_js/ClientLib/package.json" ]]; then
    clientlibs+=("$bpmn_js/ClientLib:$locks/bpmn-js.package-lock.json")
fi
if [[ -f "$bpmn_shared/package.json" && -f "$bpmn_x6/ClientLib/package.json" ]]; then
    clientlibs+=("$bpmn_x6/ClientLib:$locks/bpmn-x6.package-lock.json")
fi

for entry in "${clientlibs[@]}"; do
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
if [[ -f "$bpmn_shared/package.json" && -f "$bpmn_js/ClientLib/package.json" ]]; then
    assets+=("$bpmn_js/wwwroot/bpmn-js-designer.js" "$bpmn_js/wwwroot/bpmn-js-designer.css")
fi
if [[ -f "$bpmn_shared/package.json" && -f "$bpmn_x6/ClientLib/package.json" ]]; then
    assets+=("$bpmn_x6/wwwroot/x6-designer.js" "$bpmn_x6/wwwroot/x6-designer.css")
fi
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
    if [[ "$skip_tests" != 1 ]]; then
        npm test
    fi
    npm run build
)
(
    cd "$dom/ClientLib"
    npm ci --force
    npm run build
)
if [[ -f "$bpmn_shared/package.json" ]]; then
    (
        # The bpmn-js and X6 builds resolve the shared rules' dependencies from here, so install even without tests.
        cd "$bpmn_shared"
        npm ci --force
        if [[ "$skip_tests" != 1 ]]; then
            npm test
        fi
    )
fi
if [[ -f "$bpmn_shared/package.json" && -f "$bpmn_js/ClientLib/package.json" ]]; then
    (
        # The build also enforces the designer's size budget.
        cd "$bpmn_js/ClientLib"
        npm ci --force
        npm run build
    )
fi
if [[ -f "$bpmn_shared/package.json" && -f "$bpmn_x6/ClientLib/package.json" ]]; then
    (
        # The build also enforces the designer's size budget.
        cd "$bpmn_x6/ClientLib"
        npm ci --force
        npm run build
    )
fi

for asset in "${assets[@]}"; do
    if [[ ! -s "$asset" ]]; then
        echo "Studio ClientLib build omitted a required browser asset: $asset" >&2
        exit 1
    fi
done

if [[ "$skip_tests" == 1 ]]; then
    echo "Studio ClientLib build passed (tests skipped): generated BPMN types, the Studio bundles and ${#assets[@]} required assets."
else
    echo "Studio ClientLib proof passed: generated BPMN types, Designer tests, the Studio bundles and ${#assets[@]} required assets."
fi
