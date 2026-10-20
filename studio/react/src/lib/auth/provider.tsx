import { useMemo, type ReactNode } from "react";
import { AuthProvider as StrategyProvider, createAuthStrategy } from "@elsa-workflows/react-studio-auth";
import { useStudioConfig } from "@/config/provider";
import { useActiveServer } from "@/lib/environments/active-server";
export { useAuth, type AuthContextValue } from "@elsa-workflows/react-studio-auth";

export function AuthProvider({ children }: { children: ReactNode }) {
  const config = useStudioConfig();
  const server = useActiveServer();
  const strategy = useMemo(() => createAuthStrategy(config.auth, server), [config.auth, server]);
  return <StrategyProvider strategy={strategy}>{children}</StrategyProvider>;
}
