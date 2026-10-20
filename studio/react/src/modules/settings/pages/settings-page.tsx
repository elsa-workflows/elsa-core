"use client";

import { ArrowRight, Settings, SlidersHorizontal } from "lucide-react";
import Link from "next/link";
import { useMemo } from "react";
import { EmptyState } from "@/components/common/empty-state";
import { PageHeader } from "@/components/common/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { useExtensions } from "@/lib/modules/registry";
import type { SettingsSection } from "@/lib/modules/types";
import { usePermissions } from "@/lib/permissions";

/**
 * Settings sections from every enabled module, filtered by permission and ordered deterministically
 * (order, then label, then id), the same rules as the Blazor `SettingsSectionRegistry`. Duplicate ids
 * are dropped with a console warning rather than throwing, so one misconfigured module cannot take
 * the page down.
 */
export function useSettingsSections(): SettingsSection[] {
  const sections = useExtensions("settingsSections");
  const permissions = usePermissions();
  return useMemo(() => {
    const seen = new Set<string>();
    const unique: SettingsSection[] = [];
    for (const s of sections) {
      const key = s.id.trim().toLowerCase();
      if (!key || !s.label || !s.href) {
        console.warn("[settings] section needs an id, label and href", s);
        continue;
      }
      if (seen.has(key)) {
        console.warn(`[settings] section "${s.id}" is registered more than once; ignoring the duplicate`);
        continue;
      }
      seen.add(key);
      if (s.permissions && !permissions.hasAny(s.permissions)) continue;
      unique.push(s);
    }
    return unique.sort(
      (a, b) => (a.order ?? 0) - (b.order ?? 0) || a.label.localeCompare(b.label, undefined, { sensitivity: "base" }) || a.id.localeCompare(b.id),
    );
  }, [sections, permissions]);
}

export function SettingsPage() {
  const sections = useSettingsSections();
  return (
    <div className="flex flex-col gap-4">
      <PageHeader title="Settings" description="Configure installed Elsa Studio capabilities. Each section owns its data and authorization rules." />
      {sections.length === 0 ? (
        <EmptyState
          icon={SlidersHorizontal}
          title="No settings sections are available to your account"
          description="Modules add sections here through the settingsSections extension. Enable such a module in the studio configuration, or ask an administrator for access."
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {sections.map((section) => {
            const Icon = section.icon ?? Settings;
            return (
              <Card key={section.id} className="h-full">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-accent text-primary">
                      <Icon className="size-4" />
                    </span>
                    {section.label}
                  </CardTitle>
                  {section.description && <CardDescription>{section.description}</CardDescription>}
                </CardHeader>
                <CardContent className="flex-1" />
                <CardFooter>
                  <Button variant="outline" size="sm" nativeButton={false} render={<Link href={section.href} />}>
                    Open {section.label}
                    <ArrowRight data-icon="inline-end" />
                  </Button>
                </CardFooter>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
