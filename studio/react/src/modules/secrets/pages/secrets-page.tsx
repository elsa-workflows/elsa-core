"use client";

import { Ban, Eye, FlaskConical, MoreHorizontal, Pencil, Plus, RefreshCw, Trash2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/common/confirm-dialog";
import { DataTable, type DataTableColumn } from "@/components/common/data-table";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { PageHeader } from "@/components/common/page-header";
import { SearchInput } from "@/components/common/search-input";
import { SimpleSelect } from "@/components/common/simple-select";
import { Timestamp } from "@/components/common/timestamp";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { useStudioConfig } from "@/config/provider";
import { href } from "@/lib/modules/router";
import { Resources, usePermission, Verbs } from "@/lib/permissions";
import { SECRET_STATUSES, type SecretModel, type SecretStatus } from "../api";
import { useDeleteSecret, useRevokeSecret, useSecretDescriptors, useSecrets, useTestSecret } from "../hooks";
import { CreateSecretDialog } from "../components/create-secret-dialog";
import { EditSecretDialog } from "../components/edit-secret-dialog";
import { RotateSecretDialog } from "../components/rotate-secret-dialog";
import { SecretStatusBadge } from "../components/secret-status-badge";
import { SecretRoutes } from "../routes";

interface Filters {
  search: string;
  typeName?: string;
  storeName?: string;
  scope: string;
  status?: SecretStatus;
}

export function SecretsPage() {
  const config = useStudioConfig();
  const router = useRouter();
  const [filters, setFilters] = useState<Filters>({ search: "", scope: "" });
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(config.ui.defaultPageSize);
  const query = useMemo(
    () => ({
      search: filters.search || undefined,
      typeName: filters.typeName,
      storeName: filters.storeName,
      scope: filters.scope.trim() || undefined,
      status: filters.status,
      page,
      pageSize,
    }),
    [filters, page, pageSize],
  );
  const secrets = useSecrets(query);
  const descriptors = useSecretDescriptors();
  const confirm = useConfirm();
  const revoke = useRevokeSecret();
  const remove = useDeleteSecret();
  const test = useTestSecret();
  const canWrite = usePermission(Resources.secrets, Verbs.write);
  const canDelete = usePermission(Resources.secrets, Verbs.delete);
  const canTest = usePermission(Resources.secrets, Verbs.test);

  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<SecretModel | null>(null);
  const [rotating, setRotating] = useState<SecretModel | null>(null);

  const updateFilters = useCallback((patch: Partial<Filters>) => {
    setFilters((f) => ({ ...f, ...patch }));
    setPage(0);
  }, []);

  const onRevoke = async (secret: SecretModel) => {
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

  const onDelete = async (secret: SecretModel) => {
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
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const onTest = async (secret: SecretModel) => {
    try {
      const result = await test.mutateAsync(secret.name);
      if (result.succeeded) toast.success(`"${secret.name}" resolves correctly`);
      else toast.error(`"${secret.name}" failed to resolve${result.error ? `: ${result.error}` : ""}`);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<SecretModel>[] = [
    {
      id: "name",
      header: "Name",
      cell: (s) => (
        <div className="min-w-0">
          <div className="font-medium">{s.displayName || s.name}</div>
          {s.displayName && s.displayName !== s.name && <div className="text-xs text-muted-foreground font-mono truncate">{s.name}</div>}
        </div>
      ),
    },
    { id: "type", header: "Type", cell: (s) => descriptors.data?.types.find((t) => t.name === s.typeName)?.displayName ?? s.typeName },
    { id: "store", header: "Store", cell: (s) => descriptors.data?.stores.find((st) => st.name === s.storeName)?.displayName ?? s.storeName },
    { id: "scope", header: "Scope", cell: (s) => s.scope || <span className="text-muted-foreground">–</span> },
    {
      id: "tags",
      header: "Tags",
      cell: (s) =>
        s.tags.length === 0 ? (
          <span className="text-muted-foreground">–</span>
        ) : (
          <div className="flex flex-wrap gap-1">
            {s.tags.map((t) => (
              <Badge key={t} variant="secondary">
                {t}
              </Badge>
            ))}
          </div>
        ),
    },
    { id: "status", header: "Status", cell: (s) => <SecretStatusBadge status={s.status} /> },
    { id: "version", header: "Version", className: "tabular-nums", cell: (s) => s.currentVersion ?? <span className="text-muted-foreground">–</span> },
    { id: "expires", header: "Expires", cell: (s) => <Timestamp value={s.expiresAt} relative /> },
    { id: "updated", header: "Updated", cell: (s) => <Timestamp value={s.updatedAt ?? s.createdAt} relative /> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-12",
      className: "text-right",
      cell: (s) => (
          <DropdownMenu>
            <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label={`Actions for ${s.name}`} onClick={(e) => e.stopPropagation()} />}>
              <MoreHorizontal />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-44" onClick={(e) => e.stopPropagation()}>
              <DropdownMenuItem onClick={() => router.push(href(SecretRoutes.detail, { name: s.name }))}>
                <Eye />
                View details
              </DropdownMenuItem>
              {(canWrite || canTest) && <DropdownMenuSeparator />}
              {canWrite && (
                <DropdownMenuItem onClick={() => setEditing(s)}>
                  <Pencil />
                  Edit metadata
                </DropdownMenuItem>
              )}
              {canWrite && (
                <DropdownMenuItem onClick={() => setRotating(s)}>
                  <RefreshCw />
                  Rotate value
                </DropdownMenuItem>
              )}
              {canTest && (
                <DropdownMenuItem onClick={() => void onTest(s)}>
                  <FlaskConical />
                  Test resolution
                </DropdownMenuItem>
              )}
              {(canWrite || canDelete) && <DropdownMenuSeparator />}
              {canWrite && s.status === "Active" && (
                <DropdownMenuItem variant="destructive" onClick={() => void onRevoke(s)}>
                  <Ban />
                  Revoke
                </DropdownMenuItem>
              )}
              {canDelete && (
                <DropdownMenuItem variant="destructive" onClick={() => void onDelete(s)}>
                  <Trash2 />
                  Delete
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title="Secrets"
        description="Credentials and keys workflows reference by name. Values are write-only and never displayed."
        actions={
          canWrite && (
            <Button onClick={() => setCreateOpen(true)} disabled={!descriptors.data}>
              <Plus />
              Create secret
            </Button>
          )
        }
      >
        <div className="flex flex-wrap gap-2">
          <SearchInput value={filters.search} onChange={(search) => updateFilters({ search })} placeholder="Search name or description…" className="w-64" />
          <SimpleSelect
            value={filters.typeName}
            onChange={(typeName) => updateFilters({ typeName })}
            options={(descriptors.data?.types ?? []).map((t) => ({ value: t.name, label: t.displayName }))}
            placeholder="Any type"
            allowEmpty
            className="w-40"
          />
          <SimpleSelect
            value={filters.storeName}
            onChange={(storeName) => updateFilters({ storeName })}
            options={(descriptors.data?.stores ?? []).map((s) => ({ value: s.name, label: s.displayName }))}
            placeholder="Any store"
            allowEmpty
            className="w-40"
          />
          <Input value={filters.scope} onChange={(e) => updateFilters({ scope: e.target.value })} placeholder="Scope" className="w-32" aria-label="Scope filter" />
          <SimpleSelect
            value={filters.status}
            onChange={(status) => updateFilters({ status })}
            options={SECRET_STATUSES.map((s) => ({ value: s, label: s }))}
            placeholder="Any status"
            allowEmpty
            className="w-36"
          />
        </div>
      </PageHeader>
      <ErrorAlert error={secrets.error} title="Secrets could not be loaded" />
      <DataTable
        columns={columns}
        rows={secrets.data?.items ?? []}
        rowKey={(s) => s.id || s.name}
        isLoading={secrets.isLoading}
        onRowClick={(s) => router.push(href(SecretRoutes.detail, { name: s.name }))}
        emptyMessage="No secrets match."
        paging={{
          page,
          pageSize,
          totalCount: secrets.data?.totalCount ?? 0,
          onChange: (p, size) => {
            setPage(p);
            setPageSize(size);
          },
        }}
      />
      <CreateSecretDialog open={createOpen} onOpenChange={setCreateOpen} descriptors={descriptors.data} />
      <EditSecretDialog secret={editing} onClose={() => setEditing(null)} />
      <RotateSecretDialog secret={rotating} descriptors={descriptors.data} onClose={() => setRotating(null)} />
    </div>
  );
}
