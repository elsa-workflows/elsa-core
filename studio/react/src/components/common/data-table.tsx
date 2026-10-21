"use client";

import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  ChevronLeft,
  ChevronRight,
  Columns3,
  Loader2,
} from "lucide-react";
import { useSyncExternalStore, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { SimpleSelect } from "./simple-select";
import { cn } from "@/lib/utils";
import type { OrderDirection } from "@/lib/api/types/common";

export interface DataTableColumn<T> {
  id: string;
  header: ReactNode;
  cell: (row: T) => ReactNode;
  /** When set the header is clickable and emits `onSortChange(sortKey)`. */
  sortKey?: string;
  className?: string;
  headerClassName?: string;
  width?: string;
  /** Columns that can be hidden from the column menu; `header` must then be a string. */
  hideable?: boolean;
}

export interface SortState {
  key: string;
  direction: OrderDirection;
}

export interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  isLoading?: boolean;
  emptyMessage?: ReactNode;
  /** Optional selection; omit to hide the checkbox column. */
  selection?: { selected: Set<string>; onChange: (next: Set<string>) => void };
  sort?: SortState;
  onSortChange?: (sort: SortState) => void;
  /** Server-side paging. `page` is 0-based. */
  paging?: {
    page: number;
    pageSize: number;
    totalCount: number;
    onChange: (page: number, pageSize: number) => void;
  };
  onRowClick?: (row: T) => void;
  rowClassName?: (row: T) => string | undefined;
  className?: string;
  /** When set, a "Columns" menu lets users hide `hideable` columns; the choice is remembered under this key. */
  columnsStorageKey?: string;
  /** Rendered next to the column menu (e.g. filter chips). */
  toolbar?: ReactNode;
}

const COLUMNS_EVENT = "elsa-studio:columns-changed";

function storageKey(key: string) {
  return `elsa-studio:columns:${key}`;
}

function subscribeHidden(callback: () => void) {
  window.addEventListener(COLUMNS_EVENT, callback);
  window.addEventListener("storage", callback);
  return () => {
    window.removeEventListener(COLUMNS_EVENT, callback);
    window.removeEventListener("storage", callback);
  };
}

/** Hidden column ids as the raw stored string, so snapshots stay referentially stable. */
function readHiddenRaw(key: string | undefined): string {
  if (!key) return "[]";
  try {
    return localStorage.getItem(storageKey(key)) ?? "[]";
  } catch {
    return "[]";
  }
}

function writeHidden(key: string, hidden: string[]) {
  try {
    localStorage.setItem(storageKey(key), JSON.stringify(hidden));
  } catch {
    /* storage unavailable */
  }
  window.dispatchEvent(new Event(COLUMNS_EVENT));
}

const PAGE_SIZES = ["10", "25", "50", "100"] as const;

function parseHidden(raw: string): string[] {
  try {
    const parsed = JSON.parse(raw) as unknown;
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch {
    return [];
  }
}

/** Server-driven table: sorting, paging and selection are controlled by the caller. */
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  isLoading,
  emptyMessage = "No results.",
  selection,
  sort,
  onSortChange,
  paging,
  onRowClick,
  rowClassName,
  className,
  columnsStorageKey,
  toolbar,
}: DataTableProps<T>) {
  const hiddenRaw = useSyncExternalStore(
    subscribeHidden,
    () => readHiddenRaw(columnsStorageKey),
    () => "[]",
  );
  const hidden = parseHidden(hiddenRaw);
  const toggleHidden = (id: string) => {
    if (!columnsStorageKey) return;
    writeHidden(
      columnsStorageKey,
      hidden.includes(id) ? hidden.filter((h) => h !== id) : [...hidden, id],
    );
  };
  const hideable = columns.filter((c) => c.hideable);
  const visibleColumns = columns.filter((c) => !hidden.includes(c.id));
  const allKeys = rows.map(rowKey);
  const allSelected =
    !!selection &&
    allKeys.length > 0 &&
    allKeys.every((k) => selection.selected.has(k));
  const someSelected =
    !!selection && allKeys.some((k) => selection.selected.has(k));

  const toggleAll = () => {
    if (!selection) return;
    const next = new Set(selection.selected);
    if (allSelected) allKeys.forEach((k) => next.delete(k));
    else allKeys.forEach((k) => next.add(k));
    selection.onChange(next);
  };

  const toggleOne = (key: string) => {
    if (!selection) return;
    const next = new Set(selection.selected);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    selection.onChange(next);
  };

  const sortIcon = (key?: string) => {
    if (!key) return null;
    if (sort?.key !== key)
      return <ArrowUpDown className="size-3.5 opacity-50" />;
    return sort.direction === "Ascending" ? (
      <ArrowUp className="size-3.5" />
    ) : (
      <ArrowDown className="size-3.5" />
    );
  };

  const cycleSort = (key: string) => {
    if (!onSortChange) return;
    const direction: OrderDirection =
      sort?.key === key && sort.direction === "Ascending"
        ? "Descending"
        : "Ascending";
    onSortChange({ key, direction });
  };

  const totalPages = paging
    ? Math.max(1, Math.ceil(paging.totalCount / paging.pageSize))
    : 1;

  return (
    <div className={cn("flex flex-col gap-3", className)}>
      {(toolbar || (columnsStorageKey && hideable.length > 0)) && (
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex min-w-0 flex-1 flex-wrap items-center gap-2">
            {toolbar}
          </div>
          {columnsStorageKey && hideable.length > 0 && (
            <DropdownMenu>
              <DropdownMenuTrigger
                render={<Button variant="outline" size="sm" />}
              >
                <Columns3 /> Columns
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuGroup>
                  <DropdownMenuLabel className="eyebrow">
                    Show columns
                  </DropdownMenuLabel>
                  {hideable.map((c) => (
                    <DropdownMenuCheckboxItem
                      key={c.id}
                      checked={!hidden.includes(c.id)}
                      onCheckedChange={() => toggleHidden(c.id)}
                    >
                      {typeof c.header === "string" ? c.header : c.id}
                    </DropdownMenuCheckboxItem>
                  ))}
                </DropdownMenuGroup>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </div>
      )}
      <div className="node-card overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow>
              {selection && (
                <TableHead className="w-8 bg-muted/60">
                  <Checkbox
                    checked={allSelected}
                    indeterminate={!allSelected && someSelected}
                    onCheckedChange={toggleAll}
                    aria-label="Select all"
                  />
                </TableHead>
              )}
              {visibleColumns.map((c) => (
                <TableHead
                  key={c.id}
                  className={cn(
                    "bg-muted/60 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground",
                    c.headerClassName,
                  )}
                  style={c.width ? { width: c.width } : undefined}
                >
                  {c.sortKey ? (
                    <button
                      type="button"
                      className="inline-flex items-center gap-1 hover:text-foreground"
                      onClick={() => cycleSort(c.sortKey!)}
                    >
                      {c.header}
                      {sortIcon(c.sortKey)}
                    </button>
                  ) : (
                    c.header
                  )}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading && rows.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={visibleColumns.length + (selection ? 1 : 0)}
                  className="h-24 text-center text-muted-foreground"
                >
                  <Loader2 className="inline size-4 animate-spin mr-2" />
                  Loading…
                </TableCell>
              </TableRow>
            ) : rows.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={visibleColumns.length + (selection ? 1 : 0)}
                  className="h-24 text-center text-muted-foreground"
                >
                  {emptyMessage}
                </TableCell>
              </TableRow>
            ) : (
              rows.map((row) => {
                const key = rowKey(row);
                return (
                  <TableRow
                    key={key}
                    data-state={
                      selection?.selected.has(key) ? "selected" : undefined
                    }
                    className={cn(
                      onRowClick && "cursor-pointer",
                      rowClassName?.(row),
                    )}
                    onClick={onRowClick ? () => onRowClick(row) : undefined}
                  >
                    {selection && (
                      <TableCell
                        className="w-8"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <Checkbox
                          checked={selection.selected.has(key)}
                          onCheckedChange={() => toggleOne(key)}
                          aria-label="Select row"
                        />
                      </TableCell>
                    )}
                    {visibleColumns.map((c) => (
                      <TableCell key={c.id} className={c.className}>
                        {c.cell(row)}
                      </TableCell>
                    ))}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>
      {paging && (
        <div className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
          <div className="flex items-center gap-2">
            <span>Rows per page</span>
            <SimpleSelect
              size="sm"
              className="w-20"
              value={String(paging.pageSize) as (typeof PAGE_SIZES)[number]}
              options={PAGE_SIZES.map((s) => ({ value: s, label: s }))}
              onChange={(v) => v && paging.onChange(0, Number(v))}
            />
            {isLoading && <Loader2 className="size-4 animate-spin" />}
          </div>
          <div className="flex items-center gap-2">
            <span>
              {paging.totalCount === 0 ? 0 : paging.page * paging.pageSize + 1}–
              {Math.min((paging.page + 1) * paging.pageSize, paging.totalCount)}{" "}
              of {paging.totalCount}
            </span>
            <Button
              variant="outline"
              size="icon-sm"
              disabled={paging.page === 0}
              onClick={() => paging.onChange(paging.page - 1, paging.pageSize)}
              aria-label="Previous page"
            >
              <ChevronLeft />
            </Button>
            <span className="tabular-nums">
              {paging.page + 1} / {totalPages}
            </span>
            <Button
              variant="outline"
              size="icon-sm"
              disabled={paging.page + 1 >= totalPages}
              onClick={() => paging.onChange(paging.page + 1, paging.pageSize)}
              aria-label="Next page"
            >
              <ChevronRight />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
