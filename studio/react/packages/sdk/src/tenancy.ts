import { createContext, useContext } from "react";
import { tenancyConfigSchema, type ServerConfig } from "./config.js";

export interface TenantIdentity { name: string; claims?: Record<string, unknown>; tenantId?: string }
export interface TenantContextValue {
  tenantId?: string;
  mode: "fixed" | "claims" | "selection";
  tenants: { id: string; name: string }[];
  /** Selects a configured tenant and reloads the application. Returns false if canceled. */
  selectTenant: (id: string) => boolean;
  error?: string;
}

/** Stable identity for browser preferences, without persisting tokens or claim sets. */
export function tenantIdentityKey(user: TenantIdentity | undefined): string {
  const subject = user?.claims?.sub ?? user?.claims?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];
  return typeof subject === "string" ? subject : user?.name ?? "unauthenticated";
}

export function tenantSelectionKey(serverUrl: string, identity: string): string {
  return "elsa-studio:tenant:" + JSON.stringify([serverUrl.replace(/\/+$/, ""), identity]);
}

/** A selection is configuration, never a grant of backend authorization. */
export function resolveTenant(server: ServerConfig, user: TenantIdentity | undefined, selected?: string): Omit<TenantContextValue, "selectTenant"> {
  const policy = server.tenancy ?? tenancyConfigSchema.parse({});
  const context = { mode: policy.mode, tenants: policy.tenants };
  if (policy.mode === "fixed") return { ...context, tenantId: server.tenantId };
  if (policy.mode === "claims") {
    const claim = user?.claims?.[policy.claimType] ?? (policy.claimType === "http://schemas.microsoft.com/identity/claims/tenantid" ? user?.tenantId : undefined);
    return typeof claim === "string" ? { ...context, tenantId: claim } : { ...context, error: "Your account does not specify a tenant." };
  }
  const configured = policy.tenants.map(tenant => tenant.id);
  return { ...context, tenantId: [selected, server.tenantId, configured[0]].find(id => id !== undefined && configured.includes(id)) };
}

const TenantContext = createContext<TenantContextValue>({ mode: "fixed", tenants: [], selectTenant: () => false });
export const TenantProvider = TenantContext.Provider;
export function useTenant(): TenantContextValue { return useContext(TenantContext); }
