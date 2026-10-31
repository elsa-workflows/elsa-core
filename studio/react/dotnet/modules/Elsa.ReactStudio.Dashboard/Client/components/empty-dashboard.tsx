
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { ArrowUpRight, FileCode2, LayoutDashboard, ListChecks, type LucideIcon } from "lucide-react";
import { Card } from "@elsa-workflows/react-studio-ui/card";
import { EmptyState } from "@elsa-workflows/react-studio-ui/common/empty-state";
import { Resources, usePermissions, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { DashboardLinks } from "../links";

/** Welcome state when no dashboard widget is visible (none registered, or the user lacks every permission). */
export function EmptyDashboard() {
  const _t = useTranslation();
  const permissions = usePermissions();
  const shortcuts: { href: string; label: string; description: string; icon: LucideIcon; visible: boolean }[] = [
    {
      href: DashboardLinks.definitions,
      label: _t("Workflow definitions"),
      description: _t("Design, publish and run workflows."),
      icon: FileCode2,
      visible: permissions.has(Resources.workflowDefinitions, Verbs.view),
    },
    {
      href: DashboardLinks.instances,
      label: _t("Workflow instances"),
      description: _t("Inspect running and finished workflows."),
      icon: ListChecks,
      visible: permissions.has(Resources.workflowInstances, Verbs.view),
    },
  ].filter((s) => s.visible);

  return (
    <div className="flex flex-col gap-4">
      <EmptyState
        icon={LayoutDashboard}
        title={_t("Welcome to Elsa Studio")}
        description={_t("There is nothing to show on the dashboard yet. Modules can contribute widgets here; in the meantime jump straight to your workflows.")}
      />
      {shortcuts.length > 0 && (
        <div className="grid gap-3 sm:grid-cols-2">
          {shortcuts.map((s) => (
            <Link key={s.href} href={s.href} className="block rounded-xl outline-none focus-visible:ring-3 focus-visible:ring-ring/50">
              <Card size="sm" className="group/shortcut h-full flex-row items-center gap-3 px-4 transition-colors hover:bg-muted/50">
                <s.icon className="size-5 shrink-0 text-muted-foreground" aria-hidden />
                <div className="min-w-0 flex-1">
                  <div className="font-medium">{s.label}</div>
                  <div className="text-xs text-muted-foreground">{s.description}</div>
                </div>
                <ArrowUpRight className="size-4 text-muted-foreground opacity-0 transition-opacity group-hover/shortcut:opacity-100" aria-hidden />
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
