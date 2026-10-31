"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { Checkbox } from "@elsa-workflows/react-studio-ui/checkbox";
import { Label } from "@elsa-workflows/react-studio-ui/label";
import type { IdentityRole } from "../api";

/** Multi-select over roles rendered as a compact checkbox list. */
export function RoleChecklist({
  roles,
  selected,
  onChange,
  disabled,
}: {
  roles: IdentityRole[];
  selected: string[];
  onChange: (next: string[]) => void;
  disabled?: boolean;
}) {
  const _t = useTranslation();
  if (roles.length === 0) return <p className="text-xs text-muted-foreground">{_t("No roles defined yet.")}</p>;

  const toggle = (name: string, checked: boolean) => {
    const set = new Set(selected);
    if (checked) set.add(name);
    else set.delete(name);
    onChange([...set]);
  };

  return (
    <div className="max-h-48 overflow-y-auto rounded-md border p-2 flex flex-col gap-1.5">
      {roles.map((role) => {
        const id = `role-${role.id}`;
        return (
          <div key={role.id} className="flex items-center gap-2">
            <Checkbox
              id={id}
              checked={selected.includes(role.name)}
              onCheckedChange={(checked) => toggle(role.name, checked === true)}
              disabled={disabled}
            />
            <Label htmlFor={id} className="font-normal">
              {role.name}
              {role.permissions.length > 0 && (
                <span className="text-xs text-muted-foreground">
                  ({role.permissions.length} {_t("permission")}{role.permissions.length === 1 ? "" : _t("s")})
                </span>
              )}
            </Label>
          </div>
        );
      })}
    </div>
  );
}
