import Link from "next/link";
import type { LucideIcon } from "lucide-react";
import { ArrowUpRight } from "lucide-react";
import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import type React from "react";

/** Status tones shared with `StatusBadge` so tiles, badges and chart series read as one system. */
export type StatTone = "neutral" | "info" | "success" | "warning" | "danger";

const accentColor: Record<StatTone, string> = {
  neutral: "var(--status-neutral)",
  info: "var(--status-info)",
  success: "var(--status-success)",
  warning: "var(--status-warning)",
  danger: "var(--status-danger)",
};

export function StatTile({
  label,
  value,
  caption,
  icon: Icon,
  tone = "neutral",
  href,
  isLoading,
}: {
  label: string;
  value: string;
  caption?: string;
  icon?: LucideIcon;
  tone?: StatTone;
  href?: string;
  isLoading?: boolean;
}) {
  const accent = accentColor[tone];
  const body = (
    <>
      <div className="flex items-start justify-between gap-2">
        {Icon && (
          <span className="flex size-8 shrink-0 items-center justify-center rounded-lg" style={{ background: `color-mix(in oklab, ${accent} 14%, var(--card))`, color: accent }}>
            <Icon className="size-4" aria-hidden />
          </span>
        )}
        {href && <ArrowUpRight className="size-3.5 text-muted-foreground opacity-0 transition-opacity group-hover/tile:opacity-100" aria-hidden />}
      </div>
      {isLoading ? (
        <Skeleton className="mt-3 h-7 w-16" />
      ) : (
        <div className="mt-2 text-2xl font-semibold leading-none tracking-tight tabular-nums">{value}</div>
      )}
      <div className="mt-1.5 truncate text-xs font-medium text-foreground/80">{label}</div>
      {caption && <div className="truncate text-[11px] text-muted-foreground">{caption}</div>}
    </>
  );
  const className = "group/tile accent-rail flex h-full flex-col gap-0 rounded-lg px-4 py-3 node-card";
  if (href) {
    return (
      <Link
        href={href}
        aria-label={`${label}: ${value}`}
        className="block rounded-xl outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
      >
        <Card size="sm" className={cn(className, "transition-colors hover:border-primary/50")} style={{ "--rail-color": accent } as React.CSSProperties}>
          {body}
        </Card>
      </Link>
    );
  }
  return (
    <Card size="sm" className={className} style={{ "--rail-color": accent } as React.CSSProperties}>
      {body}
    </Card>
  );
}
