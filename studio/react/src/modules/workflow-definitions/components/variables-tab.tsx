"use client";

import { useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { useStorageDrivers, useVariableTypes } from "@/lib/api/hooks/descriptors";
import type { Variable } from "@/lib/api/types/activities";
import { newActivityId } from "@/modules/designer/model/ids";
import { useWorkflowEditor } from "../editor-context";
import { useDesigner } from "@/modules/designer";
import { DataUsageChips } from "./data-usage";

const EMPTY: Variable = { id: "", name: "", typeName: "Object", isArray: false, value: undefined, storageDriverTypeName: undefined };

export function VariablesTab() {
  const { definition, updateDefinition, readOnly } = useWorkflowEditor();
  const { dataFlow } = useDesigner();
  const [editing, setEditing] = useState<Variable | null>(null);
  const variables = definition.variables ?? [];

  const save = (v: Variable) => {
    const exists = variables.some((x) => x.id === v.id);
    updateDefinition({ variables: exists ? variables.map((x) => (x.id === v.id ? v : x)) : [...variables, v] });
    setEditing(null);
  };

  return (
    <div className="flex flex-col gap-3 p-3">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">Variables hold state for the lifetime of a workflow instance. Hover a usage count to see where it flows.</p>
        {!readOnly && (
          <Button size="sm" variant="outline" onClick={() => setEditing({ ...EMPTY, id: newActivityId() })}>
            <Plus /> Add variable
          </Button>
        )}
      </div>
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Variable</TableHead>
              <TableHead>Used</TableHead>
              <TableHead className="w-16" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {variables.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} className="text-center text-muted-foreground">No variables.</TableCell>
              </TableRow>
            )}
            {variables.map((v) => (
              <TableRow key={v.id}>
                <TableCell className="min-w-0">
                  <div className="truncate font-medium">{v.name}</div>
                  <div className="truncate text-xs text-muted-foreground">
                    {v.typeName}{v.isArray ? "[]" : ""}
                    {v.value !== undefined && v.value !== null && v.value !== "" && <> · default {String(v.value)}</>}
                    {v.storageDriverTypeName && <> · {v.storageDriverTypeName.split(",")[0].split(".").pop()?.replace("StorageDriver", "")}</>}
                  </div>
                </TableCell>
                <TableCell><DataUsageChips usage={dataFlow?.byKey.get(`variable:${v.id}`)} /></TableCell>
                <TableCell>
                  {!readOnly && (
                    <div className="flex justify-end gap-0.5">
                      <Button variant="ghost" size="icon-xs" aria-label="Edit" onClick={() => setEditing(v)}><Pencil /></Button>
                      <Button variant="ghost" size="icon-xs" aria-label="Delete" onClick={() => updateDefinition({ variables: variables.filter((x) => x.id !== v.id) })}><Trash2 /></Button>
                    </div>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
      {editing && <VariableDialog variable={editing} onSave={save} onClose={() => setEditing(null)} />}
    </div>
  );
}

function VariableDialog({ variable, onSave, onClose }: { variable: Variable; onSave: (v: Variable) => void; onClose: () => void }) {
  const [draft, setDraft] = useState(variable);
  const { items: types } = useVariableTypes();
  const { items: drivers } = useStorageDrivers();
  return (
    <Dialog open onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{variable.name ? "Edit variable" : "Add variable"}</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <Field label="Name" required>
            <Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} autoFocus />
          </Field>
          <Field label="Type">
            <SimpleSelect value={draft.typeName} options={types.map((t) => ({ value: t.typeName, label: t.displayName }))} onChange={(v) => setDraft({ ...draft, typeName: v ?? "Object" })} />
          </Field>
          <Field inline label="Array">
            <Checkbox checked={draft.isArray === true} onCheckedChange={(c) => setDraft({ ...draft, isArray: c === true })} />
          </Field>
          <Field label="Default value">
            <Input value={draft.value === undefined || draft.value === null ? "" : String(draft.value)} onChange={(e) => setDraft({ ...draft, value: e.target.value === "" ? undefined : e.target.value })} />
          </Field>
          <Field label="Storage driver" hint="Where the value is persisted between activities.">
            <SimpleSelect value={draft.storageDriverTypeName} allowEmpty options={drivers.map((d) => ({ value: d.typeName, label: d.displayName }))} onChange={(v) => setDraft({ ...draft, storageDriverTypeName: v })} />
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button disabled={!draft.name.trim()} onClick={() => onSave(draft)}>Save</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
