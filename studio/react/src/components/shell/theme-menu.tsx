"use client";

import { Check, Monitor, Moon, Palette, Sun } from "lucide-react";
import { useTheme } from "next-themes";
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
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { ELSA_THEMES } from "@/lib/theme/elsa-theme";
import { useElsaTheme } from "@/lib/theme/elsa-theme-provider";
import { cn } from "@/lib/utils";

const appearances = [
  { id: "light", label: "Light", icon: Sun },
  { id: "dark", label: "Dark", icon: Moon },
  { id: "system", label: "System", icon: Monitor },
] as const;

/** Appearance (light/dark/system) and Elsa theme pack picker. */
export function ThemeMenu() {
  const { theme: appearance, setTheme: setAppearance } = useTheme();
  const { theme, setTheme, defaultTheme } = useElsaTheme();
  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger render={<DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label="Theme" />} />}>
          <Palette />
        </TooltipTrigger>
        <TooltipContent>Theme</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuGroup>
          <DropdownMenuLabel className="eyebrow">Appearance</DropdownMenuLabel>
          <div className="grid grid-cols-3 gap-1 px-1 pb-1">
            {appearances.map((a) => (
              <button
                key={a.id}
                type="button"
                onClick={() => setAppearance(a.id)}
                className={cn(
                  "flex flex-col items-center gap-1 rounded-md border px-2 py-2 text-xs transition-colors hover:bg-accent",
                  appearance === a.id ? "border-primary bg-accent text-foreground" : "border-transparent text-muted-foreground",
                )}
              >
                <a.icon className="size-4" />
                {a.label}
              </button>
            ))}
          </div>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuLabel className="eyebrow">Palette</DropdownMenuLabel>
          {ELSA_THEMES.map((t) => (
            <DropdownMenuItem key={t.id} onClick={() => setTheme(t.id === defaultTheme ? null : t.id)} className="items-start gap-2.5">
              <span className="mt-0.5 size-4 shrink-0 rounded-full ring-2 ring-background" style={{ background: t.swatch }} />
              <span className="flex min-w-0 flex-1 flex-col">
                <span className="text-sm">
                  {t.label}
                  {t.id === defaultTheme && <span className="ml-1 text-[10px] uppercase text-muted-foreground">default</span>}
                </span>
                <span className="truncate text-xs text-muted-foreground">{t.description}</span>
              </span>
              <Check className={cn("mt-0.5 size-4", theme === t.id ? "opacity-100" : "opacity-0")} />
            </DropdownMenuItem>
          ))}
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
