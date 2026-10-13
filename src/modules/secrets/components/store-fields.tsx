"use client";

import { useCallback, useMemo, useState } from "react";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { Input } from "@/components/ui/input";
import { storeFields, type SecretStoreDescriptor, type SecretStoreFieldDescriptor } from "../api";

export interface StoreFieldsState {
  fields: SecretStoreFieldDescriptor[];
  values: Record<string, string>;
  setValue: (name: string, value: string) => void;
  /** Whether every required field has been filled in. */
  complete: boolean;
  /** What to submit as the request's `metadata`; undefined when nothing was entered. */
  metadata: Record<string, string> | undefined;
  /**
   * Back-compat: servers predating store-declared fields read the configuration key from its own request property rather
   * than from metadata, so it is sent in both places.
   */
  configurationKey: string | undefined;
  reset: () => void;
}

const defaultsFor = (fields: SecretStoreFieldDescriptor[]) =>
  Object.fromEntries(fields.filter((f) => f.defaultValue).map((f) => [f.name, f.defaultValue as string]));

/**
 * Holds the values for whichever fields a store declares. Replaces the hardcoded configuration-key input the secret forms
 * used to carry, so any store — including ones this studio has never heard of — gets a usable form.
 */
export function useStoreFields(store: SecretStoreDescriptor | undefined): StoreFieldsState {
  const fields = useMemo(() => storeFields(store), [store]);
  const [values, setValues] = useState<Record<string, string>>(() => defaultsFor(fields));

  // Switching stores swaps the whole set of fields, so stale values must not carry over. Adjusting during render rather
  // than in an effect keeps the form from flashing the previous store's values. https://react.dev/learn/you-might-not-need-an-effect
  const storeKey = store?.name ?? "";
  const [previousStoreKey, setPreviousStoreKey] = useState(storeKey);
  if (previousStoreKey !== storeKey) {
    setPreviousStoreKey(storeKey);
    setValues(defaultsFor(fields));
  }

  const setValue = useCallback((name: string, value: string) => setValues((current) => ({ ...current, [name]: value })), []);
  const reset = useCallback(() => setValues(defaultsFor(fields)), [fields]);

  const entries = fields.map((f) => [f.name, values[f.name]?.trim() ?? ""] as const).filter(([, value]) => value.length > 0);

  return {
    fields,
    values,
    setValue,
    complete: fields.every((f) => !f.required || (values[f.name]?.trim().length ?? 0) > 0),
    metadata: entries.length > 0 ? Object.fromEntries(entries) : undefined,
    configurationKey: entries.find(([name]) => name === "configurationKey")?.[1],
    reset,
  };
}

/** What a store does with a submitted value, in that store's own terms. */
export function valueHintFor(store: SecretStoreDescriptor | undefined): string | undefined {
  if (!store) return undefined;
  if (store.name === "encrypted") return "Stored encrypted by Elsa; it is never shown again after saving.";
  return `Written to ${store.displayName}; Elsa keeps only the reference and never shows the value again.`;
}

export function StoreFields({ state, idPrefix }: { state: StoreFieldsState; idPrefix: string }) {
  return (
    <>
      {state.fields.map((field) => {
        const id = `${idPrefix}-${field.name}`;
        return (
          <Field key={field.name} label={field.displayName} required={field.required} htmlFor={id} hint={field.description}>
            {field.kind === "Select" ? (
              <SimpleSelect
                value={state.values[field.name]}
                onChange={(value) => state.setValue(field.name, value ?? "")}
                options={field.options.map((o) => ({ value: o, label: o }))}
                placeholder={field.placeholder ?? undefined}
              />
            ) : (
              <Input
                id={id}
                type={field.kind === "Url" ? "url" : "text"}
                value={state.values[field.name] ?? ""}
                onChange={(e) => state.setValue(field.name, e.target.value)}
                placeholder={field.placeholder ?? undefined}
                autoComplete="off"
                required={field.required}
              />
            )}
          </Field>
        );
      })}
    </>
  );
}
