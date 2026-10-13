"use client";

import { Loader2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { describeError } from "@/components/common/error-alert";
import { Field } from "@/components/common/field";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
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
        toast.success(`User "${user.name}" updated`);
      } else {
        const created = await create.mutateAsync({ name: name.trim(), password: password || undefined, roles: selectedRoles });
        toast.success(`User "${created.name}" created`);
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
        <DialogTitle>{isEdit ? `Edit ${user.name}` : "Create user"}</DialogTitle>
        <DialogDescription>
          {isEdit ? "Change the password or the roles assigned to this user." : "Add a user account that can sign in to the studio and the API."}
        </DialogDescription>
      </DialogHeader>
      <div className="flex flex-col gap-4">
        <Field label="Name" required htmlFor="user-name">
          <Input id="user-name" value={name} onChange={(e) => setName(e.target.value)} disabled={isEdit} autoComplete="off" required={!isEdit} />
        </Field>
        <Field
          label={isEdit ? "New password" : "Password"}
          htmlFor="user-password"
          hint={isEdit ? "Leave blank to keep the current password." : "Leave blank to let the server generate one; it is shown once after creation."}
        >
          <Input id="user-password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
        </Field>
        <Field label="Roles">
          <RoleChecklist roles={roles} selected={selectedRoles} onChange={setSelectedRoles} disabled={pending} />
        </Field>
      </div>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={close} disabled={pending}>
          Cancel
        </Button>
        <Button type="submit" disabled={pending || (!isEdit && !name.trim())}>
          {pending && <Loader2 className="animate-spin" />}
          {isEdit ? "Save" : "Create"}
        </Button>
      </DialogFooter>
    </form>
  );
}
