"use client";

import { ElsaApiProvider as SharedElsaApiProvider } from "@elsa-workflows/react-studio-api-client/react";
export { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { useLayoutEffect, useMemo, useRef, type ReactNode } from "react";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@elsa-workflows/react-studio-sdk/environments/active-server";
import { studioBasePath } from "@elsa-workflows/react-studio-sdk/navigation/base-path";
import { createElsaClient } from "@elsa-workflows/react-studio-api-client";
import { useTenant, tenantIdentityKey } from "@elsa-workflows/react-studio-sdk/tenancy";


/** Builds one API client for the active server + auth strategy and provides TanStack Query. */
export function ElsaApiProvider({ children }: { children: ReactNode }) {
  const server = useActiveServer();
  const { strategy, user, status } = useAuth();
  const { tenantId } = useTenant();
  const scope = JSON.stringify([server.url.replace(/\/+$/, ""), tenantIdentityKey(user), tenantId ?? null]);

  const authenticatedScope = useRef<string | undefined>(undefined);
  useLayoutEffect(() => {
    if (status !== "authenticated") return;
    if (authenticatedScope.current !== undefined && authenticatedScope.current !== scope) {
      window.location.assign((studioBasePath() || "") + "/");
      return;
    }
    authenticatedScope.current = scope;
  }, [scope, status]);

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
