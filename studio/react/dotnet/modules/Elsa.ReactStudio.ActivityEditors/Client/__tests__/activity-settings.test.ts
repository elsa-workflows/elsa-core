import { createDesignerStore } from "@elsa-workflows/react-studio-sdk/designer/store";
import type { ActivityDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import { describe, expect, it } from "vitest";
import type { Activity } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { WorkflowDefinitionSummary } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import { persistenceConfiguration, setCustomProperty, setPersistenceRule, applyActivityVersion } from "../settings/activity-settings";

const activity = (customProperties: Record<string, unknown> = {}): Activity => ({ id: "child", type: "Example.Child", version: 1, customProperties });

describe("per-activity settings", () => {
  it("preserves inputs and unrelated custom properties while updating or clearing one setting", () => {
    const original = { ...activity({ pluginData: { keep: true }, commitStrategyName: "Existing" }), input: { expression: { type: "Literal", value: "keep" } } };
    const updated = setCustomProperty(original, "commitStrategyName", "Always");
    expect(original.customProperties?.commitStrategyName).toBe("Existing");
    expect(updated.input).toBe(original.input);
    expect(updated.customProperties?.pluginData).toBe(original.customProperties?.pluginData);
    expect(setCustomProperty(updated, "commitStrategyName", undefined).customProperties).toEqual({ pluginData: { keep: true } });
  });

  it("reads string and numeric legacy modes, prefers current settings, and does not mutate on read", () => {
    const original = activity({ logPersistenceMode: { default: "Exclude", inputs: { old: 1, skip: "Exclude" }, outputs: { result: 2 } },
      logPersistenceConfig: { inputs: { old: { evaluationMode: 1, expression: { type: "JavaScript", value: "true" } } }, future: "keep" } });
    const snapshot = JSON.stringify(original);
    expect(persistenceConfiguration(original)).toEqual({ default: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" }, future: "keep", inputs: {
      old: { evaluationMode: 1, expression: { type: "JavaScript", value: "true" } }, skip: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" },
    }, outputs: { result: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" } } });
    expect(JSON.stringify(original)).toBe(snapshot);
  });

  it("clears a legacy property without resurrecting its old mode and preserves adjacent rules", () => {
    const original = activity({ logPersistenceMode: { inputs: { secret: "Include", message: "Exclude" } }, other: 42 });
    const updated = setPersistenceRule(original, "inputs", "Secret", undefined);
    expect(persistenceConfiguration(updated).inputs).toEqual({ secret: null, message: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" } });
    expect(updated.customProperties?.logPersistenceMode).toEqual({ inputs: { message: "Exclude" } });
    expect(original.customProperties?.logPersistenceMode).toEqual({ inputs: { secret: "Include", message: "Exclude" } });
    expect(updated.customProperties?.other).toBe(42);
    expect(() => setPersistenceRule(original, "outputs", undefined, undefined)).toThrow("property name");
  });

  it("matches the runtime's null fallback and clears legacy defaults explicitly", () => {
    const original = activity({ logPersistenceMode: { default: "Exclude", inputs: { message: "Include" } },
      logPersistenceConfig: { default: null, inputs: { message: null } } });
    expect(persistenceConfiguration(original).default?.strategyType).toContain("Strategies.Exclude,");
    expect(persistenceConfiguration(original).inputs?.message?.strategyType).toContain("Strategies.Include,");
    const cleared = setPersistenceRule(original, "default", undefined, undefined);
    expect(persistenceConfiguration(cleared).default).toBeNull();
    expect(cleared.customProperties?.logPersistenceMode).toEqual({ inputs: { message: "Include" } });
  });

  it("retains expression metadata when setting another log section", () => {
    const expression = { type: "JavaScript", value: "getVariable('Persist')" };
    const original = activity({ logPersistenceConfig: { default: { evaluationMode: "Expression", expression }, inputs: { password: null } } });
    const updated = setPersistenceRule(original, "internalState", undefined, { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" });
    expect(persistenceConfiguration(updated)).toMatchObject({ default: { expression }, inputs: { password: null }, internalState: { strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" } });
  });

  it("updates the activity version and definition-version ID together and rejects another definition", () => {
    const original = { ...activity(), workflowDefinitionId: "reusable", workflowDefinitionVersionId: "version-one", input: "keep" };
    const version = { id: "version-two", definitionId: "reusable", version: 2 } as WorkflowDefinitionSummary;
    expect(applyActivityVersion(original, version)).toMatchObject({ version: 2, workflowDefinitionVersionId: "version-two", input: "keep" });
    expect(original.version).toBe(1);
    expect(() => applyActivityVersion(original, { ...version, definitionId: "another" })).toThrow("another workflow");
  });
});


it("creates toolbox workflow activities with the server's camelCase version fields", () => {
  const store = createDesignerStore({ id: "root", type: "Elsa.Flowchart", version: 1, activities: [], connections: [] });
  const descriptor = { typeName: "Example.Reusable", name: "Reusable", version: 2, kind: "Action", inputs: [], outputs: [], ports: [],
    customProperties: { RootType: "WorkflowDefinitionActivity" },
    constructionProperties: { WorkflowDefinitionId: "reusable", WorkflowDefinitionVersionId: "version-two", Version: 2, plugin: "keep" } } as unknown as ActivityDescriptor;
  const created = store.getState().addActivity(descriptor, { x: 0, y: 0 });
  expect(created).toMatchObject({ version: 2, workflowDefinitionId: "reusable", workflowDefinitionVersionId: "version-two", plugin: "keep" });
  expect(created).not.toHaveProperty("WorkflowDefinitionId");
  expect(created).not.toHaveProperty("WorkflowDefinitionVersionId");
  expect(created).not.toHaveProperty("Version");
  expect(descriptor.constructionProperties).toHaveProperty("WorkflowDefinitionVersionId", "version-two");
});
