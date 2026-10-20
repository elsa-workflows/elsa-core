"use client";

import { ArrowLeft, Check, Monitor, Moon, RotateCcw, Sun } from "lucide-react";
import Link from "next/link";
import { useTheme } from "next-themes";
import { useSyncExternalStore } from "react";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useStudioConfig } from "@/config/provider";
import { SettingsRoutes } from "@/lib/modules/settings";
import { ELSA_THEMES } from "@/lib/theme/elsa-theme";
import { useElsaTheme } from "@/lib/theme/elsa-theme-provider";
import { cn } from "@/lib/utils";

const appearances = [
  { id: "light", label: "Light", description: "Always use the light appearance.", icon: Sun },
  { id: "dark", label: "Dark", description: "Always use the dark appearance.", icon: Moon },
  { id: "system", label: "System", description: "Follow the operating system setting.", icon: Monitor },
] as const;

/** Edits the same per-browser state as the header palette menu, with room to explain each option. */
export function AppearancePage() {
  const config = useStudioConfig();
  const { theme: appearance, setTheme: setAppearance } = useTheme();
  const { theme, setTheme, defaultTheme, isOverridden } = useElsaTheme();
  // next-themes only knows the choice on the client; avoid a hydration mismatch on the first paint.
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false,
  );
  const currentAppearance = mounted ? (appearance ?? config.ui.defaultTheme) : config.ui.defaultTheme;
  const isDefault = !isOverridden && currentAppearance === config.ui.defaultTheme;

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        eyebrow="Settings"
        title="Appearance"
        description="Choices are remembered in this browser only. The defaults come from the studio configuration."
        actions={
          <>
            <Button
              variant="outline"
              size="sm"
              disabled={isDefault}
              onClick={() => {
                setAppearance(config.ui.defaultTheme);
                setTheme(null);
              }}
            >
              <RotateCcw data-icon="inline-start" />
              Reset to defaults
            </Button>
            <Button variant="ghost" size="sm" nativeButton={false} render={<Link href={SettingsRoutes.overview} />}>
              <ArrowLeft data-icon="inline-start" />
              All settings
            </Button>
          </>
        }
      />

      <Card>
        <CardHeader>
          <CardTitle>Mode</CardTitle>
          <CardDescription>Light, dark or whatever the operating system prefers.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-3" role="radiogroup" aria-label="Appearance mode">
            {appearances.map((a) => {
              const selected = currentAppearance === a.id;
              return (
                <button
                  key={a.id}
                  type="button"
                  role="radio"
                  aria-checked={selected}
                  onClick={() => setAppearance(a.id)}
                  className={cn(
                    "flex items-start gap-3 rounded-lg border p-3 text-left transition-colors hover:bg-accent",
                    selected ? "border-primary bg-accent" : "border-border",
                  )}
                >
                  <span className={cn("flex size-8 shrink-0 items-center justify-center rounded-md", selected ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground")}>
                    <a.icon className="size-4" />
                  </span>
                  <span className="flex min-w-0 flex-col">
                    <span className="text-sm font-medium">
                      {a.label}
                      {a.id === config.ui.defaultTheme && <span className="ml-1 text-[10px] uppercase text-muted-foreground">default</span>}
                    </span>
                    <span className="text-xs text-muted-foreground">{a.description}</span>
                  </span>
                </button>
              );
            })}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Palette</CardTitle>
          <CardDescription>The Elsa theme pack: accent colours for the shell, the canvas and workflow statuses.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3" role="radiogroup" aria-label="Palette">
            {ELSA_THEMES.map((t) => {
              const selected = theme === t.id;
              return (
                <button
                  key={t.id}
                  type="button"
                  role="radio"
                  aria-checked={selected}
                  onClick={() => setTheme(t.id === defaultTheme ? null : t.id)}
                  className={cn(
                    "flex items-start gap-3 rounded-lg border p-3 text-left transition-colors hover:bg-accent",
                    selected ? "border-primary bg-accent" : "border-border",
                  )}
                >
                  <span className="mt-0.5 size-6 shrink-0 rounded-full ring-2 ring-background" style={{ background: t.swatch }} />
                  <span className="flex min-w-0 flex-1 flex-col">
                    <span className="text-sm font-medium">
                      {t.label}
                      {t.id === defaultTheme && <span className="ml-1 text-[10px] uppercase text-muted-foreground">default</span>}
                    </span>
                    <span className="text-xs text-muted-foreground">{t.description}</span>
                  </span>
                  <Check className={cn("mt-0.5 size-4 shrink-0 text-primary", selected ? "opacity-100" : "opacity-0")} />
                </button>
              );
            })}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
