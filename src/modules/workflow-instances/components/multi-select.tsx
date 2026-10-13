"use client";

import { Check, ChevronsUpDown, X } from "lucide-react";
import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import type { SelectOption } from "@/components/common/simple-select";
import { cn } from "@/lib/utils";

/** Popover + searchable checkbox list for selecting several values of a filter. */
export function MultiSelect<T extends string>({
  label,
  value,
  onChange,
  options,
  searchPlaceholder = "Search…",
  emptyMessage = "No results.",
  className,
}: {
  label: string;
  value: T[];
  onChange: (next: T[]) => void;
  options: SelectOption<T>[];
  searchPlaceholder?: string;
  emptyMessage?: string;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const selected = new Set(value);

  const toggle = (v: T) => {
    const next = new Set(selected);
    if (next.has(v)) next.delete(v);
    else next.add(v);
    onChange(options.filter((o) => next.has(o.value)).map((o) => o.value));
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger
        render={<Button variant="outline" size="sm" className={cn("justify-between gap-1.5 font-normal", className)} />}
      >
        <span className="truncate">{label}</span>
        {value.length > 0 && (
          <Badge variant="secondary" className="h-4 px-1.5 text-[10px]">
            {value.length}
          </Badge>
        )}
        <ChevronsUpDown className="size-3.5 opacity-60" />
      </PopoverTrigger>
      <PopoverContent align="start" className="w-64 p-0">
        <Command>
          <CommandInput placeholder={searchPlaceholder} />
          <CommandList>
            <CommandEmpty>{emptyMessage}</CommandEmpty>
            <CommandGroup>
              {options.map((o) => (
                <CommandItem key={o.value} value={o.value} keywords={[o.label]} onSelect={() => toggle(o.value)} className="gap-2">
                  <Checkbox checked={selected.has(o.value)} tabIndex={-1} aria-hidden className="pointer-events-none" />
                  <span className="truncate">{o.label}</span>
                  {selected.has(o.value) && <Check className="ml-auto size-4" />}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
          {value.length > 0 && (
            <div className="border-t p-1">
              <Button variant="ghost" size="sm" className="w-full justify-start" onClick={() => onChange([])}>
                <X className="size-3.5" />
                Clear selection
              </Button>
            </div>
          )}
        </Command>
      </PopoverContent>
    </Popover>
  );
}
