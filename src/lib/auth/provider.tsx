"use client";

import { createContext, useContext, useEffect, useMemo, useSyncExternalStore, type ReactNode } from "react";
import { useStudioConfig } from "@/config/provider";
import { useActiveServer } from "@/lib/environments/active-server";
import { createAuthStrategy } from "./create-strategy";
import type { AuthSnapshot, AuthStrategy, PasswordCredentials } from "./types";

export interface AuthContextValue extends AuthSnapshot {
  strategy: AuthStrategy;
  login: (credentials?: PasswordCredentials, method?: unknown) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const config = useStudioConfig();
  const server = useActiveServer();
  const strategy = useMemo(() => createAuthStrategy(config.auth, server), [config.auth, server]);

  useEffect(() => {
    void strategy.initialize();
  }, [strategy]);

  const snapshot = useSyncExternalStore(
    (cb) => strategy.subscribe(cb),
    () => strategy.getSnapshot(),
    () => strategy.getSnapshot(),
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      ...snapshot,
      strategy,
      login: (c, m) => strategy.login(c, m),
      logout: () => strategy.logout(),
    }),
    [snapshot, strategy],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
