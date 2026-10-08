# Reconcile the imported Dependabot policies

The consolidated repository has one root Dependabot file. Keep Core's weekly NuGet update entry at `/`, correct its ecosystem spelling to Dependabot's accepted lowercase `nuget`, and exclude `src/extensions/**` and `src/studio/**` from that scan. The imported product policies then run against their relocated manifest trees, `/src/extensions` and `/src/studio`, with the Core entry excluding those same manifests from its recursive root scan.

Both product entries retain their daily schedule, `main` target, Feedz preview registry, one-open-PR limit, and `deps` commit prefix. Extensions keeps its `Elsa*` group. The source policies' Elsa-only allowlists are deliberately dropped: once the weekly root scan excludes the relocated product trees, those allowlists would also suppress third-party package updates for those trees. The product entries therefore scan all NuGet dependencies, including the actual `Elsa.Platform.PackageManifest.Generator` ID, without adding another ecosystem. The registry credential remains `FEEDZ_API_KEY`; availability of that repository secret and Dependabot's acceptance of the final config remain review gates. The ledger rows stay pending until the decision is merged and the validator can record the reviewed PR and merge commit.

The active configuration and ledger cover exactly the relocated policy paths; the inert `.source` files remain provenance records. No publisher workflow is changed.

## Post-landing verification (2026-09-29)

After #8409 landed on `main` (`8b34ab1e`), Dependabot ran every entry. The two product jobs returned green Actions status, with the `elsa-feedz-preview` registry and its `FEEDZ_API_KEY` credential configured:
- `nuget in /src/extensions`: [run 36503465765](https://github.com/elsa-workflows/elsa-core/actions/runs/36503465765)
- `nuget in /src/studio`: [run 36503465892](https://github.com/elsa-workflows/elsa-core/actions/runs/36503465892)

Both ledger rows are represented by the root `.github/dependabot.yml`. The green job statuses with empty product discovery do not establish that the secret was exercised or that credential/feed resolution and product central-version evaluation succeeded. Those acceptance gates remain open under Task #8636.

The same run exposed a separate, older gap in the Core root entry. `nuget in /.` ([run 36503466268](https://github.com/elsa-workflows/elsa-core/actions/runs/36503466268)) failed with `private_source_authentication_failure`: the root `NuGet.Config` lists the Elsa preview and Loom Feedz sources, but the root entry declared no registry, so Dependabot's proxy refused both hosts ("egress not allowlisted"). The root entry now declares `elsa-feedz-preview` and a token-less `valence-loom-feedz` registry. The Loom feed is public.

## Recursive discovery correction (2026-10-06)

[PR #8640](https://github.com/elsa-workflows/elsa-core/pull/8640) corrects the
product selectors to `/src/extensions/**/Elsa.*` and `/src/studio/**/Elsa.*`
after hosted runs missed nested project manifests. The two ledger rows pin the
current root configuration blob and cite this PR as their active update. Their
original disposition PR, merge commit, source blobs, modes and archived receipt
pins remain historical evidence. The pinned E96 receipt audit still compares
the same 163 source assets and validates the current ledger separately.

The earlier green Actions statuses remain historical run evidence. Empty
product discovery establishes no credential/feed or central-version acceptance.
The [recursive discovery correction](dependabot-recursive-discovery.md)
records tracked-project coverage and the remaining hosted project evaluation,
product central-version and feed-resolution gates under Task #8636. Those gates
remain open.
