"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ArrowLeft, Ban, FlaskConical, KeyRound, Loader2, Pencil, RefreshCw, Save, Trash2, X } from "lucide-react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { describeError, ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { KeyValueTable } from "@elsa-workflows/react-studio-ui/common/key-value-table";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { Alert, AlertDescription } from "@elsa-workflows/react-studio-ui/alert";
import { Badge } from "@elsa-workflows/react-studio-ui/badge";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@elsa-workflows/react-studio-ui/card";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Skeleton } from "@elsa-workflows/react-studio-ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@elsa-workflows/react-studio-ui/tabs";
import { Textarea } from "@elsa-workflows/react-studio-ui/textarea";
import type { RouteParams } from "@elsa-workflows/react-studio-sdk";
import { Resources, usePermission, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import { storeFields as declaredStoreFields, storeRequiresValue, type SecretDescriptorsResponse, type SecretModel } from "../api";
import { SecretStatusBadge } from "../components/secret-status-badge";
import { SecretValueField, secretValueError, toIsoOrNull } from "../components/secret-value-field";
import { StoreFields, useStoreFields, valueHintFor } from "../components/store-fields";
import { useDeleteSecret, useRevokeSecret, useRotateSecret, useSecret, useSecretDescriptors, useTestSecret, useUpdateSecret } from "../hooks";
import { SecretRoutes } from "../routes";

/**
 * One secret: identity and metadata on the Overview tab, value replacement on the Rotation tab.
 * Mirrors the Blazor `Secret.razor` page; the value itself is never shown.
 */
export function SecretDetailPage({ params }: { params: RouteParams }) {
  const _t = useTranslation();
  const name = params.name;
  const router = useRouter();
  const confirm = useConfirm();
  const query = useSecret(name);
  const descriptors = useSecretDescriptors();
  const secret = query.data;
  const canWrite = usePermission(Resources.secrets, Verbs.write);
  const canDelete = usePermission(Resources.secrets, Verbs.delete);
  const canTest = usePermission(Resources.secrets, Verbs.test);
  const revoke = useRevokeSecret();
  const remove = useDeleteSecret();
  const test = useTestSecret();

  const store = descriptors.data?.stores.find((s) => s.name === secret?.storeName);
  const typeLabel = descriptors.data?.types.find((t) => t.name === secret?.typeName)?.displayName ?? secret?.typeName;
  const storeLabel = store?.displayName ?? secret?.storeName;
  // Where the secret actually lives: the store's own fields, labelled as the store describes them.
  const storeRows = declaredStoreFields(store)
    .map((field) => ({ label: field.displayName, value: secret?.metadata?.[field.name] }))
    .filter((row) => !!row.value);

  const onRevoke = async () => {
    if (!secret) return;
    const ok = await confirm({
      title: _t("Revoke \"{{value0}}\"?", { value0: secret.name }),
      description: _t("Workflows can no longer resolve this secret. The record stays for auditing and can be rotated to a new value later."),
      confirmLabel: _t("Revoke"),
      destructive: true,
    });
    if (!ok) return;
    try {
      await revoke.mutateAsync(secret.name);
      toast.success(_t("Secret \"{{value0}}\" revoked", { value0: secret.name }));
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const onDelete = async () => {
    if (!secret) return;
    const ok = await confirm({
      title: _t("Delete \"{{value0}}\"?", { value0: secret.name }),
      description: _t("All versions are removed permanently. Workflows referencing it will fail to resolve the secret."),
      confirmLabel: _t("Delete"),
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(secret.name);
      toast.success(_t("Secret \"{{value0}}\" deleted", { value0: secret.name }));
      router.push(SecretRoutes.list);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const onTest = async () => {
    if (!secret) return;
    try {
      const result = await test.mutateAsync(secret.name);
      if (result.succeeded) toast.success(_t("\"{{value0}}\" resolves correctly", { value0: secret.name }));
      else toast.error(_t("\"{{value0}}\" failed to resolve{{value1}}", { value0: secret.name, value1: result.error ? `: ${result.error}` : "" }));
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        eyebrow={_t("Secret")}
        title={
          secret ? (
            <span className="flex items-center gap-3">
              <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-accent text-primary">
                <KeyRound className="size-5" />
              </span>
              <span className="min-w-0">
                <span className="block truncate">{secret.displayName || secret.name}</span>
                {secret.displayName && secret.displayName !== secret.name && (
                  <span className="block truncate font-mono text-xs font-normal text-muted-foreground">{secret.name}</span>
                )}
              </span>
            </span>
          ) : (
            name
          )
        }
        description={
          secret && (
            <span className="flex flex-wrap items-center gap-1.5">
              <SecretStatusBadge status={secret.status} />
              <Badge variant="outline">{typeLabel}</Badge>
              <Badge variant="outline">{storeLabel}</Badge>
              <Badge variant="outline">{_t("Version")}{" "}{secret.currentVersion ?? "–"}</Badge>
            </span>
          )
        }
        actions={
          <>
            {secret && canTest && (
              <Button size="sm" variant="outline" onClick={() => void onTest()} disabled={test.isPending}>
                {test.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <FlaskConical data-icon="inline-start" />}
                {_t("Test resolution")}</Button>
            )}
            <Button variant="ghost" size="sm" nativeButton={false} render={<Link href={SecretRoutes.list} />}>
              <ArrowLeft data-icon="inline-start" />
              {_t("All secrets")}</Button>
          </>
        }
      />

      <ErrorAlert error={query.error} title={_t("Could not load the secret")} />

      {query.isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-40 w-full" />
        </div>
      )}

      {secret && (
        <Tabs defaultValue="overview">
          <TabsList>
            <TabsTrigger value="overview">{_t("Overview")}</TabsTrigger>
            {canWrite && <TabsTrigger value="rotation">{_t("Rotation")}</TabsTrigger>}
          </TabsList>

          <TabsContent value="overview" className="mt-4">
            <div className="grid gap-4 lg:grid-cols-3">
              <div className="flex flex-col gap-4 lg:col-span-2">
                <DetailsCard secret={secret} canWrite={canWrite} />
                <Card>
                  <CardHeader>
                    <CardTitle>{_t("Properties")}</CardTitle>
                    <CardDescription>{_t("Fixed at creation. Create a new secret to change the type, store or scope.")}</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <KeyValueTable
                      rows={[
                        { label: _t("Technical name"), value: <span className="font-mono text-xs">{secret.name}</span> },
                        { label: _t("Type"), value: typeLabel },
                        { label: _t("Store"), value: storeLabel },
                        ...storeRows,
                        { label: _t("Scope"), value: secret.scope || undefined },
                        {
                          label: _t("Tags"),
                          value:
                            secret.tags.length > 0 ? (
                              <span className="flex flex-wrap gap-1">
                                {secret.tags.map((t) => (
                                  <Badge key={t} variant="secondary">
                                    {t}
                                  </Badge>
                                ))}
                              </span>
                            ) : undefined,
                        },
                        { label: _t("Created"), value: <Timestamp value={secret.createdAt} /> },
                        { label: _t("Updated"), value: secret.updatedAt ? <Timestamp value={secret.updatedAt} /> : undefined },
                      ]}
                    />
                  </CardContent>
                </Card>
              </div>

              <div className="flex flex-col gap-4">
                <Card>
                  <CardHeader>
                    <CardTitle>{_t("At a glance")}</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <KeyValueTable
                      className="grid-cols-[auto_1fr]"
                      rows={[
                        { label: _t("Status"), value: <SecretStatusBadge status={secret.status} /> },
                        { label: _t("Version"), value: secret.currentVersion ?? undefined },
                        { label: _t("Expires"), value: secret.expiresAt ? <Timestamp value={secret.expiresAt} relative /> : "Never" },
                      ]}
                    />
                    <p className="mt-4 text-xs text-muted-foreground">{_t("Rotate this secret when its value changes or as part of your credential policy.")}</p>
                  </CardContent>
                </Card>

                {(canWrite || canDelete) && (
                  <Card className="border-destructive/30">
                    <CardHeader>
                      <CardTitle>{_t("Danger zone")}</CardTitle>
                      <CardDescription>{_t("These actions interrupt workflows that depend on this secret.")}</CardDescription>
                    </CardHeader>
                    <CardContent className="flex flex-col gap-2">
                      {canWrite && secret.status === "Active" && (
                        <Button variant="outline" size="sm" className="justify-start text-destructive" onClick={() => void onRevoke()} disabled={revoke.isPending}>
                          <Ban data-icon="inline-start" />
                          {_t("Revoke secret")}</Button>
                      )}
                      {canDelete && (
                        <Button variant="destructive" size="sm" className="justify-start" onClick={() => void onDelete()} disabled={remove.isPending}>
                          <Trash2 data-icon="inline-start" />
                          {_t("Delete secret")}</Button>
                      )}
                    </CardContent>
                  </Card>
                )}
              </div>
            </div>
          </TabsContent>

          {canWrite && (
            <TabsContent value="rotation" className="mt-4">
              <div className="grid gap-4 lg:grid-cols-3">
                <RotationCard secret={secret} descriptors={descriptors.data} className="lg:col-span-2" />
                <Card>
                  <CardHeader>
                    <CardTitle>{_t("How rotation works")}</CardTitle>
                  </CardHeader>
                  <CardContent className="flex flex-col gap-2 text-sm text-muted-foreground">
                    <p>{_t("A successful rotation creates the next version and retires the previous one.")}</p>
                    <p>{_t("Workflows resolve the new value on their next run. Nothing needs to be republished.")}</p>
                    <p>{_t("Elsa Studio never displays a stored value, so keep a copy where you manage credentials.")}</p>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>
          )}
        </Tabs>
      )}
    </div>
  );
}

/** Display name and description, editable in place. */
function DetailsCard({ secret, canWrite }: { secret: SecretModel; canWrite: boolean }) {
  const _t = useTranslation();
  const [editing, setEditing] = useState(false);
  const [displayName, setDisplayName] = useState(secret.displayName ?? "");
  const [description, setDescription] = useState(secret.description ?? "");
  const update = useUpdateSecret();

  const start = () => {
    setDisplayName(secret.displayName ?? "");
    setDescription(secret.description ?? "");
    setEditing(true);
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      await update.mutateAsync({ name: secret.name, displayName: displayName.trim() || undefined, description: description.trim() || undefined });
      toast.success(_t("Secret \"{{value0}}\" updated", { value0: secret.name }));
      setEditing(false);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{_t("Details")}</CardTitle>
        <CardDescription>{_t("Identity and metadata for this stored secret.")}</CardDescription>
        {canWrite && !editing && (
          <CardAction>
            <Button variant="outline" size="sm" onClick={start}>
              <Pencil data-icon="inline-start" />
              {_t("Edit details")}</Button>
          </CardAction>
        )}
      </CardHeader>
      <CardContent>
        {editing ? (
          <form onSubmit={submit} className="flex flex-col gap-4">
            <Field label={_t("Display name")} htmlFor="secret-display-name">
              <Input id="secret-display-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
            </Field>
            <Field label={_t("Description")} htmlFor="secret-description">
              <Textarea id="secret-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
            </Field>
            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" size="sm" onClick={() => setEditing(false)} disabled={update.isPending}>
                <X data-icon="inline-start" />
                {_t("Cancel")}</Button>
              <Button type="submit" size="sm" disabled={update.isPending}>
                {update.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Save data-icon="inline-start" />}
                {_t("Save details")}</Button>
            </div>
          </form>
        ) : (
          <KeyValueTable
            rows={[
              { label: _t("Display name"), value: secret.displayName || undefined },
              { label: _t("Description"), value: secret.description ? <span className="whitespace-pre-wrap">{secret.description}</span> : undefined },
            ]}
          />
        )}
      </CardContent>
    </Card>
  );
}

/** Inline rotation form; the same rules as `RotateSecretDialog` (the store declares which fields it needs). */
function RotationCard({ secret, descriptors, className }: { secret: SecretModel; descriptors: SecretDescriptorsResponse | undefined; className?: string }) {
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
      setValue("");
      storeFields.reset();
      setExpiresAt("");
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>{_t("Rotate secret")}</CardTitle>
        <CardDescription>{_t("Replace the stored value and optionally set an expiry for the new version.")}</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={submit} className="flex flex-col gap-4">
          <Alert>
            <AlertDescription>
              {_t("This creates version")}{" "}{(secret.currentVersion ?? 0) + 1}. {secret.status !== "Active" && "Rotating a revoked or expired secret makes it active again."}
            </AlertDescription>
          </Alert>
          <StoreFields state={storeFields} idPrefix="rotate" />
          {requiresValue && <SecretValueField value={value} onChange={setValue} editorHint={type?.editorHint} label={_t("New value")} hint={valueHintFor(store)} required />}
          <Field label={_t("Expires at")} htmlFor="rotate-expires" hint={_t("Optional.")}>
            <Input id="rotate-expires" type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} className="max-w-xs" />
          </Field>
          <div className="flex justify-end">
            <Button type="submit" disabled={!canSubmit || rotate.isPending}>
              {rotate.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <RefreshCw data-icon="inline-start" />}
              {_t("Rotate")}</Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
