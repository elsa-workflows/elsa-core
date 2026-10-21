import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const FALLBACK_COLOR = "#6b7280";

/** Small round swatch showing a label's colour. */
export function LabelSwatch({ color, className }: { color?: string; className?: string }) {
  return <span className={cn("inline-block size-3 shrink-0 rounded-full border border-black/10", className)} style={{ backgroundColor: color || FALLBACK_COLOR }} aria-hidden />;
}

/** Pill with the label's colour as a swatch so text stays readable in both themes. */
export function LabelBadge({ name, color, className }: { name: string; color?: string; className?: string }) {
  return (
    <Badge variant="outline" className={cn("gap-1.5", className)}>
      <LabelSwatch color={color} className="size-2" />
      {name}
    </Badge>
  );
}
