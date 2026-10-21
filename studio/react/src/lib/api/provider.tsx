"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ElsaApiProvider as SharedElsaApiProvider } from "@elsa-workflows/react-studio-api-client/react";
export { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { useMemo, type ReactNode } from "react";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@elsa-workflows/react-studio-sdk/environments/active-server";
import { createElsaClient } from "@elsa-workflows/react-studio-api-client";
import { useTenant, tenantIdentityKey } from "@elsa-workflows/react-studio-sdk/tenancy";


/** Builds one API client for the active server + auth strategy and provides TanStack Query. */
export function ElsaApiProvider({ children }: { children: ReactNode }) {
  useTranslation();
  const server = useActiveServer();
  const { strategy, user } = useAuth();
  const { tenantId } = useTenant();
  const scope = JSON.stringify([server.url.replace(/\/+$/, ""), tenantIdentityKey(user), tenantId ?? null]);

  const client = useMemo(
    () =>
      createElsaClient({
        baseUrl: server.url,
        tenantId,
        getAuthHeaders: () => strategy.getAuthHeaders(),
        onUnauthorized: () => strategy.tryRefresh(),
      }),
    [server, strategy, tenantId],
  );

  return <SharedElsaApiProvider client={client} scope={scope}>{children}</SharedElsaApiProvider>;
}
