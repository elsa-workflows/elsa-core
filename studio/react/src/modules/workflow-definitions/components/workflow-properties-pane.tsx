"use client";

import { useState } from "react";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Textarea } from "@/components/ui/textarea";
import { Field } from "@/components/common/field";
import { KeyValueTable } from "@/components/common/key-value-table";
import { SimpleSelect } from "@/components/common/simple-select";
import { Timestamp } from "@/components/common/timestamp";
import { useActivationStrategies, useIncidentStrategies, useLogPersistenceStrategies, useWorkflowCommitStrategies } from "@/lib/api/hooks/descriptors";
import { useExtensions } from "@/lib/modules/registry";
import { useWorkflowEditor } from "../editor-context";
import { InputsOutputsTab } from "./inputs-outputs-tab";
import { VariablesTab } from "./variables-tab";
import { VersionsTab } from "./versions-tab";

/** Right-hand pane of the editor: workflow metadata, settings, variables, I/O, versions and module tabs. */
export function WorkflowPropertiesPane({ onViewVersion }: { onViewVersion: (version: number | undefined) => void }) {
  const [tab, setTab] = useState("properties");
  const extraTabs = useExtensions("workflowPropertiesTabs");
  return (
    <Tabs value={tab} onValueChange={(v) => setTab(String(v))} className="flex h-full min-h-0 flex-col">
      <TabsList className="mx-3 mt-2 w-[calc(100%-1.5rem)] justify-start overflow-x-auto [&>*]:shrink-0">
        <TabsTrigger value="properties">Properties</TabsTrigger>
        <TabsTrigger value="variables">Variables</TabsTrigger>
        <TabsTrigger value="io">I/O</TabsTrigger>
        {extraTabs.map((t) => (
          <TabsTrigger key={t.id} value={t.id}>{t.label}</TabsTrigger>
        ))}
        <TabsTrigger value="versions">Versions</TabsTrigger>
      </TabsList>
      <ScrollArea className="min-h-0 flex-1">
        <TabsContent value="properties"><PropertiesTab /></TabsContent>
        <TabsContent value="variables"><VariablesTab /></TabsContent>
        <TabsContent value="io"><InputsOutputsTab /></TabsContent>
        {extraTabs.map((t) => (
          <TabsContent key={t.id} value={t.id} className="p-3"><t.component /></TabsContent>
        ))}
        <TabsContent value="versions"><VersionsTab onView={onViewVersion} /></TabsContent>
      </ScrollArea>
    </Tabs>
  );
}

function PropertiesTab() {
  const { definition, updateDefinition, readOnly } = useWorkflowEditor();
  const activation = useActivationStrategies();
  const incidents = useIncidentStrategies();
  const logPersistence = useLogPersistenceStrategies();
  const commit = useWorkflowCommitStrategies();
  const options = definition.options ?? {};
  const setOption = (patch: Record<string, unknown>) => updateDefinition((d) => ({ options: { ...d.options, ...patch } }));
  const strategyOptions = (items: { typeName?: string; name?: string; displayName: string }[]) =>
    items.map((i) => ({ value: (i.typeName ?? i.name)!, label: i.displayName }));

  return (
    <div className="flex flex-col gap-5 p-3">
      <section className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold">Metadata</h3>
        <Field label="Name" required>
          <Input value={definition.name ?? ""} readOnly={readOnly} onChange={(e) => updateDefinition({ name: e.target.value })} />
        </Field>
        <Field label="Description">
          <Textarea rows={3} value={definition.description ?? ""} readOnly={readOnly} onChange={(e) => updateDefinition({ description: e.target.value })} />
        </Field>
      </section>

      <section className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold">Settings</h3>
        <Field label="Activation strategy" hint="Controls when a new instance may be created.">
          <SimpleSelect value={options.activationStrategyType} allowEmpty disabled={readOnly} options={strategyOptions(activation.items)} onChange={(v) => setOption({ activationStrategyType: v })} />
        </Field>
        <Field label="Incident strategy">
          <SimpleSelect value={options.incidentStrategyType} allowEmpty disabled={readOnly} options={strategyOptions(incidents.items)} onChange={(v) => setOption({ incidentStrategyType: v })} />
        </Field>
        <Field label="Log persistence strategy">
          <SimpleSelect
            value={typeof options.logPersistenceStrategyType === "string" ? options.logPersistenceStrategyType : undefined}
            allowEmpty
            disabled={readOnly}
            options={strategyOptions(logPersistence.items)}
            onChange={(v) => setOption({ logPersistenceStrategyType: v })}
          />
        </Field>
        <Field label="Commit strategy">
          <SimpleSelect value={options.commitStrategyName} allowEmpty disabled={readOnly} options={strategyOptions(commit.items)} onChange={(v) => setOption({ commitStrategyName: v })} />
        </Field>
        <Field inline label="Usable as activity" hint="Expose this workflow in the activity toolbox of other workflows.">
          <Checkbox checked={options.usableAsActivity === true} disabled={readOnly} onCheckedChange={(c) => setOption({ usableAsActivity: c === true })} />
        </Field>
        {options.usableAsActivity && (
          <>
            <Field inline label="Auto-update referencing workflows" hint="On publish, consumers are updated to this version.">
              <Checkbox checked={options.autoUpdateConsumingWorkflows === true} disabled={readOnly} onCheckedChange={(c) => setOption({ autoUpdateConsumingWorkflows: c === true })} />
            </Field>
            <Field label="Activity category">
              <Input value={options.activityCategory ?? ""} readOnly={readOnly} onChange={(e) => setOption({ activityCategory: e.target.value || undefined })} placeholder="Workflows" />
            </Field>
          </>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold">Info</h3>
        <KeyValueTable
          rows={[
            { label: "Definition id", value: <code className="font-mono text-xs">{definition.definitionId}</code> },
            { label: "Version id", value: <code className="font-mono text-xs">{definition.id}</code> },
            { label: "Version", value: definition.version },
            { label: "Status", value: definition.isPublished ? "Published" : "Draft" },
            { label: "Created", value: <Timestamp value={definition.createdAt} /> },
            { label: "Read-only", value: definition.isReadonly ? "Yes" : "No" },
          ]}
        />
      </section>
    </div>
  );
}
