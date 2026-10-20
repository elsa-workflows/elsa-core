"use client";

import type { ReactNode } from "react";
import { SidebarInset, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { Separator } from "@/components/ui/separator";
import { ConfirmProvider } from "@/components/common/confirm-dialog";
import { useExtensions } from "@/lib/modules/registry";
import { cn } from "@/lib/utils";
import { useShellChrome } from "./shell-chrome";
import { StudioSidebar } from "./studio-sidebar";
import { ThemeMenu } from "./theme-menu";
import { UserMenu } from "./user-menu";

export function AppShell({ title, children }: { title?: string; children: ReactNode }) {
  const headerItems = useExtensions("headerItems");
  const custom = useShellChrome((s) => s.header);
  const fullBleed = useShellChrome((s) => s.fullBleed);
  return (
    <ConfirmProvider>
      <SidebarProvider>
        <StudioSidebar />
        <SidebarInset className="min-w-0 bg-background">
          <header className="sticky top-0 z-20 flex h-12 shrink-0 items-center gap-2 border-b bg-background/85 px-3 backdrop-blur supports-[backdrop-filter]:bg-background/70">
            <SidebarTrigger className="text-muted-foreground" />
            <Separator orientation="vertical" className="mx-1 h-5" />
            {custom ? (
              <div className="flex min-w-0 flex-1 items-center gap-2">{custom}</div>
            ) : (
              <div className="flex min-w-0 flex-1 items-center gap-2 text-sm">
                <span className="port-dot bg-primary" aria-hidden />
                <span className="truncate font-medium">{title}</span>
              </div>
            )}
            <div className="flex shrink-0 items-center gap-1">
              {headerItems.map(({ id, component: Item }) => (
                <Item key={id} />
              ))}
              <Separator orientation="vertical" className="mx-1 h-5" />
              <ThemeMenu />
              <UserMenu />
            </div>
          </header>
          <main className={cn("flex flex-1 flex-col min-h-0", fullBleed ? "" : "p-4 lg:p-6")}>{children}</main>
        </SidebarInset>
      </SidebarProvider>
    </ConfirmProvider>
  );
}
