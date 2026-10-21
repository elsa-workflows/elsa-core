"use client";

import { ElsaApiProvider as SharedElsaApiProvider } from "@elsa-workflows/react-studio-api-client/react";
export { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { useMemo, type ReactNode } from "react";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@elsa-workflows/react-studio-sdk/environments/active-server";
import { createElsaClient } from "@elsa-workflows/react-studio-api-client";
import { useTenant, tenantIdentityKey, resolveTenant } from "@elsa-workflows/react-studio-sdk/tenancy";


/** Builds one API client for the active server + auth strategy and provides TanStack Query. */
export function ElsaApiProvider({ children }: { children: ReactNode }) {
  const server = useActiveServer();
  const { strategy, user, status } = useAuth();
  const { tenantId } = useTenant();
  const scope = JSON.stringify([server.url.replace(/\/+$/, ""), status, tenantIdentityKey(user), tenantId ?? null]);

  const client = useMemo(
    () =>
      createElsaClient({
        baseUrl: server.url,
        tenantId,
        validateRequestScope: () => {
          const current = strategy.getSnapshot();
          const tenant = resolveTenant(server, current.user, tenantId);
          const currentScope = JSON.stringify([server.url.replace(/\/+$/, ""), current.status, tenantIdentityKey(current.user), tenant.tenantId ?? null]);
          if (tenant.error || currentScope !== scope) throw new Error("The account or tenant changed. Reload Studio to continue.");
        },
        getAuthHeaders: () => strategy.getAuthHeaders(),
        onUnauthorized: () => strategy.tryRefresh(),
      }),
    [server, strategy, tenantId, scope],
  );

  return <SharedElsaApiProvider client={client} scope={scope}>{children}</SharedElsaApiProvider>;
}
