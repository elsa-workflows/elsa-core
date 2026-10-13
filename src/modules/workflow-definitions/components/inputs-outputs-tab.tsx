"use client";

import { useState } from "react";
import { Pencil, Plus, Trash2, X } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { useStorageDrivers, useVariableTypes } from "@/lib/api/hooks/descriptors";
import type { InputDefinition, OutputDefinition } from "@/lib/api/types/workflow-definitions";
import { useWorkflowEditor } from "../editor-context";
import { useDesigner } from "@/modules/designer";
import { DataUsageChips } from "./data-usage";

const UI_HINTS = ["singleline", "multiline", "checkbox", "dropdown", "json-editor", "code-editor", "multitext", "datetime-picker"];

export function InputsOutputsTab() {
  const { definition, updateDefinition, readOnly } = useWorkflowEditor();
  const { dataFlow } = useDesigner();
  const [input, setInput] = useState<{ item: InputDefinition; index: number } | null>(null);
  const [output, setOutput] = useState<{ item: OutputDefinition; index: number } | null>(null);
  const [outcome, setOutcome] = useState("");
  const inputs = definition.inputs ?? [];
  const outputs = definition.outputs ?? [];
  const outcomes = definition.outcomes ?? [];

  const replaceAt = <T,>(list: T[], index: number, item: T) => (index < 0 ? [...list, item] : list.map((x, i) => (i === index ? item : x)));

  return (
    <div className="flex flex-col gap-6 p-3">
      <Section
        title="Inputs"
        description="Values a caller provides when starting the workflow."
        onAdd={readOnly ? undefined : () => setInput({ item: { name: "", type: "Object", isArray: false }, index: -1 })}
      >
        <DefinitionTable
          rows={inputs}
          columns={[["Name", (i) => i.name], ["Type", (i) => `${i.type}${i.isArray ? "[]" : ""}`], ["UI hint", (i) => i.uiHint ?? "–"]]}
          usage={(i) => <DataUsageChips usage={dataFlow?.byKey.get(`input:${i.name}`)} />}
          readOnly={readOnly}
          onEdit={(i, index) => setInput({ item: i, index })}
          onDelete={(index) => updateDefinition({ inputs: inputs.filter((_, j) => j !== index) })}
        />
      </Section>
      <Section
        title="Outputs"
        description="Values the workflow exposes when it finishes (set with the SetOutput activity)."
        onAdd={readOnly ? undefined : () => setOutput({ item: { name: "", type: "Object", isArray: false }, index: -1 })}
      >
        <DefinitionTable
          rows={outputs}
          columns={[["Name", (o) => o.name], ["Type", (o) => `${o.type}${o.isArray ? "[]" : ""}`], ["Display name", (o) => o.displayName ?? "–"]]}
          usage={(o) => <DataUsageChips usage={dataFlow?.byKey.get(`output:${o.name}`)} />}
          readOnly={readOnly}
          onEdit={(o, index) => setOutput({ item: o, index })}
          onDelete={(index) => updateDefinition({ outputs: outputs.filter((_, j) => j !== index) })}
        />
      </Section>
      <Section title="Outcomes" description="Named outcomes used as ports when this workflow is used as an activity.">
        <div className="flex flex-wrap gap-1.5">
          {outcomes.map((o) => (
            <Badge key={o} variant="secondary" className="gap-1 pr-1">
              {o}
              {!readOnly && (
                <button type="button" aria-label="Remove" className="rounded hover:bg-muted-foreground/20" onClick={() => updateDefinition({ outcomes: outcomes.filter((x) => x !== o) })}>
                  <X className="size-3" />
                </button>
              )}
            </Badge>
          ))}
          {outcomes.length === 0 && <span className="text-sm text-muted-foreground">No outcomes.</span>}
        </div>
        {!readOnly && (
          <form
            className="mt-2 flex gap-1.5"
            onSubmit={(e) => {
              e.preventDefault();
              const v = outcome.trim();
              if (v && !outcomes.includes(v)) updateDefinition({ outcomes: [...outcomes, v] });
              setOutcome("");
            }}
          >
            <Input value={outcome} onChange={(e) => setOutcome(e.target.value)} placeholder="Outcome name" className="max-w-xs" />
            <Button type="submit" variant="outline" size="sm" disabled={!outcome.trim()}><Plus /> Add</Button>
          </form>
        )}
      </Section>

      {input && (
        <InputDialog
          item={input.item}
          onClose={() => setInput(null)}
          onSave={(item) => {
            updateDefinition({ inputs: replaceAt(inputs, input.index, item) });
            setInput(null);
          }}
        />
      )}
      {output && (
        <OutputDialog
          item={output.item}
          onClose={() => setOutput(null)}
          onSave={(item) => {
            updateDefinition({ outputs: replaceAt(outputs, output.index, item) });
            setOutput(null);
          }}
        />
      )}
    </div>
  );
}

function Section({ title, description, onAdd, children }: { title: string; description: string; onAdd?: () => void; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-2">
      <div className="flex items-start justify-between gap-2">
        <div>
          <h3 className="text-sm font-semibold">{title}</h3>
          <p className="text-xs text-muted-foreground">{description}</p>
        </div>
        {onAdd && (
          <Button size="sm" variant="outline" onClick={onAdd}><Plus /> Add</Button>
        )}
      </div>
      {children}
    </section>
  );
}

function DefinitionTable<T>({
  rows,
  columns,
  readOnly,
  onEdit,
  onDelete,
  usage,
}: {
  rows: T[];
  columns: [string, (row: T) => string][];
  readOnly: boolean;
  onEdit: (row: T, index: number) => void;
  onDelete: (index: number) => void;
  usage?: (row: T) => React.ReactNode;
}) {
  return (
    <div className="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            {columns.map(([h]) => <TableHead key={h}>{h}</TableHead>)}
            {usage && <TableHead>Used</TableHead>}
            <TableHead className="w-20" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={columns.length + (usage ? 2 : 1)} className="text-center text-muted-foreground">None.</TableCell>
            </TableRow>
          )}
          {rows.map((row, index) => (
            <TableRow key={index}>
              {columns.map(([h, cell]) => <TableCell key={h}>{cell(row)}</TableCell>)}
              {usage && <TableCell>{usage(row)}</TableCell>}
              <TableCell>
                {!readOnly && (
                  <div className="flex justify-end gap-0.5">
                    <Button variant="ghost" size="icon-xs" aria-label="Edit" onClick={() => onEdit(row, index)}><Pencil /></Button>
                    <Button variant="ghost" size="icon-xs" aria-label="Delete" onClick={() => onDelete(index)}><Trash2 /></Button>
                  </div>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function InputDialog({ item, onSave, onClose }: { item: InputDefinition; onSave: (i: InputDefinition) => void; onClose: () => void }) {
  const [draft, setDraft] = useState(item);
  const { items: types } = useVariableTypes();
  const { items: drivers } = useStorageDrivers();
  return (
    <Dialog open onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>{item.name ? "Edit input" : "Add input"}</DialogTitle></DialogHeader>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Name" required className="col-span-2"><Input value={draft.name} autoFocus onChange={(e) => setDraft({ ...draft, name: e.target.value })} /></Field>
          <Field label="Type"><SimpleSelect value={draft.type} options={types.map((t) => ({ value: t.typeName, label: t.displayName }))} onChange={(v) => setDraft({ ...draft, type: v ?? "Object" })} /></Field>
          <Field inline label="Array" className="self-end pb-2"><Checkbox checked={draft.isArray === true} onCheckedChange={(c) => setDraft({ ...draft, isArray: c === true })} /></Field>
          <Field label="Display name"><Input value={draft.displayName ?? ""} onChange={(e) => setDraft({ ...draft, displayName: e.target.value || undefined })} /></Field>
          <Field label="Category"><Input value={draft.category ?? ""} onChange={(e) => setDraft({ ...draft, category: e.target.value || undefined })} /></Field>
          <Field label="Description" className="col-span-2"><Input value={draft.description ?? ""} onChange={(e) => setDraft({ ...draft, description: e.target.value || undefined })} /></Field>
          <Field label="UI hint"><SimpleSelect value={draft.uiHint} allowEmpty options={UI_HINTS.map((h) => ({ value: h, label: h }))} onChange={(v) => setDraft({ ...draft, uiHint: v })} /></Field>
          <Field label="Storage driver"><SimpleSelect value={draft.storageDriverType} allowEmpty options={drivers.map((d) => ({ value: d.typeName, label: d.displayName }))} onChange={(v) => setDraft({ ...draft, storageDriverType: v })} /></Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button disabled={!draft.name.trim()} onClick={() => onSave(draft)}>Save</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function OutputDialog({ item, onSave, onClose }: { item: OutputDefinition; onSave: (o: OutputDefinition) => void; onClose: () => void }) {
  const [draft, setDraft] = useState(item);
  const { items: types } = useVariableTypes();
  return (
    <Dialog open onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader><DialogTitle>{item.name ? "Edit output" : "Add output"}</DialogTitle></DialogHeader>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Name" required className="col-span-2"><Input value={draft.name} autoFocus onChange={(e) => setDraft({ ...draft, name: e.target.value })} /></Field>
          <Field label="Type"><SimpleSelect value={draft.type} options={types.map((t) => ({ value: t.typeName, label: t.displayName }))} onChange={(v) => setDraft({ ...draft, type: v ?? "Object" })} /></Field>
          <Field inline label="Array" className="self-end pb-2"><Checkbox checked={draft.isArray === true} onCheckedChange={(c) => setDraft({ ...draft, isArray: c === true })} /></Field>
          <Field label="Display name"><Input value={draft.displayName ?? ""} onChange={(e) => setDraft({ ...draft, displayName: e.target.value || undefined })} /></Field>
          <Field label="Description"><Input value={draft.description ?? ""} onChange={(e) => setDraft({ ...draft, description: e.target.value || undefined })} /></Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button disabled={!draft.name.trim()} onClick={() => onSave(draft)}>Save</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
