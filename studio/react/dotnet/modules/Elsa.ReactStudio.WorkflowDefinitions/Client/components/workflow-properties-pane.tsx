"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { useState } from "react";
import { Checkbox } from "@elsa-workflows/react-studio-ui/checkbox";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { ScrollArea } from "@elsa-workflows/react-studio-ui/scroll-area";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@elsa-workflows/react-studio-ui/tabs";
import { Textarea } from "@elsa-workflows/react-studio-ui/textarea";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { KeyValueTable } from "@elsa-workflows/react-studio-ui/common/key-value-table";
import { SimpleSelect } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { useActivationStrategies, useIncidentStrategies, useLogPersistenceStrategies, useWorkflowCommitStrategies } from "@elsa-workflows/react-studio-api-client/hooks/descriptors";
import { useExtensions } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
import { useWorkflowEditor } from "../editor-context";
import { InputsOutputsTab } from "./inputs-outputs-tab";
import { VariablesTab } from "./variables-tab";
import { VersionsTab } from "./versions-tab";

/** Right-hand pane of the editor: workflow metadata, settings, variables, I/O, versions and module tabs. */
export function WorkflowPropertiesPane({ onViewVersion }: { onViewVersion: (version: number | undefined) => void }) {
  const _t = useTranslation();
  const [tab, setTab] = useState("properties");
  const extraTabs = useExtensions("workflowPropertiesTabs");
  return (
    <Tabs value={tab} onValueChange={(v) => setTab(String(v))} className="flex h-full min-h-0 flex-col">
      <TabsList className="mx-3 mt-2 w-[calc(100%-1.5rem)] justify-start overflow-x-auto [&>*]:shrink-0">
        <TabsTrigger value="properties">{_t("Properties")}</TabsTrigger>
        <TabsTrigger value="variables">{_t("Variables")}</TabsTrigger>
        <TabsTrigger value="io">{_t("I/O")}</TabsTrigger>
        {extraTabs.map((t) => (
          <TabsTrigger key={t.id} value={t.id}>{t.label}</TabsTrigger>
        ))}
        <TabsTrigger value="versions">{_t("Versions")}</TabsTrigger>
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
  const _t = useTranslation();
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
        <h3 className="text-sm font-semibold">{_t("Metadata")}</h3>
        <Field label={_t("Name")} required>
          <Input value={definition.name ?? ""} readOnly={readOnly} onChange={(e) => updateDefinition({ name: e.target.value })} />
        </Field>
        <Field label={_t("Description")}>
          <Textarea rows={3} value={definition.description ?? ""} readOnly={readOnly} onChange={(e) => updateDefinition({ description: e.target.value })} />
        </Field>
      </section>

      <section className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold">{_t("Settings")}</h3>
        <Field label={_t("Activation strategy")} hint={_t("Controls when a new instance may be created.")}>
          <SimpleSelect value={options.activationStrategyType} allowEmpty disabled={readOnly} options={strategyOptions(activation.items)} onChange={(v) => setOption({ activationStrategyType: v })} />
        </Field>
        <Field label={_t("Incident strategy")}>
          <SimpleSelect value={options.incidentStrategyType} allowEmpty disabled={readOnly} options={strategyOptions(incidents.items)} onChange={(v) => setOption({ incidentStrategyType: v })} />
        </Field>
        <Field label={_t("Log persistence strategy")}>
          <SimpleSelect
            value={typeof options.logPersistenceStrategyType === "string" ? options.logPersistenceStrategyType : undefined}
            allowEmpty
            disabled={readOnly}
            options={strategyOptions(logPersistence.items)}
            onChange={(v) => setOption({ logPersistenceStrategyType: v })}
          />
        </Field>
        <Field label={_t("Commit strategy")}>
          <SimpleSelect value={options.commitStrategyName} allowEmpty disabled={readOnly} options={strategyOptions(commit.items)} onChange={(v) => setOption({ commitStrategyName: v })} />
        </Field>
        <Field inline label={_t("Usable as activity")} hint={_t("Expose this workflow in the activity toolbox of other workflows.")}>
          <Checkbox checked={options.usableAsActivity === true} disabled={readOnly} onCheckedChange={(c) => setOption({ usableAsActivity: c === true })} />
        </Field>
        {options.usableAsActivity && (
          <>
            <Field inline label={_t("Auto-update referencing workflows")} hint={_t("On publish, consumers are updated to this version.")}>
              <Checkbox checked={options.autoUpdateConsumingWorkflows === true} disabled={readOnly} onCheckedChange={(c) => setOption({ autoUpdateConsumingWorkflows: c === true })} />
            </Field>
            <Field label={_t("Activity category")}>
              <Input value={options.activityCategory ?? ""} readOnly={readOnly} onChange={(e) => setOption({ activityCategory: e.target.value || undefined })} placeholder={_t("Workflows")} />
            </Field>
          </>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold">{_t("Info")}</h3>
        <KeyValueTable
          rows={[
            { label: _t("Definition id"), value: <code className="font-mono text-xs">{definition.definitionId}</code> },
            { label: _t("Version id"), value: <code className="font-mono text-xs">{definition.id}</code> },
            { label: _t("Version"), value: definition.version },
            { label: _t("Status"), value: definition.isPublished ? "Published" : "Draft" },
            { label: _t("Created"), value: <Timestamp value={definition.createdAt} /> },
            { label: _t("Read-only"), value: definition.isReadonly ? "Yes" : "No" },
          ]}
        />
      </section>
    </div>
  );
}
