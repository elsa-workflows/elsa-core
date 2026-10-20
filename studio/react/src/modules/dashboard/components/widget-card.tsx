import type { ReactNode } from "react";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { ErrorAlert } from "@/components/common/error-alert";
import { cn } from "@/lib/utils";
import type { DashboardCapability } from "../hooks";

/**
 * Shell shared by every built-in widget: title, optional action slot, and uniform loading / error
 * states so widgets only render their happy path.
 */
export function WidgetCard({
  title,
  description,
  action,
  isLoading,
  error,
  skeleton,
  className,
  contentClassName,
  scroll,
  children,
}: {
  title: ReactNode;
  description?: ReactNode;
  action?: ReactNode;
  isLoading?: boolean;
  error?: unknown;
  /** Rendered while loading; defaults to three text lines. */
  skeleton?: ReactNode;
  className?: string;
  contentClassName?: string;
  /**
   * Scrolls the content instead of letting it set the card's height. Zones give list widgets a fixed
   * height so a dashboard keeps the same shape whether a server has three workflows or three hundred.
   */
  scroll?: boolean;
  children?: ReactNode;
}) {
  return (
    <Card className={cn("h-full node-card", className)}>
      <CardHeader className="border-b border-dashed pb-3">
        <CardTitle className="flex items-center gap-2 text-sm font-semibold">
          <span className="port-dot bg-primary ring-0" aria-hidden />
          {title}
        </CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
        {action && <CardAction>{action}</CardAction>}
      </CardHeader>
      <CardContent className={cn("min-h-0 flex-1", scroll && "overflow-y-auto overscroll-contain", contentClassName)}>
        {error ? <ErrorAlert error={error} title="Could not load" /> : isLoading ? (skeleton ?? <LinesSkeleton />) : children}
      </CardContent>
    </Card>
  );
}

export function LinesSkeleton({ lines = 3 }: { lines?: number }) {
  return (
    <div className="flex flex-col gap-2.5" aria-busy="true" aria-label="Loading">
      {Array.from({ length: lines }, (_, i) => (
        <Skeleton key={i} className="h-4" style={{ width: `${88 - i * 14}%` }} />
      ))}
    </div>
  );
}

/** Short, muted note explaining what the fallback source cannot provide. */
export function SourceNote({ capability, children }: { capability: DashboardCapability | undefined; children: ReactNode }) {
  if (capability !== "fallback") return null;
  return <p className="text-xs text-muted-foreground">{children}</p>;
}

export function WidgetEmpty({ children }: { children: ReactNode }) {
  return <p className="py-6 text-center text-sm text-muted-foreground">{children}</p>;
}
