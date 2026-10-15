# Elsa Studio Constitution

## Core Principles

### I. Modular Studio Features

Every user-facing capability SHOULD be packaged as a focused Studio module with explicit service registration, feature registration, menu integration, and route ownership.

- Modules live under `src/modules/Elsa.Studio.*`.
- Shared framework concerns belong under `src/framework/`.
- Bundled defaults are composed from `src/bundles/Elsa.Studio`.
- New modules MUST avoid reaching into another module's internals; cross-module integration uses public contracts, services, notifications, or route/query links.

### II. Backend Capability Awareness

Studio MUST treat the backend as versioned and feature-variable.

- Backend-dependent UI MUST check remote feature availability before calling optional endpoints.
- Missing backend features MUST produce hidden UI or clear unavailable states, not unhandled 404s.
- API clients MUST use `IBackendApiClientProvider`.
- SignalR connections MUST use `IHttpConnectionOptionsConfigurator`.
- Environment/backend switching MUST dispose stale connections and reload remote capability state.

### III. UX Consistency and Density

Studio is an operational workbench. Interfaces MUST be efficient, scannable, and consistent with existing Studio patterns.

- Prefer dense tables, toolbars, tabs, drawers, and dialogs over marketing-style pages.
- Use established component libraries and existing layout conventions.
- Preserve filter and pagination state in URLs when it improves shareability or back-navigation.
- Long-running or live views MUST show loading, empty, disconnected, unauthorized, and error states distinctly.
- Text and controls MUST remain usable at common desktop and mobile widths.

### IV. Async, Disposal, and Real-Time Discipline

All remote calls, subscriptions, and long-running interactions MUST be asynchronous and cancellation-aware.

- Razor components that own subscriptions, timers, or JS resources MUST dispose them.
- SignalR observers MUST surface connection status and reconnect failures.
- Client-side collections that can grow from live updates MUST have explicit caps, virtualization, or pruning.
- UI updates from background callbacks MUST marshal through the component render context.

### V. Testing and Verification

New behavior MUST be testable at the smallest practical level and manually verifiable in the sample host when UI surface changes.

- Client services and filter/query mapping SHOULD have unit tests.
- Component behavior SHOULD be tested where existing test infrastructure supports it.
- Authentication, remote-feature absence, and disconnected backend behavior MUST be considered in acceptance tests.
- UI changes MUST be verified against representative data, including empty, large, error, and long-text states.

### VI. Focused Change Sets

Changes SHOULD be small, reviewable, and scoped to one feature or concern.

- Avoid unrelated formatting, dependency updates, and design-system churn.
- Public contracts and module registration changes MUST be documented.
- Keep PRs independently useful and easy to validate.

### VII. Simplicity, DRY, and Maintainability

Prefer the simplest implementation that satisfies the spec and fits Studio's existing patterns.

- Do not introduce abstractions until they remove real duplication or isolate a real variability point.
- Extract repeated filter mapping, connection setup, and row formatting once duplication becomes structural.
- Inline trivially small helpers.
- Comments explain non-obvious constraints and lifecycle decisions, not routine control flow.

## Technology Stack and Constraints

- **Runtime**: Blazor Server and Blazor WebAssembly capable Studio modules.
- **Language**: C# latest with nullable reference types and implicit usings from repository build props.
- **UI**: Existing Studio component libraries and layout conventions.
- **API clients**: Refit-style clients through `IBackendApiClientProvider`.
- **Real time**: SignalR client with authentication configured through Studio authentication abstractions.
- **Packaging**: Module projects under `src/modules/`; bundle references under `src/bundles/Elsa.Studio`.

## Development Workflow

- Specs live under `specs/<feature-id>/`.
- Implementation plans MUST identify source module boundaries and backend feature dependencies.
- Tasks SHOULD be sliced so backend contracts, client services, UI, and contextual integrations can be reviewed separately.
- Verification notes SHOULD include the backend feature state used during testing.

## Governance

This constitution guides Spec Kit plans and tasks for Elsa Studio. Amendments require updating affected templates or specs when guidance changes. During review, unresolved violations MUST be documented in the plan's complexity tracking section with the simpler alternative that was rejected.

**Version**: 1.0.0 | **Ratified**: 2026-05-06 | **Last Amended**: 2026-05-06
