import type { HttpClient } from "../http";
import type { ListResponse } from "../types/common";
import type {
  ActivityDescriptor,
  ExpressionDescriptor,
  FeatureDescriptor,
  IncidentStrategyDescriptor,
  StorageDriverDescriptor,
  VariableDescriptor,
  WorkflowActivationStrategyDescriptor,
} from "../types/descriptors";

export function descriptorsApi(http: HttpClient) {
  return {
    activities: (refresh = false, signal?: AbortSignal) =>
      http.get<ListResponse<ActivityDescriptor>>("descriptors/activities", { query: { refresh: refresh || undefined }, signal }),

    activityInputOptions: (activityTypeName: string, propertyName: string, context?: unknown, version?: number) =>
      http.post<{ items: Record<string, unknown> }>(
        `descriptors/activities/${encodeURIComponent(activityTypeName)}/options/${encodeURIComponent(propertyName)}`,
        { version, context },
      ),

    expressions: (signal?: AbortSignal) => http.get<ListResponse<ExpressionDescriptor>>("descriptors/expression-descriptors", { signal }),
    variables: (signal?: AbortSignal) => http.get<ListResponse<VariableDescriptor>>("descriptors/variables", { signal }),
    storageDrivers: (signal?: AbortSignal) => http.get<ListResponse<StorageDriverDescriptor>>("descriptors/storage-drivers", { signal }),
    activationStrategies: (signal?: AbortSignal) =>
      http.get<ListResponse<WorkflowActivationStrategyDescriptor>>("descriptors/workflow-activation-strategies", { signal }),
    incidentStrategies: (signal?: AbortSignal) =>
      http.get<ListResponse<IncidentStrategyDescriptor>>("descriptors/incident-strategies", { signal }),
    logPersistenceStrategies: (signal?: AbortSignal) =>
      http.get<ListResponse<IncidentStrategyDescriptor>>("descriptors/log-persistence-strategies", { signal }),
    workflowCommitStrategies: (signal?: AbortSignal) =>
      http.get<ListResponse<IncidentStrategyDescriptor>>("descriptors/commit-strategies/workflows", { signal }),
    activityCommitStrategies: (signal?: AbortSignal) =>
      http.get<ListResponse<IncidentStrategyDescriptor>>("descriptors/commit-strategies/activities", { signal }),

    installedFeatures: (signal?: AbortSignal) => http.get<ListResponse<FeatureDescriptor>>("features/installed", { signal }),
    packageVersion: (signal?: AbortSignal) => http.get<{ packageVersion: string }>("package/version", { signal }),
    javascriptTypeDefinitions: (workflowDefinitionId: string, activityTypeName?: string, propertyName?: string) =>
      http.post<string>(
        `scripting/javascript/type-definitions/${encodeURIComponent(workflowDefinitionId)}`,
        { activityTypeName, propertyName },
        { parse: "text" },
      ),
  };
}

export type DescriptorsApi = ReturnType<typeof descriptorsApi>;
