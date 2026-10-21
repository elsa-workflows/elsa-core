"use client";

import { ThemeProvider } from "next-themes";
import type { ReactNode } from "react";
import { Toaster } from "@elsa-workflows/react-studio-ui/sonner";
import { TooltipProvider } from "@elsa-workflows/react-studio-ui/tooltip";
import { StudioConfigProvider } from "@/config/provider";
import type { StudioConfig } from "@/config/schema";
import { ElsaApiProvider } from "@/lib/api/provider";
import { AuthProvider } from "@/lib/auth/provider";
import { ModuleRegistryProvider } from "@/lib/modules/registry";
import { ElsaThemeProvider } from "@/lib/theme/elsa-theme-provider";
import { studioModules } from "@/modules";
import type { StudioModule } from "@elsa-workflows/react-studio-sdk";

export function Providers({
  config,
  modules = studioModules,
  children,
}: {
  config: StudioConfig;
  modules?: StudioModule[];
  children: ReactNode;
}) {
  const moduleProviders = modules.flatMap(module =>
    (module.extensions?.providers ?? (module.Provider ? [{ id: "provider", order: 0, component: module.Provider }] : []))
      .map(provider => ({ ...provider, moduleId: module.id })));
  moduleProviders.sort((a, b) => (a.order ?? 0) - (b.order ?? 0) ||
    compare(a.moduleId, b.moduleId) || compare(a.id, b.id));
  const wrapped = moduleProviders.reduceRight((acc, provider) => {
    const P = provider.component;
    return <P>{acc}</P>;
  }, children);

  return (
    <ThemeProvider
      attribute="class"
      defaultTheme={config.ui.defaultTheme}
      enableSystem
      disableTransitionOnChange
    >
      <StudioConfigProvider config={config}>
        <ElsaThemeProvider>
          <AuthProvider>
            <ElsaApiProvider>
              <ModuleRegistryProvider modules={modules}>
                <TooltipProvider>{wrapped}</TooltipProvider>
                <Toaster richColors closeButton position="bottom-right" />
              </ModuleRegistryProvider>
            </ElsaApiProvider>
          </AuthProvider>
        </ElsaThemeProvider>
      </StudioConfigProvider>
    </ThemeProvider>
  );
}

function compare(a: string, b: string) { return a < b ? -1 : a > b ? 1 : 0; }
