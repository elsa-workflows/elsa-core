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
import type { SecretModel } from "../api";
import { useUpdateSecret } from "../hooks";

/** Edits the mutable metadata (display name, description). Everything else is fixed or rotated. */
export function EditSecretDialog({ secret, onClose }: { secret: SecretModel | null; onClose: () => void }) {
  useTranslation();
  return (
    <Dialog open={!!secret} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">{secret && <EditSecretForm key={secret.name} secret={secret} close={onClose} />}</DialogContent>
    </Dialog>
  );
}

function EditSecretForm({ secret, close }: { secret: SecretModel; close: () => void }) {
  const _t = useTranslation();
  const [displayName, setDisplayName] = useState(secret.displayName ?? "");
  const [description, setDescription] = useState(secret.description ?? "");
  const update = useUpdateSecret();

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      await update.mutateAsync({ name: secret.name, displayName: displayName.trim() || undefined, description: description.trim() || undefined });
      toast.success(_t("Secret \"{{value0}}\" updated", { value0: secret.name }));
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>{_t("Edit")}{" "}{secret.name}</DialogTitle>
        <DialogDescription>{_t("Only metadata can be edited here. Use Rotate to replace the value.")}</DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label={_t("Display name")} htmlFor="edit-secret-display-name">
          <Input id="edit-secret-display-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </Field>
        <Field label={_t("Description")} htmlFor="edit-secret-description">
          <Textarea id="edit-secret-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
        </Field>
        <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-xs text-muted-foreground">
          <dt>{_t("Type")}</dt>
          <dd>{secret.typeName}</dd>
          <dt>{_t("Store")}</dt>
          <dd>{secret.storeName}</dd>
          <dt>{_t("Scope")}</dt>
          <dd>{secret.scope || "–"}</dd>
        </dl>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={update.isPending}>
          {_t("Cancel")}</Button>
        <Button type="submit" disabled={update.isPending}>
          {update.isPending && <Loader2 className="animate-spin" />}
          {_t("Save")}</Button>
      </DialogFooter>
    </form>
  );
}
