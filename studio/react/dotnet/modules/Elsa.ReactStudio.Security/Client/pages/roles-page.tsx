"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Pencil, Plus, Trash2 } from "lucide-react";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { DataTable, type DataTableColumn } from "@elsa-workflows/react-studio-ui/common/data-table";
import { describeError, ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { SearchInput } from "@elsa-workflows/react-studio-ui/common/search-input";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Resources, usePermission, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import type { IdentityRole, RoleDeletionImpact } from "../api";
import { useDeleteRole, useRoleDeletionImpact, useRoles } from "../hooks";
import { DeletionBlockedDialog } from "../components/deletion-blocked-dialog";
import { RoleDialog } from "../components/role-dialog";

export function RolesPage() {
  const _t = useTranslation();
  const roles = useRoles();
  const remove = useDeleteRole();
  const impact = useRoleDeletionImpact();
  const confirm = useConfirm();
  const canCreate = usePermission(Resources.roles, Verbs.create);
  const canUpdate = usePermission(Resources.roles, Verbs.update);
  const canDelete = usePermission(Resources.roles, Verbs.delete);

  const [search, setSearch] = useState("");
  const [dialog, setDialog] = useState<{ open: boolean; role?: IdentityRole }>({ open: false });
  const [blocked, setBlocked] = useState<{ role: IdentityRole; impact: RoleDeletionImpact } | null>(null);

  const rows = useMemo(() => {
    const list = roles.data ?? [];
    const q = search.trim().toLowerCase();
    const filtered = q ? list.filter((r) => r.name.toLowerCase().includes(q) || r.permissions.some((p) => p.toLowerCase().includes(q))) : list;
    return [...filtered].sort((a, b) => a.name.localeCompare(b.name));
  }, [roles.data, search]);

  const onDelete = async (role: IdentityRole) => {
    let report: RoleDeletionImpact | undefined;
    try {
      report = await impact.mutateAsync(role.id);
    } catch (err) {
      // An older server without the impact endpoint: fall back to a plain confirmation.
      if ((err as { status?: number }).status !== 404) {
        toast.error(describeError(err));
        return;
      }
    }
    if (report && !report.canDelete) {
      setBlocked({ role, impact: report });
      return;
    }
    const warnings = report?.warnings ?? [];
    const ok = await confirm({
      title: _t("Delete role \"{{value0}}\"?", { value0: role.name }),
      description: (
        <div className="flex flex-col gap-2">
          <p>{_t("Users and applications holding this role lose its")}{" "}{role.permissions.length} {_t("permission(s).")}</p>
          {warnings.length > 0 && (
            <ul className="list-disc pl-5 text-amber-700 dark:text-amber-300">
              {warnings.map((w, i) => (
                <li key={i}>{w}</li>
              ))}
            </ul>
          )}
        </div>
      ),
      confirmLabel: _t("Delete"),
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(role.id);
      toast.success(_t("Role \"{{value0}}\" deleted", { value0: role.name }));
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<IdentityRole>[] = [
    {
      id: "name",
      header: _t("Name"),
      cell: (r) => (
        <div className="min-w-0">
          <div className="font-medium">{r.name}</div>
          {r.id !== r.name && <div className="text-xs text-muted-foreground font-mono truncate">{r.id}</div>}
        </div>
      ),
    },
    {
      id: "permissions",
      header: _t("Permissions"),
      cell: (r) => (
        <span title={r.permissions.join("\n")} className="tabular-nums">
          {r.permissions.length}
        </span>
      ),
    },
    { id: "tenant", header: _t("Tenant"), cell: (r) => r.tenantId || <span className="text-muted-foreground">–</span> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-24",
      className: "text-right",
      cell: (r) => (
        <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          {canUpdate && (
            <Button variant="ghost" size="icon-sm" aria-label={_t("Edit {{value0}}", { value0: r.name })} onClick={() => setDialog({ open: true, role: r })}>
              <Pencil />
            </Button>
          )}
          {canDelete && (
            <Button variant="ghost" size="icon-sm" aria-label={_t("Delete {{value0}}", { value0: r.name })} disabled={impact.isPending} onClick={() => void onDelete(r)}>
              <Trash2 />
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title={_t("Roles")}
        description={_t("Named sets of permissions granted to users and applications.")}
        actions={
          canCreate && (
            <Button onClick={() => setDialog({ open: true })}>
              <Plus />
              {_t("Create role")}</Button>
          )
        }
      >
        <SearchInput value={search} onChange={setSearch} placeholder={_t("Filter by name or permission…")} className="max-w-sm" />
      </PageHeader>
      <ErrorAlert error={roles.error} title={_t("Roles could not be loaded")} />
      <DataTable columns={columns} rows={rows} rowKey={(r) => r.id} isLoading={roles.isLoading} emptyMessage={search ? _t("No roles match the filter.") : _t("No roles yet.")} />

      <RoleDialog open={dialog.open} onOpenChange={(open) => setDialog((d) => ({ ...d, open }))} role={dialog.role} />
      <DeletionBlockedDialog state={blocked} onClose={() => setBlocked(null)} />
    </div>
  );
}
