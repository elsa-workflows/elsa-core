import { useMemo, useState, type ReactNode } from "react";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@elsa-workflows/react-studio-sdk/environments/active-server";
import { TenantProvider, resolveTenant, tenantIdentityKey, tenantSelectionKey } from "@elsa-workflows/react-studio-sdk/tenancy";
import { approveNavigation, hasUnsavedChanges } from "@elsa-workflows/react-studio-sdk/unsaved-changes";
import { studioBasePath } from "@elsa-workflows/react-studio-sdk/navigation/base-path";

export function TenancyProvider({ children }: { children: ReactNode }) {
  const server = useActiveServer();
  const auth = useAuth();
  const preferenceKey = tenantSelectionKey(server.url, tenantIdentityKey(auth.user));
  // Preference changes take effect through a full navigation. Read again when
  // identity/server changes, without carrying a previous user's selection.
  const selected = useMemo(() => {
    try { return localStorage.getItem(preferenceKey) ?? undefined; } catch { return undefined; }
  }, [preferenceKey]);
  const tenant = resolveTenant(server, auth.user, selected);
  const [error, setError] = useState<string>();
  const value = { ...tenant, error: error ?? tenant.error,
    selectTenant: (id: string) => {
      if (tenant.mode !== "selection" || !tenant.tenants.some(item => item.id === id) || id === tenant.tenantId) return false;
      if (hasUnsavedChanges() && !window.confirm("Switch tenant and discard unsaved changes?")) return false;
      try { localStorage.setItem(preferenceKey, id); }
      catch { setError("Tenant selection could not be saved. Enable browser storage and try again."); return false; }
      // Reload clears every module's stores, query cache, open editors and pending
      // browser requests, including state owned by third-party modules.
      approveNavigation();
      window.location.assign((studioBasePath() || "") + "/");
      return true;
    } };
  if (auth.status === "initializing") return <p role="status">Loading account…</p>;
  if (auth.status === "authenticated" && tenant.error) return <div role="alert"><p>{tenant.error}</p>{auth.strategy.interactive && <button onClick={() => void auth.logout()}>Sign out</button>}</div>;
  return <TenantProvider value={value}>{children}</TenantProvider>;
}
