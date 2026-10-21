"use client";

import { Component, type ErrorInfo, type ReactNode } from "react";
import { ErrorAlert } from "@/components/common/error-alert";
import type { DashboardRange, DashboardWidget, DashboardZone } from "@/lib/modules/types";
import { cn } from "@/lib/utils";

/**
 * Row heights for the zones that hold list widgets. Fixing them is what stops a card from growing
 * with its row count: the list scrolls inside the card, and the columns beside it stay aligned.
 * Below `lg` the rows stack and size to content, where a scroll-inside-a-scroll would be worse.
 */
const PRIMARY_ROW_HEIGHT = "lg:h-[21rem]";
const ACTIVITY_ROW_HEIGHT = "lg:h-[34rem]";

/**
 * Lays widgets out by zone:
 *
 *   metrics   full-width auto-fit grid of tiles (widgets render tiles directly, no wrapper)
 *   trend     full width
 *   primary   full width
 *   activity  2/3   |  findings + secondary  1/3
 *   status    full width
 *
 * Zones without visible widgets collapse. Each widget runs inside its own error boundary so a
 * widget contributed by another module can never take the page down.
 */
export function DashboardZones({ widgets, range }: { widgets: DashboardWidget[]; range: DashboardRange }) {
  const zone = (z: DashboardZone) => widgets.filter((w) => w.zone === z);
  const metrics = zone("metrics");
  const trend = zone("trend");
  const primary = zone("primary");
  const activity = zone("activity");
  const findings = zone("findings");
  const secondary = zone("secondary");
  const status = zone("status");
  const side = [...findings, ...secondary];

  return (
    <div className="flex flex-col gap-4">
      {metrics.length > 0 && (
        <section aria-label="Metrics" className="grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] gap-3">
          {metrics.map((w) => (
            <WidgetBoundary key={w.id} id={w.id}>
              <w.component range={range} />
            </WidgetBoundary>
          ))}
        </section>
      )}
      <Stack widgets={trend} range={range} label="Trends" />
      {primary.length > 0 && (
        <section aria-label="Primary" className={cn("grid gap-4 lg:grid-cols-2", PRIMARY_ROW_HEIGHT)}>
          {primary.map((w) => (
            <WidgetBoundary key={w.id} id={w.id}>
              <w.component range={range} />
            </WidgetBoundary>
          ))}
        </section>
      )}
      {(activity.length > 0 || side.length > 0) && (
        <div className={cn("grid gap-4 lg:grid-cols-3", ACTIVITY_ROW_HEIGHT)}>
          {activity.length > 0 && (
            <div className={cn("min-h-0", side.length > 0 ? "lg:col-span-2" : "lg:col-span-3")}>
              <Stack widgets={activity} range={range} label="Activity" fill />
            </div>
          )}
          {side.length > 0 && (
            <div className={cn("min-h-0", activity.length > 0 ? "lg:col-span-1" : "lg:col-span-3")}>
              <Stack widgets={side} range={range} label="Findings" fill />
            </div>
          )}
        </div>
      )}
      <Stack widgets={status} range={range} label="Status" />
    </div>
  );
}

/** `fill` stretches the stack to its row, letting the last widget absorb whatever height is left. */
function Stack({ widgets, range, label, fill }: { widgets: DashboardWidget[]; range: DashboardRange; label: string; fill?: boolean }) {
  if (widgets.length === 0) return null;
  return (
    <section aria-label={label} className={cn("flex flex-col gap-4", fill && "lg:h-full")}>
      {widgets.map((w, i) => (
        <div key={w.id} className={cn("min-h-0", fill && i === widgets.length - 1 && "lg:flex-1")}>
          <WidgetBoundary id={w.id}>
            <w.component range={range} />
          </WidgetBoundary>
        </div>
      ))}
    </section>
  );
}

class WidgetBoundary extends Component<{ id: string; children: ReactNode }, { error?: Error }> {
  state: { error?: Error } = {};

  static getDerivedStateFromError(error: Error) {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(`Dashboard widget "${this.props.id}" crashed`, error, info.componentStack);
  }

  render() {
    if (this.state.error) return <ErrorAlert error={this.state.error} title={`Widget "${this.props.id}" failed to render`} />;
    return this.props.children;
  }
}
