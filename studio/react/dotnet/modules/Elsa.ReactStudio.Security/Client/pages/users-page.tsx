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
import { Badge } from "@elsa-workflows/react-studio-ui/badge";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Resources, usePermission, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import type { IdentityUser } from "../api";
import { useDeleteUser, useRoles, useUsers } from "../hooks";
import { GeneratedPasswordDialog } from "../components/generated-password-dialog";
import { UserDialog } from "../components/user-dialog";

export function UsersPage() {
  const _t = useTranslation();
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
      title: _t("Delete user \"{{value0}}\"?", { value0: user.name }),
      description: _t("The account is removed permanently and any API keys issued to it stop working."),
      confirmLabel: _t("Delete"),
      destructive: true,
    });
    if (!ok) return;
    try {
      await remove.mutateAsync(user.id);
      toast.success(_t("User \"{{value0}}\" deleted", { value0: user.name }));
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  const columns: DataTableColumn<IdentityUser>[] = [
    { id: "name", header: _t("Name"), cell: (u) => <span className="font-medium">{u.name}</span> },
    {
      id: "roles",
      header: _t("Roles"),
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
    { id: "tenant", header: _t("Tenant"), cell: (u) => u.tenantId || <span className="text-muted-foreground">–</span> },
    {
      id: "actions",
      header: "",
      headerClassName: "w-24",
      className: "text-right",
      cell: (u) => (
        <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          {canUpdate && (
            <Button variant="ghost" size="icon-sm" aria-label={_t("Edit {{value0}}", { value0: u.name })} onClick={() => setDialog({ open: true, user: u })}>
              <Pencil />
            </Button>
          )}
          {canDelete && (
            <Button variant="ghost" size="icon-sm" aria-label={_t("Delete {{value0}}", { value0: u.name })} onClick={() => void onDelete(u)}>
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
        title={_t("Users")}
        description={_t("Accounts that can sign in to the studio and call the API.")}
        actions={
          canCreate && (
            <Button onClick={() => setDialog({ open: true })}>
              <Plus />
              {_t("Create user")}</Button>
          )
        }
      >
        <SearchInput value={search} onChange={setSearch} placeholder={_t("Filter by name, role or tenant…")} className="max-w-sm" />
      </PageHeader>
      <ErrorAlert error={users.error} title={_t("Users could not be loaded")} />
      <DataTable columns={columns} rows={rows} rowKey={(u) => u.id} isLoading={users.isLoading} emptyMessage={search ? _t("No users match the filter.") : _t("No users yet.")} />

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
