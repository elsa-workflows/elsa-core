import { UserManager, WebStorageStateStore, type User } from "oidc-client-ts";
import type { AuthConfig } from "@/config/schema";
import { BaseAuthStrategy, userFromJwt } from "../types";

/**
 * OpenID Connect authorization-code flow with PKCE, executed entirely in the browser.
 * The resulting access token (or id token, configurable) is sent to the Elsa API as a bearer token,
 * which the server validates with its own JWT bearer configuration.
 */
export class OidcAuthStrategy extends BaseAuthStrategy {
  readonly kind = "oidc" as const;
  readonly interactive = true;
  readonly usesCredentials = false;

  private manager?: UserManager;
  private user?: User | null;

  constructor(
    private readonly options: AuthConfig["oidc"],
    private readonly scope: string,
  ) {
    super();
  }

  private getManager(): UserManager {
    if (this.manager) return this.manager;
    const origin = window.location.origin;
    this.manager = new UserManager({
      authority: this.options.authority,
      client_id: this.options.clientId,
      scope: this.options.scope,
      redirect_uri: absolute(this.options.redirectUri ?? "/auth/callback", origin),
      post_logout_redirect_uri: absolute(this.options.postLogoutRedirectUri ?? "/", origin),
      response_type: "code",
      automaticSilentRenew: true,
      userStore: new WebStorageStateStore({ store: window.localStorage, prefix: `elsa-studio:oidc:${this.scope}:` }),
      extraQueryParams: this.options.extraQueryParams,
    });
    this.manager.events.addUserLoaded((u) => this.apply(u));
    this.manager.events.addUserUnloaded(() => this.apply(null));
    this.manager.events.addAccessTokenExpired(() => void this.tryRefresh());
    return this.manager;
  }

  private token(u: User | null | undefined): string | undefined {
    if (!u) return undefined;
    return this.options.tokenType === "id_token" ? u.id_token : u.access_token;
  }

  private apply(u: User | null) {
    this.user = u;
    const token = this.token(u);
    if (!u || !token || u.expired) {
      this.setSnapshot({ status: "unauthenticated" });
      return;
    }
    const fromJwt = userFromJwt(token);
    this.setSnapshot({
      status: "authenticated",
      user: {
        ...fromJwt,
        name: (u.profile.name as string) ?? (u.profile.preferred_username as string) ?? fromJwt.name,
        claims: { ...fromJwt.claims, ...u.profile },
      },
    });
  }

  async initialize() {
    if (!this.options.authority || !this.options.clientId) {
      this.setSnapshot({ status: "unauthenticated", error: "OIDC authority/clientId are not configured." });
      return;
    }
    const u = await this.getManager().getUser();
    if (u && u.expired && u.refresh_token) {
      await this.tryRefresh();
      return;
    }
    this.apply(u);
  }

  async getAuthHeaders(): Promise<Record<string, string>> {
    if (this.user?.expired) await this.tryRefresh();
    const token = this.token(this.user);
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  async tryRefresh() {
    try {
      const u = await this.getManager().signinSilent();
      this.apply(u);
      return !!u && !u.expired;
    } catch {
      this.apply(null);
      return false;
    }
  }

  async login() {
    await this.getManager().signinRedirect({ state: { returnTo: window.location.pathname + window.location.search } });
  }

  async completeSignIn() {
    const u = await this.getManager().signinCallback();
    if (u) this.apply(u);
    const returnTo = (u?.state as { returnTo?: string } | undefined)?.returnTo ?? "/";
    window.history.replaceState({}, "", returnTo);
  }

  async logout() {
    const m = this.getManager();
    await m.removeUser();
    this.apply(null);
    await m.signoutRedirect().catch(() => undefined);
  }
}

function absolute(uri: string, origin: string) {
  return /^https?:\/\//i.test(uri) ? uri : `${origin}${uri.startsWith("/") ? "" : "/"}${uri}`;
}
