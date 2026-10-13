"use client";

import { Field } from "@/components/common/field";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";

const MULTILINE_HINTS = ["multiline", "multi-line", "pem", "certificate", "key", "json", "textarea"];

/** Write-only secret value input; the control is chosen from the type's `editorHint`. */
export function SecretValueField({
  value,
  onChange,
  editorHint,
  label = "Value",
  hint,
  required,
  disabled,
}: {
  value: string;
  onChange: (value: string) => void;
  editorHint?: string;
  label?: string;
  hint?: string;
  required?: boolean;
  disabled?: boolean;
}) {
  const multiline = !!editorHint && MULTILINE_HINTS.some((h) => editorHint.toLowerCase().includes(h));
  const id = "secret-value";
  return (
    <Field label={label} htmlFor={id} required={required} hint={hint ?? "Submitted once; it is never shown again after saving."}>
      {multiline ? (
        <Textarea id={id} value={value} onChange={(e) => onChange(e.target.value)} rows={6} className="font-mono text-xs" spellCheck={false} disabled={disabled} />
      ) : (
        <Input id={id} type="password" value={value} onChange={(e) => onChange(e.target.value)} autoComplete="new-password" disabled={disabled} />
      )}
    </Field>
  );
}

/** `datetime-local` input value (local time, no seconds) -> ISO string, or null when blank. */
export function toIsoOrNull(local: string): string | null {
  if (!local) return null;
  const d = new Date(local);
  return Number.isNaN(d.getTime()) ? null : d.toISOString();
}
