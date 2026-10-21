"use client";

import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type { DashboardRange } from "@/lib/modules/types";

export const DASHBOARD_RANGES: DashboardRange[] = ["1h", "24h", "7d"];

export function RangeToggle({ value, onChange }: { value: DashboardRange; onChange: (range: DashboardRange) => void }) {
  return (
    <Tabs value={value} onValueChange={(v) => onChange(v as DashboardRange)}>
      <TabsList aria-label="Time range">
        {DASHBOARD_RANGES.map((r) => (
          <TabsTrigger key={r} value={r} className="px-2.5">
            {r}
          </TabsTrigger>
        ))}
      </TabsList>
    </Tabs>
  );
}
