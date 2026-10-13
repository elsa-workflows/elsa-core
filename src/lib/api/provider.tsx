"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@/lib/environments/active-server";
import { createElsaClient, type ElsaClient } from "./client";

const ElsaClientContext = createContext<ElsaClient | null>(null);

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

  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 10_000, retry: (count, err) => count < 2 && !(err as { status?: number }).status },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <ElsaClientContext.Provider value={client}>{children}</ElsaClientContext.Provider>
    </QueryClientProvider>
  );
}

export function useElsaClient(): ElsaClient {
  const ctx = useContext(ElsaClientContext);
  if (!ctx) throw new Error("useElsaClient must be used inside <ElsaApiProvider>");
  return ctx;
}

/** Query-key prefix scoped to the active server so switching environments never mixes caches. */
export function useQueryScope() {
  const server = useActiveServer();
  return server.url;
}
