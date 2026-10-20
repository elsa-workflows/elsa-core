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
import type { SecretModel } from "../api";
import { useUpdateSecret } from "../hooks";

/** Edits the mutable metadata (display name, description). Everything else is fixed or rotated. */
export function EditSecretDialog({ secret, onClose }: { secret: SecretModel | null; onClose: () => void }) {
  return (
    <Dialog open={!!secret} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">{secret && <EditSecretForm key={secret.name} secret={secret} close={onClose} />}</DialogContent>
    </Dialog>
  );
}

function EditSecretForm({ secret, close }: { secret: SecretModel; close: () => void }) {
  const [displayName, setDisplayName] = useState(secret.displayName ?? "");
  const [description, setDescription] = useState(secret.description ?? "");
  const update = useUpdateSecret();

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      await update.mutateAsync({ name: secret.name, displayName: displayName.trim() || undefined, description: description.trim() || undefined });
      toast.success(`Secret "${secret.name}" updated`);
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>Edit {secret.name}</DialogTitle>
        <DialogDescription>Only metadata can be edited here. Use Rotate to replace the value.</DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label="Display name" htmlFor="edit-secret-display-name">
          <Input id="edit-secret-display-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </Field>
        <Field label="Description" htmlFor="edit-secret-description">
          <Textarea id="edit-secret-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
        </Field>
        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-xs text-muted-foreground">
          <dt>Type</dt>
          <dd>{secret.typeName}</dd>
          <dt>Store</dt>
          <dd>{secret.storeName}</dd>
          <dt>Scope</dt>
          <dd>{secret.scope || "–"}</dd>
        </dl>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={update.isPending}>
          Cancel
        </Button>
        <Button type="submit" disabled={update.isPending}>
          {update.isPending && <Loader2 className="animate-spin" />}
          Save
        </Button>
      </DialogFooter>
    </form>
  );
}
