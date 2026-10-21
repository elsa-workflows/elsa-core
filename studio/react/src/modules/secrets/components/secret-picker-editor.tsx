"use client";

import { Alert, AlertDescription } from "@/components/ui/alert";
import type { UiHintEditorProps } from "@/modules/activity-editors/types";
import { SECRET_EXPRESSION_TYPE, type SecretReference } from "../api";
import { SecretPicker } from "./secret-picker";

/** Options an input descriptor may carry for the `secret-picker` hint. */
interface SecretPickerOptions {
  typeNames?: string[];
  storeNames?: string[];
  scope?: string;
}

function readOptions(options: unknown): SecretPickerOptions {
  if (!options || typeof options !== "object") return {};
  const o = options as Record<string, unknown>;
  const strings = (v: unknown) => (Array.isArray(v) ? v.filter((x): x is string => typeof x === "string") : undefined);
  return { typeNames: strings(o.typeNames), storeNames: strings(o.storeNames), scope: typeof o.scope === "string" ? o.scope : undefined };
}

/** Reads the secret name from a `Secret` expression, accepting both a `SecretReference` and a bare string. */
export function secretNameOf(value: unknown): string | undefined {
  if (typeof value === "string") return value || undefined;
  if (value && typeof value === "object" && typeof (value as SecretReference).name === "string") return (value as SecretReference).name || undefined;
  return undefined;
}

/**
 * UI-hint editor for `secret-picker`. Writes the input as
 * `{ typeName, expression: { type: "Secret", value: { name, typeName, scope } } }`, which is the
 * `SecretReference` shape `SecretExpressionHandler` expects on the server.
 */
export function SecretPickerEditor({ descriptor, value, onChange, readOnly }: UiHintEditorProps) {
  const options = readOptions(descriptor.options);
  const expression = value?.expression;
  const isSecretExpression = !expression || expression.type === SECRET_EXPRESSION_TYPE;
  const selected = isSecretExpression ? secretNameOf(expression?.value) : undefined;

  return (
    <div className="flex flex-col gap-2">
      {!isSecretExpression && (
        <Alert>
          <AlertDescription>
            This input currently uses a {expression.type} expression. Picking a secret replaces it.
          </AlertDescription>
        </Alert>
      )}
      <SecretPicker
        value={selected}
        typeNames={options.typeNames}
        storeNames={options.storeNames}
        scope={options.scope}
        disabled={readOnly || descriptor.isReadOnly}
        onChange={(name, secret) => {
          if (!name) {
            onChange(undefined);
            return;
          }
          const reference: SecretReference = { name, typeName: secret?.typeName ?? null, scope: secret?.scope ?? null };
          onChange({ typeName: descriptor.typeName, expression: { type: SECRET_EXPRESSION_TYPE, value: reference } });
        }}
      />
    </div>
  );
}
