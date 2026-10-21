import type { AuthConfig } from "@/config/schema";
import { readTokens, writeTokens } from "../storage";
import { BaseAuthStrategy, isJwtExpired, userFromJwt, type PasswordCredentials } from "../types";

interface LoginResponse {
  isAuthenticated: boolean;
  accessToken?: string;
  refreshToken?: string;
}

/**
 * Username/password login against Elsa's Identity module.
 *
 *  - `POST {api}/identity/login { username, password }` → `{ isAuthenticated, accessToken, refreshToken }`
 *  - `POST {api}/identity/refresh-token` with `Authorization: Bearer <refreshToken>` → same shape
 *  - `POST {api}/identity/logout { refreshToken }` revokes the session
 */
export class ElsaIdentityAuthStrategy extends BaseAuthStrategy {
  readonly kind = "elsa-identity" as const;
  readonly interactive = true;
  readonly usesCredentials = true;

  private refreshing?: Promise<boolean>;

  constructor(
    private readonly apiBaseUrl: string,
    private readonly options: AuthConfig["elsaIdentity"],
    private readonly scope: string,
  ) {
    super();
  }

  private url(path: string) {
    return `${this.apiBaseUrl.replace(/\/+$/, "")}/${path.replace(/^\/+/, "")}`;
  }

  async initialize() {
    const tokens = readTokens(this.scope);
    if (!tokens?.accessToken) {
      this.setSnapshot({ status: "unauthenticated" });
      return;
    }
    if (isJwtExpired(tokens.accessToken)) {
      const ok = await this.tryRefresh();
      if (!ok) {
        this.setSnapshot({ status: "unauthenticated" });
        return;
      }
    }
    this.setSnapshot({ status: "authenticated", user: userFromJwt(readTokens(this.scope)!.accessToken) });
  }

  async getAuthHeaders(): Promise<Record<string, string>> {
    const tokens = readTokens(this.scope);
    if (!tokens?.accessToken) return {};
    if (isJwtExpired(tokens.accessToken) && tokens.refreshToken) await this.tryRefresh();
    const current = readTokens(this.scope);
    return current?.accessToken ? { Authorization: `Bearer ${current.accessToken}` } : {};
  }

  tryRefresh(): Promise<boolean> {
    if (!this.refreshing) {
      this.refreshing = this.doRefresh().finally(() => (this.refreshing = undefined));
    }
    return this.refreshing;
  }

  private async doRefresh(): Promise<boolean> {
    const tokens = readTokens(this.scope);
    if (!tokens?.refreshToken) return false;
    try {
      const res = await fetch(this.url(this.options.refreshPath), {
        method: "POST",
        headers: { Authorization: `Bearer ${tokens.refreshToken}`, Accept: "application/json" },
      });
      if (!res.ok) throw new Error(`refresh failed (${res.status})`);
      const data = (await res.json()) as LoginResponse;
      if (!data.isAuthenticated || !data.accessToken) throw new Error("refresh rejected");
      writeTokens(this.scope, { accessToken: data.accessToken, refreshToken: data.refreshToken });
      this.setSnapshot({ status: "authenticated", user: userFromJwt(data.accessToken) });
      return true;
    } catch {
      writeTokens(this.scope, undefined);
      this.setSnapshot({ status: "unauthenticated" });
      return false;
    }
  }

  async login(credentials?: PasswordCredentials) {
    if (!credentials) throw new Error("Credentials are required.");
    const res = await fetch(this.url(this.options.loginPath), {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ username: credentials.username, password: credentials.password }),
    });
    const data = res.ok ? ((await res.json()) as LoginResponse) : undefined;
    if (!res.ok || !data?.isAuthenticated || !data.accessToken) {
      this.setSnapshot({ status: "unauthenticated", error: "Invalid username or password." });
      throw new Error("Invalid username or password.");
    }
    writeTokens(this.scope, { accessToken: data.accessToken, refreshToken: data.refreshToken });
    this.setSnapshot({ status: "authenticated", user: userFromJwt(data.accessToken) });
  }

  async logout() {
    const tokens = readTokens(this.scope);
    writeTokens(this.scope, undefined);
    this.setSnapshot({ status: "unauthenticated" });
    if (tokens?.refreshToken && tokens.accessToken) {
      await fetch(this.url("/identity/logout"), {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${tokens.accessToken}` },
        body: JSON.stringify({ refreshToken: tokens.refreshToken }),
      }).catch(() => undefined);
    }
  }
}
