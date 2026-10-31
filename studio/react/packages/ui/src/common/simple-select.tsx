"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@elsa-workflows/react-studio-ui/select";
import { cn } from "../utils.js";

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
  placeholder,
  className,
  size = "default",
  disabled,
  allowEmpty,
  id,
  "aria-label": ariaLabel,
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
  id?: string;
  "aria-label"?: string;
}) {
  const t = useTranslation();
  const EMPTY = "__none__";
  const generic = placeholder === undefined || placeholder === "Select…";
  const displayPlaceholder = generic ? t("Select…") : placeholder;
  // The empty entry reads as the placeholder ("Incidents: any", "Not captured"…) rather than a bare "(none)".
  const all = allowEmpty ? [{ value: EMPTY as T, label: generic ? t("(none)") : displayPlaceholder }, ...options] : options;
  const items = Object.fromEntries(all.map((o) => [o.value, o.label]));
  return (
    <Select
      value={value ?? (allowEmpty ? EMPTY : null)}
      onValueChange={(v) => onChange(v === EMPTY || v === null ? undefined : (v as T))}
      items={items}
      disabled={disabled}
    >
      <SelectTrigger id={id} aria-label={ariaLabel} className={cn("w-full", className)} size={size}>
        <SelectValue placeholder={displayPlaceholder} />
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
