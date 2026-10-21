"use client";

import { ElsaApiProvider as SharedElsaApiProvider } from "@elsa-workflows/react-studio-api-client/react";
export { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { useMemo, type ReactNode } from "react";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@elsa-workflows/react-studio-sdk/environments/active-server";
import { createElsaClient } from "@elsa-workflows/react-studio-api-client";


/** Builds one API client for the active server + auth strategy and provides TanStack Query. */
export function ElsaApiProvider({ children }: { children: ReactNode }) {
  const server = useActiveServer();
  const { strategy } = useAuth();

  const client = useMemo(
    () =>
      createElsaClient({
        baseUrl: server.url,
        tenantId: server.tenantId,
        getAuthHeaders: () => strategy.getAuthHeaders(),
        onUnauthorized: () => strategy.tryRefresh(),
      }),
    [server, strategy],
  );

  return <SharedElsaApiProvider client={client} scope={server.url}>{children}</SharedElsaApiProvider>;
}
