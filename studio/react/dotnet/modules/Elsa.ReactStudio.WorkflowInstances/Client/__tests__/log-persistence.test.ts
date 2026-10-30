import { describe, expect, it } from "vitest";
import { workflowLogPersistenceMode } from "../components/log-persistence";

describe("workflow log persistence mode", () => {
  it("reads the strategy, the legacy mode, and an expression", () => {
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" } },
    })).toEqual({ kind: "mode", text: "Exclude" });
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: 0, strategyType: "Elsa.Workflows.LogPersistence.Strategies.Inherit, Elsa.Workflows.Core" }, internalState: { strategyType: "Include" } },
    })).toEqual({ kind: "mode", text: "Inherit" });
    expect(workflowLogPersistenceMode({ logPersistenceMode: { default: "Include" } })).toEqual({ kind: "mode", text: "Include" });
    expect(workflowLogPersistenceMode({ logPersistenceMode: { default: 2 } })).toEqual({ kind: "mode", text: "Exclude" });
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core" } },
      logPersistenceMode: { default: "Exclude" },
    })).toEqual({ kind: "mode", text: "Include" });
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: null },
      logPersistenceMode: { default: "Exclude" },
    })).toEqual({ kind: "mode", text: "Exclude" });
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: "Expression", expression: { type: "JavaScript", value: "LogPersistenceMode.Exclude" } } },
    })).toEqual({ kind: "expression", text: "LogPersistenceMode.Exclude" });
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: "Expression", expression: { type: "JavaScript", value: "Include" } } },
    })).toEqual({ kind: "expression", text: "Include" });
  });

  it("treats a blank expression as Inherit instead of the stale strategy", () => {
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: {
        default: {
          evaluationMode: "Expression",
          strategyType: "Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core",
          expression: { type: "JavaScript", value: "  " },
        },
      },
    })).toEqual({ kind: "mode", text: "Inherit" });
  });

  it("inherits when the workflow does not set a mode and ignores a broken payload", () => {
    expect(workflowLogPersistenceMode(undefined)).toEqual({ kind: "mode", text: "Inherit" });
    expect(workflowLogPersistenceMode({})).toEqual({ kind: "mode", text: "Inherit" });
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: "not-an-object" },
      logPersistenceMode: { default: "Exclude" },
    })).toEqual({ kind: "mode", text: "Inherit" });
  });
});
