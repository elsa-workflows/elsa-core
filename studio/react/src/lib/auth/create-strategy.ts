import type { AuthConfig, ServerConfig } from "@/config/schema";
import { ApiKeyAuthStrategy } from "./strategies/api-key";
import { ElsaExternalAuthStrategy } from "./strategies/elsa-external";
import { ElsaIdentityAuthStrategy } from "./strategies/elsa-identity";
import { NoAuthStrategy } from "./strategies/none";
import { OidcAuthStrategy } from "./strategies/oidc";
import type { AuthStrategy } from "./types";

/** Picks the strategy named by configuration. Tokens are scoped per server URL. */
export function createAuthStrategy(auth: AuthConfig, server: ServerConfig): AuthStrategy {
  const scope = encodeURIComponent(server.url);
  switch (auth.provider) {
    case "none":
      return new NoAuthStrategy();
    case "api-key":
      return new ApiKeyAuthStrategy(auth.apiKey);
    case "oidc":
      return new OidcAuthStrategy(auth.oidc, scope);
    case "elsa-external":
      return new ElsaExternalAuthStrategy(server.url, auth.elsaExternal, scope);
    case "elsa-identity":
    default:
      return new ElsaIdentityAuthStrategy(server.url, auth.elsaIdentity, scope);
  }
}
