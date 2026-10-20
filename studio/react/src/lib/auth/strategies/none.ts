import { BaseAuthStrategy } from "../types";

/** Used when the Elsa server runs without authorization. */
export class NoAuthStrategy extends BaseAuthStrategy {
  readonly kind = "none" as const;
  readonly interactive = false;
  readonly usesCredentials = false;

  async initialize() {
    this.setSnapshot({ status: "authenticated", user: { name: "Anonymous" } });
  }
  async getAuthHeaders(): Promise<Record<string, string>> {
    return {};
  }
  async tryRefresh() {
    return false;
  }
  async login() {}
  async logout() {}
}
