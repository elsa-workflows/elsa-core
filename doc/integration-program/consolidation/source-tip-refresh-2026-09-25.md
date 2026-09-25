# Refreshed Extensions and Studio source tips

Program #8194; story #8286; draft import PR #8409. The initial history-bearing
import kept Core `e96fd36`, Extensions `ba8b71d` and Studio `20ceaee` as its
parents. After the [pinned import head](https://github.com/elsa-workflows/elsa-core/pull/8409)
`fb68c8e`, Extensions advanced to `1c134e0` and Studio advanced to `f0eeb3c`.
The latter includes the reviewed Secrets list-page capability fix in Studio
PR #1060. This refresh adds those exact source deltas to the *same draft
import*; it does not replace or squash its original merge ancestry.

The [machine-readable receipt](source-tip-refresh-2026-09-25.json) lists the
six changed source paths, their mapped paths, old/new Git blobs and modes,
and the two new upstream commit IDs. Commit `7e248a2` changes only those six
mapped paths. Merge commit `35594a3` joins that tree to both new upstream
histories with parents `7e248a2`, `1c134e0`, and `f0eeb3c`; its tree is
identical to its first parent. The two changed upstream PR workflows and the
Extensions CI build file remain inert `.source` provenance. No active Core
package or wiki workflow changed.

The [verifier](../../../scripts/integration-program/verify_import_source_tip_refresh.py)
checks all three merge parents, reachability of the old and new tips, the
complete upstream diff against the six mapped paths, exact source and mapped
blob/mode pairs, and unchanged active Core publication workflows. Its three
negative/positive tests reject altered blobs, relocation, and a missing
upstream parent. It must run again on the final import head, after later Core
main integration. The earlier E96 receipt remains historical evidence for
its original pins; it must not be misread as describing these newer files.

Disposable rehearsal at `35594a3` passed `git fsck --full --no-reflogs`,
the verifier, five Dapper compare-and-swap tests and six Mongo
compare-and-swap tests on net10.0, and a Studio Secrets module build on
net8.0/net9.0/net10.0 with zero errors. The local checkout intentionally had
no remote, so its SourceLink warnings are not remote-checkout proof. The
complete consolidated NUKE/Test, package-consumer proof, final-head Studio
browser flow, and Secrets upgrade/custody checks remain separate gates. No
package was published and no production environment or provider account was
used.
