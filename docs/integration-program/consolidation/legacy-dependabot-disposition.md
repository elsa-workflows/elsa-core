# Reconcile the imported Dependabot policies

## Current bounded policy under Task #8636

The October 8 owner decision permits coordinated Core, Extensions and Studio
proposals with affected-product CI. [PR #8719](https://github.com/elsa-workflows/elsa-core/pull/8719)
merged a root-solution job plus the retained testing project. Its actual managed
run [38108457632](https://github.com/elsa-workflows/elsa-core/actions/runs/38108457632)
reached the 55-minute limit with ongoing Core strategy starts, before any
Extensions or Studio strategy start. It did not satisfy operational acceptance.

[PR #8724](https://github.com/elsa-workflows/elsa-core/pull/8724) prepares five
project-parent scheduling units: Core production, Core tests, Extensions, Studio,
and shared build/tooling. Static coverage preserves all 356 selected projects,
including all 173 Extensions/Studio projects, the retained testing project, and
separate root tool-manifest coverage. Both Feedz declarations and the
`FEEDZ_API_KEY` name remain; no package version or publisher changes.

Daily jobs are staggered from 06:00 through 18:00 Europe/Amsterdam. Each keeps
a one-PR cap and the `Elsa*` group, so up to five version-update PRs can be open.
Staggering does not enforce a global cap. Shared central files, tool manifests
and referenced projects can overlap; the integration owner reconciles proposals
and requires all affected-product CI and ordinary review/merge gates.

The ledger preserves the original #8495 decision, merge and imported source pins.
Its active configuration blob includes the subsequent React Studio npm entry in
[PR #8925](https://github.com/elsa-workflows/elsa-core/pull/8925), and its active
update links to that PR. The separate weekly npm job has a one-PR cap and preserves
all five NuGet entries unchanged. #8724, #8640 and #8719 remain historical evidence. The
[bounded discovery record](dependabot-recursive-discovery.md) records the complete
census, static import checks and remaining native/managed acceptance gates.
This does not claim approval or merge of #8724, completed evaluated discovery,
feed resolution or Task acceptance. Existing Studio proposal #8673 remains a
bounded proposal, not complete inventory evidence.

## Historical separate product policies (superseded)

The former policy retained Core's weekly NuGet update entry at `/`, corrected its
ecosystem spelling to lowercase `nuget`, and excluded `src/extensions/**` and
`src/studio/**`. Imported product entries scanned the then-relocated trees at
`/src/extensions` and `/src/studio` separately. These paths predate the sibling
`core/`, `extensions/` and `studio/` layout and are not current instructions.

Both product entries retained their daily schedule, `main` target, Feedz preview
registry, one-open-PR limit and `deps` commit prefix. Extensions retained its
`Elsa*` group. Their Elsa-only allowlists were dropped so third-party dependencies
in the excluded product trees could receive updates, including the actual
`Elsa.Platform.PackageManifest.Generator` ID. The credential name remained
`FEEDZ_API_KEY`. The original decision required a reviewed merge and ledger pins;
its provenance remains preserved alongside the subsequent corrections.

The former root configuration represented the relocated policy paths; inert
`.source` files retained source provenance. No publisher workflow changed.

## Post-landing verification (2026-09-29)

After #8409 landed on `main` (`8b34ab1e`), Dependabot ran every entry. The two product jobs returned green Actions status, with the `elsa-feedz-preview` registry and its `FEEDZ_API_KEY` credential configured:
- `nuget in /src/extensions`: [run 36503465765](https://github.com/elsa-workflows/elsa-core/actions/runs/36503465765)
- `nuget in /src/studio`: [run 36503465892](https://github.com/elsa-workflows/elsa-core/actions/runs/36503465892)

Both ledger rows are represented by the root `.github/dependabot.yml`. The green job statuses with empty product discovery do not establish that the secret was exercised or that credential/feed resolution and product central-version evaluation succeeded. Those acceptance gates remain open under Task #8636.

The same run exposed a separate, older gap in the Core root entry. `nuget in /.` ([run 36503466268](https://github.com/elsa-workflows/elsa-core/actions/runs/36503466268)) failed with `private_source_authentication_failure`: the root `NuGet.Config` lists the Elsa preview and Loom Feedz sources, but the root entry declared no registry, so Dependabot's proxy refused both hosts ("egress not allowlisted"). The root entry now declares `elsa-feedz-preview` and a token-less `valence-loom-feedz` registry. The Loom feed is public.

## Recursive discovery correction (2026-10-06)

[PR #8640](https://github.com/elsa-workflows/elsa-core/pull/8640) corrected the
product selectors to `/src/extensions/**/Elsa.*` and `/src/studio/**/Elsa.*`
after hosted runs missed nested project manifests. At that checkpoint, the two
ledger rows pinned the then-current root configuration blob and cited #8640 as
their active update. The current coordinated correction is recorded separately
above. The original disposition PR, merge commit, source blobs, modes and
archived receipt pins remain historical evidence. The pinned E96 receipt audit still compares
the same 163 source assets and validates the current ledger separately.

The earlier green Actions statuses remain historical run evidence. Empty
product discovery establishes no credential/feed or central-version acceptance.
The [recursive discovery correction](dependabot-recursive-discovery.md)
records tracked-project coverage and the remaining hosted project evaluation,
product central-version and feed-resolution gates under Task #8636. Those gates
remain open.
