"use client";

import { useMemo } from "react";
import { describeError } from "@/components/common/error-alert";
import { SimpleSelect } from "@/components/common/simple-select";
import type { SecretModel, SecretPickerRequest } from "../api";
import { usePickerSecrets } from "../hooks";

export interface SecretPickerProps {
  /** Technical name of the selected secret. */
  value: string | undefined;
  onChange: (name: string | undefined, secret: SecretModel | undefined) => void;
  /** Restrict to these secret types (e.g. `["text"]`). */
  typeNames?: string[];
  storeNames?: string[];
  scope?: string;
  /** Include retired/revoked secrets too; default lists active ones only. */
  includeInactive?: boolean;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  size?: "sm" | "default";
}

/** Select over the secrets the server deems compatible (`POST /secrets/picker`). Never shows values. */
export function SecretPicker({ value, onChange, typeNames, storeNames, scope, includeInactive, placeholder, disabled, className, size }: SecretPickerProps) {
  const request = useMemo<SecretPickerRequest>(
    () => ({ typeNames: typeNames ?? [], storeNames: storeNames ?? [], scope: scope || undefined, activeOnly: !includeInactive }),
    [typeNames, storeNames, scope, includeInactive],
  );
  const query = usePickerSecrets(request);
  const items = useMemo(() => query.data?.items ?? [], [query.data]);

  // Keep a value that is not (or no longer) in the list selectable so the editor never silently drops it.
  const options = useMemo(() => {
    const list = items.map((s) => ({ value: s.name, label: s.displayName && s.displayName !== s.name ? `${s.displayName} (${s.name})` : s.name, description: s.description }));
    if (value && !items.some((s) => s.name === value)) list.unshift({ value, label: `${value} (not found)`, description: undefined });
    return list;
  }, [items, value]);

  return (
    <div className={className}>
      <SimpleSelect
        value={value}
        onChange={(name) => onChange(name, items.find((s) => s.name === name))}
        options={options}
        placeholder={query.isLoading ? "Loading secrets…" : query.isError ? "Secrets unavailable" : (placeholder ?? "Select a secret…")}
        disabled={disabled || query.isLoading}
        allowEmpty
        size={size}
      />
      {query.isError && <p className="mt-1 text-xs text-destructive">{describeError(query.error)}</p>}
    </div>
  );
}
