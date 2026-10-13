"use client";

import { Loader2 } from "lucide-react";
import { useMemo, useState, type FormEvent } from "react";
import { toast } from "sonner";
import { describeError } from "@/components/common/error-alert";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { storeRequiresValue, type SecretDescriptorsResponse } from "../api";
import { useCreateSecret } from "../hooks";
import { SecretValueField, toIsoOrNull } from "./secret-value-field";
import { StoreFields, useStoreFields, valueHintFor } from "./store-fields";

const NAME_PATTERN = /^[a-zA-Z0-9][a-zA-Z0-9._:-]*$/;

export function CreateSecretDialog({
  open,
  onOpenChange,
  descriptors,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  descriptors: SecretDescriptorsResponse | undefined;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <CreateSecretForm descriptors={descriptors} close={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function CreateSecretForm({ descriptors, close }: { descriptors: SecretDescriptorsResponse | undefined; close: () => void }) {
  const types = useMemo(() => descriptors?.types ?? [], [descriptors]);
  const stores = useMemo(() => descriptors?.stores ?? [], [descriptors]);
  const [name, setName] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [description, setDescription] = useState("");
  const [typeName, setTypeName] = useState<string | undefined>(types[0]?.name);
  const [storeName, setStoreName] = useState<string | undefined>();
  const [scope, setScope] = useState("");
  const [tags, setTags] = useState("");
  const [value, setValue] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const create = useCreateSecret();

  const type = types.find((t) => t.name === typeName);
  const availableStores = useMemo(
    () => stores.filter((s) => !type || type.supportedStoreNames.length === 0 || type.supportedStoreNames.includes(s.name)),
    [stores, type],
  );
  const effectiveStore = availableStores.find((s) => s.name === storeName) ?? availableStores[0];
  // Each store declares the fields it needs (a configuration key, vault coordinates, …) and whether it also takes a value.
  const storeFields = useStoreFields(effectiveStore);
  const requiresValue = storeRequiresValue(effectiveStore);

  const nameValid = NAME_PATTERN.test(name.trim());
  const canSubmit = nameValid && !!typeName && !!effectiveStore && storeFields.complete && (!requiresValue || !!value);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!canSubmit || !typeName || !effectiveStore) return;
    try {
      const created = await create.mutateAsync({
        name: name.trim(),
        displayName: displayName.trim() || undefined,
        description: description.trim() || undefined,
        typeName,
        storeName: effectiveStore.name,
        scope: scope.trim() || undefined,
        tags: tags
          .split(",")
          .map((t) => t.trim())
          .filter(Boolean),
        value: requiresValue ? value : undefined,
        configurationKey: storeFields.configurationKey,
        metadata: storeFields.metadata,
        expiresAt: toIsoOrNull(expiresAt),
      });
      toast.success(`Secret "${created.name}" created`);
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>Create secret</DialogTitle>
        <DialogDescription>
          {requiresValue
            ? "The value is submitted once and never read back. Only metadata is ever returned."
            : "Elsa records a reference to a value held elsewhere. Only metadata is ever returned."}
        </DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Name" required htmlFor="secret-name" hint="Technical, immutable identifier used in expressions." error={name && !nameValid ? "Letters, digits, dots, dashes, underscores and colons only." : undefined}>
            <Input id="secret-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="smtp-password" autoComplete="off" required />
          </Field>
          <Field label="Display name" htmlFor="secret-display-name">
            <Input id="secret-display-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="SMTP password" />
          </Field>
        </div>
        <Field label="Description" htmlFor="secret-description">
          <Textarea id="secret-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={2} />
        </Field>
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Type" required hint={type?.description}>
            <SimpleSelect
              value={typeName}
              onChange={(v) => {
                setTypeName(v);
                setStoreName(undefined);
              }}
              options={types.map((t) => ({ value: t.name, label: t.displayName }))}
              placeholder={types.length ? "Pick a type" : "No types available"}
              disabled={types.length === 0}
            />
          </Field>
          <Field label="Store" required hint={effectiveStore?.description}>
            <SimpleSelect
              value={effectiveStore?.name}
              onChange={setStoreName}
              options={availableStores.map((s) => ({ value: s.name, label: s.displayName }))}
              placeholder={availableStores.length ? "Pick a store" : "No store supports this type"}
              disabled={availableStores.length === 0}
            />
          </Field>
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Scope" htmlFor="secret-scope" hint="Optional grouping such as Email or Http; pickers can filter on it.">
            <Input id="secret-scope" value={scope} onChange={(e) => setScope(e.target.value)} />
          </Field>
          <Field label="Tags" htmlFor="secret-tags" hint="Comma-separated.">
            <Input id="secret-tags" value={tags} onChange={(e) => setTags(e.target.value)} placeholder="prod, email" />
          </Field>
        </div>
        <StoreFields state={storeFields} idPrefix="secret" />
        {requiresValue && <SecretValueField value={value} onChange={setValue} editorHint={type?.editorHint} hint={valueHintFor(effectiveStore)} required />}
        <Field label="Expires at" htmlFor="secret-expires" hint="Optional. Expired secrets stop resolving.">
          <Input id="secret-expires" type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} />
        </Field>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={create.isPending}>
          Cancel
        </Button>
        <Button type="submit" disabled={!canSubmit || create.isPending}>
          {create.isPending && <Loader2 className="animate-spin" />}
          Create
        </Button>
      </DialogFooter>
    </form>
  );
}
