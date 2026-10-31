"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Ban, Eye, FlaskConical, MoreHorizontal, Pencil, Plus, RefreshCw, Trash2 } from "lucide-react";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { useCallback, useMemo, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { DataTable, type DataTableColumn } from "@elsa-workflows/react-studio-ui/common/data-table";
import { describeError, ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { SearchInput } from "@elsa-workflows/react-studio-ui/common/search-input";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { Badge } from "@elsa-workflows/react-studio-ui/badge";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from "@elsa-workflows/react-studio-ui/dropdown-menu";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { useStudioConfig } from "@elsa-workflows/react-studio-sdk/config-provider";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { Resources, usePermission, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
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
  const _t = useTranslation();
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

  const onDelete = async (secret: SecretModel) => {
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
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const onTest = async (secret: SecretModel) => {
    try {
      const result = await test.mutateAsync(secret.name);
      if (result.succeeded) toast.success(_t("\"{{value0}}\" resolves correctly", { value0: secret.name }));
      else toast.error(_t("\"{{value0}}\" failed to resolve{{value1}}", { value0: secret.name, value1: result.error ? `: ${result.error}` : "" }));
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<SecretModel>[] = [
    {
      id: "name",
      header: _t("Name"),
      cell: (s) => (
        <div className="min-w-0">
          <div className="font-medium">{s.displayName || s.name}</div>
          {s.displayName && s.displayName !== s.name && <div className="text-xs text-muted-foreground font-mono truncate">{s.name}</div>}
        </div>
      ),
    },
    { id: "type", header: _t("Type"), cell: (s) => descriptors.data?.types.find((t) => t.name === s.typeName)?.displayName ?? s.typeName },
    { id: "store", header: _t("Store"), cell: (s) => descriptors.data?.stores.find((st) => st.name === s.storeName)?.displayName ?? s.storeName },
    { id: "scope", header: _t("Scope"), cell: (s) => s.scope || <span className="text-muted-foreground">–</span> },
    {
      id: "tags",
      header: _t("Tags"),
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
    { id: "status", header: _t("Status"), cell: (s) => <SecretStatusBadge status={s.status} /> },
    { id: "version", header: _t("Version"), className: "tabular-nums", cell: (s) => s.currentVersion ?? <span className="text-muted-foreground">–</span> },
    { id: "expires", header: _t("Expires"), cell: (s) => <Timestamp value={s.expiresAt} relative /> },
    { id: "updated", header: _t("Updated"), cell: (s) => <Timestamp value={s.updatedAt ?? s.createdAt} relative /> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-12",
      className: "text-right",
      cell: (s) => (
          <DropdownMenu>
            <DropdownMenuTrigger render={<Button variant="ghost" size="icon-sm" aria-label={_t("Actions for {{value0}}", { value0: s.name })} onClick={(e) => e.stopPropagation()} />}>
              <MoreHorizontal />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-44" onClick={(e) => e.stopPropagation()}>
              <DropdownMenuItem onClick={() => router.push(href(SecretRoutes.detail, { name: s.name }))}>
                <Eye />
                {_t("View details")}</DropdownMenuItem>
              {(canWrite || canTest) && <DropdownMenuSeparator />}
              {canWrite && (
                <DropdownMenuItem onClick={() => setEditing(s)}>
                  <Pencil />
                  {_t("Edit metadata")}</DropdownMenuItem>
              )}
              {canWrite && (
                <DropdownMenuItem onClick={() => setRotating(s)}>
                  <RefreshCw />
                  {_t("Rotate value")}</DropdownMenuItem>
              )}
              {canTest && (
                <DropdownMenuItem onClick={() => void onTest(s)}>
                  <FlaskConical />
                  {_t("Test resolution")}</DropdownMenuItem>
              )}
              {(canWrite || canDelete) && <DropdownMenuSeparator />}
              {canWrite && s.status === "Active" && (
                <DropdownMenuItem variant="destructive" onClick={() => void onRevoke(s)}>
                  <Ban />
                  {_t("Revoke")}</DropdownMenuItem>
              )}
              {canDelete && (
                <DropdownMenuItem variant="destructive" onClick={() => void onDelete(s)}>
                  <Trash2 />
                  {_t("Delete")}</DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title={_t("Secrets")}
        description={_t("Credentials and keys workflows reference by name. Values are write-only and never displayed.")}
        actions={
          canWrite && (
            <Button onClick={() => setCreateOpen(true)} disabled={!descriptors.data}>
              <Plus />
              {_t("Create secret")}</Button>
          )
        }
      >
        <div className="flex flex-wrap gap-2">
          <SearchInput value={filters.search} onChange={(search) => updateFilters({ search })} placeholder={_t("Search name or description…")} className="w-64" />
          <SimpleSelect
            value={filters.typeName}
            onChange={(typeName) => updateFilters({ typeName })}
            options={(descriptors.data?.types ?? []).map((t) => ({ value: t.name, label: t.displayName }))}
            placeholder={_t("Any type")}
            allowEmpty
            className="w-40"
          />
          <SimpleSelect
            value={filters.storeName}
            onChange={(storeName) => updateFilters({ storeName })}
            options={(descriptors.data?.stores ?? []).map((s) => ({ value: s.name, label: s.displayName }))}
            placeholder={_t("Any store")}
            allowEmpty
            className="w-40"
          />
          <Input value={filters.scope} onChange={(e) => updateFilters({ scope: e.target.value })} placeholder={_t("Scope")} className="w-32" aria-label={_t("Scope filter")} />
          <SimpleSelect
            value={filters.status}
            onChange={(status) => updateFilters({ status })}
            options={SECRET_STATUSES.map((s) => ({ value: s, label: s }))}
            placeholder={_t("Any status")}
            allowEmpty
            className="w-36"
          />
        </div>
      </PageHeader>
      <ErrorAlert error={secrets.error} title={_t("Secrets could not be loaded")} />
      <DataTable
        columns={columns}
        rows={secrets.data?.items ?? []}
        rowKey={(s) => s.id || s.name}
        isLoading={secrets.isLoading}
        onRowClick={(s) => router.push(href(SecretRoutes.detail, { name: s.name }))}
        emptyMessage={_t("No secrets match.")}
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
