# Elsa Extensions in the consolidated source tree

Extensions modules live under [`src/extensions`](../../src/extensions), grouped
by functionality. The [repository inventory](../integration-program/inventory/README.md)
records their projects, public package IDs and activity identities. The
[integration catalog](../integration-program/research/README.md) tracks
researched connector candidates; a catalog entry does not mean that an
implementation or package exists.

Open the canonical [`Elsa.sln`](../../Elsa.sln) for backend and Studio changes
in one checkout. Imported Extensions projects currently use project references
and are nonpackable by default. The program's intended independent package
boundary is recorded in the [release-unit manifest](../integration-program/release-units.json),
which currently defines only `Elsa.Slack`. Its isolated artifact and
clean-consumer proof have not cut over the publisher. The separate Extensions
publisher still packages solution outputs; connector-only release without
republishing unrelated packages remains a delivery gate. The
[Slack release-unit handoff](../integration-program/publisher-handoff.md)
documents the current artifact-only proof and its separate publisher approval.

The former Extensions [repository README](../integration-program/legacy/extensions/README.md.source)
is retained as import provenance. Its planned-provider status table, separate
repository paths, and example installation commands are historical claims;
use each active module's documentation and released package metadata for
current availability. The [3.6 notes](changelogs/3.6.0.md) and
[3.7 notes](changelogs/3.7.0.md) remain
release history rather than a live connector catalog.
