"use client";

import { Pencil, Plus, Trash2 } from "lucide-react";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/common/confirm-dialog";
import { DataTable, type DataTableColumn } from "@/components/common/data-table";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { PageHeader } from "@/components/common/page-header";
import { SearchInput } from "@/components/common/search-input";
import { Button } from "@/components/ui/button";
import { Resources, usePermission, Verbs } from "@/lib/permissions";
import type { IdentityRole, RoleDeletionImpact } from "../api";
import { useDeleteRole, useRoleDeletionImpact, useRoles } from "../hooks";
import { DeletionBlockedDialog } from "../components/deletion-blocked-dialog";
import { RoleDialog } from "../components/role-dialog";

export function RolesPage() {
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
      title: `Delete role "${role.name}"?`,
      description: (
        <div className="flex flex-col gap-2">
          <p>Users and applications holding this role lose its {role.permissions.length} permission(s).</p>
          {warnings.length > 0 && (
            <ul className="list-disc pl-5 text-amber-700 dark:text-amber-300">
              {warnings.map((w, i) => (
                <li key={i}>{w}</li>
              ))}
            </ul>
          )}
        </div>
      ),
      confirmLabel: "Delete",
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(role.id);
      toast.success(`Role "${role.name}" deleted`);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<IdentityRole>[] = [
    {
      id: "name",
      header: "Name",
      cell: (r) => (
        <div className="min-w-0">
          <div className="font-medium">{r.name}</div>
          {r.id !== r.name && <div className="text-xs text-muted-foreground font-mono truncate">{r.id}</div>}
        </div>
      ),
    },
    {
      id: "permissions",
      header: "Permissions",
      cell: (r) => (
        <span title={r.permissions.join("\n")} className="tabular-nums">
          {r.permissions.length}
        </span>
      ),
    },
    { id: "tenant", header: "Tenant", cell: (r) => r.tenantId || <span className="text-muted-foreground">–</span> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-24",
      className: "text-right",
      cell: (r) => (
        <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          {canUpdate && (
            <Button variant="ghost" size="icon-sm" aria-label={`Edit ${r.name}`} onClick={() => setDialog({ open: true, role: r })}>
              <Pencil />
            </Button>
          )}
          {canDelete && (
            <Button variant="ghost" size="icon-sm" aria-label={`Delete ${r.name}`} disabled={impact.isPending} onClick={() => void onDelete(r)}>
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
        title="Roles"
        description="Named sets of permissions granted to users and applications."
        actions={
          canCreate && (
            <Button onClick={() => setDialog({ open: true })}>
              <Plus />
              Create role
            </Button>
          )
        }
      >
        <SearchInput value={search} onChange={setSearch} placeholder="Filter by name or permission…" className="max-w-sm" />
      </PageHeader>
      <ErrorAlert error={roles.error} title="Roles could not be loaded" />
      <DataTable columns={columns} rows={rows} rowKey={(r) => r.id} isLoading={roles.isLoading} emptyMessage={search ? "No roles match the filter." : "No roles yet."} />

      <RoleDialog open={dialog.open} onOpenChange={(open) => setDialog((d) => ({ ...d, open }))} role={dialog.role} />
      <DeletionBlockedDialog state={blocked} onClose={() => setBlocked(null)} />
    </div>
  );
}
