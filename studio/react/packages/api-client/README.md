# Elsa React Studio API client Preview

Typed Elsa workflow models and the HTTP client used by React Studio. This is an
unpublished 3.11 Preview workspace package. Its models and HTTP client have no
React or host-source imports. The optional `/react` subpath provides the shared
API-client context and TanStack Query cache for Studio modules. The host creates
the authenticated client and supplies it to that provider.

```ts
import { createElsaClient } from "@elsa-workflows/react-studio-api-client";

const client = createElsaClient({
  baseUrl: "https://example.test/elsa/api",
  getAuthHeaders: () => ({}),
});
const definitions = await client.workflowDefinitions.list({ page: 0, pageSize: 20 });
```

Provide `getAuthHeaders`, `onUnauthorized`, and `tenantId` in the client options
to connect host authentication and tenant selection. Headers are evaluated for
each request and again after one successful refresh following a 401. The client
does not retry a second 401. Request options support cancellation through
`AbortSignal`. `ApiError` preserves the response status and server problem data.

Workflow and activity types are exported from the package root and typed
subpaths such as `types/workflow-definitions`. Generated JavaScript uses explicit
relative ESM extensions so packed consumers do not need source aliases.


`client.resilience.strategies(signal)` reads configured strategy IDs and display
names. `client.resilience.retries(activityInstanceId, { skip, take }, signal)`
returns paged retry records; the ID is an activity execution record ID, not a graph
activity ID. Both methods share the HTTP client's current authentication and tenant.
`CommitStrategyDescriptor` models the commit catalogs' `name` field; other strategy
catalogs retain their `typeName` field.
