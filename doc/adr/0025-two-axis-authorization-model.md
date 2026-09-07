# Two-axis authorization model with open resources and open verbs

**Status**: Accepted

**Date**: 2026-08-24

## Decision

A permission is `{resource}:{verb}`. Both axes are open, string-keyed, and contributed by modules through permission descriptors discovered from the same assemblies as their endpoints.

The resource axis is hierarchical. A trailing `*` matches the named node and every descendant at any depth, so `workflows/*:view` is a single grant covering definitions, instances, executions and every descriptor endpoint, including resources registered in later releases. On the verb axis, `*` matches any verb. `*:*` is superuser, and a bare `*` normalizes to it at parse time.

Wildcards are the only construct with forward reach. There are no aggregates, and no verb implies another. Coherence without closure comes from a recommended core verb set — `view`, `create`, `update`, `write`, `delete`, `execute` — published as convention per Principle III, with the catalog marking non-core verbs for review. A resource declares either `create` + `update` or `write`, never both, depending on whether its API separates the operations.

All permission decisions route through a single evaluator. Concerns that are not permission checks — notably deployment read-only mode — keep their own enforcement.

## Rationale

The prior vocabulary had no model behind it. `read:*` was a literal claim value rather than a pattern, so it authorized twelve of roughly forty read endpoints; 57 permission strings appeared as inline literals across 174 call sites in three competing naming schemes; omitting a declaration failed open; and four parallel enforcement mechanisms left no single place to audit.

A **closed verb enumeration was drafted and rejected**. Fitting a census of all 150 permission-declaring endpoints to seven verbs forced six mappings, invented three sub-resources to express what the enum could not, and produced five open questions that were all artifacts of the closure — with only 16 first-party modules in one repository.

The decisive argument was that the enum was not buying what it appeared to. It had been justified on implication, but aggregates were already excluded, and no verb implies another in this model or in the proposal that prompted it. So the bitwise containment check expressed "a grant may carry several verbs and a requirement may need several", which set containment satisfies identically. With implication gone, the enum's remaining benefits were compactness — already surrendered by storing one string per resource-verb pair — and resembling the original proposal.

A coarse per-role module gate was also proposed and rejected. Its stated benefits — authoring broad roles without enumerating everything, and new endpoints being covered automatically — both fall out of the hierarchical resource axis, which delivers them in a single grant that composes with the verb axis instead of overriding it. Two independent gates keyed by the same taxonomy would drift, and a 403 would take two screens to explain.

## Consequences

- Legacy permission strings stop authorizing. A permanent alias layer would keep two vocabularies valid forever, so the break is deliberate, reported by a startup validator, and documented in `doc/migrations/authorization-model.md`. `*` survives, so no instance can lock itself out.
- Migration expands rather than renames where new sub-resources are finer-grained than what they replace.
- Wildcards confer forward reach on both axes. This is the property that makes section-wide grants viable; it is mitigated by a reach report showing what a grant covers today, and by a deployment-level allow/deny boundary.
- The vocabulary can fragment, since modules may coin synonyms. Mitigated by convention and by the catalog marking non-core verbs, not by enforcement.
- Every endpoint must declare exactly one of a permission, anonymous access, or authenticated-only access. An automated gate enforces this with no exemption list.
