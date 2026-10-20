"use client";

import { Loader2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Checkbox } from "@/components/ui/checkbox";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import type { UserTaskDetail, UserTaskFormField } from "../api";

function initialValues(fields: UserTaskFormField[]): Record<string, string> {
  const out: Record<string, string> = {};
  for (const f of fields) out[f.key] = f.value == null ? "" : typeof f.value === "string" ? f.value : JSON.stringify(f.value);
  return out;
}

function coerce(field: UserTaskFormField, raw: string): unknown {
  const type = field.type.toLowerCase();
  if (type === "number" || type === "integer") return raw === "" ? null : Number(raw);
  if (type === "boolean" || type === "checkbox") return raw === "true";
  if (type === "json" || type === "object") {
    try {
      return raw === "" ? null : JSON.parse(raw);
    } catch {
      return raw;
    }
  }
  return raw;
}

/**
 * Completes a task with one of its configured actions. When the task has a pinned form the
 * fields are rendered from its projection; otherwise an optional free-form JSON payload is offered.
 */
export function CompleteTaskDialog({
  task,
  open,
  onOpenChange,
  onSubmit,
  isPending,
}: {
  task: UserTaskDetail;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (actionKey: string, data: unknown) => Promise<unknown>;
  isPending: boolean;
}) {
  // `allowedActions` lists operations (assign, complete, …), not completion action keys, so it must not
  // filter the actions; the server validates the chosen key on submit.
  const actions = task.form?.actions?.length ? task.form.actions : task.actions;
  const fields = task.form?.fields ?? [];
  const [actionKey, setActionKey] = useState(actions[0]?.key ?? "Complete");
  const [values, setValues] = useState(() => initialValues(fields));
  const [json, setJson] = useState("");
  const [error, setError] = useState<string>();

  const submit = async () => {
    let data: unknown = undefined;
    if (fields.length) {
      const missing = fields.filter((f) => f.required && !f.masked && !values[f.key]?.trim());
      if (missing.length) {
        setError(`Required: ${missing.map((f) => f.label).join(", ")}`);
        return;
      }
      const obj: Record<string, unknown> = {};
      for (const f of fields) if (!f.masked || values[f.key]) obj[f.key] = coerce(f, values[f.key] ?? "");
      data = obj;
    } else if (json.trim()) {
      try {
        data = JSON.parse(json);
      } catch {
        setError("The payload must be valid JSON.");
        return;
      }
    }
    setError(undefined);
    await onSubmit(actionKey, data);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Complete task</DialogTitle>
          <DialogDescription>{task.title}</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-3">
          <Field label="Action" required>
            <SimpleSelect value={actionKey} onChange={(v) => v && setActionKey(v)} options={actions.map((a) => ({ value: a.key, label: a.label }))} />
          </Field>
          {fields.length > 0 ? (
            fields.map((f) => (
              <Field key={f.key} label={f.label} required={f.required} htmlFor={`f-${f.key}`} hint={f.masked ? "Protected field; leave empty to keep the current value." : undefined}>
                <FormFieldInput field={f} id={`f-${f.key}`} value={values[f.key] ?? ""} onChange={(v) => setValues((s) => ({ ...s, [f.key]: v }))} />
              </Field>
            ))
          ) : (
            <Field label="Response payload (JSON)" htmlFor="complete-json" hint="Optional. Sent as the task's completion data.">
              <Textarea id="complete-json" rows={5} value={json} onChange={(e) => setJson(e.target.value)} className="font-mono text-xs" placeholder="{ }" />
            </Field>
          )}
          {error && <p className="text-xs text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Cancel
          </Button>
          <Button onClick={submit} disabled={isPending || actions.length === 0}>
            {isPending && <Loader2 className="animate-spin" data-icon="inline-start" />}
            Complete
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function FormFieldInput({ field, id, value, onChange }: { field: UserTaskFormField; id: string; value: string; onChange: (v: string) => void }) {
  const type = field.type.toLowerCase();
  if (type === "boolean" || type === "checkbox")
    return <Checkbox id={id} checked={value === "true"} onCheckedChange={(c) => onChange(c ? "true" : "false")} />;
  if (type === "textarea" || type === "multiline" || type === "json" || type === "object")
    return <Textarea id={id} rows={3} value={value} onChange={(e) => onChange(e.target.value)} className={type === "json" ? "font-mono text-xs" : undefined} />;
  const inputType = type === "number" || type === "integer" ? "number" : type === "date" ? "date" : type === "datetime" ? "datetime-local" : type === "email" ? "email" : field.masked ? "password" : "text";
  return <Input id={id} type={inputType} value={value} onChange={(e) => onChange(e.target.value)} />;
}
