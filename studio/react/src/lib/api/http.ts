import type { ApiProblem } from "./types/common";

export type QueryValue = string | number | boolean | null | undefined | Array<string | number | boolean>;
export type Query = Record<string, QueryValue>;

export interface RequestOptions {
  query?: Query;
  body?: unknown;
  signal?: AbortSignal;
  headers?: Record<string, string>;
  /** Parse mode for the response. Defaults to `json`; `text` and `blob` are available for exports. */
  parse?: "json" | "text" | "blob" | "none";
}

export class ApiError extends Error {
  readonly problem: ApiProblem;
  constructor(problem: ApiProblem) {
    super(problem.detail ? `${problem.title}: ${problem.detail}` : problem.title);
    this.name = "ApiError";
    this.problem = problem;
  }
  get status() {
    return this.problem.status;
  }
}

export interface HttpClientOptions {
  baseUrl: string;
  tenantId?: string;
  /** Supplies authentication headers for each request. */
  getAuthHeaders: () => Promise<Record<string, string>> | Record<string, string>;
  /**
   * Called once when a request is rejected with 401. Return `true` if credentials were
   * refreshed and the request should be retried.
   */
  onUnauthorized?: () => Promise<boolean>;
}

export interface HttpClient {
  request<T>(method: string, path: string, options?: RequestOptions): Promise<T>;
  get<T>(path: string, options?: RequestOptions): Promise<T>;
  post<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T>;
  put<T>(path: string, body?: unknown, options?: RequestOptions): Promise<T>;
  delete<T>(path: string, options?: RequestOptions): Promise<T>;
  readonly baseUrl: string;
}

function buildQuery(query?: Query): string {
  if (!query) return "";
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === "") continue;
    if (Array.isArray(value)) value.forEach((v) => params.append(key, String(v)));
    else params.set(key, String(value));
  }
  const s = params.toString();
  return s ? `?${s}` : "";
}

async function toProblem(res: Response): Promise<ApiProblem> {
  let raw: unknown;
  const text = await res.text().catch(() => "");
  try {
    raw = text ? JSON.parse(text) : undefined;
  } catch {
    raw = text;
  }
  const obj = (raw ?? {}) as Record<string, unknown>;
  return {
    status: res.status,
    title: (obj.title as string) ?? (obj.message as string) ?? res.statusText ?? `HTTP ${res.status}`,
    detail: (obj.detail as string) ?? (typeof raw === "string" && raw ? raw : undefined),
    errors: obj.errors as Record<string, string[]> | undefined,
    raw,
  };
}

/** Minimal fetch wrapper used by every API module: base URL, auth, tenant header, errors, 401 retry. */
export function createHttpClient(options: HttpClientOptions): HttpClient {
  const baseUrl = options.baseUrl.replace(/\/+$/, "");

  async function request<T>(method: string, path: string, opts: RequestOptions = {}, retried = false): Promise<T> {
    const url = `${baseUrl}/${path.replace(/^\/+/, "")}${buildQuery(opts.query)}`;
    const headers: Record<string, string> = {
      Accept: "application/json",
      ...(await options.getAuthHeaders()),
      ...(options.tenantId ? { "X-Tenant-Id": options.tenantId } : {}),
      ...opts.headers,
    };
    let body: BodyInit | undefined;
    if (opts.body instanceof FormData) {
      body = opts.body; // the browser sets the multipart boundary itself
    } else if (opts.body !== undefined) {
      headers["Content-Type"] = "application/json";
      body = JSON.stringify(opts.body);
    }

    const res = await fetch(url, { method, headers, body, signal: opts.signal });

    if (res.status === 401 && !retried && options.onUnauthorized) {
      const refreshed = await options.onUnauthorized();
      if (refreshed) return request<T>(method, path, opts, true);
    }

    if (!res.ok) throw new ApiError(await toProblem(res));

    const parse = opts.parse ?? "json";
    if (parse === "none" || res.status === 204) return undefined as T;
    if (parse === "text") return (await res.text()) as T;
    if (parse === "blob") return (await res.blob()) as T;
    const text = await res.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  return {
    baseUrl,
    request,
    get: (path, o) => request("GET", path, o),
    post: (path, body, o) => request("POST", path, { ...o, body }),
    put: (path, body, o) => request("PUT", path, { ...o, body }),
    delete: (path, o) => request("DELETE", path, o),
  };
}
