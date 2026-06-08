# Elsa AI Host

Elsa AI Host owns Weaver's provider-neutral server surface: chat orchestration, context resolution, tool registration, proposal governance, audit, and Studio-facing capabilities. Provider SDK types stay outside this module.

## Grounding Tools

Built-in grounding tools are registered as `IAITool` implementations. Read-only tools are enabled by default; proposal tools must be enabled explicitly before a provider can invoke them.

Activity tools:

- `activities.search`
- `activities.getDescriptor`

Workflow definition tools:

- `workflows.search`
- `workflows.getDefinition`
- `workflows.getDefinitionGraph`
- `workflows.findUsages`

Proposal-only tools:

- `workflows.validateDraft`
- `workflows.proposeCreate`
- `workflows.proposeUpdate`

Runtime inspection tools:

- `instances.search`
- `instances.get`
- `instances.getExecutionHistory`
- `instances.getActivityState`
- `incidents.search`
- `incidents.get`

The tools read from Elsa server-side sources such as `IActivityRegistry`, `IWorkflowDefinitionStore`, and `IWorkflowInstanceStore`. If a source is not registered, the tool returns an unavailable result and `/ai/capabilities` reports the disabled reason for Studio.

All tool results are bounded by `AIHostOptions.Grounding`, redacted before returning to the model or Studio, and audited by the existing AI Host tool invocation path.

## Proposal Safety

`workflows.proposeCreate` and `workflows.proposeUpdate` write only to `IAIProposalStore`. They do not persist workflow definitions. Approval and apply remain separate governed actions.
