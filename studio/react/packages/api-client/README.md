# Elsa React Studio API client Preview

Typed Elsa workflow models and the HTTP client used by React Studio. This is an
unpublished 3.11 Preview workspace package. It has no React or host-source imports.

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
