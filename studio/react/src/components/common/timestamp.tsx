"use client";

import { format, formatDistanceToNowStrict, isValid, parseISO } from "date-fns";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

export function formatTimestamp(value: string | Date | undefined | null, pattern = "yyyy-MM-dd HH:mm:ss") {
  if (!value) return "";
  const d = typeof value === "string" ? parseISO(value) : value;
  return isValid(d) ? format(d, pattern) : String(value);
}

/** Absolute timestamp with a relative-time tooltip. Renders a dash for empty values. */
export function Timestamp({ value, relative = false }: { value: string | Date | undefined | null; relative?: boolean }) {
  if (!value) return <span className="text-muted-foreground">–</span>;
  const d = typeof value === "string" ? parseISO(value) : value;
  if (!isValid(d)) return <span>{String(value)}</span>;
  const absolute = format(d, "yyyy-MM-dd HH:mm:ss");
  const rel = formatDistanceToNowStrict(d, { addSuffix: true });
  return (
    <Tooltip>
      <TooltipTrigger render={<span className="whitespace-nowrap tabular-nums" />}>{relative ? rel : absolute}</TooltipTrigger>
      <TooltipContent>{relative ? absolute : rel}</TooltipContent>
    </Tooltip>
  );
}
