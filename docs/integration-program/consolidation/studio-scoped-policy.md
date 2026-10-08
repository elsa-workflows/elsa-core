# Studio policy in the consolidated tree

Program #8194; story #8286. The pinned Studio `20ceaee` source has two
substantive tooling-policy differences from Core: its Spec Kit plan skill
keeps repository-wide `AGENTS.md` feature-neutral, and its
[constitution](https://github.com/elsa-workflows/elsa-studio/blob/20ceaeeed7e671f0c9662003e82063026f2216de/.specify/memory/constitution.md)
contains Studio-specific development rules. The current Core root plan skill
updates the root Spec Kit plan marker and Core has its own root constitution.
Neither root file can simply be overwritten by the Studio copy.

The active representation is [`src/studio/AGENTS.md`](../../../src/studio/AGENTS.md),
with a pointer from the root instructions. It scopes the Studio constitution's
module boundaries, supported Blazor hosts, backend feature awareness, client
and SignalR abstractions, UI states, async/disposal behavior, testing and
focused-change rules to Studio work in the consolidated layout. It also
states the Studio planning rule: feature-specific context stays in that
feature's `specs/` plan, and a Studio-only plan does not rewrite the Core-root
plan pointer. Core's existing global instructions and constitution retain
their authority for shared code and non-Studio plans.

The source constitution's original paths (`src/modules`, `src/framework`,
`src/bundles`) are adapted to the prepared import's `src/studio/modules`,
`src/studio/framework`, and `src/studio/bundles`. The historical Studio
constitution and plan skill remain immutable `.source` provenance in the
history-bearing import. Their changed bytes are intentional; the scoped
guidance is a reviewed behavioral representation, not a byte-for-byte copy.

This policy can be reviewed before the import, but its effectiveness for
contributors must be checked again on the final import head: verify the
mapped Studio paths, the root pointer, and that the active Core Spec Kit
installation has not changed. It does not authorize the history merge,
Secrets activation, publisher cutover, or repository retirement.
