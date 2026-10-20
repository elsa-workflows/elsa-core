# Elsa React Studio SDK v1 Preview

This package defines the contribution API for React Studio modules. It is an
unpublished workspace package during the 3.11 Preview adaptation. Public package
publication and compatibility guarantees are tracked separately in #8858.

A module exports `register(api)` and contributes named slots. The host checks
both manifest API versions before importing the module. Contributions are ordered
by `order` (default zero), module ID, then slot ID. IDs are unique within a
module and slot kind. Registration is transactional: a failed module contributes
nothing, and a failed update leaves the previous version active.

```tsx
import type { StudioApi } from "@elsa-workflows/react-studio-sdk";

export function register(api: StudioApi) {
  api.contribute("headerItems", {
    id: "weather",
    order: 100,
    component: () => <span>Weather extension</span>,
  });
  const interval = setInterval(() => {}, 60_000);
  api.onDispose(() => clearInterval(interval));
}
```

Register cleanup with `onDispose` as soon as a resource is allocated. A returned
cleanup function is also supported. Removal, replacement and registration failure
run cleanup once, in reverse registration order. Contributions must be made
during the active `register` call; asynchronous registration is supported.

The SDK includes route, navigation, dashboard, settings, editor, designer, action,
tab, header and provider contracts. The current bundled host consumes the imported
Studio's existing slots through an adapter. The additional property, expression
and designer contracts describe the next renderer integration; declaring a slot
does not yet make those new renderers available. Physical RCL discovery and
independent module assets remain part of #8845.

Modules share the host's React instance. Import workflow models from
`@elsa-workflows/react-studio-api-client` and editor context from this package;
do not import host `src/` files. `@elsa-workflows/react-studio-sdk/host` contains
the host runtime, including `StudioRegistry`, and is not needed by modules.

The `/designer/*` subpaths contain the shared workflow model operations, store
and React context. The `/components` subpath exposes typed access to the Designer
and ActivityEditors implementations contributed by their modules. These controls
are registered through component slots so other modules share their context
without linking their implementation source.

From `studio/react`, `pnpm docs:api` generates the reference under `docs/api/`.
`pnpm test:packages` verifies that a separate consumer can install packed packages
and compile a module using only their exported declarations.
