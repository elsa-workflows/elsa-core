"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Tabs, TabsList, TabsTrigger } from "@elsa-workflows/react-studio-ui/tabs";
import type { DashboardRange } from "@elsa-workflows/react-studio-sdk";

export const DASHBOARD_RANGES: DashboardRange[] = ["1h", "24h", "7d"];

export function RangeToggle({ value, onChange }: { value: DashboardRange; onChange: (range: DashboardRange) => void }) {
  const _t = useTranslation();
  return (
    <Tabs value={value} onValueChange={(v) => onChange(v as DashboardRange)}>
      <TabsList aria-label={_t("Time range")}>
        {DASHBOARD_RANGES.map((r) => (
          <TabsTrigger key={r} value={r} className="px-2.5">
            {r}
          </TabsTrigger>
        ))}
      </TabsList>
    </Tabs>
  );
}
