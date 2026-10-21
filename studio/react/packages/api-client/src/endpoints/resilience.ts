import type { HttpClient } from "../http.js";
import type { ListResponse, PagedList } from "../types/common.js";
import type { ResilienceStrategy, RetryAttemptRecord } from "../types/resilience.js";

export function resilienceApi(http: HttpClient) {
  return {
    strategies: (signal?: AbortSignal) => http.get<ListResponse<ResilienceStrategy>>("resilience/strategies", { signal }),
    retries: (activityInstanceId: string, range: { skip?: number; take?: number } = {}, signal?: AbortSignal) =>
      http.get<PagedList<RetryAttemptRecord>>(`resilience/retries/${encodeURIComponent(activityInstanceId)}`, { query: range, signal }),
  };
}
