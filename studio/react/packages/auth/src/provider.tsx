"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { createContext, useContext, useEffect, useMemo, useSyncExternalStore, type ReactNode } from "react";
import type { AuthSnapshot, AuthStrategy, PasswordCredentials } from "./types.js";

export interface AuthContextValue extends AuthSnapshot {
  strategy: AuthStrategy;
  login: (credentials?: PasswordCredentials, method?: unknown) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ strategy, children }: { strategy: AuthStrategy; children: ReactNode }) {
  useTranslation();

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
  if (!ctx) throw new Error(translate("useAuth must be used inside <AuthProvider>"));
  return ctx;
}
