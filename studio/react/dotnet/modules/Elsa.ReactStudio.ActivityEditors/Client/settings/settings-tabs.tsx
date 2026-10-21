
import { translate, useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useElsaClient, useQueryScope } from "@elsa-workflows/react-studio-api-client/react";
import { useActivityCommitStrategies, useLogPersistenceStrategies } from "@elsa-workflows/react-studio-api-client/hooks/descriptors";
import type { Activity, ActivityInput } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { ActivityDescriptor, InputDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Checkbox } from "@elsa-workflows/react-studio-ui/checkbox";
import { Input } from "@elsa-workflows/react-studio-ui/input";
import { Field } from "@elsa-workflows/react-studio-ui/common/field";
import { SimpleSelect, type SelectOption } from "@elsa-workflows/react-studio-ui/common/simple-select";
import { ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { useDesigner } from "@elsa-workflows/react-studio-sdk/designer/context";
import { ExpressionEditor } from "../expression-editor";
import type { UiHintContext } from "../types";
import { persistenceConfiguration, setCustomProperty, setPersistenceRule, applyActivityVersion, type PersistenceRule, type ResilienceConfiguration } from "./activity-settings";

interface SettingsProps {
  activity: Activity;
  readOnly: boolean;
  onChange: (update: (current: Activity) => Activity) => void;
}
interface ExpressionSettingsProps extends SettingsProps { syntaxes: string[]; context: UiHintContext }

export function CommitStrategySettings({ activity, readOnly, onChange }: SettingsProps) {
  const _t = useTranslation();
  const { items, isLoading, error } = useActivityCommitStrategies();
  const value = (activity.customProperties?.commitStrategyName ?? activity.customProperties?.CommitStrategyName) as string | undefined;
  return <>
    <ErrorAlert error={error} title={_t("Could not load commit strategies")} />
    {isLoading && <p role="status">{_t("Loading commit strategies…")}</p>}
    <Field label={_t("Commit strategy")} hint={_t("Inherit the workflow's strategy or override when this activity commits workflow state.")}>
      <SimpleSelect aria-label={_t("Commit strategy")} value={value} allowEmpty placeholder={_t("Inherit")} disabled={readOnly}
        options={withCurrent(items.map(item => ({ value: item.name, label: item.displayName })), value)}
        onChange={next => onChange(current => {
          // Remove the legacy alias too, so clearing cannot resurrect it.
          const updated = setCustomProperty(current, "CommitStrategyName", undefined);
          return setCustomProperty(updated, "commitStrategyName", next);
        })} />
    </Field>
  </>;
}

export function TaskSettings({ activity, readOnly, onChange }: SettingsProps) {
  const _t = useTranslation();
  return <Field inline label={_t("Run asynchronously")} hint={_t("Schedule the activity on a background worker instead of running it inline.")}>
    <Checkbox aria-label={_t("Run asynchronously")} checked={(activity.customProperties?.runAsynchronously ?? activity.customProperties?.RunAsynchronously) === true}
      disabled={readOnly} onCheckedChange={checked => onChange(current => setCustomProperty(current, "runAsynchronously", checked === true))} />
  </Field>;
}

export function LogPersistenceSettings({ activity, descriptor, readOnly, onChange, syntaxes, context }: ExpressionSettingsProps & { descriptor?: ActivityDescriptor }) {
  const _t = useTranslation();
  const { items, isLoading, error } = useLogPersistenceStrategies();
  const configuration = persistenceConfiguration(activity);
  const options = items.map(item => ({ value: item.typeName, label: item.displayName }));
  const rule = (label: string, section: "default" | "internalState" | "inputs" | "outputs", property?: string) => {
    const name = property ? property.charAt(0).toLowerCase() + property.slice(1) : undefined;
    const value = name ? (configuration[section] as Record<string, PersistenceRule | null> | undefined)?.[name] : configuration[section] as PersistenceRule | null;
    return <PersistenceRuleEditor key={`${section}:${property ?? ""}`} label={label} rule={value} options={options}
      readOnly={readOnly} syntaxes={syntaxes} context={context}
      onChange={next => onChange(current => setPersistenceRule(current, section, property, next))} />;
  };
  return <>
    <ErrorAlert error={error} title={_t("Could not load log persistence strategies")} />
    {isLoading && <p role="status">{_t("Loading log persistence strategies…")}</p>}
    {rule(translate("Default log persistence"), "default")}
    {rule(translate("Internal state log persistence"), "internalState")}
    {descriptor?.inputs.map(input => rule(`Input: ${input.displayName ?? input.name}`, "inputs", input.name))}
    {descriptor?.outputs.map(output => rule(`Output: ${output.displayName ?? output.name}`, "outputs", output.name))}
  </>;
}

function PersistenceRuleEditor({ label, rule, options, readOnly, syntaxes, context, onChange }: {
  label: string; rule?: PersistenceRule | null; options: SelectOption[]; readOnly: boolean; syntaxes: string[]; context: UiHintContext;
  onChange: (rule: PersistenceRule | undefined) => void;
}) {
  const _t = useTranslation();
  const mode = !rule ? undefined : rule.evaluationMode === "Expression" || rule.evaluationMode === 1 ? "Expression" : "Strategy";
  const descriptor: InputDescriptor = { name: "Persist", typeName: "LogPersistenceMode", defaultSyntax: "Literal" };
  return <fieldset className="flex flex-col gap-2 rounded-md border p-3"><legend className="px-1 text-sm font-medium">{label}</legend>
    <SimpleSelect aria-label={_t("{{value0}} mode", { value0: label })} value={mode} allowEmpty placeholder={_t("Inherit")} disabled={readOnly}
      options={[{ value: "Strategy", label: _t("Strategy") }, { value: "Expression", label: _t("Expression") }]}
      onChange={next => onChange(next ? { ...rule, evaluationMode: next } : undefined)} />
    {mode === "Strategy" && <SimpleSelect aria-label={_t("{{value0}} strategy", { value0: label })} value={rule?.strategyType} allowEmpty placeholder={_t("Inherit")} disabled={readOnly}
      options={withCurrent(options, rule?.strategyType)} onChange={next => onChange({ ...rule!, strategyType: next ?? null })} />}
    {mode === "Expression" && <ExpressionEditor descriptor={descriptor} value={{ typeName: "LogPersistenceMode", expression: rule?.expression ?? { type: "Literal", value: "Inherit" } }}
      syntaxes={syntaxes} readOnly={readOnly} context={context} onChange={next => onChange({ ...rule!, expression: next?.expression ?? null })}>
      {literal => <SimpleSelect aria-label={_t("{{value0}} expression value", { value0: label })} value={typeof literal.value === "number" ? ["Inherit", "Include", "Exclude"][literal.value] : String(literal.value ?? "Inherit")}
        disabled={readOnly} options={["Inherit", "Include", "Exclude"].map(value => ({ value, label: value }))} onChange={literal.onChange} />}
    </ExpressionEditor>}
  </fieldset>;
}

export function ResilienceSettings({ activity, readOnly, onChange, syntaxes, context }: ExpressionSettingsProps) {
  const _t = useTranslation();
  const client = useElsaClient();
  const scope = useQueryScope();
  const query = useQuery({ queryKey: [scope, "resilience", "strategies"], queryFn: ({ signal }) => client.resilience.strategies(signal) });
  const configuration = activity.customProperties?.resilienceStrategy as ResilienceConfiguration | undefined;
  const mode = !configuration ? undefined : configuration.mode === "Expression" || configuration.mode === 1 ? "Expression" : "Identifier";
  const disabled = readOnly;
  const write = (next: ResilienceConfiguration | undefined) => onChange(current => setCustomProperty(current, "resilienceStrategy", next));
  return <>
    <ErrorAlert error={query.error} title={_t("Could not load resilience strategies")} />
    {query.isLoading && <p role="status">{_t("Loading resilience strategies…")}</p>}
    <Field label={_t("Resilience mode")}>
      <SimpleSelect aria-label={_t("Resilience mode")} value={mode} allowEmpty placeholder={_t("Default")} disabled={disabled}
        options={[{ value: "Identifier", label: _t("Strategy") }, { value: "Expression", label: _t("Expression") }]}
        onChange={next => write(next ? { ...configuration, mode: next } : undefined)} />
    </Field>
    {mode === "Identifier" && <Field label={_t("Resilience strategy")}>
      <SimpleSelect aria-label={_t("Resilience strategy")} value={configuration?.strategyId} disabled={disabled} allowEmpty placeholder={_t("Default")}
        options={withCurrent((query.data?.items ?? []).map(item => ({ value: item.id, label: item.displayName })), configuration?.strategyId)}
        onChange={next => write({ ...configuration!, strategyId: next ?? null })} />
    </Field>}
    {mode === "Expression" && <Field label={_t("Resilience expression")} hint={_t("Evaluate to a strategy ID or strategy object.")}>
      <ExpressionEditor descriptor={{ name: "Strategy", typeName: "Object", defaultSyntax: "Literal" }}
        value={{ typeName: "Object", expression: configuration?.expression ?? { type: "Literal", value: "" } } as ActivityInput}
        syntaxes={syntaxes} readOnly={disabled} context={context} onChange={next => write({ ...configuration!, expression: next?.expression ?? null })}>
        {literal => <Input aria-label={_t("Resilience expression value")} value={typeof literal.value === "string" ? literal.value : JSON.stringify(literal.value) ?? ""} readOnly={disabled} onChange={event => literal.onChange(event.target.value)} />}
      </ExpressionEditor>
    </Field>}
  </>;
}

export function VersionSettings({ activity, readOnly, onChange }: SettingsProps) {
  const _t = useTranslation();
  const client = useElsaClient();
  const scope = useQueryScope();
  const { registry } = useDesigner();
  const [page, setPage] = useState(0);
  const query = useQuery({ queryKey: [scope, "workflow-activity-versions", activity.workflowDefinitionId, page],
    enabled: !!activity.workflowDefinitionId,
    queryFn: ({ signal }) => client.workflowDefinitions.list({ definitionIds: [String(activity.workflowDefinitionId)], versionOptions: "AllVersions", orderBy: "Version", orderDirection: "Descending", page, pageSize: 10 }, signal) });
  return <>
    <p className="text-sm text-muted-foreground">{_t("Current activity version:")}{" "}{activity.version}</p>
    <ErrorAlert error={query.error} title={_t("Could not load workflow activity versions")} />
    {!activity.workflowDefinitionId && <p className="text-sm">{_t("This activity has no workflow definition ID.")}</p>}
    {query.isLoading && <p role="status">{_t("Loading versions…")}</p>}
    {query.data?.items.map(version => {
      const available = registry.descriptors.some(descriptor => descriptor.typeName === activity.type && descriptor.version === version.version);
      const selected = activity.workflowDefinitionVersionId === version.id;
      return <div key={version.id} className="flex items-center justify-between gap-3 rounded-md border p-2 text-sm">
        <span>{_t("Version")}{" "}{version.version}{version.isPublished ? _t(" · Published") : _t(" · Draft")}{selected ? _t(" · Current") : ""}{!available ? _t(" · Unavailable in activity catalog") : ""}</span>
        <Button size="sm" disabled={readOnly || selected || !available} onClick={() => onChange(current => applyActivityVersion(current, version))}>{_t("Use version")}{" "}{version.version}</Button>
      </div>;
    })}
    {query.data && query.data.items.length === 0 && <p>{_t("No versions found.")}</p>}
    <div className="flex gap-2">
      <Button size="sm" variant="outline" disabled={page === 0 || query.isFetching} onClick={() => setPage(value => value - 1)}>{_t("Previous versions")}</Button>
      <Button size="sm" variant="outline" disabled={query.isFetching || !query.data || (page + 1) * 10 >= query.data.totalCount} onClick={() => setPage(value => value + 1)}>{_t("Next versions")}</Button>
    </div>
  </>;
}

function withCurrent(options: SelectOption[], value?: string | null): SelectOption[] {
  return value && !options.some(option => option.value === value) ? [{ value, label: translate("{{value0}} (current)", { value0: value }) }, ...options] : options;
}
