# Elsa.Slack publisher handoff preflight

This check prepares a future publisher cutover. It does not change publisher ownership, edit a package-publishing workflow, or publish a package. The release-unit manifest records `elsa-extensions` and its `packages.yml` workflow as the sole current Elsa.Slack publisher. The package proof source commits and official package SHA-256 are pinned in the manifest; mapped-source commits are pinned separately. The proof scripts read those pins from the manifest so a stale handoff receipt fails against the reviewed source snapshot.

Run the current-owner preflight and tooling tests from the repository root:

```sh
python3 scripts/integration-program/check_publisher_handoff.py \
  --manifest doc/integration-program/release-units.json
python3 -m unittest discover -s scripts/integration-program -p 'test_*.py'
python3 -O -m unittest discover -s scripts/integration-program -p 'test_*.py'
```

The preflight requires exactly one current publisher per package ID. A proposed publisher must include a versioned simulation receipt that records the same package and source pins, an approved review reference, stable release version, exact old/new workflow identities, hashes and evidence links for both workflow states, and timezone-qualified observation times proving the old workflow was disabled first. The local `-proof` version is never valid as a release version. Simulations return the current owner unchanged and explicitly report `publication_performed=false` and `live_publisher_changed=false`.

The pull-request lane runs the manifest tests and current-owner preflight with read-only repository permissions. The lane has no publisher credential or publish step. The separate production package workflows are not modified by this change.

## Results and limits

On the refreshed `origin/main` base, the current-owner preflight passed and reported Extensions as the sole publisher. The tooling suite passed all 223 tests in both normal and optimized Python modes. Negative tests cover missing/dual owners, malformed or stale source pins, local proof-version reuse, missing or unapproved receipts, missing evidence that the old publisher is disabled, and enabling the new path before the old path is disabled. A valid synthetic receipt tests the transition rules without creating or accepting live cutover evidence.

This is not approval to cut over and does not establish the current state of either remote workflow. For a future handoff, attach immutable workflow-file and run evidence to an explicitly reviewed receipt, prove the Extensions workflow is disabled before enabling the Core workflow, then separately verify package provenance and the stable package version. If a future cutover fails, disable the new publisher first and restore the old publisher only after confirming the new path is disabled; never allow both paths to publish the same package ID, and never reuse a published version. This preparation itself needs no rollback because it leaves live ownership and workflows unchanged; revert its tooling/docs change if it must be withdrawn.
