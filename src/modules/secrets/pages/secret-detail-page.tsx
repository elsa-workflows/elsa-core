"use client";

import { ArrowLeft, Ban, FlaskConical, KeyRound, Loader2, Pencil, RefreshCw, Save, Trash2, X } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/common/confirm-dialog";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { Field } from "@/components/common/field";
import { KeyValueTable } from "@/components/common/key-value-table";
import { PageHeader } from "@/components/common/page-header";
import { Timestamp } from "@/components/common/timestamp";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import type { RouteParams } from "@/lib/modules/types";
import { Resources, usePermission, Verbs } from "@/lib/permissions";
import { storeFields as declaredStoreFields, storeRequiresValue, type SecretDescriptorsResponse, type SecretModel } from "../api";
import { SecretStatusBadge } from "../components/secret-status-badge";
import { SecretValueField, toIsoOrNull } from "../components/secret-value-field";
import { StoreFields, useStoreFields } from "../components/store-fields";
import { useDeleteSecret, useRevokeSecret, useRotateSecret, useSecret, useSecretDescriptors, useTestSecret, useUpdateSecret } from "../hooks";
import { SecretRoutes } from "../routes";

/**
 * One secret: identity and metadata on the Overview tab, value replacement on the Rotation tab.
 * Mirrors the Blazor `Secret.razor` page; the value itself is never shown.
 */
export function SecretDetailPage({ params }: { params: RouteParams }) {
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
      title: `Revoke "${secret.name}"?`,
      description: "Workflows can no longer resolve this secret. The record stays for auditing and can be rotated to a new value later.",
      confirmLabel: "Revoke",
      destructive: true,
    });
    if (!ok) return;
    try {
      await revoke.mutateAsync(secret.name);
      toast.success(`Secret "${secret.name}" revoked`);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const onDelete = async () => {
    if (!secret) return;
    const ok = await confirm({
      title: `Delete "${secret.name}"?`,
      description: "All versions are removed permanently. Workflows referencing it will fail to resolve the secret.",
      confirmLabel: "Delete",
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(secret.name);
      toast.success(`Secret "${secret.name}" deleted`);
      router.push(SecretRoutes.list);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const onTest = async () => {
    if (!secret) return;
    try {
      const result = await test.mutateAsync(secret.name);
      if (result.succeeded) toast.success(`"${secret.name}" resolves correctly`);
      else toast.error(`"${secret.name}" failed to resolve${result.error ? `: ${result.error}` : ""}`);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        eyebrow="Secret"
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
              <Badge variant="outline">Version {secret.currentVersion ?? "–"}</Badge>
            </span>
          )
        }
        actions={
          <>
            {secret && canTest && (
              <Button size="sm" variant="outline" onClick={() => void onTest()} disabled={test.isPending}>
                {test.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <FlaskConical data-icon="inline-start" />}
                Test resolution
              </Button>
            )}
            <Button variant="ghost" size="sm" nativeButton={false} render={<Link href={SecretRoutes.list} />}>
              <ArrowLeft data-icon="inline-start" />
              All secrets
            </Button>
          </>
        }
      />

      <ErrorAlert error={query.error} title="Could not load the secret" />

      {query.isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-40 w-full" />
        </div>
      )}

      {secret && (
        <Tabs defaultValue="overview">
          <TabsList>
            <TabsTrigger value="overview">Overview</TabsTrigger>
            {canWrite && <TabsTrigger value="rotation">Rotation</TabsTrigger>}
          </TabsList>

          <TabsContent value="overview" className="mt-4">
            <div className="grid gap-4 lg:grid-cols-3">
              <div className="flex flex-col gap-4 lg:col-span-2">
                <DetailsCard secret={secret} canWrite={canWrite} />
                <Card>
                  <CardHeader>
                    <CardTitle>Properties</CardTitle>
                    <CardDescription>Fixed at creation. Create a new secret to change the type, store or scope.</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <KeyValueTable
                      rows={[
                        { label: "Technical name", value: <span className="font-mono text-xs">{secret.name}</span> },
                        { label: "Type", value: typeLabel },
                        { label: "Store", value: storeLabel },
                        ...storeRows,
                        { label: "Scope", value: secret.scope || undefined },
                        {
                          label: "Tags",
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
                        { label: "Created", value: <Timestamp value={secret.createdAt} /> },
                        { label: "Updated", value: secret.updatedAt ? <Timestamp value={secret.updatedAt} /> : undefined },
                      ]}
                    />
                  </CardContent>
                </Card>
              </div>

              <div className="flex flex-col gap-4">
                <Card>
                  <CardHeader>
                    <CardTitle>At a glance</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <KeyValueTable
                      className="grid-cols-[auto_1fr]"
                      rows={[
                        { label: "Status", value: <SecretStatusBadge status={secret.status} /> },
                        { label: "Version", value: secret.currentVersion ?? undefined },
                        { label: "Expires", value: secret.expiresAt ? <Timestamp value={secret.expiresAt} relative /> : "Never" },
                      ]}
                    />
                    <p className="mt-4 text-xs text-muted-foreground">Rotate this secret when its value changes or as part of your credential policy.</p>
                  </CardContent>
                </Card>

                {(canWrite || canDelete) && (
                  <Card className="border-destructive/30">
                    <CardHeader>
                      <CardTitle>Danger zone</CardTitle>
                      <CardDescription>These actions interrupt workflows that depend on this secret.</CardDescription>
                    </CardHeader>
                    <CardContent className="flex flex-col gap-2">
                      {canWrite && secret.status === "Active" && (
                        <Button variant="outline" size="sm" className="justify-start text-destructive" onClick={() => void onRevoke()} disabled={revoke.isPending}>
                          <Ban data-icon="inline-start" />
                          Revoke secret
                        </Button>
                      )}
                      {canDelete && (
                        <Button variant="destructive" size="sm" className="justify-start" onClick={() => void onDelete()} disabled={remove.isPending}>
                          <Trash2 data-icon="inline-start" />
                          Delete secret
                        </Button>
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
                    <CardTitle>How rotation works</CardTitle>
                  </CardHeader>
                  <CardContent className="flex flex-col gap-2 text-sm text-muted-foreground">
                    <p>A successful rotation creates the next version and retires the previous one.</p>
                    <p>Workflows resolve the new value on their next run. Nothing needs to be republished.</p>
                    <p>Elsa Studio never displays a stored value, so keep a copy where you manage credentials.</p>
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
      toast.success(`Secret "${secret.name}" updated`);
      setEditing(false);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Details</CardTitle>
        <CardDescription>Identity and metadata for this stored secret.</CardDescription>
        {canWrite && !editing && (
          <CardAction>
            <Button variant="outline" size="sm" onClick={start}>
              <Pencil data-icon="inline-start" />
              Edit details
            </Button>
          </CardAction>
        )}
      </CardHeader>
      <CardContent>
        {editing ? (
          <form onSubmit={submit} className="flex flex-col gap-4">
            <Field label="Display name" htmlFor="secret-display-name">
              <Input id="secret-display-name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
            </Field>
            <Field label="Description" htmlFor="secret-description">
              <Textarea id="secret-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
            </Field>
            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" size="sm" onClick={() => setEditing(false)} disabled={update.isPending}>
                <X data-icon="inline-start" />
                Cancel
              </Button>
              <Button type="submit" size="sm" disabled={update.isPending}>
                {update.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <Save data-icon="inline-start" />}
                Save details
              </Button>
            </div>
          </form>
        ) : (
          <KeyValueTable
            rows={[
              { label: "Display name", value: secret.displayName || undefined },
              { label: "Description", value: secret.description ? <span className="whitespace-pre-wrap">{secret.description}</span> : undefined },
            ]}
          />
        )}
      </CardContent>
    </Card>
  );
}

/** Inline rotation form; the same rules as `RotateSecretDialog` (the store declares which fields it needs). */
function RotationCard({ secret, descriptors, className }: { secret: SecretModel; descriptors: SecretDescriptorsResponse | undefined; className?: string }) {
  const type = descriptors?.types.find((t) => t.name === secret.typeName);
  const store = descriptors?.stores.find((s) => s.name === secret.storeName);
  const storeFields = useStoreFields(store);
  const requiresValue = storeRequiresValue(store);
  const [value, setValue] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const rotate = useRotateSecret();
  const canSubmit = storeFields.complete && (!requiresValue || !!value);

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
      toast.success(`Secret "${secret.name}" rotated to version ${updated.currentVersion ?? "?"}`);
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
        <CardTitle>Rotate secret</CardTitle>
        <CardDescription>Replace the stored value and optionally set an expiry for the new version.</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={submit} className="flex flex-col gap-4">
          <Alert>
            <AlertDescription>
              This creates version {(secret.currentVersion ?? 0) + 1}. {secret.status !== "Active" && "Rotating a revoked or expired secret makes it active again."}
            </AlertDescription>
          </Alert>
          <StoreFields state={storeFields} idPrefix="rotate" />
          {requiresValue && <SecretValueField value={value} onChange={setValue} editorHint={type?.editorHint} label="New value" required />}
          <Field label="Expires at" htmlFor="rotate-expires" hint="Optional.">
            <Input id="rotate-expires" type="datetime-local" value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} className="max-w-xs" />
          </Field>
          <div className="flex justify-end">
            <Button type="submit" disabled={!canSubmit || rotate.isPending}>
              {rotate.isPending ? <Loader2 className="animate-spin" data-icon="inline-start" /> : <RefreshCw data-icon="inline-start" />}
              Rotate
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
