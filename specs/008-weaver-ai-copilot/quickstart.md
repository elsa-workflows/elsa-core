# Quickstart: Weaver AI Copilot Platform

## Goal

Validate the MVP path for Weaver without relying on direct Studio-to-provider calls:

1. Enable AI abstractions, host, and Copilot adapter modules.
2. Start a server-hosted Weaver chat.
3. Resolve workflow context server-side.
4. Stream assistant and tool events.
5. Create a workflow proposal.
6. Validate, approve, apply, and durably audit the proposal.

The Copilot adapter should use `GitHub.Copilot.SDK` as the agent runtime. Elsa Server supplies context, tool callbacks, redaction, audit, and proposal enforcement; it should not emulate Copilot's tool continuation loop.

## Worktree Setup

Implementation should run from dedicated git worktrees, not the primary local checkout. Create separate worktrees for Core and the paired Studio module so feature work, generated artifacts, and test output stay isolated:

```bash
git -C /Users/sipke/Projects/Elsa/elsa-core worktree add -b codex/008-weaver-ai-copilot-core ../elsa-core-weaver codex/008-weaver-ai-copilot
git -C /Users/sipke/Projects/Elsa/elsa-studio worktree add -b codex/008-weaver-ai-copilot-studio ../elsa-studio-weaver main
```

Run Core tasks from `/Users/sipke/Projects/Elsa/elsa-core-weaver` and Studio tasks from `/Users/sipke/Projects/Elsa/elsa-studio-weaver`. If either target branch already exists, attach the worktree to that branch instead of creating a duplicate branch.

## Server Setup

```csharp
services
    .AddElsa(elsa =>
    {
        elsa.UseAI(ai =>
        {
            ai.UseHost();
            ai.UseCopilot(copilot =>
            {
                copilot.RuntimePath = "copilot";
                copilot.Model = "configured-model";
            });
        });
    });
```

## Manual Validation

1. Start Elsa Server with Weaver enabled.
2. Request `GET /ai/capabilities` and verify streaming, proposal review, attachment kinds, and agents are listed.
3. Verify `GET /ai/capabilities` advertises grounding families for activities, workflows, proposals, and runtime, including disabled reasons when stores are not registered.
4. Request `GET /ai/tools` as an authorized user and verify Activity Registry, workflow definition, proposal, instance, and incident tools are returned.
5. Ask Weaver which installed activity can receive an HTTP request and verify the answer uses `activities.search` or `activities.getDescriptor`.
6. Start `POST /ai/chat` with a `WorkflowDefinition` attachment reference and ask Weaver to explain it.
7. Verify stream events include assistant deltas and any tool lifecycle events.
8. Verify Elsa logs/audit show server-side tool execution and that Copilot SDK session events, not Host-managed continuation turns, drove the agent loop.
9. Ask Weaver to generate a simple workflow.
10. Verify a proposal is created and `GET /ai/proposals/{id}` returns payload, rationale, warnings, diagnostics, and graph preview.
11. Attempt to apply without approval and verify the server rejects the transition.
12. Approve and apply the proposal as an authorized user.
13. Verify the workflow is persisted, validation passed, and durable audit records exist for prompt, tool calls, approval, and apply.
14. Restart the server with durable persistence configured and verify proposals and audit records are still available.
15. Disconnect during a chat turn, reconnect within the configured grace window, and verify durable outputs produced while disconnected are recoverable.
16. Ask for runtime trends using attached references plus a selected time range and diagnostics scope, then verify results do not include data outside that scope.
17. Ask Weaver why a failed workflow instance failed and verify it uses `instances.getExecutionHistory`, `instances.getActivityState`, `incidents.search`, or `incidents.get` without exposing sensitive state.

## Targeted Test Commands

```bash
dotnet test test/unit/Elsa.AI.Abstractions.UnitTests/Elsa.AI.Abstractions.UnitTests.csproj
dotnet test test/unit/Elsa.AI.Host.UnitTests/Elsa.AI.Host.UnitTests.csproj
dotnet test test/unit/Elsa.AI.Copilot.UnitTests/Elsa.AI.Copilot.UnitTests.csproj
dotnet test test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj
```

## Boundary Checks

```bash
rg "Copilot" src/modules/Elsa.AI.Abstractions src/modules/Elsa.AI.Host
rg "GitHub" src/modules/Elsa.AI.Abstractions src/modules/Elsa.AI.Host
```

Expected result: no provider SDK types or provider-owned event names appear in abstractions, host contracts, workflow models, or Studio contracts.

## Studio Prototype Review

Before implementing the paired Studio module, inspect the prototype branch:

```bash
git -C ../elsa-extensions-investigation fetch origin feat/ai:refs/remotes/origin/feat/ai
git -C ../elsa-extensions-investigation show --stat 93f0e09d71e57f5daff1e2d593f0a51faaa80417
git -C ../elsa-extensions-investigation diff --name-status main..origin/feat/ai -- src/modules/agents/Elsa.Studio.Agents
```

Use it for route, menu, table, dialog, validation, and agent editor conventions. Keep Weaver chat and proposal review as the primary UI.
