# Version GitHub activities that use provider IDs

Date: 2026-09-24

## Status

Proposed for the source-consolidation proof; upstream import remains pending.

## Context

The released Extensions 3.8.4 serialized payloads for `GetComment`, `DeleteComment`, `UpdateComment`, and `GetGist` use the JSON property `id` for the provider's comment or gist ID. Their CLR `Input<T> Id` properties hide the inherited workflow `Activity.Id`, so the payload does not retain an independent Elsa activity ID. The pinned compatibility receipt records `IdentityPreserved: false` for these four entries.

Changing the existing property or class in place would make old workflows ambiguous and could change the public CLR contract. The workflow type name also has to stay stable so Elsa can resolve historical definitions by their explicit activity version.

## Decision

Keep the four existing CLR classes and their version-1 logical identities unchanged. Add sibling version-2 classes that explicitly use the same logical `TypeName` and `Version = 2`, inherit the normal string activity ID, and expose the provider value as `CommentId` or `GistId`. Do not change Core's descriptor naming or dispatch rules for this compatibility fix.

Payloads with `version: 1` continue to resolve to the old CLR classes and retain their provider input. A payload with the legacy object-valued `id` and no version dispatches to the latest version and fails deserialization as a version-2 activity; it must be migrated explicitly before use.

The offline migration accepts a WorkflowDefinitionModel-like envelope with exactly one `root` or `Root` activity and ordered `Elsa.Sequence` descendants. It requires a map bound to both a caller-chosen workflow key and the SHA-256 digest of the exact original file bytes. The caller provides one new Elsa activity ID for each affected JSON Pointer. The tool moves the old `id` input wrapper intact to its new provider-input property, writes the supplied ID and version 2, and writes only to a new output file. It refuses absent, extra, duplicate, or colliding IDs and ambiguous input wrappers. It does not traverse arbitrary business data.

Flowchart connections and other container topologies are outside the migration utility. It fails closed on `nodes`, `connections`, `branches`, and unsupported container activities instead of guessing at edge rewrites. V2 activities themselves remain usable with ordinary Elsa serializers and supported workflow containers.

## Consequences

The patch preserves the existing v1 CLR types and adds explicit v2 descriptor identities without framework changes. Workflow authors can now separate Elsa's activity ID from the GitHub provider's ID. Existing workflows with explicit version 1 remain readable; versionless legacy payloads need a reviewed offline migration.

The current proof is a mapped-source patch against Core `6f493809eae0e1652ca185b901a982984f4ef799` and Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`. It passes six focused tests on net10.0 and 20 Python tests. It does not establish the history-preserving import, other target frameworks, arbitrary workflow topology, a production workflow corpus, SourceLink identity, release packaging, or publication. Keep the parent task open until the patch is incorporated into the actual imported source and its compatibility matrix is rerun.

The migration fixture combines a WorkflowDefinitionModel root/Sequence envelope captured by the mapped-source serializer test with the four exact `Serialized` payloads from the released compatibility receipt. Studio CodeView currently uses PascalCase for top-level `Root`, `DefinitionId`, and `Name`, while activity properties are camelCase; the utility preserves whichever supported root casing appears in the input. Serializing legacy hidden-`Id` classes through the model's object path emitted duplicate JSON `id` keys in the fixture experiment, so that output is not presented as a valid historical workflow export. The historical receipt, not that duplicate-key output, is the payload source of truth.
