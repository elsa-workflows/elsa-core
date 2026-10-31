# Elsa 3 source cutoff

[Task #8749](https://github.com/elsa-workflows/elsa-core/issues/8749) closes the source-integration boundary of [program #8740](https://github.com/elsa-workflows/elsa-core/issues/8740). Core, Extensions, Studio, Apps and Templates now have product roots in this repository. This prepared evidence record defines the cutoff; activation is established by the lead's actual-main acceptance receipt on #8749 as described below. Its [machine-readable inventory](source-cutoff.json) pins the observed inputs and links every open contribution to its accepted disposition.

The cutoff becomes effective only after the reviewed normal shared-to-main merge, final donor freshness reconciliation, applicable exact-head review/CI and the lead's actual-main parent/tree/history readback are recorded on #8749. That postmerge receipt supplies the final PR and merge identities without requiring this document to contain its own commit SHA. Preparation and accepted shared checkpoints do not constitute actual-main acceptance.

## Retained history

The actual-main baseline is `c46850de361cb84f267845ba66394c87262e2aab`. Accepted shared head `5403eda00557c20e39306863d53213afb7308e55` includes the accepted Apps and Templates source slices. History-only commit `d3ed761296f1801d502b772fccaf30cc2db33166` has these ordered parents:

1. Accepted shared head `5403eda00557c20e39306863d53213afb7308e55`.
2. Observed Studio main `d57fc46fd3605b7b2ee839306c7420a170c17ddd`.
3. Observed Extensions main `2611ee6d9b6fb5e50f4c0b3fbc9cc15b355b4751`.

Its tree is exactly the accepted shared tree, `d7d2d2e89c905f1ed9374fd163897778bfdcbce6`. The extra parents retain donor routing-notice history without applying those trees or replacing the accepted Russian `Выходы` correction. Original imports, ledgers, authors, source assets and tag-object evidence remain unchanged. Ref observations retain identities; they do not imply adoption of every branch's content or creation of replacement release refs.

## Inventory and contribution dispositions

The full paginated donor read spans **2026-10-12 01:17:52.672815–01:18:08.288878 UTC**. Each repository's own interval, pagination completion, raw head/tag advertisements (including peeled targets) and issue/PR identities are retained in [source-cutoff.json](source-cutoff.json). There are four donors plus the target Core repository, not five donors.

| Donor | Named head/tag refs | Peeled-target rows | Advertised rows | Open issues | Open PRs | Accepted register |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Studio | 122 | 4 | 126 | 137 | 15 | [Studio/Extensions](source-handoff-register.json) |
| Extensions | 76 | 5 | 81 | 68 | 16 | [Studio/Extensions](source-handoff-register.json) |
| Apps | 10 | 0 | 10 | 1 | 1 | [Apps](apps-source-import.json) |
| Templates | 5 | 1 | 6 | 1 | 1 | [Templates](templates-source-import.json) |
| Total | 213 | 10 | 223 | 207 | 33 | Per-item pointers in the cutoff inventory |

The 223 advertised rows include 213 named head/tag refs and ten synthetic `^{}` peeled-target rows. Raw annotated tag-object identities and peeled commit targets remain distinct in the inventory.

Observed open identity sets match the accepted registers. The Studio/Extensions comparison also checks issue URL/title/author and PR URL/title/author/head/base/baseRef/draft. Apps/Templates comparisons use the fields retained by their smaller import registers; current additional PR fields are recorded observations. This comparison does not audit current bodies, labels, assignees or update timestamps. The original Studio/Extensions register retains its richer dated metadata. Cross-repository reads are not an atomic freeze, and donors remain capable of receiving changes.

Each cutoff item has a JSON Pointer to its accepted register row and an explicit disposition. The [contribution handoff](source-contributor-handoff.md) explains the retained 205 Studio/Extensions reports and bounded 31 PR reviews. Their unresolved proposals remain tracked for individual triage rather than bulk implementation. In particular:

- [Studio #1126](https://github.com/elsa-workflows/elsa-studio/pull/1126) has accepted original hover-fix adoption through [Core #8656](https://github.com/elsa-workflows/elsa-core/pull/8656); source maintenance/backports remain separate.
- [Apps #9](https://github.com/elsa-workflows/elsa-apps/pull/9) is retained as non-content ancestry only. Its proposed 3.8.4 pins are superseded by imported main's released 3.9.0 consumers.
- [Templates #9](https://github.com/elsa-workflows/elsa-templates/pull/9), original `5dc2c5cf4eb6ed17b2228912f712993e48adb56b`, is applied with attribution after immutable original main at `79469e023b7a26b0ff6ca3a5a64104f654466ea2`. Later integration fixes preserve that attribution. The donor PR remains open.
- [Apps #3](https://github.com/elsa-workflows/elsa-apps/issues/3) and [Templates #3](https://github.com/elsa-workflows/elsa-templates/issues/3) remain attributed unresolved reports. Source acceptance supplies no authentication or ambiguous-endpoint resolution.

The relevant 3.8.x/3.9.0 release-ref census contains Core 12, Studio 13, Extensions 10, Apps 0 and Templates 2 rows. Core/Studio/Extensions match the accepted [#8624 catch-up](https://github.com/elsa-workflows/elsa-core/pull/8624); Apps/Templates are reconciled with their dedicated imports. No added, removed or changed relevant refs were found. These counts include raw annotated-tag identities; peeled rows are retained separately in the full donor advertisements. Apps' zero applies to this release range: its older `release/3.6.0` branch is present among the ten raw refs, and it advertises no tags.

The separate GitHub release-catalog read at **2026-10-12 01:33:22.132591 UTC** reports latest release 3.9.0 for Core, Studio, Extensions and Templates, with no Apps releases. The inventory retains the catalog evidence hash and relevant record counts. A GitHub release label is not package compatibility or support-policy proof.

## Reproducing and reconciling the read

For each donor (`elsa-studio`, `elsa-extensions`, `elsa-apps`, `elsa-templates`), record UTC start/end times and retain the following outputs. These commands read current observations; they cannot recreate a past API response after upstream changes.

```sh
repo=elsa-studio
out="/tmp/$repo-source-cutoff"
mkdir -p "$out"
date -u +%Y-%m-%dT%H:%M:%SZ > "$out/started-at.txt"
git ls-remote --heads --tags "https://github.com/elsa-workflows/$repo.git" > "$out/refs.tsv"
gh api --paginate --slurp "repos/elsa-workflows/$repo/pulls?state=open&per_page=100" > "$out/pulls-pages.json"
gh api --paginate --slurp "repos/elsa-workflows/$repo/issues?state=open&per_page=100" > "$out/issues-pages.json"
jq '[.[][] | {number,title,url:.html_url,head:.head.sha,base:.base.sha,baseRef:.base.ref,author:.user.login,draft}] | sort_by(.number)' "$out/pulls-pages.json" > "$out/pulls.json"
jq '[.[][] | select(has("pull_request") | not) | {number,title,url:.html_url,author:.user.login}] | sort_by(.number)' "$out/issues-pages.json" > "$out/issues.json"
date -u +%Y-%m-%dT%H:%M:%SZ > "$out/completed-at.txt"
```

Confirm pagination reached its end, compare complete identity sets and PR head/base/draft fields, and compare every raw ref name and SHA. Preserve raw annotated tag IDs alongside peeled targets; do not flatten them into commit IDs. Compare Core's relevant release refs as well. The final premerge read must also refresh actual main/shared, dependencies and matching Core continuation PRs, plus the live board's single active delivery Task.

For a donor arrival before final merge, log its observed identity and read interval, compare it with accepted source content and disposition, and reconcile material changes before acceptance. After cutoff, use the same procedure and open or reuse an attributed Core continuation when applicable. Record whether each arrival is included source, an adopted contribution, ancestry-only history or unresolved follow-up, with evidence. Do not silently ignore later arrivals or treat the snapshot as a donor freeze. Original ledgers remain immutable; later reconciliation gets a new dated receipt.

## Product and release boundaries

[CONTRIBUTING](../../CONTRIBUTING.md#product-ownership-and-local-checks) is the current five-product development entrypoint. New development targets Core/main. Apps remains nine nonpackable released Elsa 3.9.0 consumers, and Templates retains its independent pack/product solution and root filter with navigation-only root solution entries. Ordinary independent product releases do not republish unrelated products. The dated first aligned 3.10 baseline remains an [#8741 operational handoff](https://github.com/elsa-workflows/elsa-core/issues/8741), as recorded in the existing ADR.

Retained 3.8/3.9 maintenance continues through its existing source-repository procedure until a separately approved replacement is verified writable and usable. Recording release refs does not activate replacement branches, publisher authority or a new support policy. Donor workflows, source repositories and package/image registries are not retired, disabled or republished by this source cutoff.

## Accepted native checkpoints

These are dated proofs for the named accepted shared source heads. They are not automatic proof for the later #8749 final integration head. Artifact SHA-256 values identify downloaded artifact archives; exact run/artifact IDs and digests are retained in [source-cutoff.json](source-cutoff.json).

| Accepted source head | Native scope | Package receipt | Public receipt |
| --- | --- | --- | --- |
| Apps `dc27edc05f83ecd8feb18d894a501e0694774d98` | Mixed solution: 7,865 passed, 144 skipped; all six Ubuntu AMD64 image profiles passed | Run 38179158250, artifact 11717839407; 232 packages / 132 exclusions, including nine Apps consumers; `published=false` | [Measured proof](https://github.com/elsa-workflows/elsa-core/issues/8741#issuecomment-6114938822), [source closeout](https://github.com/elsa-workflows/elsa-core/issues/8746#issuecomment-6114943633) |
| Templates `c8d670e1681661f9c8a8873b1c2e8819e4ec1edb` | Templates 20/20, zero skips; mixed solution 7,865 passed, 144 skipped, zero failed across 103 rows; tools 978 tests in each normal/optimized run, four skips each; six Apps image profiles passed | Run 38187936003, artifact 11721623179; 232 packages / 132 exclusions, 464 package hashes; SDK 10.0.401, Node 22.23.3; three clean-consumer TFMs and six admission cells; `published=false` | [Exact gate](https://github.com/elsa-workflows/elsa-core/pull/8750#issuecomment-6116066488), [source closeout](https://github.com/elsa-workflows/elsa-core/issues/8748#issuecomment-6116081254) |

The Templates gate recorded 21 successful checks and three conditional skips, independent Spec/Standards `APPROVE/HIGH` and required Greptile 5/5. Skips remain skips. The [Templates import guide](templates-source-import.md#validation-and-acceptance) retains the earlier failed registration/namespace proof and subsequent corrections. No local heavy native suite was run for this documentation cutoff.

Current package inventory is evaluated dynamically. These measured 232/132 results do not replace the historical **225 packages / 124 exclusions** candidate baseline, its artifact bytes or recovery/executor validator contracts. Future candidate preparation needs explicit versioned inventory compatibility under #8741/#8220.

## Operational handoff and remaining acceptance

[Program #8741](https://github.com/elsa-workflows/elsa-core/issues/8741) retains owners [#8216](https://github.com/elsa-workflows/elsa-core/issues/8216), [#8219](https://github.com/elsa-workflows/elsa-core/issues/8219), [#8220](https://github.com/elsa-workflows/elsa-core/issues/8220), [#8636](https://github.com/elsa-workflows/elsa-core/issues/8636) and [#8643](https://github.com/elsa-workflows/elsa-core/issues/8643), with [publisher/full-release verification #8725](https://github.com/elsa-workflows/elsa-core/issues/8725). This includes candidate/archive compatibility, independent release selection, NuGet/npm/image ownership, trust and publisher containment, deployment/rollback, maintenance replacement and source retirement.

The inherited Templates [Monaco/browser limitation](https://github.com/elsa-workflows/elsa-core/issues/8741#issuecomment-6115369407) and [production signing-key limitation](https://github.com/elsa-workflows/elsa-core/issues/8741#issuecomment-6115738624) remain explicit operational follow-ups. Generated builds do not certify browser designer behavior, production identity, MySQL, ARM64 manifests or registry publication. The product [identity configuration and verification guidance](../../templates/README.md#configure-identity) carries the concrete deployment caveats.

The lead owns final exact-head independent review, Greptile, applicable CI, freshness reconciliation, the reviewed actual-main merge and actual-main readback. The #8749 postmerge receipt must identify the merge and checked tree/history, link the final gates, report scope and skips, and confirm the remaining operational boundaries. Until that receipt exists, this cutoff remains prepared.
