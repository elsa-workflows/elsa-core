import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import type { ElsaClient } from "./client.js";

const ElsaClientContext = createContext<{ client: ElsaClient; scope: string } | null>(null);

/** Shares one host-created client and query cache with independently loaded modules. */
export function ElsaApiProvider({ client, scope = client.http.baseUrl, children }: {
  client: ElsaClient; scope?: string; children: ReactNode;
}) {
  const [queryClient] = useState(() => new QueryClient({ defaultOptions: {
    queries: { staleTime: 10_000, retry: (count, error) => count < 2 && !(error as { status?: number }).status },
  } }));
  const value = useMemo(() => ({ client, scope }), [client, scope]);
  return <QueryClientProvider client={queryClient}>
    <ElsaClientContext.Provider value={value}>{children}</ElsaClientContext.Provider>
  </QueryClientProvider>;
}

function useClientContext() {
  const context = useContext(ElsaClientContext);
  if (!context) throw new Error("Elsa API hooks require <ElsaApiProvider>");
  return context;
}

export function useElsaClient(): ElsaClient { return useClientContext().client; }
export function useQueryScope(): string { return useClientContext().scope; }
