"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useWorkflowInstances } from "../hooks";

/** Live count of running instances for the navigation; refreshes with the list polling interval. */
export function RunningCountBadge() {
  const _t = useTranslation();
  const list = useWorkflowInstances({ status: "Running", pageSize: 1 }, { poll: true });
  const count = list.data?.totalCount ?? 0;
  if (!count) return null;
  return (
    <span
      className="ml-auto inline-flex h-5 min-w-5 items-center justify-center rounded-full bg-status-info/15 px-1.5 text-[11px] font-semibold tabular-nums text-status-info"
      title={count === 1 ? _t("{{count}} running instance", { count }) : _t("{{count}} running instances", { count })}
    >
      {count}
    </span>
  );
}
