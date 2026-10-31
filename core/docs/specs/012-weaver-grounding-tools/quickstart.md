# Quickstart: Weaver Grounding Tools

## Goal

Validate that Weaver can answer questions and create proposals grounded in installed Elsa data without direct database or provider SDK exposure.

## Setup

1. Start an Elsa Server with AI Host and Copilot enabled.
2. Ensure several activities are installed, including at least one trigger activity and one action activity.
3. Create or seed:
   - one published workflow definition,
   - one workflow using a custom or versioned activity,
   - one failed workflow instance with an incident.
4. Configure durable proposal and audit storage if validating proposal lifecycle.

## Manual Validation

1. Request `GET /ai/capabilities`.
2. Verify grounding capabilities advertise activity, workflow, proposal, and runtime tool families.
3. Request `GET /ai/tools`.
4. Verify these tools are available for an authorized workflow author:
   - `activities.search`
   - `activities.getDescriptor`
   - `workflows.search`
   - `workflows.getDefinition`
   - `workflows.validateDraft`
   - `workflows.proposeCreate`
   - `workflows.proposeUpdate`
   - `instances.search`
   - `instances.get`
   - `incidents.search`
5. Ask Weaver: "What activities can start a workflow from an HTTP request?"
6. Verify the answer references installed activities only.
7. Ask Weaver: "Create a workflow that starts on HTTP POST and sends an email."
8. Verify Weaver searches activities, validates the draft, and creates a proposal instead of saving a workflow directly.
9. Ask Weaver to explain a seeded workflow definition.
10. Verify the answer includes real triggers, activities, inputs, outputs, and graph structure.
11. Ask Weaver why a seeded failed instance failed.
12. Verify the answer references the failed activity, incident message, timeline, and redacted state.

## Targeted Test Commands

```bash
dotnet test test/unit/Elsa.AI.Host.UnitTests/Elsa.AI.Host.UnitTests.csproj
dotnet test test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj
dotnet build Elsa.sln -m:1
```
