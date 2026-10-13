import { z } from "zod";

/**
 * Runtime configuration schema for the studio.
 *
 * The configuration is resolved on the server at request time (see `./load.ts`) from
 * a JSON file and/or environment variables, validated with this schema and handed to
 * the browser through `StudioConfigProvider`. Nothing in here may contain secrets:
 * every value ends up in the client bundle.
 */

export const authProviderSchema = z.enum([
  /** No authentication; the server runs with authorization disabled. */
  "none",
  /** Username/password login against the Elsa Identity module (JWT access + refresh tokens). */
  "elsa-identity",
  /** A static API key, sent on every request. */
  "api-key",
  /** OpenID Connect authorization-code flow with PKCE against an external identity provider. */
  "oidc",
  /** Elsa's own External Authentication broker (OAuth2 + PKCE served by the Elsa server). */
  "elsa-external",
]);
export type AuthProvider = z.infer<typeof authProviderSchema>;

export const authConfigSchema = z.object({
  provider: authProviderSchema.default("elsa-identity"),
  /** Elsa Identity options. */
  elsaIdentity: z
    .object({
      /** Login endpoint path relative to the API base URL. */
      loginPath: z.string().default("/identity/login"),
      refreshPath: z.string().default("/identity/refresh-token"),
    })
    .prefault({}),
  /** API key options. The key is public to the browser; use only in trusted setups. */
  apiKey: z
    .object({
      key: z.string().default(""),
      /** How the key is transmitted. */
      scheme: z.enum(["authorization-header", "x-api-key-header"]).default("authorization-header"),
    })
    .prefault({}),
  /** Elsa External Authentication broker options. */
  elsaExternal: z
    .object({
      /** Client id registered with the broker. */
      clientId: z.string().default("elsa-studio"),
      redirectUri: z.string().optional(),
    })
    .prefault({}),
  /** OpenID Connect options (authorization code + PKCE, handled in the browser). */
  oidc: z
    .object({
      authority: z.string().default(""),
      clientId: z.string().default(""),
      scope: z.string().default("openid profile email"),
      /** Absolute or site-relative; defaults to `<origin>/auth/callback`. */
      redirectUri: z.string().optional(),
      postLogoutRedirectUri: z.string().optional(),
      /** Which token is sent to the Elsa API as bearer. */
      tokenType: z.enum(["access_token", "id_token"]).default("access_token"),
      /** Optional extra query parameters for the authorize request (e.g. audience). */
      extraQueryParams: z.record(z.string(), z.string()).prefault({}),
    })
    .prefault({}),
});
export type AuthConfig = z.infer<typeof authConfigSchema>;

export const serverConfigSchema = z.object({
  /** Display name for this backend (shown in the environment switcher). */
  name: z.string().default("Default"),
  /** Base URL of the Elsa API, e.g. `https://localhost:5001/elsa/api`. */
  url: z.string().min(1),
  /** Optional tenant id sent as `X-Tenant-Id` header. */
  tenantId: z.string().optional(),
});
export type ServerConfig = z.infer<typeof serverConfigSchema>;

export const featureFlagSchema = z.object({
  enabled: z.boolean().default(true),
  /** Free-form options passed to the module. */
  options: z.record(z.string(), z.unknown()).prefault({}),
});
export type FeatureFlag = z.infer<typeof featureFlagSchema>;

export const studioConfigSchema = z.object({
  /** Application title shown in the sidebar and browser tab. */
  title: z.string().default("Elsa Studio"),
  /** Backend servers. The first one is active by default; the `environments` module lets users switch. */
  servers: z.array(serverConfigSchema).min(1),
  auth: authConfigSchema.prefault({}),
  /**
   * Feature flags keyed by module id. Core modules (`dashboard`, `workflow-definitions`,
   * `workflow-instances`) ignore the `enabled` flag; every other module is disabled unless
   * explicitly enabled here.
   */
  features: z.record(z.string(), featureFlagSchema).prefault({}),
  ui: z
    .object({
      /** Light/dark appearance. */
      defaultTheme: z.enum(["light", "dark", "system"]).default("system"),
      /** Elsa theme pack (colour palette); users can override it from the header menu. */
      theme: z
        .enum(["classic", "workflow-constellation", "workflow-aurora", "execution-timeline", "human-automation"])
        .default("classic"),
      /** Polling interval in ms for live views (instances list, instance viewer). */
      pollIntervalMs: z.number().int().positive().default(5000),
      defaultPageSize: z.number().int().positive().default(25),
    })
    .prefault({}),
});
export type StudioConfig = z.infer<typeof studioConfigSchema>;
export type StudioConfigInput = z.input<typeof studioConfigSchema>;
