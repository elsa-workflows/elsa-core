"use client";

import { format, isValid, parseISO } from "date-fns";
import { CalendarClock, Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { SimpleSelect, type SelectOption } from "@/components/common/simple-select";
import type { TimestampFilter } from "@/lib/api/types/workflow-instances";

type Column = TimestampFilter["column"];
type Operator = TimestampFilter["operator"];

const COLUMNS: SelectOption<Column>[] = [
  { value: "CreatedAt", label: "Created" },
  { value: "UpdatedAt", label: "Updated" },
  { value: "FinishedAt", label: "Finished" },
];

const OPERATORS: SelectOption<Operator>[] = [
  { value: "Is", label: "is" },
  { value: "IsNot", label: "is not" },
  { value: "LessThan", label: "before" },
  { value: "LessThanOrEqual", label: "on or before" },
  { value: "GreaterThan", label: "after" },
  { value: "GreaterThanOrEqual", label: "on or after" },
];

interface DraftRow {
  column: Column;
  operator: Operator;
  /** Value of the `datetime-local` input (local time, no zone). */
  local: string;
}

function toDraft(filters: TimestampFilter[]): DraftRow[] {
  return filters.map((f) => {
    const d = parseISO(f.timestamp);
    return { column: f.column, operator: f.operator, local: isValid(d) ? format(d, "yyyy-MM-dd'T'HH:mm") : "" };
  });
}

function toFilters(rows: DraftRow[]): TimestampFilter[] {
  return rows
    .filter((r) => r.local)
    .map((r) => ({ column: r.column, operator: r.operator, timestamp: new Date(r.local).toISOString() }))
    .filter((f) => f.timestamp !== "Invalid Date");
}

const newRow = (): DraftRow => ({ column: "CreatedAt", operator: "GreaterThanOrEqual", local: "" });

/** Repeatable column / operator / date-time rows that become `timestampFilters` on Apply. */
export function DateFilterPopover({ value, onChange }: { value: TimestampFilter[]; onChange: (next: TimestampFilter[]) => void }) {
  const [open, setOpen] = useState(false);
  const [rows, setRows] = useState<DraftRow[]>(() => (value.length ? toDraft(value) : [newRow()]));

  const update = (index: number, patch: Partial<DraftRow>) => setRows((r) => r.map((row, i) => (i === index ? { ...row, ...patch } : row)));

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) setRows(value.length ? toDraft(value) : [newRow()]);
        setOpen(next);
      }}
    >
      <PopoverTrigger render={<Button variant="outline" size="sm" className="gap-1.5 font-normal" />}>
        <CalendarClock className="size-3.5" />
        Dates
        {value.length > 0 && (
          <Badge variant="secondary" className="h-4 px-1.5 text-[10px]">
            {value.length}
          </Badge>
        )}
      </PopoverTrigger>
      <PopoverContent align="start" className="w-[32rem] max-w-[calc(100vw-2rem)]">
        <div className="flex flex-col gap-2">
          {rows.map((row, i) => (
            <div key={i} className="grid grid-cols-[7rem_8rem_1fr_auto] items-center gap-1.5">
              <SimpleSelect size="sm" value={row.column} options={COLUMNS} onChange={(v) => v && update(i, { column: v })} />
              <SimpleSelect size="sm" value={row.operator} options={OPERATORS} onChange={(v) => v && update(i, { operator: v })} />
              <Input type="datetime-local" className="h-7 text-xs" value={row.local} onChange={(e) => update(i, { local: e.target.value })} />
              <Button variant="ghost" size="icon-sm" aria-label="Remove" onClick={() => setRows((r) => r.filter((_, j) => j !== i))}>
                <Trash2 />
              </Button>
            </div>
          ))}
          {rows.length === 0 && <p className="text-xs text-muted-foreground">No date filters.</p>}
        </div>
        <div className="flex items-center justify-between gap-2 pt-1">
          <Button variant="ghost" size="sm" onClick={() => setRows((r) => [...r, newRow()])}>
            <Plus className="size-3.5" />
            Add
          </Button>
          <div className="flex items-center gap-1.5">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setRows([]);
                onChange([]);
                setOpen(false);
              }}
            >
              Clear
            </Button>
            <Button
              size="sm"
              onClick={() => {
                onChange(toFilters(rows));
                setOpen(false);
              }}
            >
              Apply
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
