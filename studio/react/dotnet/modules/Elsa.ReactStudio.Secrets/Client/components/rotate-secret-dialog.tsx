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
import { storeRequiresValue, type SecretDescriptorsResponse, type SecretModel } from "../api";
import { useRotateSecret } from "../hooks";
import { SecretValueField, secretValueError, toIsoOrNull } from "./secret-value-field";
import { StoreFields, useStoreFields, valueHintFor } from "./store-fields";

/** Supplies a new value (a new version); the previous version is retired by the server. */
export function RotateSecretDialog({
  secret,
  descriptors,
  onClose,
}: {
  secret: SecretModel | null;
  descriptors: SecretDescriptorsResponse | undefined;
  onClose: () => void;
}) {
  useTranslation();
  return (
    <Dialog open={!!secret} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        {secret && <RotateSecretForm key={secret.name} secret={secret} descriptors={descriptors} close={onClose} />}
      </DialogContent>
    </Dialog>
  );
}

function RotateSecretForm({ secret, descriptors, close }: { secret: SecretModel; descriptors: SecretDescriptorsResponse | undefined; close: () => void }) {
  const _t = useTranslation();
  const type = descriptors?.types.find((t) => t.name === secret.typeName);
  const store = descriptors?.stores.find((s) => s.name === secret.storeName);
  const storeFields = useStoreFields(store);
  const requiresValue = storeRequiresValue(store);
  const [value, setValue] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const rotate = useRotateSecret();
  const canSubmit = storeFields.complete && (!requiresValue || !!value) && !secretValueError(value, type?.editorHint);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    try {
      const updated = await rotate.mutateAsync({
        name: secret.name,
        value: requiresValue ? value : undefined,
        configurationKey: storeFields.configurationKey,
        metadata: storeFields.metadata,
        expiresAt: toIsoOrNull(expiresAt),
      });
      toast.success(_t("Secret \"{{value0}}\" rotated to version {{value1}}", { value0: secret.name, value1: updated.currentVersion ?? "?" }));
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>{_t("Rotate")}{" "}{secret.name}</DialogTitle>
        <DialogDescription>
          {_t("Creates version")}{" "}{(secret.currentVersion ?? 0) + 1}{_t(". Workflows resolve the new value on their next run; the old one is retired.")}</DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <StoreFields state={storeFields} idPrefix="rotate" />
        {requiresValue && <SecretValueField value={value} onChange={setValue} editorHint={type?.editorHint} label={_t("New value")} hint={valueHintFor(store)} required />}
        <Field label={_t("Expires at")} htmlFor="rotate-expires" hint={_t("Optional.")}>
          <Input id="rotate-expires" type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} />
        </Field>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={rotate.isPending}>
          {_t("Cancel")}</Button>
        <Button type="submit" disabled={!canSubmit || rotate.isPending}>
          {rotate.isPending && <Loader2 className="animate-spin" />}
          {_t("Rotate")}</Button>
      </DialogFooter>
    </form>
  );
}
