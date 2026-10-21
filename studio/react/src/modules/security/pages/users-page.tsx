"use client";

import { Pencil, Plus, Trash2 } from "lucide-react";
import { useMemo, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/common/confirm-dialog";
import { DataTable, type DataTableColumn } from "@/components/common/data-table";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { PageHeader } from "@/components/common/page-header";
import { SearchInput } from "@/components/common/search-input";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Resources, usePermission, Verbs } from "@/lib/permissions";
import type { IdentityUser } from "../api";
import { useDeleteUser, useRoles, useUsers } from "../hooks";
import { GeneratedPasswordDialog } from "../components/generated-password-dialog";
import { UserDialog } from "../components/user-dialog";

export function UsersPage() {
  const users = useUsers();
  const roles = useRoles();
  const remove = useDeleteUser();
  const confirm = useConfirm();
  const canCreate = usePermission(Resources.users, Verbs.create);
  const canUpdate = usePermission(Resources.users, Verbs.update);
  const canDelete = usePermission(Resources.users, Verbs.delete);

  const [search, setSearch] = useState("");
  const [dialog, setDialog] = useState<{ open: boolean; user?: IdentityUser }>({ open: false });
  const [generated, setGenerated] = useState<{ name: string; password: string } | null>(null);

  const rows = useMemo(() => {
    const list = users.data ?? [];
    const q = search.trim().toLowerCase();
    const filtered = q
      ? list.filter((u) => u.name.toLowerCase().includes(q) || u.roles.some((r) => r.toLowerCase().includes(q)) || (u.tenantId ?? "").toLowerCase().includes(q))
      : list;
    return [...filtered].sort((a, b) => a.name.localeCompare(b.name));
  }, [users.data, search]);

  const onDelete = async (user: IdentityUser) => {
    const ok = await confirm({
      title: `Delete user "${user.name}"?`,
      description: "The account is removed permanently and any API keys issued to it stop working.",
      confirmLabel: "Delete",
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(user.id);
      toast.success(`User "${user.name}" deleted`);
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<IdentityUser>[] = [
    { id: "name", header: "Name", cell: (u) => <span className="font-medium">{u.name}</span> },
    {
      id: "roles",
      header: "Roles",
      cell: (u) =>
        u.roles.length === 0 ? (
          <span className="text-muted-foreground">–</span>
        ) : (
          <div className="flex flex-wrap gap-1">
            {u.roles.map((r) => (
              <Badge key={r} variant="secondary">
                {r}
              </Badge>
            ))}
          </div>
        ),
    },
    { id: "tenant", header: "Tenant", cell: (u) => u.tenantId || <span className="text-muted-foreground">–</span> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-24",
      className: "text-right",
      cell: (u) => (
        <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          {canUpdate && (
            <Button variant="ghost" size="icon-sm" aria-label={`Edit ${u.name}`} onClick={() => setDialog({ open: true, user: u })}>
              <Pencil />
            </Button>
          )}
          {canDelete && (
            <Button variant="ghost" size="icon-sm" aria-label={`Delete ${u.name}`} onClick={() => void onDelete(u)}>
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
        title="Users"
        description="Accounts that can sign in to the studio and call the API."
        actions={
          canCreate && (
            <Button onClick={() => setDialog({ open: true })}>
              <Plus />
              Create user
            </Button>
          )
        }
      >
        <SearchInput value={search} onChange={setSearch} placeholder="Filter by name, role or tenant…" className="max-w-sm" />
      </PageHeader>
      <ErrorAlert error={users.error} title="Users could not be loaded" />
      <DataTable columns={columns} rows={rows} rowKey={(u) => u.id} isLoading={users.isLoading} emptyMessage={search ? "No users match the filter." : "No users yet."} />

      <UserDialog
        open={dialog.open}
        onOpenChange={(open) => setDialog((d) => ({ ...d, open }))}
        user={dialog.user}
        roles={roles.data ?? []}
        onCreated={(created) => created.generatedPassword && setGenerated({ name: created.name, password: created.generatedPassword })}
      />
      <GeneratedPasswordDialog userName={generated?.name ?? ""} password={generated?.password} onClose={() => setGenerated(null)} />
    </div>
  );
}
