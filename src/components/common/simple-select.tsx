"use client";

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { cn } from "@/lib/utils";

export interface SelectOption<T extends string = string> {
  value: T;
  label: string;
  description?: string;
}

/**
 * Single-select over a list of `{ value, label }` options. Wraps the Base UI select so callers never
 * have to deal with its `items` mapping or the empty-value quirks.
 */
export function SimpleSelect<T extends string>({
  value,
  onChange,
  options,
  placeholder = "Select…",
  className,
  size = "default",
  disabled,
  allowEmpty,
}: {
  value: T | undefined | null;
  onChange: (value: T | undefined) => void;
  options: SelectOption<T>[];
  placeholder?: string;
  className?: string;
  size?: "sm" | "default";
  disabled?: boolean;
  /** Adds a "(none)" entry that clears the value. */
  allowEmpty?: boolean;
}) {
  const EMPTY = "__none__";
  // The empty entry reads as the placeholder ("Incidents: any", "Not captured"…) rather than a bare "(none)".
  const all = allowEmpty ? [{ value: EMPTY as T, label: placeholder === "Select…" ? "(none)" : placeholder }, ...options] : options;
  const items = Object.fromEntries(all.map((o) => [o.value, o.label]));
  return (
    <Select
      value={value ?? (allowEmpty ? EMPTY : null)}
      onValueChange={(v) => onChange(v === EMPTY || v === null ? undefined : (v as T))}
      items={items}
      disabled={disabled}
    >
      <SelectTrigger className={cn("w-full", className)} size={size}>
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {all.map((o) => (
          <SelectItem key={o.value} value={o.value}>
            {o.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
