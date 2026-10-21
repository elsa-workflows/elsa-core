"use client";

import { ThemeProvider } from "next-themes";
import type { ReactNode } from "react";
import { Toaster } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { StudioConfigProvider } from "@/config/provider";
import type { StudioConfig } from "@/config/schema";
import { ElsaApiProvider } from "@/lib/api/provider";
import { AuthProvider } from "@/lib/auth/provider";
import { ModuleRegistryProvider } from "@/lib/modules/registry";
import { ElsaThemeProvider } from "@/lib/theme/elsa-theme-provider";
import { studioModules } from "@/modules";

export function Providers({
  config,
  children,
}: {
  config: StudioConfig;
  children: ReactNode;
}) {
  const moduleProviders = studioModules.filter((m) => m.Provider);
  const wrapped = moduleProviders.reduceRight((acc, m) => {
    const P = m.Provider!;
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
              <ModuleRegistryProvider modules={studioModules}>
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
