import type { HttpClient } from "@/lib/api/http";

/** An environment advertised by the server (`GET /environments`). The API names the URL `serverUrl`. */
export interface ServerEnvironment {
  name: string;
  serverUrl?: string;
  /** Older payloads used `url`; both are accepted. */
  url?: string;
  customProperties?: Record<string, unknown>;
}

export function environmentUrl(env: ServerEnvironment): string | undefined {
  return env.serverUrl ?? env.url;
}

export interface EnvironmentsResponse {
  environments: ServerEnvironment[];
  defaultEnvironmentName?: string | null;
}

export function environmentsApi(http: HttpClient) {
  return {
    list: (signal?: AbortSignal) => http.get<EnvironmentsResponse>("environments", { signal }),
  };
}

export type EnvironmentsApi = ReturnType<typeof environmentsApi>;

/** Normalises a base URL for comparison (lower-case, no trailing slashes). */
export function normalizeUrl(url: string | undefined | null): string {
  return (url ?? "").trim().replace(/\/+$/, "").toLowerCase();
}
