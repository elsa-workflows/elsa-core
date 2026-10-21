import type { AuthConfig } from "@/config/schema";
import { BaseAuthStrategy } from "../types";

/**
 * Static API key. Elsa's default authentication feature accepts it as
 * `Authorization: ApiKey <key>`; an `X-Api-Key` header variant is offered for reverse proxies.
 */
export class ApiKeyAuthStrategy extends BaseAuthStrategy {
  readonly kind = "api-key" as const;
  readonly interactive = false;
  readonly usesCredentials = false;

  constructor(private readonly options: AuthConfig["apiKey"]) {
    super();
  }

  async initialize() {
    if (!this.options.key) {
      this.setSnapshot({ status: "unauthenticated", error: "No API key configured (auth.apiKey.key)." });
      return;
    }
    this.setSnapshot({ status: "authenticated", user: { name: "API key" } });
  }
  async getAuthHeaders(): Promise<Record<string, string>> {
    if (!this.options.key) return {};
    return this.options.scheme === "x-api-key-header"
      ? { "X-Api-Key": this.options.key }
      : { Authorization: `ApiKey ${this.options.key}` };
  }
  async tryRefresh() {
    return false;
  }
  async login() {}
  async logout() {}
}
