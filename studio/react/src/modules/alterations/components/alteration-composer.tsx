"use client";

import { Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { AlterationTypes, type Alteration, type AlterationType } from "../api";

/** Draft row: the alteration as typed by the user, before conversion to the API shape. */
export interface AlterationDraft {
  key: string;
  type: AlterationType;
  targetVersion: string;
  variableId: string;
  value: string;
  activityId: string;
  activityInstanceId: string;
}

let seq = 0;
export function newDraft(type: AlterationType = "ScheduleActivity"): AlterationDraft {
  return { key: `a${++seq}`, type, targetVersion: "", variableId: "", value: "", activityId: "", activityInstanceId: "" };
}

function parseValue(text: string): unknown {
  const trimmed = text.trim();
  if (trimmed === "") return null;
  try {
    return JSON.parse(trimmed);
  } catch {
    return text;
  }
}

/** Converts drafts to API alterations; returns the first validation error, if any. */
export function toAlterations(drafts: AlterationDraft[]): { alterations: Alteration[]; error?: string } {
  const alterations: Alteration[] = [];
  for (const [i, d] of drafts.entries()) {
    const n = i + 1;
    switch (d.type) {
      case "Migrate": {
        const v = Number(d.targetVersion);
        if (!Number.isInteger(v) || v <= 0) return { alterations, error: `Alteration ${n}: target version must be a positive integer.` };
        alterations.push({ type: "Migrate", targetVersion: v });
        break;
      }
      case "ModifyVariable":
        if (!d.variableId.trim()) return { alterations, error: `Alteration ${n}: variable id is required.` };
        alterations.push({ type: "ModifyVariable", variableId: d.variableId.trim(), value: parseValue(d.value) });
        break;
      case "ScheduleActivity":
      case "CancelActivity": {
        const activityId = d.activityId.trim() || undefined;
        const activityInstanceId = d.activityInstanceId.trim() || undefined;
        if (!activityId && !activityInstanceId) return { alterations, error: `Alteration ${n}: an activity id or activity instance id is required.` };
        alterations.push({ type: d.type, activityId, activityInstanceId });
        break;
      }
      case "Cancel":
        alterations.push({ type: "Cancel" });
        break;
    }
  }
  if (alterations.length === 0) return { alterations, error: "Add at least one alteration." };
  return { alterations };
}

const typeOptions = AlterationTypes.map((t) => ({ value: t.value, label: t.label }));

export function AlterationComposer({ drafts, onChange }: { drafts: AlterationDraft[]; onChange: (next: AlterationDraft[]) => void }) {
  const [addType, setAddType] = useState<AlterationType>("ScheduleActivity");

  const update = (key: string, patch: Partial<AlterationDraft>) => onChange(drafts.map((d) => (d.key === key ? { ...d, ...patch } : d)));
  const remove = (key: string) => onChange(drafts.filter((d) => d.key !== key));

  return (
    <div className="flex flex-col gap-3">
      {drafts.length === 0 && <p className="text-sm text-muted-foreground">No alterations yet. Add one below.</p>}
      {drafts.map((d, i) => {
        const meta = AlterationTypes.find((t) => t.value === d.type);
        return (
          <div key={d.key} className="rounded-lg border p-3 flex flex-col gap-3">
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted-foreground tabular-nums w-5">{i + 1}.</span>
              <SimpleSelect
                className="w-56"
                size="sm"
                value={d.type}
                options={typeOptions}
                onChange={(v) => v && update(d.key, { type: v })}
              />
              <span className="text-xs text-muted-foreground truncate hidden sm:inline">{meta?.description}</span>
              <Button variant="ghost" size="icon-sm" className="ml-auto" aria-label="Remove alteration" onClick={() => remove(d.key)}>
                <Trash2 />
              </Button>
            </div>
            <DraftFields draft={d} onChange={(patch) => update(d.key, patch)} />
          </div>
        );
      })}
      <div className="flex items-center gap-2">
        <SimpleSelect className="w-56" size="sm" value={addType} options={typeOptions} onChange={(v) => v && setAddType(v)} />
        <Button variant="outline" size="sm" onClick={() => onChange([...drafts, newDraft(addType)])}>
          <Plus data-icon="inline-start" />
          Add alteration
        </Button>
      </div>
    </div>
  );
}

function DraftFields({ draft, onChange }: { draft: AlterationDraft; onChange: (patch: Partial<AlterationDraft>) => void }) {
  switch (draft.type) {
    case "Migrate":
      return (
        <Field label="Target version" required htmlFor={`${draft.key}-v`}>
          <Input id={`${draft.key}-v`} type="number" min={1} value={draft.targetVersion} onChange={(e) => onChange({ targetVersion: e.target.value })} className="w-40" />
        </Field>
      );
    case "ModifyVariable":
      return (
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Variable id" required htmlFor={`${draft.key}-id`} hint="The variable's id as defined in the workflow (not its display name).">
            <Input id={`${draft.key}-id`} value={draft.variableId} onChange={(e) => onChange({ variableId: e.target.value })} />
          </Field>
          <Field label="Value" htmlFor={`${draft.key}-val`} hint="JSON is parsed (numbers, booleans, objects); anything else is sent as a string.">
            <Textarea id={`${draft.key}-val`} rows={2} value={draft.value} onChange={(e) => onChange({ value: e.target.value })} className="font-mono text-xs" />
          </Field>
        </div>
      );
    case "ScheduleActivity":
    case "CancelActivity":
      return (
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Activity id" htmlFor={`${draft.key}-aid`}>
            <Input id={`${draft.key}-aid`} value={draft.activityId} onChange={(e) => onChange({ activityId: e.target.value })} />
          </Field>
          <Field label="Activity instance id" htmlFor={`${draft.key}-aiid`} hint="Either field is enough.">
            <Input id={`${draft.key}-aiid`} value={draft.activityInstanceId} onChange={(e) => onChange({ activityInstanceId: e.target.value })} />
          </Field>
        </div>
      );
    case "Cancel":
      return <p className="text-xs text-muted-foreground">Cancels the workflow instance. No parameters.</p>;
  }
}
