# Elsa.Slack package-scoped publisher handoff preflight

This check prepares a future publisher cutover. It does not change publisher ownership, edit a package-publishing workflow, or publish a package. The release-unit manifest records `elsa-extensions` and its `packages.yml` workflow as the sole current Elsa.Slack publisher. The package proof source commits and official package SHA-256 are pinned in the manifest; mapped-source commits are pinned separately. The proof scripts read those pins from the manifest so a stale handoff receipt fails against the reviewed source snapshot.

Run the current-owner preflight and tooling tests from the repository root:

```sh
python3 scripts/integration-program/check_publisher_handoff.py \
  --manifest doc/integration-program/release-units.json
python3 -m unittest discover -s scripts/integration-program -p 'test_*.py'
python3 -O -m unittest discover -s scripts/integration-program -p 'test_*.py'
```

The preflight requires exactly one current publisher per package ID. A proposed publisher must include a versioned simulation receipt that records the same package and source pins, an approved review reference, stable release version, exact old/new workflow identities, hashes and evidence links, and timezone-qualified observation times proving **Elsa.Slack was excluded from the old workflow before it was enabled in the new one**. The receipt explicitly scopes both changes to this package and requires unrelated publishing to remain unchanged. It does not require disabling the entire Extensions workflow: that workflow also publishes other package IDs. A version-1 receipt claiming whole-workflow disablement is rejected. The existing Core `packages.yml` is an all-solution publisher and is not a valid target for this bounded Slack handoff; a reviewed, dedicated `.github/workflows/release-elsa-slack.yml` path is the planned target.

The local `-proof` version is never valid as a release version. The current manifest pins 3.8.4 as the latest stable version observed in the [official NuGet package index](https://api.nuget.org/v3-flatcontainer/elsa.slack/index.json) on 2026-09-24; the preflight rejects that version and older versions. Simulations return the current owner unchanged and explicitly report `publication_performed=false`, `live_publisher_changed=false` and `live_feed_history_verified=false`. Refresh the official and current-publisher feed history before any real version allocation; the pinned floor is only a guard against already-known reuse.

The pull-request lane runs the manifest tests and current-owner preflight with read-only repository permissions. The lane has no publisher credential or publish step. The separate production package workflows are not modified by this change.

## Cutover artifact and execution order

The reviewed cutover change in Extensions must exclude only `Elsa.Slack` from `Compile+Test+Pack` output on every branch and release event that feeds `.github/workflows/packages.yml`. Leave its other package artifacts and both publishing jobs intact. Prove this with an isolated build that lists every produced `.nupkg` and `.snupkg`, shows no `Elsa.Slack` package, and compares unrelated package IDs against an equivalent pre-change build. A workflow-level disable would interrupt unrelated Extensions releases and does not satisfy this handoff.

The reviewed Core change must add a dedicated, initially inert Slack workflow. Its pack command must target only `src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj`, explicitly override that imported subtree's default `IsPackable=false`, set one independently allocated `PackageVersion`, and verify exactly one `Elsa.Slack` `.nupkg` and `.snupkg` before any push. Retain the imported subtree's default nonpackable state so the existing Core `packages.yml` cannot accidentally republish it. The dedicated workflow needs a protected approval gate and no automatic package-feed publication trigger before the reserved publication approval. Its publisher identity and feed credentials must be checked against the actual NuGet/Feedz policy when approved, rather than inferred from a local pack.

Execute the approved handoff in this order: freeze both source tips and package-feed version history; merge the Extensions Slack-only exclusion with publication suppressed, then independently verify its live workflow and absence of a Slack artifact while other package IDs remain packable. Verify no in-flight old-publisher job can still push Slack. Only then enable the dedicated Core publisher, record both immutable workflow hashes and observation times in a schema-2 receipt, and update the manifest's sole current owner. Run the local artifact/clean-consumer/SourceLink proof again on the exact release head. Package-feed publication is a separate, explicitly approved step. If enabling Core fails, disable its Slack publisher first, confirm no in-flight Core Slack push, then restore Slack eligibility in Extensions; never operate two eligible publishers or reuse a published version.

The receipt validator checks its declared scope, order, version, source pins and evidence references; it does **not** inspect live GitHub workflows, prove that an artifact list is complete, authorize a feed push, or confirm a production handoff. Those are operator checks using the exact reviewed commits and run artifacts.

## Results and limits

On the refreshed `origin/main` base, the current-owner preflight reports Extensions as the sole publisher. Negative tests cover missing/dual owners, malformed or stale source pins, local proof-version reuse, missing or unapproved receipts, whole-workflow receipts, another package ID, non-scoped changes, and enabling the new path before the old package is excluded. A valid synthetic receipt tests transition rules without creating or accepting live cutover evidence.

This is not approval to cut over and does not establish the current state of either remote workflow. Once approved and executed, update the manifest's sole current publisher to Core while retaining historical Extensions source provenance. This preparation itself needs no rollback because it leaves live ownership and workflows unchanged; revert its tooling/docs change if it must be withdrawn.
