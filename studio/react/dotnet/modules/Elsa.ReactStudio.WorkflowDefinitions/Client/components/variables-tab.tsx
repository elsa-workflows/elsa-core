"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Checkbox } from "@elsa-workflows/react-studio-ui/checkbox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@elsa-workflows/react-studio-ui/table";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { useStorageDrivers, useVariableTypes } from "@elsa-workflows/react-studio-api-client/hooks/descriptors";
import type { Variable } from "@elsa-workflows/react-studio-api-client/types/activities";
import { newActivityId } from "@elsa-workflows/react-studio-sdk/designer/model/ids";
import { useWorkflowEditor } from "../editor-context";
import { useDesigner } from "@elsa-workflows/react-studio-sdk/designer/index";
import { DataUsageChips } from "./data-usage";

const EMPTY: Variable = { id: "", name: "", typeName: "Object", isArray: false, value: undefined, storageDriverTypeName: undefined };

export function VariablesTab() {
  const _t = useTranslation();
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
        <p className="text-sm text-muted-foreground">{_t("Variables hold state for the lifetime of a workflow instance. Hover a usage count to see where it flows.")}</p>
        {!readOnly && (
          <Button size="sm" variant="outline" onClick={() => setEditing({ ...EMPTY, id: newActivityId() })}>
            <Plus /> {_t("Add variable")}</Button>
        )}
      </div>
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{_t("Variable")}</TableHead>
              <TableHead>{_t("Used")}</TableHead>
              <TableHead className="w-16" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {variables.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} className="text-center text-muted-foreground">{_t("No variables.")}</TableCell>
              </TableRow>
            )}
            {variables.map((v) => (
              <TableRow key={v.id}>
                <TableCell className="min-w-0">
                  <div className="truncate font-medium">{v.name}</div>
                  <div className="truncate text-xs text-muted-foreground">
                    {v.typeName}{v.isArray ? "[]" : ""}
                    {v.value !== undefined && v.value !== null && v.value !== "" && <> {_t("· default")}{" "}{String(v.value)}</>}
                    {v.storageDriverTypeName && <> · {v.storageDriverTypeName.split(",")[0].split(".").pop()?.replace("StorageDriver", "")}</>}
                  </div>
                </TableCell>
                <TableCell><DataUsageChips usage={dataFlow?.byKey.get(`variable:${v.id}`)} /></TableCell>
                <TableCell>
                  {!readOnly && (
                    <div className="flex justify-end gap-0.5">
                      <Button variant="ghost" size="icon-xs" aria-label={_t("Edit")} onClick={() => setEditing(v)}><Pencil /></Button>
                      <Button variant="ghost" size="icon-xs" aria-label={_t("Delete")} onClick={() => updateDefinition({ variables: variables.filter((x) => x.id !== v.id) })}><Trash2 /></Button>
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
  const _t = useTranslation();
  const [draft, setDraft] = useState(variable);
  const { items: types } = useVariableTypes();
  const { items: drivers } = useStorageDrivers();
  return (
    <Dialog open onOpenChange={(o) => !o && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{variable.name ? _t("Edit variable") : _t("Add variable")}</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <Field label={_t("Name")} required>
            <Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} autoFocus />
          </Field>
          <Field label={_t("Type")}>
            <SimpleSelect value={draft.typeName} options={types.map((t) => ({ value: t.typeName, label: t.displayName }))} onChange={(v) => setDraft({ ...draft, typeName: v ?? "Object" })} />
          </Field>
          <Field inline label={_t("Array")}>
            <Checkbox checked={draft.isArray === true} onCheckedChange={(c) => setDraft({ ...draft, isArray: c === true })} />
          </Field>
          <Field label={_t("Default value")}>
            <Input value={draft.value === undefined || draft.value === null ? "" : String(draft.value)} onChange={(e) => setDraft({ ...draft, value: e.target.value === "" ? undefined : e.target.value })} />
          </Field>
          <Field label={_t("Storage driver")} hint={_t("Where the value is persisted between activities.")}>
            <SimpleSelect value={draft.storageDriverTypeName} allowEmpty options={drivers.map((d) => ({ value: d.typeName, label: d.displayName }))} onChange={(v) => setDraft({ ...draft, storageDriverTypeName: v })} />
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>{_t("Cancel")}</Button>
          <Button disabled={!draft.name.trim()} onClick={() => onSave(draft)}>{_t("Save")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
