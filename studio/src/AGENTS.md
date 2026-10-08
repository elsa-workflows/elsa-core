# Studio contributor guidance

These instructions apply to Studio modules, hosts, frameworks, and tests under
`src/studio/`. Follow the repository-root `AGENTS.md` for shared build, review,
testing, package and ADR
rules. This guidance carries the Studio-specific rules from the pinned
[Studio constitution](https://github.com/elsa-workflows/elsa-studio/blob/20ceaeeed7e671f0c9662003e82063026f2216de/.specify/memory/constitution.md)
into the consolidated source layout.

## Module boundaries

- Put user-facing features in focused `src/studio/modules/Elsa.Studio.*`
  modules with explicit service and feature registration, menu integration,
  and route ownership. Shared framework concerns belong in
  `src/studio/framework/`; bundled defaults are composed in
  `src/studio/bundles/Elsa.Studio/`.
- Do not reach into another module's internals. Use public contracts,
  services, notifications, or route/query links for cross-module behavior.
- Keep modules usable in the supported Blazor Server and WebAssembly hosts
  unless the module is explicitly host-specific. Use the existing component
  libraries and layout conventions.
- Keep backend contracts and Studio UI changes reviewable together when a
  feature requires both. Preserve public package and activity identities.

## Backend capability and environment changes

- Treat the backend as versioned and feature-variable. Check remote feature
  availability before using optional endpoints; hide unavailable controls or
  show a clear unavailable state instead of allowing an unhandled 404.
- Use Refit-style clients through `IBackendApiClientProvider` for backend API
  calls and `IHttpConnectionOptionsConfigurator` for SignalR connections.
- On backend or environment switches, dispose stale connections and reload
  capability state. Do not carry tenant-specific state across that boundary.

## UI and lifecycle

- Consult the [Studio design baseline](../../doc/studio/README.md) when shaping
  Studio UI; verify the relevant current module behavior and tests as well.
- Follow existing Studio component and layout patterns. Favor scannable
  tables, toolbars, tabs, drawers, and dialogs for operational views; keep
  filters and pagination in URLs when this improves navigation.
- Distinguish loading, empty, disconnected, unauthorized, and error states in
  long-running or live views. Keep controls usable at common desktop and
  mobile widths, including with long text or large data sets.
- Make remote calls and long-running interactions asynchronous and
  cancellation-aware. Components that own subscriptions, timers, or JS
  resources dispose them. Show SignalR connection and reconnect state;
  cap, virtualize, or prune live collections. Marshal background callbacks
  through the component render context.

## Plans and verification

- Keep Studio feature-specific requirements, plans, and tasks under that
  feature's `specs/` directory. Do not change the repository-root
  `AGENTS.md` plan pointer solely for a Studio feature; put its implementation
  context and backend dependencies in the feature's `plan.md`.
- Test client services and filter/query mapping at the smallest practical
  level. Add component tests where the existing infrastructure supports them.
  Consider authentication, absent remote features, and disconnected backends
  in acceptance tests.
- When UI behavior changes, verify it in a representative host with empty,
  large, error, and long-text states. Record the backend feature state used
  during verification. Document public contracts and module registration
  changes.
- Keep changes focused. Extract shared setup or mapping when repetition is
  structural; inline trivial helpers and explain lifecycle constraints in
  comments rather than routine control flow.
