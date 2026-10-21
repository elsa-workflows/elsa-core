# Use native owned children for compensation scopes

Status: Proposed. This ADR records the E3 design for [#8953](https://github.com/elsa-workflows/elsa-core/issues/8953). The source observations are pinned to [`7ca753ccc212cfcffe3e6cf3dd8e01090e9bcd6f`](https://github.com/elsa-workflows/elsa-core/commit/7ca753ccc212cfcffe3e6cf3dd8e01090e9bcd6f). It does not qualify implementation, persistence, security, or Studio behavior.

## Context

The accepted compensation policy requires paired forward and handler activities with durable logical identity across repeated executions. Elsa Core already serializes nested activities in public activity properties and exposes `StateMachine` children through a hidden `IEnumerable<IActivity>` projection for native graph traversal. Studio separately resolves embedded child JSON and selects custom designers. These extension points do not currently provide compensation rows, stable step/mapping identity, strict pre-converter pins, safe recovery capture or occurrence inspection.

## Proposed decision

Implement the first native scope in Elsa Workflows Core as an activity that owns versioned serialized step rows. A row owns its forward and inline handler `IActivity` children and stable logical StepId/MappingIds. A hidden non-serialized activity projection exposes those owned child objects to Core traversal, following the StateMachine pattern. Studio adds a Workflows.Core JSON resolver for row-owned children and a custom Workflows diagram designer for paired authoring; graph ports remain data/control-flow mechanisms, not the paired editor.

The protected M1 profile is narrow: exact root definition-version provenance, exact child activity type/version pins, inline handlers only, direct compatible capture mappings, and an explicit default-deny activity/participant/profile allowlist whose source suppression and protected capture fields are proved under E2. `WorkflowDefinitionActivity` handlers and unversioned converter-backed mappings are unsupported. Preserve ordinary Elsa 3 fallback behavior.

Protected strict-pin validation must inspect original nested JSON tokens before `ActivityJsonConverter` can fall back to the latest activity descriptor, both on the current save/publish request boundary and before stored definition materialization. The current POST model binds `Root` as `IActivity`; the compensation-only raw-token admission seam is required design work and is not supplied by current post-materialization workflow validation. Keep the exact request DTO/route spelling open until the Core/Management caller is implemented and reviewed. Semantic validation uses the native workflow validation notification only after exact safe materialization.

Recovery inspection consumes the E2-safe, paged instance summary → occurrence → attempt → exact execution caller. It must not depend on the current generic full-instance GET or latest execution by diagram node. Definition labels/layout require separate definition-resource authorization. Exact public DTO, endpoint and permission names remain open; M4 mutation UI is outside this decision.

## Consequences and proof still required

The activity graph, serialized ownership and Studio JSON tree need explicit symmetric tests. Save/reopen must preserve StepId/MappingIds and incomplete/unknown drafts. Runtime must associate the exact scheduled child completion/fault callback with its occurrence/operation/attempt tuple without treating Activity.Id, NodeId or arbitrary Tag as the durable operation identity. E1/E2/E3 must qualify strict pins, persisted restore, actual participant source suppression, trusted-tenant authorization, protected projection, both O3 handoffs and the real Studio host. Until those proofs pass, the decision remains Proposed and M1 remains Not Ready.

Reusable workflow-as-activity handlers may be reconsidered at M3, or earlier for a concrete caller after strict definition-version selection and restore are proved. Converted mappings may be reconsidered when an immutable versioned recovery-converter identity and execution path exist. Unsupported profiles fail closed before effect; they do not change ordinary Elsa 3 workflows.

See the canonical [E3 caller contract](../../core/docs/specs/native-compensation/e3-studio-contract.md) and [native compensation packet](../../core/docs/specs/native-compensation/README.md).
