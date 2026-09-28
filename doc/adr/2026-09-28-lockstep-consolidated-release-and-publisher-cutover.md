# Release the consolidated packages in lockstep and cut publishers over at 3.10.0

- Status: Accepted
- Date: 2026-09-28
- Related: #8194, #8249, #8259, #8260, #8286; defers the per-package version policy of [Bounded connector release unit and package proof](2026-09-23-bounded-connector-release-unit.md)

## Context

After the history import (#8286), Core, Extensions and Studio source live in one repository and one solution, `Elsa.sln`. Until now each repository published its own packages, and all three released in lockstep: every Core, Studio and Extensions package on NuGet is at 3.8.4. `main` is the 3.10 line, and 3.9 is a maintenance line.

The bounded release-unit ADR proposed a separate, monotonic version stream for each package ID, starting with `Elsa.Slack`. #8260 is proving that a connector can be packed and consumed on its own, using local artifacts only. That proof does not settle how the first consolidated release is versioned, or when the Extensions and Studio publishers stop.

Two publishers for one package ID and version line would let them race or overwrite each other's intent. Changing how consumers find matching versions, at the same time as the repositories merge, adds risk to a release that already changes a lot.

## Decision

1. **The first consolidated release is lockstep.** The root `packages.yml` packs every packable project in `Elsa.sln` at one version, 3.10.0. That is greater than every published version of every affected package ID, so each ID stays monotonic. Until the cutover, the import keeps every Extensions and Studio project unpackable through a product-wide `IsPackable=false` in `src/extensions` and `src/studio` (`Directory.Build.props` and `Directory.Build.targets`). The root workflow therefore packs only Core today. Per-package version streams, as proposed for `Elsa.Slack`, need a later ADR that accepts them once #8260's proof is complete.
2. **elsa-core becomes the sole publisher at 3.10.0.** The Extensions and Studio repositories keep publishing 3.8.x and 3.9.x patches for their own packages. The cutover is one reviewed change, made when 3.10.0 is cut:
   - lift the product-wide `IsPackable=false` so the root pack includes the imported packages;
   - verify the produced package set against the Extensions and Studio package IDs;
   - disable the original repositories' publishing for 3.10 and later.

   From then on, each package ID has one publisher per version line.
3. **Publication stays behind the existing gates.** The root workflow publishes only on an approved `workflow_dispatch` ([package publisher gates](../integration-program/consolidation/package-publisher-gates.md)), and 3.10.x goes only to Feedz. The imported Extensions and Studio `packages.yml` files stay inert at their `.source` paths.
4. **Deprecated packages get no consolidated version.** The legacy Extensions Secrets packages (`Elsa.Secrets.Api`, `.Core`, `.Management`, `.Models`, `.Scripting`) also carry their own `IsPackable=false`, so they stay out when the cutover lifts the product-wide setting. A guard test checks it. They are marked deprecated on NuGet at 3.10.0 and keep their 3.8.x versions ([upgrade guide](../migrations/secrets-legacy-extensions-upgrade.md)).
5. **The #8260 proof publishes nothing.** It runs on the import head with a clean local feed and consumer restores. No proof package goes to Feedz or NuGet.org.

## Consequences

Consumers keep the rule they know: take the same version of every Elsa package. The first consolidated release has one version to allocate and one publisher to operate.

Packing the whole solution also packs connectors that did not change. That is the cost the release-unit ADR set out to avoid, and it is accepted for 3.10.0. The per-package policy stays proposed, not rejected.

The cutover is a maintainer step at release time and is not automated here. Until it happens, no imported package can be published from elsa-core, even by an approved dispatch. Until then, a 3.8 or 3.9 fix for an Extensions or Studio package is released from its original repository, and has to be ported to `main` separately.
