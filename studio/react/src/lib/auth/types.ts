import type { AuthProvider } from "@/config/schema";

export interface AuthUser {
  name: string;
  /** Raw claims or profile data, when available. */
  claims?: Record<string, unknown>;
  roles?: string[];
  permissions?: string[];
  tenantId?: string;
}

export type AuthStatus = "initializing" | "authenticated" | "unauthenticated";

export interface AuthSnapshot {
  status: AuthStatus;
  user?: AuthUser;
  error?: string;
}

export interface PasswordCredentials {
  username: string;
  password: string;
}

/**
 * Contract every authentication provider implements. The HTTP client only needs
 * `getAuthHeaders` and `tryRefresh`; the shell uses the rest for login UI and the user menu.
 */
export interface AuthStrategy {
  readonly kind: AuthProvider;
  /** Whether the UI should show a login page when unauthenticated. */
  readonly interactive: boolean;
  /** Whether login is done with a username/password form (vs. a redirect). */
  readonly usesCredentials: boolean;
  initialize(): Promise<void>;
  getSnapshot(): AuthSnapshot;
  subscribe(listener: () => void): () => void;
  getAuthHeaders(): Promise<Record<string, string>>;
  /** Attempts to refresh credentials after a 401; resolves `true` if the request should be retried. */
  tryRefresh(): Promise<boolean>;
  login(credentials?: PasswordCredentials, method?: unknown): Promise<void>;
  logout(): Promise<void>;
  /** For redirect-based flows: completes the sign-in when landing on the callback route. */
  completeSignIn?(): Promise<void>;
}

/** Small observable base shared by the strategies. */
export abstract class BaseAuthStrategy implements AuthStrategy {
  abstract readonly kind: AuthProvider;
  abstract readonly interactive: boolean;
  abstract readonly usesCredentials: boolean;
  protected snapshot: AuthSnapshot = { status: "initializing" };
  private listeners = new Set<() => void>();

  getSnapshot() {
    return this.snapshot;
  }
  subscribe(listener: () => void) {
    this.listeners.add(listener);
    return () => void this.listeners.delete(listener);
  }
  protected setSnapshot(next: AuthSnapshot) {
    this.snapshot = next;
    this.listeners.forEach((l) => l());
  }
  abstract initialize(): Promise<void>;
  abstract getAuthHeaders(): Promise<Record<string, string>>;
  abstract tryRefresh(): Promise<boolean>;
  abstract login(credentials?: PasswordCredentials, method?: unknown): Promise<void>;
  abstract logout(): Promise<void>;
}

export function decodeJwtPayload(token: string): Record<string, unknown> | undefined {
  try {
    const part = token.split(".")[1];
    const json = atob(part.replace(/-/g, "+").replace(/_/g, "/"));
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return undefined;
  }
}

export function userFromJwt(token: string): AuthUser {
  const claims = decodeJwtPayload(token) ?? {};
  const str = (v: unknown) => (typeof v === "string" ? v : undefined);
  const list = (v: unknown): string[] | undefined =>
    Array.isArray(v) ? v.map(String) : typeof v === "string" ? v.split(",").map((s) => s.trim()).filter(Boolean) : undefined;
  return {
    name:
      str(claims.name) ??
      str(claims.preferred_username) ??
      str(claims.email) ??
      str(claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"]) ??
      str(claims.sub) ??
      "user",
    claims,
    roles: list(claims.roles ?? claims.role ?? claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]),
    permissions: list(claims.permissions),
    tenantId: str(claims.tenantId ?? claims["http://schemas.microsoft.com/identity/claims/tenantid"]),
  };
}

export function isJwtExpired(token: string, skewSeconds = 30): boolean {
  const exp = decodeJwtPayload(token)?.exp;
  if (typeof exp !== "number") return false;
  return Date.now() / 1000 + skewSeconds >= exp;
}
