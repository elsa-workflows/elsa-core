import { useMemo, type ComponentProps } from "react";
import { useQuery } from "@tanstack/react-query";
import { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { CodeEditor } from "@elsa-workflows/react-studio-ui/common/code-editor";
import type { UiHintContext } from "./types";
import { parseServerTypes, typedCompletions } from "./type-completions";

const emptyTypes = parseServerTypes("");

/** Saved server type information augments the current, possibly unsaved workflow members. */
export function ExpressionCodeEditor({ context, propertyName, ...props }: ComponentProps<typeof CodeEditor> & { context?: UiHintContext; propertyName: string }) {
  const client = useElsaClient();
  const scope = useQueryScope();
  const language = props.language ?? "plaintext";
  const definitionId = context?.workflowDefinitionId;
  const activityType = context?.activityType;
  const query = useQuery({ queryKey: [scope, "expression-types", definitionId, activityType, propertyName],
    enabled: !!definitionId && ["javascript", "typescript", "csharp", "python"].includes(language),
    queryFn: ({ signal }) => client.descriptors.javascriptTypeDefinitions(definitionId!, activityType, propertyName, signal),
    select: parseServerTypes, staleTime: 60_000, retry: false });
  const variables = context?.variables;
  const inputs = context?.inputs;
  const activities = context?.activities;
  const completions = useMemo(() => typedCompletions(variables ? { variables, inputs, activities } : undefined, language, query.data ?? emptyTypes),
    [variables, inputs, activities, language, query.data]);
  return <>
    <CodeEditor {...props} completions={completions} />
    {query.isError && <p className="text-xs text-muted-foreground" role="status">Server type information is unavailable. Workflow member completion remains available.</p>}
  </>;
}
