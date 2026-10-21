"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ChevronRight, LogOut, Plus } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
  useSidebar,
} from "@/components/ui/sidebar";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useStudioConfig } from "@/config/provider";
import { useAuth } from "@/lib/auth/provider";
import { useActiveServer } from "@/lib/environments/active-server";
import { useNavigation } from "@/lib/modules/registry";
import { NavGroups, type NavItem } from "@/lib/modules/types";
import { Resources, usePermission, Verbs } from "@/lib/permissions";
import { cn } from "@/lib/utils";
import { BrandMark } from "./brand-mark";

function isActive(item: NavItem, pathname: string) {
  if (!item.href) return false;
  if (item.match === "exact" || item.href === "/") return pathname === item.href;
  return pathname === item.href || pathname.startsWith(item.href + "/");
}

function hasActiveChild(item: NavItem, pathname: string): boolean {
  return !!item.children?.some((c) => isActive(c, pathname) || hasActiveChild(c, pathname));
}

/** Icon in a tinted tile; the tile turns primary when the item is active. */
function IconTile({ item, active }: { item: NavItem; active: boolean }) {
  if (!item.icon) return null;
  return (
    <span
      className={cn(
        "flex size-7 shrink-0 items-center justify-center rounded-md transition-colors group-data-[collapsible=icon]:size-6",
        active ? "bg-primary text-primary-foreground shadow-sm" : "bg-sidebar-accent/70 text-muted-foreground group-hover/menu-button:bg-sidebar-accent group-hover/menu-button:text-foreground",
      )}
    >
      <item.icon className="size-4" />
    </span>
  );
}

export function StudioSidebar() {
  const config = useStudioConfig();
  const server = useActiveServer();
  const pathname = usePathname();
  const router = useRouter();
  const nav = useNavigation();
  const { user, strategy, logout } = useAuth();
  const { state } = useSidebar();
  const canCreate = usePermission(Resources.workflowDefinitions, Verbs.create);
  const collapsed = state === "collapsed";

  const groups = Object.values(NavGroups)
    .sort((a, b) => a.order - b.order)
    .map((g) => ({ ...g, items: nav.filter((i) => (i.group ?? NavGroups.general.id) === g.id) }))
    .filter((g) => g.items.length > 0);

  const name = user?.name ?? "Anonymous";
  const initials = name
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((s) => s[0]?.toUpperCase())
    .join("");

  return (
    <Sidebar collapsible="icon" className="border-r">
      {/* Brand header on a soft primary gradient */}
      <SidebarHeader className="relative overflow-hidden border-b border-sidebar-border/70 bg-[linear-gradient(160deg,color-mix(in_oklab,var(--primary)_16%,var(--sidebar))_0%,var(--sidebar)_70%)]">
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" render={<Link href="/" />} className="hover:bg-transparent">
              <BrandMark className="size-8 shrink-0 shadow-sm" />
              <div className="grid flex-1 text-left text-sm leading-tight group-data-[collapsible=icon]:hidden">
                <span className="truncate font-semibold tracking-tight">{config.title}</span>
                <span className="flex items-center gap-1.5 truncate text-[11px] text-muted-foreground">
                  <span className="relative flex size-2">
                    <span className="absolute inline-flex size-full animate-ping rounded-full bg-status-success opacity-50" />
                    <span className="relative inline-flex size-2 rounded-full bg-status-success" />
                  </span>
                  {server.name}
                </span>
              </div>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
        {canCreate && (
          <div className="px-1 pb-1 pt-1.5 group-data-[collapsible=icon]:px-0">
            {collapsed ? (
              <Tooltip>
                <TooltipTrigger render={<Button size="icon-sm" className="mx-auto flex" aria-label="New workflow" onClick={() => router.push("/workflows/definitions?create=1")} />}>
                  <Plus />
                </TooltipTrigger>
                <TooltipContent side="right">New workflow</TooltipContent>
              </Tooltip>
            ) : (
              <Button size="sm" className="w-full justify-start shadow-sm" onClick={() => router.push("/workflows/definitions?create=1")}>
                <Plus /> New workflow
              </Button>
            )}
          </div>
        )}
      </SidebarHeader>

      <SidebarContent className="gap-0 pt-1">
        {groups.map((group) => (
          <SidebarGroup key={group.id} className="py-1.5">
            <SidebarGroupLabel className="eyebrow h-7 px-2">{group.label}</SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu className="gap-0.5">
                {group.items.map((item) => {
                  const active = isActive(item, pathname);
                  const childActive = hasActiveChild(item, pathname);
                  if (item.children?.length) {
                    return (
                      <Collapsible key={item.id} defaultOpen={childActive || item.id === "workflows"} className="group/collapsible">
                        <SidebarMenuItem>
                          <CollapsibleTrigger render={<SidebarMenuButton tooltip={item.label} className={cn("h-9 group/menu-button", childActive && "text-foreground")} />}>
                            <IconTile item={item} active={childActive && collapsed} />
                            <span className="font-medium">{item.label}</span>
                            <ChevronRight className="ml-auto size-4 text-muted-foreground transition-transform duration-200 group-data-[panel-open]/collapsible:rotate-90" />
                          </CollapsibleTrigger>
                          <CollapsibleContent>
                            <SidebarMenuSub className="mx-3 mt-0.5 gap-0.5 border-l-2 border-sidebar-border/80 px-0 pl-2">
                              {item.children.map((child) => {
                                const childIsActive = isActive(child, pathname);
                                return (
                                  <SidebarMenuSubItem key={child.id} className="relative">
                                    {childIsActive && <span className="absolute -left-[10px] top-1/2 h-4 w-0.5 -translate-y-1/2 rounded-full bg-primary" aria-hidden />}
                                    <SidebarMenuSubButton isActive={childIsActive} render={<Link href={child.href ?? "#"} />} className={cn("h-8 transition-colors", childIsActive && "bg-primary/10 font-medium text-foreground hover:bg-primary/15")}>
                                      {child.icon && <child.icon className={cn(childIsActive ? "!text-primary" : "!text-muted-foreground")} />}
                                      <span>{child.label}</span>
                                      {child.badge && <child.badge />}
                                    </SidebarMenuSubButton>
                                  </SidebarMenuSubItem>
                                );
                              })}
                            </SidebarMenuSub>
                          </CollapsibleContent>
                        </SidebarMenuItem>
                      </Collapsible>
                    );
                  }
                  return (
                    <SidebarMenuItem key={item.id} className="relative">
                      {active && <span className="absolute -left-2 top-1/2 h-5 w-1 -translate-y-1/2 rounded-r-full bg-primary group-data-[collapsible=icon]:-left-2" aria-hidden />}
                      <SidebarMenuButton
                        tooltip={item.label}
                        isActive={active}
                        render={<Link href={item.href ?? "#"} />}
                        className={cn("h-9 group/menu-button transition-colors", active && "bg-primary/10 hover:bg-primary/15 data-active:bg-primary/10")}
                      >
                        <IconTile item={item} active={active} />
                        <span className={cn(active && "font-medium")}>{item.label}</span>
                        {item.badge && <item.badge />}
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  );
                })}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        ))}
      </SidebarContent>

      <SidebarFooter className="border-t border-sidebar-border/70">
        <div className="flex items-center gap-2 rounded-md px-1 py-1 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:px-0">
          <Avatar className="size-8 ring-2 ring-primary/20">
            <AvatarFallback className="bg-primary/10 text-xs font-semibold text-primary">{initials || "?"}</AvatarFallback>
          </Avatar>
          <div className="min-w-0 flex-1 group-data-[collapsible=icon]:hidden">
            <div className="truncate text-sm font-medium">{name}</div>
            <div className="truncate text-[11px] text-muted-foreground">{user?.roles?.length ? user.roles.join(", ") : strategy.kind === "none" ? "No authentication" : "Signed in"}</div>
          </div>
          {strategy.interactive && (
            <Tooltip>
              <TooltipTrigger render={<Button variant="ghost" size="icon-xs" aria-label="Sign out" onClick={() => void logout()} className="text-muted-foreground group-data-[collapsible=icon]:hidden" />}>
                <LogOut />
              </TooltipTrigger>
              <TooltipContent side="right">Sign out</TooltipContent>
            </Tooltip>
          )}
        </div>
        <div className="px-2 pb-1 text-[10px] text-muted-foreground group-data-[collapsible=icon]:hidden">Elsa Workflows 3 · Studio</div>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
}
