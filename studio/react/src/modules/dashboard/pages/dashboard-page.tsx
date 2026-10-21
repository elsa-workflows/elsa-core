"use client";

import { RefreshCw } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "@/components/common/page-header";
import { Timestamp } from "@/components/common/timestamp";
import { Button } from "@/components/ui/button";
import { useActiveServer } from "@/lib/environments/active-server";
import { useExtensions } from "@/lib/modules/registry";
import type { DashboardRange } from "@/lib/modules/types";
import { usePermissions } from "@/lib/permissions";
import { cn } from "@/lib/utils";
import { DashboardZones } from "../components/dashboard-zones";
import { EmptyDashboard } from "../components/empty-dashboard";
import { RangeToggle } from "../components/range-toggle";
import { RuntimeStatusChip } from "../components/runtime-status-chip";
import { useDashboardOverview, useFallbackInstanceCounts, useRefreshDashboard } from "../hooks";

const DEFAULT_RANGE: DashboardRange = "24h";

export function DashboardPage() {
  const [range, setRange] = useState<DashboardRange>(DEFAULT_RANGE);
  const server = useActiveServer();
  const permissions = usePermissions();
  const widgets = useExtensions("dashboardWidgets");
  const visibleWidgets = useMemo(() => widgets.filter((w) => !w.permissions || permissions.hasAny(w.permissions)), [widgets, permissions]);

  const overview = useDashboardOverview(range);
  const fallback = useFallbackInstanceCounts();
  const { refresh, isFetching } = useRefreshDashboard();

  const capability = overview.capability;
  const backendName = overview.data?.backendName || server.name;
  const environmentName = overview.data?.environmentName;
  const updatedAt = Math.max(overview.dataUpdatedAt, fallback.dataUpdatedAt);

  return (
    <div className="flex flex-col gap-2">
      <PageHeader
        title="Dashboard"
        description={
          <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <span className="font-medium text-foreground">{backendName}</span>
            {environmentName && <span>· {environmentName}</span>}
            <RuntimeStatusChip runtime={overview.data?.runtime} capability={capability} />
            {updatedAt > 0 && (
              <span className="text-xs">
                Updated <Timestamp value={new Date(updatedAt)} relative />
              </span>
            )}
          </span>
        }
        actions={
          <>
            <RangeToggle value={range} onChange={setRange} />
            <Button variant="outline" size="icon" aria-label="Refresh" onClick={() => void refresh()} disabled={isFetching}>
              <RefreshCw className={cn(isFetching && "animate-spin")} />
            </Button>
          </>
        }
      />
      <RelativeTimeTicker />
      {visibleWidgets.length === 0 ? <EmptyDashboard /> : <DashboardZones widgets={visibleWidgets} range={range} />}
    </div>
  );
}

/** Re-renders the page every 15 s so the "Updated … ago" label keeps up between refetches. */
function RelativeTimeTicker() {
  const [, setTick] = useState(0);
  useEffect(() => {
    const id = window.setInterval(() => setTick((t) => t + 1), 15_000);
    return () => window.clearInterval(id);
  }, []);
  return null;
}
