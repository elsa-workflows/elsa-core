import { describe, expect, it } from "vitest";
import { workflowLogPersistenceMode } from "../components/log-persistence";

describe("workflow log persistence mode", () => {
  it("reads the strategy, the legacy mode, and an expression", () => {
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Exclude, Elsa.Workflows.Core" } },
    })).toBe("Exclude");
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: 0, strategyType: "Elsa.Workflows.LogPersistence.Strategies.Inherit, Elsa.Workflows.Core" }, internalState: { strategyType: "Include" } },
    })).toBe("Inherit");
    expect(workflowLogPersistenceMode({ logPersistenceMode: { default: "Include" } })).toBe("Include");
    expect(workflowLogPersistenceMode({ logPersistenceMode: { default: 2 } })).toBe("Exclude");
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: "Strategy", strategyType: "Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core" } },
      logPersistenceMode: { default: "Exclude" },
    })).toBe("Include");
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: null },
      logPersistenceMode: { default: "Exclude" },
    })).toBe("Exclude");
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: { evaluationMode: "Expression", expression: { type: "JavaScript", value: "LogPersistenceMode.Exclude" } } },
    })).toBe("LogPersistenceMode.Exclude");
  });

  it("inherits when the workflow does not set a mode and ignores a broken payload", () => {
    expect(workflowLogPersistenceMode(undefined)).toBe("Inherit");
    expect(workflowLogPersistenceMode({})).toBe("Inherit");
    expect(workflowLogPersistenceMode({
      logPersistenceConfig: { default: "not-an-object" },
      logPersistenceMode: { default: "Exclude" },
    })).toBe("Inherit");
  });
});
