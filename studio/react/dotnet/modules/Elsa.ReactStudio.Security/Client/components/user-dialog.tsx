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
import type { IdentityRole, IdentityUser } from "../api";
import { useCreateUser, useUpdateUser } from "../hooks";
import { RoleChecklist } from "./role-checklist";

interface UserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** When set the dialog edits this user; otherwise it creates a new one. */
  user?: IdentityUser;
  roles: IdentityRole[];
  /** Receives the created user (including the one-time `generatedPassword`, if any). */
  onCreated?: (user: IdentityUser) => void;
}

export function UserDialog({ open, onOpenChange, user, roles, onCreated }: UserDialogProps) {
  useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        {/* The form is mounted per open so its state starts from the current user. */}
        <UserForm key={user?.id ?? "new"} user={user} roles={roles} onCreated={onCreated} close={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function UserForm({ user, roles, onCreated, close }: Pick<UserDialogProps, "user" | "roles" | "onCreated"> & { close: () => void }) {
  const _t = useTranslation();
  const isEdit = !!user;
  const [name, setName] = useState(user?.name ?? "");
  const [password, setPassword] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>(user?.roles ?? []);
  const create = useCreateUser();
  const update = useUpdateUser();
  const pending = create.isPending || update.isPending;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    try {
      if (user) {
        await update.mutateAsync({ id: user.id, password: password || undefined, roles: selectedRoles });
        toast.success(_t("User \"{{value0}}\" updated", { value0: user.name }));
      } else {
        const created = await create.mutateAsync({ name: name.trim(), password: password || undefined, roles: selectedRoles });
        toast.success(_t("User \"{{value0}}\" created", { value0: created.name }));
        onCreated?.(created);
      }
      close();
    } catch (err) {
      toast.error(describeError(err));
    }
  };

  return (
    <form onSubmit={submit} className="contents">
      <DialogHeader>
        <DialogTitle>{isEdit ? _t("Edit {{value0}}", { value0: user.name }) : _t("Create user")}</DialogTitle>
        <DialogDescription>
          {isEdit ? _t("Change the password or the roles assigned to this user.") : _t("Add a user account that can sign in to the studio and the API.")}
        </DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label={_t("Name")} required htmlFor="user-name">
          <Input id="user-name" value={name} onChange={(e) => setName(e.target.value)} disabled={isEdit} autoComplete="off" required={!isEdit} />
        </Field>
        <Field
          label={isEdit ? _t("New password") : _t("Password")}
          htmlFor="user-password"
          hint={isEdit ? _t("Leave blank to keep the current password.") : _t("Leave blank to let the server generate one; it is shown once after creation.")}
        >
          <Input id="user-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
        </Field>
        <Field label={_t("Roles")}>
          <RoleChecklist roles={roles} selected={selectedRoles} onChange={setSelectedRoles} disabled={pending} />
        </Field>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={pending}>
          {_t("Cancel")}</Button>
        <Button type="submit" disabled={pending || (!isEdit && !name.trim())}>
          {pending && <Loader2 className="animate-spin" />}
          {isEdit ? _t("Save") : _t("Create")}
        </Button>
      </DialogFooter>
    </form>
  );
}
