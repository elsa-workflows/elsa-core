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
      <CardContent className={cn("flex-1", contentClassName)}>
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
