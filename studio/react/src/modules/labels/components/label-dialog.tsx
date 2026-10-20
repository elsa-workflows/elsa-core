"use client";

import { Loader2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { describeError } from "@/components/common/error-alert";
import { Field } from "@/components/common/field";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import type { Label } from "../api";
import { useCreateLabel, useUpdateLabel } from "../hooks";
import { LabelSwatch } from "./label-badge";

const DEFAULT_COLOR = "#3b82f6";
const HEX_PATTERN = /^#[0-9a-fA-F]{6}$/;

export function LabelDialog({ open, onOpenChange, label }: { open: boolean; onOpenChange: (open: boolean) => void; label?: Label }) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <LabelForm key={label?.id ?? "new"} label={label} close={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function LabelForm({ label, close }: { label?: Label; close: () => void }) {
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
        toast.success(`Label "${body.name}" updated`);
      } else {
        await create.mutateAsync(body);
        toast.success(`Label "${body.name}" created`);
      }
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>{isEdit ? `Edit label ${label.name}` : "Create label"}</DialogTitle>
        <DialogDescription>Labels tag workflow definitions so they can be grouped and filtered.</DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label="Name" required htmlFor="label-name" hint="Names are unique, case-insensitively.">
          <Input id="label-name" value={name} onChange={(e) => setName(e.target.value)} required maxLength={255} autoComplete="off" />
        </Field>
        <Field label="Description" htmlFor="label-description">
          <Textarea id="label-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={2} />
        </Field>
        <Field label="Colour" htmlFor="label-color">
          <div className="flex items-center gap-2">
            <input
              id="label-color"
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              className="size-8 cursor-pointer rounded-md border border-input bg-transparent p-0.5"
              aria-label="Pick colour"
            />
            <Input
              value={color}
              onChange={(e) => setColor(e.target.value)}
              pattern="#[0-9a-fA-F]{6}"
              className="w-32 font-mono"
              aria-label="Colour hex value"
            />
            <LabelSwatch color={HEX_PATTERN.test(color) ? color : undefined} className="size-5" />
          </div>
        </Field>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={pending}>
          Cancel
        </Button>
        <Button type="submit" disabled={pending || !name.trim() || !HEX_PATTERN.test(color)}>
          {pending && <Loader2 className="animate-spin" />}
          {isEdit ? "Save" : "Create"}
        </Button>
      </DialogFooter>
    </form>
  );
}
