"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Loader2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@elsa-workflows/react-studio-ui/dialog";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Textarea } from "@elsa-workflows/react-studio-ui/textarea";
import type { Label } from "../api";
import { useCreateLabel, useUpdateLabel } from "../hooks";
import { LabelSwatch } from "./label-badge";

const DEFAULT_COLOR = "#3b82f6";
const HEX_PATTERN = /^#[0-9a-fA-F]{6}$/;

export function LabelDialog({ open, onOpenChange, label }: { open: boolean; onOpenChange: (open: boolean) => void; label?: Label }) {
  useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <LabelForm key={label?.id ?? "new"} label={label} close={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function LabelForm({ label, close }: { label?: Label; close: () => void }) {
  const _t = useTranslation();
  const isEdit = !!label;
  const [name, setName] = useState(label?.name ?? "");
  const [description, setDescription] = useState(label?.description ?? "");
  const [color, setColor] = useState(label?.color && HEX_PATTERN.test(label.color) ? label.color : DEFAULT_COLOR);
  const create = useCreateLabel();
  const update = useUpdateLabel();
  const pending = create.isPending || update.isPending;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    const body = { name: name.trim(), description: description.trim() || undefined, color };
    try {
      if (label) {
        await update.mutateAsync({ id: label.id, ...body });
        toast.success(_t("Label \"{{value0}}\" updated", { value0: body.name }));
      } else {
        await create.mutateAsync(body);
        toast.success(_t("Label \"{{value0}}\" created", { value0: body.name }));
      }
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>{isEdit ? _t("Edit label {{value0}}", { value0: label.name }) : _t("Create label")}</DialogTitle>
        <DialogDescription>{_t("Labels tag workflow definitions so they can be grouped and filtered.")}</DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label={_t("Name")} required htmlFor="label-name" hint={_t("Names are unique, case-insensitively.")}>
          <Input id="label-name" value={name} onChange={(e) => setName(e.target.value)} required maxLength={255} autoComplete="off" />
        </Field>
        <Field label={_t("Description")} htmlFor="label-description">
          <Textarea id="label-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={2} />
        </Field>
        <Field label={_t("Colour")} htmlFor="label-color">
          <div className="flex items-center gap-2">
            <input
              id="label-color"
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              className="size-8 cursor-pointer rounded-md border border-input bg-transparent p-0.5"
              aria-label={_t("Pick colour")}
            />
            <Input
              value={color}
              onChange={(e) => setColor(e.target.value)}
              pattern="#[0-9a-fA-F]{6}"
              className="w-32 font-mono"
              aria-label={_t("Colour hex value")}
            />
            <LabelSwatch color={HEX_PATTERN.test(color) ? color : undefined} className="size-5" />
          </div>
        </Field>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={pending}>
          {_t("Cancel")}</Button>
        <Button type="submit" disabled={pending || !name.trim() || !HEX_PATTERN.test(color)}>
          {pending && <Loader2 className="animate-spin" />}
          {isEdit ? _t("Save") : _t("Create")}
        </Button>
      </DialogFooter>
    </form>
  );
}
