import { descriptorsApi } from "./endpoints/descriptors.js";
import { identityApi } from "./endpoints/identity.js";
import { workflowDefinitionsApi } from "./endpoints/workflow-definitions.js";
import { workflowInstancesApi } from "./endpoints/workflow-instances.js";
import { createHttpClient, type HttpClient, type HttpClientOptions } from "./http.js";

/**
 * Root API client. Core resource groups are attached here; optional modules build their own
 * group from `client.http` (see `src/modules/<module>/api.ts`) so the core never depends on them.
 */
export function createElsaClient(options: HttpClientOptions) {
  const http = createHttpClient(options);
  return {
    http,
    workflowDefinitions: workflowDefinitionsApi(http),
    workflowInstances: workflowInstancesApi(http),
    descriptors: descriptorsApi(http),
    identity: identityApi(http),
  };
}

export type ElsaClient = ReturnType<typeof createElsaClient>;
export type { HttpClient };
