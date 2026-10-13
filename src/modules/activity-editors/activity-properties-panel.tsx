"use client";

import { createElement, useMemo, useState } from "react";
import { Info } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { Field } from "@/components/common/field";
import { KeyValueTable } from "@/components/common/key-value-table";
import { SimpleSelect } from "@/components/common/simple-select";
import { EmptyState } from "@/components/common/empty-state";
import { useExpressionDescriptors } from "@/lib/api/hooks/descriptors";
import type { Activity, ActivityInput, Variable } from "@/lib/api/types/activities";
import type { InputDescriptor, OutputDescriptor } from "@/lib/api/types/descriptors";
import type { OutputDefinition } from "@/lib/api/types/workflow-definitions";
import { useExtensions } from "@/lib/modules/registry";
import { useDesigner, useDesignerState } from "@/modules/designer/context";
import { activityDisplayText } from "@/modules/designer/model/display";
import { selectableSyntaxes } from "./expression";
import { useUiHintEditor } from "./registry";
import type { UiHintContext } from "./types";

export interface ActivityPropertiesPanelProps {
  workflowDefinitionId?: string;
  variables: Variable[];
  workflowOutputs: OutputDefinition[];
  workflowInputs?: { name: string }[];
}

function inputPropertyName(input: InputDescriptor) {
  return input.name.charAt(0).toLowerCase() + input.name.slice(1);
}

/** Edits the selected activity: inputs (by UI hint), output bindings, common settings, info, plus module tabs. */
export function ActivityPropertiesPanel({ workflowDefinitionId, variables, workflowOutputs, workflowInputs = [] }: ActivityPropertiesPanelProps) {
  const { registry, dataFlow } = useDesigner();
  const activity = useDesignerState((s) => s.selectedActivity());
  // Select stable references only (a fresh array from the selector would re-render forever).
  const containerActivities = useDesignerState((s) => s.children());
  const selectedId = useDesignerState((s) => s.selectedIds[0]);
  const siblingActivities = useMemo(
    () =>
      containerActivities
        .filter((a) => a.id !== selectedId && a.name)
        .map((a) => ({ name: a.name!, outputs: (registry.get(a.type, a.version)?.outputs ?? []).map((o) => o.name) })),
    [containerActivities, selectedId, registry],
  );
  const readOnly = useDesignerState((s) => s.readOnly);
  const updateActivity = useDesignerState((s) => s.updateActivity);
  const extraTabs = useExtensions("activityTabs");
  const { items: expressionDescriptors } = useExpressionDescriptors();
  const syntaxes = useMemo(() => selectableSyntaxes(expressionDescriptors), [expressionDescriptors]);
  const [tab, setTab] = useState("input");

  if (!activity) {
    return <EmptyState icon={Info} title="No activity selected" description="Select an activity on the canvas to edit its properties." className="m-3 border-none" />;
  }

  const descriptor = registry.get(activity.type, activity.version);
  const update = (patch: Partial<Activity> | ((a: Activity) => Activity)) =>
    updateActivity(activity.id, (a) => (typeof patch === "function" ? patch(a) : { ...a, ...patch }));

  const context: UiHintContext = {
    workflowDefinitionId,
    activityType: activity.type,
    activityVersion: activity.version,
    activity: { ...activity, __workflowOutputs: workflowOutputs },
    variables: variables.map((v) => ({ id: v.id, name: v.name, typeName: v.typeName })),
    outputs: descriptor?.outputs ?? [],
    inputs: workflowInputs,
    activities: siblingActivities,
  };
  const dataSummary = dataFlow?.byActivity.get(activity.id);

  const inputs = [...(descriptor?.inputs ?? [])].filter((i) => i.isBrowsable !== false).sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
  const outputs = (descriptor?.outputs ?? []).filter((o) => o.isBrowsable !== false);

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex items-center gap-2 border-b px-3 py-2">
        <div className="min-w-0 flex-1">
          <div className="truncate text-sm font-semibold">{activityDisplayText(activity, descriptor)}</div>
          <div className="truncate text-xs text-muted-foreground">{descriptor?.displayName ?? activity.type}</div>
        </div>
      </div>
      <Tabs value={tab} onValueChange={(v) => setTab(String(v))} className="flex min-h-0 flex-1 flex-col">
        <TabsList className="mx-3 mt-2 w-[calc(100%-1.5rem)] justify-start overflow-x-auto [&>*]:shrink-0">
          <TabsTrigger value="input">Input</TabsTrigger>
          <TabsTrigger value="output">Output</TabsTrigger>
          <TabsTrigger value="common">Common</TabsTrigger>
          {extraTabs.map((t) => (
            <TabsTrigger key={t.id} value={t.id}>
              {t.label}
            </TabsTrigger>
          ))}
          <TabsTrigger value="info">Info</TabsTrigger>
        </TabsList>
        <ScrollArea className="min-h-0 flex-1">
          <TabsContent value="input" className="flex flex-col gap-4 p-3">
            {inputs.length === 0 && <p className="text-sm text-muted-foreground">This activity has no inputs.</p>}
            {inputs.map((input) => (
              <InputField key={input.name} input={input} activity={activity} syntaxes={syntaxes} readOnly={readOnly} context={context} onChange={update} />
            ))}
          </TabsContent>
          <TabsContent value="output" className="flex flex-col gap-4 p-3">
            {outputs.length === 0 && <p className="text-sm text-muted-foreground">This activity has no outputs.</p>}
            {outputs.map((output) => (
              <OutputField key={output.name} output={output} activity={activity} variables={variables} readOnly={readOnly} onChange={update} />
            ))}
          </TabsContent>
          <TabsContent value="common" className="flex flex-col gap-4 p-3">
            <Field label="Name" hint="Used to reference this activity from expressions and connections.">
              <Input value={activity.name ?? ""} readOnly={readOnly} onChange={(e) => update({ name: e.target.value })} />
            </Field>
            <Field label="Display text">
              <Input value={(activity.metadata?.displayText as string) ?? ""} readOnly={readOnly} onChange={(e) => update((a) => ({ ...a, metadata: { ...a.metadata, displayText: e.target.value } }))} />
            </Field>
            <Field label="Description">
              <Textarea rows={3} value={(activity.metadata?.description as string) ?? ""} readOnly={readOnly} onChange={(e) => update((a) => ({ ...a, metadata: { ...a.metadata, description: e.target.value } }))} />
            </Field>
            <Field inline label="Show description on the canvas">
              <Checkbox checked={activity.metadata?.showDescription === true} disabled={readOnly} onCheckedChange={(c) => update((a) => ({ ...a, metadata: { ...a.metadata, showDescription: c === true } }))} />
            </Field>
            {descriptor?.kind === "Trigger" && (
              <Field inline label="Can start workflow" hint="When checked this trigger starts a new instance of the workflow.">
                <Checkbox
                  checked={activity.customProperties?.canStartWorkflow === true}
                  disabled={readOnly}
                  onCheckedChange={(c) => update((a) => ({ ...a, customProperties: { ...a.customProperties, canStartWorkflow: c === true } }))}
                />
              </Field>
            )}
            <Field inline label="Run asynchronously" hint="Schedule the activity on a background worker instead of running it inline.">
              <Checkbox
                checked={activity.customProperties?.runAsynchronously === true}
                disabled={readOnly}
                onCheckedChange={(c) => update((a) => ({ ...a, customProperties: { ...a.customProperties, runAsynchronously: c === true } }))}
              />
            </Field>
          </TabsContent>
          {extraTabs.map((t) => (
            <TabsContent key={t.id} value={t.id} className="p-3">
              <t.component />
            </TabsContent>
          ))}
          <TabsContent value="info" className="flex flex-col gap-4 p-3">
            {dataSummary && (dataSummary.reads.length > 0 || dataSummary.writes.length > 0) && (
              <div className="flex flex-col gap-1.5">
                <div className="eyebrow">Data</div>
                <div className="flex flex-wrap gap-1">
                  {dataSummary.writes.map((d) => (
                    <span key={`w-${d.kind}-${d.key}`} className="rounded-full bg-status-warning/15 px-2 py-0.5 text-xs font-medium text-status-warning">↑ writes {d.name}</span>
                  ))}
                  {dataSummary.reads.map((d) => (
                    <span key={`r-${d.kind}-${d.key}`} className="rounded-full bg-status-info/15 px-2 py-0.5 text-xs font-medium text-status-info">↓ reads {d.name}</span>
                  ))}
                </div>
              </div>
            )}
            <KeyValueTable
              rows={[
                { label: "Id", value: <code className="font-mono text-xs">{activity.id}</code> },
                { label: "Node id", value: <code className="font-mono text-xs break-all">{activity.nodeId}</code> },
                { label: "Type", value: activity.type },
                { label: "Version", value: activity.version },
                { label: "Kind", value: descriptor?.kind },
                { label: "Category", value: descriptor?.category },
                { label: "Description", value: descriptor?.description },
              ]}
            />
          </TabsContent>
        </ScrollArea>
      </Tabs>
    </div>
  );
}

function InputField({
  input,
  activity,
  syntaxes,
  readOnly,
  context,
  onChange,
}: {
  input: InputDescriptor;
  activity: Activity;
  syntaxes: string[];
  readOnly: boolean;
  context: UiHintContext;
  onChange: (patch: (a: Activity) => Activity) => void;
}) {
  const Editor = useUiHintEditor(input.uiHint);
  const prop = inputPropertyName(input);
  const raw = activity[prop];
  const value = input.isWrapped === false ? (raw as ActivityInput | undefined) : (raw as ActivityInput | undefined);
  return (
    <Field label={input.displayName ?? input.name} hint={input.description}>
      {createElement(Editor, {
        descriptor: input,
        value,
        syntaxes: input.supportedSyntaxes?.length ? ["Literal", ...input.supportedSyntaxes.filter((s) => s !== "Literal")] : syntaxes,
        readOnly: readOnly || input.isReadOnly,
        context,
        onChange: (next) => onChange((a) => ({ ...a, [prop]: next })),
      })}
    </Field>
  );
}

function OutputField({
  output,
  activity,
  variables,
  readOnly,
  onChange,
}: {
  output: OutputDescriptor;
  activity: Activity;
  variables: Variable[];
  readOnly: boolean;
  onChange: (patch: (a: Activity) => Activity) => void;
}) {
  const prop = inputPropertyName({ name: output.name } as InputDescriptor);
  const current = activity[prop] as { memoryReference?: { id: string } } | undefined;
  return (
    <Field label={output.displayName ?? output.name} hint={output.description ?? `Store the ${output.name} (${output.typeName}) in a variable.`}>
      <SimpleSelect
        value={current?.memoryReference?.id}
        allowEmpty
        disabled={readOnly}
        placeholder="Not captured"
        options={variables.map((v) => ({ value: v.id, label: `${v.name} (${v.typeName})` }))}
        onChange={(id) => onChange((a) => ({ ...a, [prop]: id ? { typeName: output.typeName, memoryReference: { id } } : undefined }))}
      />
    </Field>
  );
}
