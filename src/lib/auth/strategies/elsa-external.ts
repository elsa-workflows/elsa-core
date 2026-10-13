import type { AuthConfig } from "@/config/schema";
import { readTokens, writeTokens } from "../storage";
import { BaseAuthStrategy, isJwtExpired, userFromJwt, type PasswordCredentials } from "../types";

/**
 * Elsa's External Authentication broker (`Elsa.ExternalAuthentication` module).
 *
 * The Elsa server itself acts as an OAuth2 authorization server with PKCE in front of one or more
 * upstream identity providers plus the local user store:
 *
 *  - `GET  /external-authentication/login-methods?clientId=` lists local and external methods.
 *  - external method: navigate to `initiationUrl` with `client_id`, `redirect_uri`, `response_type=code`,
 *    `code_challenge`, `code_challenge_method=S256`, `return_path`.
 *  - local method: `POST /external-authentication/local/authorize` with credentials → `{ redirectUri }`.
 *  - callback lands on `redirect_uri?code=&state=`; `POST /external-authentication/token`
 *    (form-encoded, `grant_type=authorization_code`, `code_verifier`) → `{ accessToken, refreshToken, expiresIn }`.
 *  - refresh with `grant_type=refresh_token`, logout via `POST /external-authentication/logout`.
 */
export interface LoginMethod {
  id?: string;
  key: string;
  kind: "local" | "external";
  displayName: string;
  iconId?: string;
  order?: number;
  isPreferred?: boolean;
  initiationUrl?: string;
}

interface TokenResponse {
  accessToken: string;
  tokenType: string;
  expiresIn?: number;
  refreshToken?: string;
}

const PKCE_KEY = "elsa-studio:external-auth:pkce";

export class ElsaExternalAuthStrategy extends BaseAuthStrategy {
  readonly kind = "elsa-external" as const;
  readonly interactive = true;
  readonly usesCredentials = true;

  private refreshing?: Promise<boolean>;

  constructor(
    private readonly apiBaseUrl: string,
    private readonly options: AuthConfig["elsaExternal"],
    private readonly scope: string,
  ) {
    super();
  }

  private url(path: string) {
    return `${this.apiBaseUrl.replace(/\/+$/, "")}/${path.replace(/^\/+/, "")}`;
  }

  private redirectUri() {
    const uri = this.options.redirectUri ?? "/auth/callback";
    return /^https?:\/\//i.test(uri) ? uri : `${window.location.origin}${uri}`;
  }

  async initialize() {
    const tokens = readTokens(this.scope);
    if (!tokens?.accessToken) return this.setSnapshot({ status: "unauthenticated" });
    if (isJwtExpired(tokens.accessToken) && !(await this.tryRefresh())) return this.setSnapshot({ status: "unauthenticated" });
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
    if (!this.refreshing) this.refreshing = this.doRefresh().finally(() => (this.refreshing = undefined));
    return this.refreshing;
  }

  private async doRefresh() {
    const tokens = readTokens(this.scope);
    if (!tokens?.refreshToken) return false;
    try {
      const data = await this.token({ grant_type: "refresh_token", refresh_token: tokens.refreshToken, client_id: this.options.clientId });
      this.store(data);
      return true;
    } catch {
      writeTokens(this.scope, undefined);
      this.setSnapshot({ status: "unauthenticated" });
      return false;
    }
  }

  private async token(form: Record<string, string>): Promise<TokenResponse> {
    const res = await fetch(this.url("/external-authentication/token"), {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded", Accept: "application/json" },
      body: new URLSearchParams(form).toString(),
    });
    if (!res.ok) throw new Error(`token endpoint failed (${res.status})`);
    return (await res.json()) as TokenResponse;
  }

  private store(data: TokenResponse) {
    writeTokens(this.scope, { accessToken: data.accessToken, refreshToken: data.refreshToken });
    this.setSnapshot({ status: "authenticated", user: userFromJwt(data.accessToken) });
  }

  /** Lists the login methods the server offers; the login page renders one button per method. */
  async getLoginMethods(): Promise<LoginMethod[]> {
    const res = await fetch(this.url(`/external-authentication/login-methods?clientId=${encodeURIComponent(this.options.clientId)}`));
    if (!res.ok) return [];
    const data = (await res.json()) as { methods: LoginMethod[] };
    return (data.methods ?? []).sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
  }

  private async beginPkce() {
    const verifier = randomString(64);
    const challenge = await sha256Base64Url(verifier);
    const state = randomString(16);
    sessionStorage.setItem(PKCE_KEY, JSON.stringify({ verifier, state, returnTo: window.location.pathname + window.location.search }));
    return { challenge, state };
  }

  /** Starts an external method (redirect) or, with credentials, the local method. */
  async login(credentials?: PasswordCredentials, method?: LoginMethod) {
    const { challenge, state } = await this.beginPkce();
    const common = {
      client_id: this.options.clientId,
      redirect_uri: this.redirectUri(),
      response_type: "code",
      code_challenge: challenge,
      code_challenge_method: "S256",
      state,
    };

    if (credentials) {
      const res = await fetch(this.url("/external-authentication/local/authorize"), {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({
          clientId: common.client_id,
          redirectUri: common.redirect_uri,
          responseType: common.response_type,
          codeChallenge: common.code_challenge,
          codeChallengeMethod: common.code_challenge_method,
          state,
          username: credentials.username,
          password: credentials.password,
        }),
      });
      if (!res.ok) {
        this.setSnapshot({ status: "unauthenticated", error: "Invalid username or password." });
        throw new Error("Invalid username or password.");
      }
      const { redirectUri } = (await res.json()) as { redirectUri: string };
      window.location.assign(redirectUri);
      return;
    }

    const initiation = method?.initiationUrl ?? (await this.getLoginMethods()).find((m) => m.kind === "external")?.initiationUrl;
    if (!initiation) throw new Error("No external login method is available.");
    const target = new URL(initiation, this.apiBaseUrl);
    Object.entries(common).forEach(([k, v]) => target.searchParams.set(k, v));
    window.location.assign(target.toString());
  }

  async completeSignIn() {
    const params = new URLSearchParams(window.location.search);
    const code = params.get("code");
    const state = params.get("state");
    const stored = JSON.parse(sessionStorage.getItem(PKCE_KEY) ?? "null") as { verifier: string; state: string; returnTo?: string } | null;
    sessionStorage.removeItem(PKCE_KEY);
    if (!code || !stored || (state && stored.state !== state)) throw new Error("Invalid sign-in callback.");
    const data = await this.token({
      grant_type: "authorization_code",
      code,
      client_id: this.options.clientId,
      redirect_uri: this.redirectUri(),
      code_verifier: stored.verifier,
    });
    this.store(data);
    window.history.replaceState({}, "", stored.returnTo ?? "/");
  }

  async logout() {
    const tokens = readTokens(this.scope);
    writeTokens(this.scope, undefined);
    this.setSnapshot({ status: "unauthenticated" });
    if (tokens?.refreshToken) {
      await fetch(this.url("/external-authentication/logout"), {
        method: "POST",
        headers: { "Content-Type": "application/json", ...(tokens.accessToken ? { Authorization: `Bearer ${tokens.accessToken}` } : {}) },
        body: JSON.stringify({ refreshToken: tokens.refreshToken, clientId: this.options.clientId }),
      }).catch(() => undefined);
    }
  }
}

function randomString(length: number) {
  const bytes = crypto.getRandomValues(new Uint8Array(length));
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
  return Array.from(bytes, (b) => alphabet[b % alphabet.length]).join("");
}

async function sha256Base64Url(input: string) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(input));
  return btoa(String.fromCharCode(...new Uint8Array(digest))).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
