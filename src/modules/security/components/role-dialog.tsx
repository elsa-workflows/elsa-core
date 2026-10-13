"use client";

import { Loader2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { describeError } from "@/components/common/error-alert";
import { Field } from "@/components/common/field";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import type { IdentityRole } from "../api";
import { useCreateRole, usePermissionCatalog, useUpdateRole } from "../hooks";
import { PermissionEditor } from "./permission-editor";

export function RoleDialog({ open, onOpenChange, role }: { open: boolean; onOpenChange: (open: boolean) => void; role?: IdentityRole }) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <RoleForm key={role?.id ?? "new"} role={role} close={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function RoleForm({ role, close }: { role?: IdentityRole; close: () => void }) {
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
        toast.success(`Role "${name.trim()}" updated`);
      } else {
        const created = await create.mutateAsync({ name: name.trim(), permissions });
        toast.success(`Role "${created.name}" created`);
      }
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>{isEdit ? `Edit role ${role.name}` : "Create role"}</DialogTitle>
        <DialogDescription>Roles bundle resource:verb permissions; users and applications inherit every permission of their roles.</DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label="Name" required htmlFor="role-name">
          <Input id="role-name" value={name} onChange={(e) => setName(e.target.value)} required autoComplete="off" />
        </Field>
        {catalog.isError && (
          <p className="text-xs text-destructive">The permission catalog could not be loaded ({describeError(catalog.error)}); custom tokens still work.</p>
        )}
        <PermissionEditor value={permissions} onChange={setPermissions} categories={catalog.categories} isLoading={catalog.isLoading} disabled={pending} />
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={pending}>
          Cancel
        </Button>
        <Button type="submit" disabled={pending || !name.trim()}>
          {pending && <Loader2 className="animate-spin" />}
          {isEdit ? "Save" : "Create"}
        </Button>
      </DialogFooter>
    </form>
  );
}
