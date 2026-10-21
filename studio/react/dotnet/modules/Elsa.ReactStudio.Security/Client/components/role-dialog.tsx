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
import type { IdentityRole } from "../api";
import { useCreateRole, usePermissionCatalog, useUpdateRole } from "../hooks";
import { PermissionEditor } from "./permission-editor";

export function RoleDialog({ open, onOpenChange, role }: { open: boolean; onOpenChange: (open: boolean) => void; role?: IdentityRole }) {
  useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <RoleForm key={role?.id ?? "new"} role={role} close={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function RoleForm({ role, close }: { role?: IdentityRole; close: () => void }) {
  const _t = useTranslation();
  const isEdit = !!role;
  const [name, setName] = useState(role?.name ?? "");
  const [permissions, setPermissions] = useState<string[]>(role?.permissions ?? []);
  const catalog = usePermissionCatalog();
  const create = useCreateRole();
  const update = useUpdateRole();
  const pending = create.isPending || update.isPending;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      if (role) {
        await update.mutateAsync({ id: role.id, name: name.trim(), permissions });
        toast.success(_t("Role \"{{value0}}\" updated", { value0: name.trim() }));
      } else {
        const created = await create.mutateAsync({ name: name.trim(), permissions });
        toast.success(_t("Role \"{{value0}}\" created", { value0: created.name }));
      }
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>{isEdit ? _t("Edit role {{value0}}", { value0: role.name }) : _t("Create role")}</DialogTitle>
        <DialogDescription>{_t("Roles bundle resource:verb permissions; users and applications inherit every permission of their roles.")}</DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label={_t("Name")} required htmlFor="role-name">
          <Input id="role-name" value={name} onChange={(e) => setName(e.target.value)} required autoComplete="off" />
        </Field>
        {catalog.isError && (
          <p className="text-xs text-destructive">{_t("The permission catalog could not be loaded (")}{describeError(catalog.error)}{_t("); custom tokens still work.")}</p>
        )}
        <PermissionEditor value={permissions} onChange={setPermissions} categories={catalog.categories} isLoading={catalog.isLoading} disabled={pending} />
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={pending}>
          {_t("Cancel")}</Button>
        <Button type="submit" disabled={pending || !name.trim()}>
          {pending && <Loader2 className="animate-spin" />}
          {isEdit ? _t("Save") : _t("Create")}
        </Button>
      </DialogFooter>
    </form>
  );
}
