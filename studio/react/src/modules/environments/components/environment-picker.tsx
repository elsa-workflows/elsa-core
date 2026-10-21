"use client";

import { Check, ChevronDown, Server } from "lucide-react";
import { useMemo } from "react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useStudioConfig } from "@/config/provider";
import { useActiveServerStore } from "@/lib/environments/active-server";
import { cn } from "@/lib/utils";
import { environmentUrl, normalizeUrl } from "../api";
import { useServerEnvironments } from "../hooks";

/**
 * Header switch between the backends listed in `config.servers`. Environments the active server
 * advertises through `GET /environments` are listed too; those whose URL matches a configured
 * server switch to it, the others are shown for information only (the active-server store can
 * only point at configured entries).
 */
export function EnvironmentPicker() {
  const { servers } = useStudioConfig();
  const index = useActiveServerStore((s) => s.index);
  const setIndex = useActiveServerStore((s) => s.setIndex);
  const remote = useServerEnvironments();
  const active = servers[index] ?? servers[0];

  const remoteEnvironments = useMemo(() => {
    const byUrl = new Map(servers.map((s, i) => [normalizeUrl(s.url), i] as const));
    return (remote.data?.environments ?? []).map((env) => ({
      ...env,
      configuredIndex: byUrl.get(normalizeUrl(environmentUrl(env))),
      isDefault: !!remote.data?.defaultEnvironmentName && env.name === remote.data.defaultEnvironmentName,
    }));
  }, [servers, remote.data]);

  // Only servers in the configuration can be targeted, so a single entry leaves nothing to switch.
  if (servers.length <= 1) return null;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="outline" size="sm" aria-label="Switch environment" className="max-w-56" />}>
        <Server className="text-muted-foreground" />
        <span className="truncate">{active.name}</span>
        <ChevronDown className="text-muted-foreground" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-72">
        <DropdownMenuGroup>
          <DropdownMenuLabel>Configured servers</DropdownMenuLabel>
          {servers.map((server, i) => (
            <DropdownMenuItem key={`${server.url}-${i}`} onClick={() => setIndex(i)} className="items-start">
              <Check className={cn("mt-0.5", i === index ? "opacity-100" : "opacity-0")} />
              <div className="min-w-0 flex flex-col">
                <span className="truncate">{server.name}</span>
                <span className="truncate text-xs text-muted-foreground">{server.url}</span>
              </div>
            </DropdownMenuItem>
          ))}
        </DropdownMenuGroup>
        {remoteEnvironments.length > 0 && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuLabel>Advertised by {active.name}</DropdownMenuLabel>
              {remoteEnvironments.map((env) => {
                const target = env.configuredIndex;
                return (
                  <DropdownMenuItem
                    key={env.name}
                    disabled={target === undefined}
                    onClick={() => target !== undefined && setIndex(target)}
                    className="items-start"
                    title={target === undefined ? "Not in the studio configuration; add it to servers[] to make it selectable." : undefined}
                  >
                    <Check className={cn("mt-0.5", target !== undefined && target === index ? "opacity-100" : "opacity-0")} />
                    <div className="min-w-0 flex flex-col">
                      <span className="truncate">
                        {env.name}
                        {env.isDefault && <span className="ml-1 text-xs text-muted-foreground">(default)</span>}
                      </span>
                      <span className="truncate text-xs text-muted-foreground">{environmentUrl(env)}</span>
                    </div>
                  </DropdownMenuItem>
                );
              })}
            </DropdownMenuGroup>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
