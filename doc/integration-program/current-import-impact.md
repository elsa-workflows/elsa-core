# Current imported-source test impact

Program #8194; release-unit proof #8260. The inventory-pinned selector in
[`package_closure.py`](../../scripts/integration-program/package_closure.py) describes the earlier two-repository snapshot. It cannot certify which tests depend on the current history-import commit. This selector restores the actual imported `Elsa.sln` and reads every solution project's evaluated `project.assets.json`, including transitive project references outside the solution. Test identity comes from the restored `Microsoft.NET.Test.Sdk` dependency, so relocated Studio tests are included when they are actually affected.

Run on a clean, exact checkout:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 scripts/integration-program/current_import_impact.py \
  --expected-head "$(git rev-parse HEAD)" \
  --output /tmp/current-import-impact.json
```

The script restores the solution with source references, rejects missing or escaping project inputs, and records the source commit, project/asset/build-configuration hashes, test/TFM selections and working-tree state. A dirty checkout produces a diagnostic receipt but exits nonzero. The read-only [CI workflow](../../.github/workflows/current-import-impact.yml) checks out the PR head, runs selector policy tests, then uploads this receipt. It has no package-push step or publishing credential.

For a change to `src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj`, the selected tests must match the mapped `Elsa.Slack` release-unit manifest exactly. A change to shared `src/modules/Elsa/Elsa.csproj` must include the Slack test and select more test/TFM nodes. Both scenarios keep the package-to-pack list at the single declared `Elsa.Slack` release unit; that list is **not** a Core package release plan. `Elsa.Mqtt` remains an unchanged package control.

This receipt is **selection evidence only**. It does not execute the selected tests, establish that runtime service effects are absent, certify package compatibility, or authorize any package/feed publication. Run the selected current-source tests and inspect their actual TRX/skips before accepting the dependency-closure gate. The history import and one-publisher handoff remain separate gates.
