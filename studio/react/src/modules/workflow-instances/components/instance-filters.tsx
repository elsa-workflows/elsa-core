"use client";

import { SearchInput } from "@/components/common/search-input";
import { SimpleSelect, type SelectOption } from "@/components/common/simple-select";
import type { TimestampFilter, WorkflowStatus, WorkflowSubStatus } from "@/lib/api/types/workflow-instances";
import { WORKFLOW_STATUSES, WORKFLOW_SUB_STATUSES } from "../model";
import { DateFilterPopover } from "./date-filter-popover";
import { MultiSelect } from "./multi-select";

export type HasIncidents = "true" | "false";

export interface InstanceFilterValues {
  search: string;
  definitionIds: string[];
  statuses: WorkflowStatus[];
  subStatuses: WorkflowSubStatus[];
  hasIncidents?: HasIncidents;
  timestampFilters: TimestampFilter[];
}

const STATUS_OPTIONS: SelectOption<WorkflowStatus>[] = WORKFLOW_STATUSES.map((s) => ({ value: s, label: s }));
const SUB_STATUS_OPTIONS: SelectOption<WorkflowSubStatus>[] = WORKFLOW_SUB_STATUSES.map((s) => ({ value: s, label: s }));
const INCIDENT_OPTIONS: SelectOption<HasIncidents>[] = [
  { value: "true", label: "With incidents" },
  { value: "false", label: "Without incidents" },
];

export function InstanceFilters({
  value,
  onChange,
  definitionOptions,
}: {
  value: InstanceFilterValues;
  onChange: (patch: Partial<InstanceFilterValues>) => void;
  definitionOptions: SelectOption<string>[];
}) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <SearchInput value={value.search} onChange={(search) => onChange({ search })} placeholder="Search id, correlation id, name…" className="w-64" />
      <MultiSelect label="Workflow" value={value.definitionIds} onChange={(definitionIds) => onChange({ definitionIds })} options={definitionOptions} searchPlaceholder="Search workflows…" />
      <MultiSelect label="Status" value={value.statuses} onChange={(statuses) => onChange({ statuses })} options={STATUS_OPTIONS} />
      <MultiSelect label="Sub-status" value={value.subStatuses} onChange={(subStatuses) => onChange({ subStatuses })} options={SUB_STATUS_OPTIONS} />
      <SimpleSelect
        size="sm"
        className="w-44"
        value={value.hasIncidents}
        options={INCIDENT_OPTIONS}
        placeholder="Incidents: any"
        allowEmpty
        onChange={(hasIncidents) => onChange({ hasIncidents })}
      />
      <DateFilterPopover value={value.timestampFilters} onChange={(timestampFilters) => onChange({ timestampFilters })} />
    </div>
  );
}
