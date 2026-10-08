# Source contribution handoff

This record implements the contribution-register slice [Core #8670](https://github.com/elsa-workflows/elsa-core/issues/8670). [Structural consolidation #8667](https://github.com/elsa-workflows/elsa-core/issues/8667) is accepted on main at `658a9da06b2b77417f5c07eee00fff68478ba15c`. Recording this handoff does not archive either source repository.

The [register](source-handoff-register.json) preserves 205 open issues (137 Studio, 68 Extensions) and 31 open PRs (15 Studio, 16 Extensions), read on 2026-10-08 at 17:22:59–17:23:05 UTC. Each row retains its original identity, readable source URL, author, title, labels, milestone, assignees and observed open state. PR rows also retain exact head/base identities and draft status. A second complete read at 17:54:14–17:54:21 UTC confirmed all 236 items unchanged, including PR heads/bases and contributor metadata. No additions or removals were found. This is a dated snapshot; refresh it before source archival or a source-item mutation.

## Where to contribute

Current consolidated development issues and PRs belong in [Elsa Core](https://github.com/elsa-workflows/elsa-core). Use `core/`, `extensions/` or `studio/` for the relevant product's source, tests and documentation; shared solution, build and integration tooling stays at the root. Follow [CONTRIBUTING](../../CONTRIBUTING.md) for product entrypoints and review requirements. Retained 3.8/3.9 maintenance follows the accepted source-repository procedure until an explicitly approved replacement is verified.

Before continuing a source contribution, check for a matching Core issue or PR. Reuse a verified continuation when one exists; otherwise, open a scoped Core tracker/PR when the work is scheduled. Link the original source issue/PR and preserve author attribution. An old source item remaining open is a retained report or proposal, not automatically an active Core delivery task.

## Recorded dispositions

The 205 issue dispositions retain **source history for future triage in Core**. Each original URL remains the readable reference. This preserves reports and requests without rejecting them or promising bulk implementation. Any later transfer or continuation needs its actual Core destination recorded; this handoff performs neither.

All 31 bounded PR reviews are retained per row, including represented portions, residual changes, evidence hashes and original follow-up URLs. These source comparisons are not new build/browser/runtime acceptance or whole-PR adoption based on matching names or dependency versions. Only [Studio #1126](https://github.com/elsa-workflows/elsa-studio/pull/1126) records accepted Core adoption of the original hover bug through [Core #8656](https://github.com/elsa-workflows/elsa-core/pull/8656); its source maintenance/backport disposition remains separate. Other proposals retain their original source URL when no accepted Core destination has been established.

Thirteen source issues reference twelve Core items. Those links are **related-only**, including existing closed items; they are not transfers, resolution evidence or accepted continuation destinations. Per-issue relationship rationales are retained. Historical ledger references remain locators with the same boundary.

## Remaining source-side cutover work

1. Refresh open inventories and source refs immediately before source archival or individual transfers/closures. Reconcile additions, changed PR heads and newly accepted Core continuations. The recorded API interval is not an atomic cross-repository freeze.
2. Apply and verify source README/contribution notices and issue/PR routing as part of the reviewed operational cutover. This Core record does not claim those source-side edits or contributor notifications have happened. Preserve original URLs and attribution.
3. Preserve the accepted 3.8/3.9 maintenance boundaries until an explicit decision establishes a verified writable replacement before source archival, or separately approves ending that maintenance. Keeping a source repository writable for retained maintenance remains an interim option. This record selects no support policy or operational owner.
4. Verify the selected single-publisher cutover under [#8220](https://github.com/elsa-workflows/elsa-core/issues/8220), including Studio npm/ClientLib ownership and containment of competing source publishers. Existing package/source proofs do not establish credential or registry cutover.
5. Execute only the concretely authorized operational actions, then verify actual repository archived/read-only state and record the final disposition. The register alone is not archival completion.

No source issue or PR has been transferred or closed by this slice. No publisher has been activated or disabled, and no package has been published by it. These are explicit remaining operations, not implicit consequences of merging documentation.

## Evidence and historical records

The register carries the API read interval, complete page counts/end-of-pagination evidence and SHA-256 values. Original API responses and detailed source-review reports remain retained unpublished audit evidence; their hashes are identities, not public attachments. The register includes the necessary contributor metadata and bounded rationale without copying source bodies or runtime logs. PR base SHAs reflect their API responses, not a claim that every PR base equals the current advertised main tip.

Keep [upstream-work.json](consolidation/upstream-work.json) unchanged as a frozen historical import receipt. This handoff register records the later open contribution inventory; it does not rewrite the provenance of an earlier import.

Slack expansion remains deferred, and React Studio discovery follows cutover. Neither workstream is activated by this record.
