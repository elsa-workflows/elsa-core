
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useLayoutEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@elsa-workflows/react-studio-sdk/environments/active-server";
import { TenantProvider, resolveTenant, tenantIdentityKey, tenantSelectionKey } from "@elsa-workflows/react-studio-sdk/tenancy";
import { approveNavigation, hasUnsavedChanges } from "@elsa-workflows/react-studio-sdk/unsaved-changes";
import { reloadStudioContext } from "@elsa-workflows/react-studio-sdk/navigation/reload";

export function TenancyProvider({ children }: { children: ReactNode }) {
  const _t = useTranslation();
  const server = useActiveServer();
  const auth = useAuth();
  const preferenceKey = tenantSelectionKey(server.url, tenantIdentityKey(auth.user));
  // Preference changes take effect through a full navigation. Read again when
  // identity/server changes, without carrying a previous user's selection.
  const selected = useMemo(() => {
    try { return localStorage.getItem(preferenceKey) ?? undefined; } catch { return undefined; }
  }, [preferenceKey]);
  const tenant = resolveTenant(server, auth.user, selected);
  // This owner remains mounted while a missing tenant claim blocks its children.
  // Remember the last valid authenticated context across that intermediate state.
  const previousScope = useRef<string | undefined>(undefined);
  const scope = JSON.stringify([server.url.replace(/\/+$/, ""), tenantIdentityKey(auth.user), tenant.tenantId ?? null]);
  const changed = auth.status === "authenticated" && !tenant.error && previousScope.current !== undefined && previousScope.current !== scope;
  useLayoutEffect(() => {
    if (auth.status !== "authenticated" || tenant.error) return;
    if (previousScope.current !== undefined && previousScope.current !== scope) {
      reloadStudioContext();
      return;
    }
    previousScope.current = scope;
  }, [scope, auth.status, tenant.error]);
  const [error, setError] = useState<string>();
  const value = { ...tenant, error: error ?? tenant.error,
    selectTenant: (id: string) => {
      if (tenant.mode !== "selection" || !tenant.tenants.some(item => item.id === id) || id === tenant.tenantId) return false;
      if (hasUnsavedChanges() && !window.confirm(_t("Switch tenant and discard unsaved changes?"))) return false;
      try { localStorage.setItem(preferenceKey, id); }
      catch { setError(_t("Tenant selection could not be saved. Enable browser storage and try again.")); return false; }
      // Reload clears every module's stores, query cache, open editors and pending
      // browser requests, including state owned by third-party modules.
      return reloadStudioContext(approveNavigation);
    } };
  if (auth.status === "initializing") return <p role="status">{_t("Loading account…")}</p>;
  if (changed) return <div role="alert">{_t("The account or tenant changed. Reload Studio to continue.")}{" "}<button onClick={() => reloadStudioContext()}>{_t("Reload Studio")}</button></div>;
  if (auth.status === "authenticated" && tenant.error) return <div role="alert"><p>{tenant.error}</p>{auth.strategy.interactive && <button onClick={() => void auth.logout()}>{_t("Sign out")}</button>}</div>;
  return <TenantProvider value={value}>{children}</TenantProvider>;
}
