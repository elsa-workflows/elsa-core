# Studio Contract: Weaver AI Copilot Platform

## Studio Responsibilities

- Render the Weaver chat panel.
- Send user messages and context references to Elsa Server.
- Render streaming assistant, tool, and proposal events.
- Render proposal details, graph preview, graph diff, warnings, and validation diagnostics.
- Initiate approve, reject, and apply actions through server APIs.
- Persist UI state only as needed for user navigation.

## Prototype Reference

Before implementing `Elsa.Studio.AI`, review the `elsa-extensions` `origin/feat/ai` prototype at commit `93f0e09d71e57f5daff1e2d593f0a51faaa80417` and its parents. Carry forward route/menu/table/form conventions where useful:

- `/ai/*` menu grouping and settings submenu patterns.
- Dense management tables with row navigation, bulk actions, and contextual menus.
- Tabbed agent editor structure for metadata, input/output variables, service/plugin selection, and execution settings.
- Refit-style client interfaces and FluentValidation-backed forms.

Do not carry forward raw API key value display, provider-specific service JSON editing as the primary user journey, or a management-page-first experience. Weaver opens on chat and proposal review; administration remains secondary and permission-gated.

## Studio Non-Responsibilities

- Do not invoke model providers.
- Do not host AI runtimes.
- Do not send provider credentials.
- Do not send raw workflow/runtime datasets when a reference can be sent.
- Do not apply workflow changes client-side.

## Context Attachment Shape

```ts
type AiContextAttachment =
  | { kind: "WorkflowDefinition"; referenceId: string; activityId?: string }
  | { kind: "WorkflowInstance"; referenceId: string; activityId?: string }
  | { kind: "DiagnosticsScope"; referenceId?: string; timeRange?: AiTimeRange }
  | { kind: "Tenant"; referenceId: string }
  | { kind: "TimeRange"; timeRange: AiTimeRange };
```

## Stream Rendering Expectations

- `assistant.delta`: append text to the active assistant message.
- `tool.started`: show the tool name, status, and safe arguments summary.
- `tool.progress`: update the tool progress row.
- `tool.completed`: show result summary and optional safe details.
- `proposal.created`: show a proposal notification and link to review.
- `conversation.failed`: show failure state without losing completed prior events.

## Proposal Review UI

The review surface displays:

- Proposal status.
- Rationale.
- Workflow payload preview.
- Graph diff or preview.
- Validation diagnostics.
- Warnings.
- Approval, rejection, and apply actions based on permissions and proposal state.

## Conversation Persistence

Studio can request existing conversation history from server APIs once those endpoints are implemented. Provider session details remain server-only.
