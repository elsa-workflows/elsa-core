"use client";

import { useQuery } from "@tanstack/react-query";
import { SimpleSelect } from "@/components/common/simple-select";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function WorkflowDefinitionPickerEditor(props: UiHintEditorProps) {
  const client = useElsaClient();
  const scope = useQueryScope();
  const { data } = useQuery({
    queryKey: [scope, "workflow-definitions", "picker"],
    queryFn: ({ signal }) => client.workflowDefinitions.list({ versionOptions: "Published", pageSize: 200, orderBy: "Name" }, signal),
    staleTime: 60_000,
  });
  const options = (data?.items ?? []).map((d) => ({ value: d.definitionId, label: d.name ?? d.definitionId }));
  return (
    <ExpressionEditor {...props}>
      {({ value, onChange }) => (
        <SimpleSelect value={typeof value === "string" ? value : undefined} allowEmpty disabled={props.readOnly} options={options} onChange={(v) => onChange(v)} />
      )}
    </ExpressionEditor>
  );
}
