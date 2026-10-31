/** A configured server strategy; additional settings depend on its $type. */
export interface ResilienceStrategy {
  id: string;
  displayName: string;
  $type?: string;
  [key: string]: unknown;
}

/** Persisted retry history, keyed by the activity execution instance ID. */
export interface RetryAttemptRecord {
  id: string;
  activityInstanceId: string;
  activityId: string;
  workflowInstanceId: string;
  attemptNumber: number;
  retryDelay: string;
  details: Record<string, string>;
}
