# Diagnostics Console Streaming Roadmap

## Purpose

Track the cross-repository plan for Aspire-style console streaming before creating formal Spec Kit feature specs.

This roadmap is intentionally lighter than a feature spec. It records agreed direction, repo ownership, sequencing, and open follow-up work. When implementation begins, create one formal Spec Kit feature spec in each repository:

- Core: `Elsa.Diagnostics.ConsoleLogs`
- Studio: `Elsa.Studio.Diagnostics.ConsoleLogs`

## Product Direction

Console Streaming is a separate diagnostics surface from Structured Logs.

- Structured Logs answer: "What semantic `ILogger` events happened, with properties, scopes, correlation, and trace context?"
- Console Streaming answers: "What raw text is the backend process writing to stdout or stderr right now?"

The user experience should feel similar to Aspire's dashboard console stream: dense, live, source-aware, easy to pause, easy to search, and useful during local development as well as clustered deployments.

## Agreed Boundaries

- Keep Structured Logs in `Elsa.Studio.Diagnostics.StructuredLogs` and `Elsa.Diagnostics.StructuredLogs`.
- Add Console Streaming as its own diagnostics module, not a rename or replacement of Structured Logs.
- Treat stdout and stderr as raw console lines, even when those lines happen to contain formatted log output.
- Do not parse console lines into structured log records.
- Do not add trace waterfalls, metrics, or OpenTelemetry exploration here.
- Use in-process console capture for the first slice.
- Defer direct Kubernetes, Docker, or orchestrator log API integration.
- Keep cluster support provider-driven through source identity and shared streaming/storage abstractions.

## Proposed Names

- Core package/module: `Elsa.Diagnostics.ConsoleLogs`
- Studio package/module: `Elsa.Studio.Diagnostics.ConsoleLogs`
- Studio route: `/diagnostics/console`
- Navigation label: `Console`
- Permission: `read:diagnostics:console-logs`
- Remote feature name: diagnostics-specific and distinct from structured logs.

Final names should be confirmed in the formal specs before implementation.

## Core Responsibilities

Core should own capture, buffering, security, and transport.

- Provide an opt-in console logs feature.
- Capture `Console.Out` and `Console.Error` through a tee so existing console behavior is preserved.
- Emit line-oriented console events with stream identity: stdout or stderr.
- Keep bounded recent history for initial Studio backfill.
- Provide live SignalR streaming.
- Provide REST endpoints for recent lines and known sources.
- Track dropped lines when buffers or subscriber channels overflow.
- Include source identity for each line: source ID, display name, service name, process ID, machine name, and available pod/container metadata.
- Support merged streams across sources and source-specific filtering.
- Support text, stream, source, and time filters.
- Support configurable redaction before lines leave the backend.
- Provide options for buffer capacity, channel capacity, maximum line length, redaction, ANSI handling, source heartbeat timeout, and provider selection.
- Avoid feedback loops from console streaming diagnostics writing into the captured console stream.

## Studio Responsibilities

Studio should own the Aspire-like viewing experience.

- Add a Diagnostics navigation entry labeled `Console`.
- Gate the page on the Core console logs remote feature.
- Load recent lines before connecting to the live stream.
- Connect to the console logs SignalR hub using existing authenticated SignalR patterns.
- Default to a merged `All sources` view.
- Provide a source selector with stale/disconnected source states.
- Show stdout and stderr distinctly.
- Preserve terminal-like density without turning Structured Logs into a console UI.
- Provide pause/resume, follow-tail, clear local view, reconnect, copy visible lines, and download/export visible lines.
- Provide text search with highlights.
- Provide wrap and compact toggles.
- Preserve useful filters in the URL query string.
- Cap rendered rows locally and show when older local rows were discarded.
- Show distinct states for unavailable feature, unauthorized, disconnected, reconnecting, empty, and no matches.

## Candidate Contract Shape

Formal specs should refine these names and fields.

### Console Log Line

- `Id`
- `Timestamp`
- `ReceivedAt`
- `Sequence`
- `Stream`: `stdout` or `stderr`
- `Text`
- `Source`
- `IsTruncated`
- `DroppedBeforeCount`

### Console Log Source

- `Id`
- `DisplayName`
- `ServiceName`
- `ProcessId`
- `MachineName`
- `PodName`
- `ContainerName`
- `Namespace`
- `NodeName`
- `LastSeenAt`
- `Health`

### Console Log Filter

- `SourceId`
- `Streams`
- `Text`
- `From`
- `To`
- `Take`

## Suggested Milestones

### Milestone 1 - Formalize Specs

- Create Core Spec Kit feature spec for `Elsa.Diagnostics.ConsoleLogs`.
- Create Studio Spec Kit feature spec for `Elsa.Studio.Diagnostics.ConsoleLogs`.
- Confirm final route, permission, feature name, and contract names.
- Align both specs on REST and SignalR endpoints.

### Milestone 2 - Core MVP

- Add the opt-in console capture feature.
- Implement stdout/stderr tee capture.
- Add bounded in-memory recent buffer.
- Add source identity for the local process.
- Add recent-lines endpoint.
- Add source-list endpoint.
- Add live SignalR hub.
- Add authorization and redaction.
- Add tests for capture, filtering, buffering, dropped counts, and unauthorized access.

### Milestone 3 - Studio MVP

- Add the Studio console logs module.
- Add Diagnostics navigation and route.
- Add typed API and SignalR clients.
- Build the console viewer with recent backfill plus live streaming.
- Implement source, stream, and text filters.
- Implement pause/resume, follow-tail, clear, reconnect, copy, wrap, and compact controls.
- Add unavailable, unauthorized, disconnected, empty, and no-match states.
- Add component tests or mocked client tests where the repo's current patterns support them.

### Milestone 4 - Cluster-Ready Provider Shape

- Validate that source identity works for multiple Core instances.
- Define or reuse provider abstractions for shared streams.
- Keep Kubernetes/Docker log API integration deferred unless a later spec explicitly includes it.
- Document how clustered deployments should configure shared console streaming.

### Milestone 5 - Polish and Documentation

- Document the distinction between Structured Logs, Console Streaming, and future OpenTelemetry exploration.
- Add quickstarts for local development and clustered deployments.
- Add operational guidance for redaction and permissions.
- Add troubleshooting notes for console capture limitations.

## Open Questions for Formal Specs

- Should ANSI escape sequences be preserved by default, stripped by default, or user-toggleable in Studio?
- Should console lines be split strictly on newline, or should long partial writes be flushed after an idle timeout?
- Should stderr always be highlighted, or only visually tagged?
- Should the backend support downloading recent console lines, or should Studio export only its local visible buffer?
- Should redaction run on the raw line text only, or also on source metadata?
- Should direct console capture be disabled automatically in environments where stdout/stderr are already redirected in unsupported ways?

## Spec Strategy

Use one formal Spec Kit feature per repository when ready:

- Core spec: backend feature, contracts, endpoints, hub, permissions, capture behavior, buffering, provider model, tests.
- Studio spec: module identity, route, navigation, feature gating, API clients, SignalR client, viewer UX, states, tests.

Do not try to capture both repositories in one spec. The roadmap can stay shared, but each repo needs its own executable spec because the implementation surfaces, tests, and acceptance criteria are different.
