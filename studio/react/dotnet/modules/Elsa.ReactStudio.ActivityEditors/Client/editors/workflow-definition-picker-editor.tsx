"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useQuery } from "@tanstack/react-query";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import type { UiHintEditorProps } from "../types";
import { ExpressionEditor } from "../expression-editor";

export function WorkflowDefinitionPickerEditor(props: UiHintEditorProps) {
  useTranslation();
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
