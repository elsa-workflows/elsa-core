import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  className,
}: {
  icon?: LucideIcon;
  title: ReactNode;
  description?: ReactNode;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed bg-card/60 p-10 text-center", className)}>
      {Icon && (
        <span className="flex size-12 items-center justify-center rounded-xl bg-accent text-primary">
          <Icon className="size-6" />
        </span>
      )}
      <div className="font-medium">{title}</div>
      {description && <p className="text-sm text-muted-foreground max-w-md">{description}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}
