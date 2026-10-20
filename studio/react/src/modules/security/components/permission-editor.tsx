"use client";

import { Plus, X } from "lucide-react";
import { useMemo, useState } from "react";
import { Field } from "@/components/common/field";
import { SimpleSelect } from "@/components/common/simple-select";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ANY_VERB, permissionToken, splitPermissionToken, type PermissionCategory } from "../api";

/**
 * Edits a list of `resource:verb` tokens. The user picks a category, then a resource, and toggles
 * the verbs the server reports for it (plus the `*` wildcard). Tokens the catalog does not know
 * about (wildcards such as `workflows/*:view`) can be typed in by hand and are kept as-is.
 */
export function PermissionEditor({
  value,
  onChange,
  categories,
  isLoading,
  disabled,
}: {
  value: string[];
  onChange: (next: string[]) => void;
  categories: PermissionCategory[];
  isLoading?: boolean;
  disabled?: boolean;
}) {
  const [category, setCategory] = useState<string | undefined>();
  const [resource, setResource] = useState<string | undefined>();
  const [custom, setCustom] = useState("");

  const activeCategory = useMemo(() => categories.find((c) => c.category === category), [categories, category]);
  const descriptor = useMemo(() => activeCategory?.resources.find((r) => r.resource === resource), [activeCategory, resource]);
  const tokens = useMemo(() => new Set(value), [value]);

  const setToken = (token: string, present: boolean) => {
    const next = new Set(value);
    if (present) next.add(token);
    else next.delete(token);
    onChange([...next]);
  };

  const addCustom = () => {
    const token = custom.trim();
    if (!token || !splitPermissionToken(token)) return;
    setToken(token, true);
    setCustom("");
  };

  const sorted = useMemo(() => [...value].sort((a, b) => a.localeCompare(b)), [value]);
  const verbs = descriptor ? [...descriptor.supportedVerbs, ANY_VERB] : [];

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Category">
          <SimpleSelect
            value={category}
            onChange={(v) => {
              setCategory(v);
              setResource(undefined);
            }}
            options={categories.map((c) => ({ value: c.category, label: c.category }))}
            placeholder={isLoading ? "Loading…" : "Pick a category"}
            disabled={disabled || isLoading || categories.length === 0}
          />
        </Field>
        <Field label="Resource">
          <SimpleSelect
            value={resource}
            onChange={setResource}
            options={(activeCategory?.resources ?? []).map((r) => ({ value: r.resource, label: r.displayName, description: r.resource }))}
            placeholder={activeCategory ? "Pick a resource" : "Pick a category first"}
            disabled={disabled || !activeCategory}
          />
        </Field>
      </div>

      {descriptor && (
        <div className="rounded-md border p-3 flex flex-col gap-2">
          <div className="text-sm">
            <span className="font-medium">{descriptor.displayName}</span>{" "}
            <code className="text-xs text-muted-foreground">{descriptor.resource}</code>
          </div>
          {descriptor.description && <p className="text-xs text-muted-foreground">{descriptor.description}</p>}
          <div className="flex flex-wrap gap-x-4 gap-y-2 pt-1">
            {verbs.map((verb) => {
              const token = permissionToken(descriptor.resource, verb);
              const id = `perm-${token}`;
              return (
                <div key={verb} className="flex items-center gap-2">
                  <Checkbox id={id} checked={tokens.has(token)} onCheckedChange={(c) => setToken(token, c === true)} disabled={disabled} />
                  <Label htmlFor={id} className="font-normal">
                    {verb === ANY_VERB ? "all verbs (*)" : verb}
                    {descriptor.nonCoreVerbs.includes(verb) && <span className="text-xs text-muted-foreground">(module)</span>}
                  </Label>
                </div>
              );
            })}
          </div>
        </div>
      )}

      <Field label="Custom token" hint="For wildcards the catalog cannot express, e.g. workflows/*:view or *:*.">
        <div className="flex gap-2">
          <Input
            value={custom}
            onChange={(e) => setCustom(e.target.value)}
            placeholder="resource:verb"
            disabled={disabled}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                addCustom();
              }
            }}
          />
          <Button type="button" variant="outline" onClick={addCustom} disabled={disabled || !splitPermissionToken(custom.trim())}>
            <Plus />
            Add
          </Button>
        </div>
      </Field>

      <Field label={`Granted permissions (${value.length})`}>
        {sorted.length === 0 ? (
          <p className="text-xs text-muted-foreground">No permissions granted. Users in this role can only access what every principal can.</p>
        ) : (
          <div className="flex flex-wrap gap-1.5 max-h-40 overflow-y-auto">
            {sorted.map((token) => (
              <Badge key={token} variant="secondary" className="font-mono gap-1 pr-1">
                {token}
                <button
                  type="button"
                  className="rounded-full hover:bg-foreground/10 p-0.5"
                  aria-label={`Remove ${token}`}
                  onClick={() => setToken(token, false)}
                  disabled={disabled}
                >
                  <X className="size-3" />
                </button>
              </Badge>
            ))}
          </div>
        )}
      </Field>
    </div>
  );
}
