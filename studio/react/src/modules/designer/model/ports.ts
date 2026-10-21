import type { Activity, ActivityInput } from "@/lib/api/types/activities";
import { DEFAULT_DONE_PORT, DEFAULT_IN_PORT } from "@/lib/api/types/activities";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";

export interface OutPort {
  name: string;
  displayName: string;
}

export interface EmbeddedPort {
  name: string;
  displayName: string;
}

export interface ActivityPorts {
  /** `undefined` for start activities, which cannot be targeted. */
  inPort?: string;
  outPorts: OutPort[];
  embeddedPorts: EmbeddedPort[];
}

function literalValue(input: unknown): unknown {
  const wrapped = input as ActivityInput | undefined;
  if (wrapped && typeof wrapped === "object" && "expression" in wrapped) return wrapped.expression?.value;
  return input;
}

/**
 * Mirrors the studio's activity port providers:
 *  - every `Flow` port of the descriptor is an outgoing handle;
 *  - `Elsa.FlowSwitch` adds one handle per case label;
 *  - any input with the `dynamic-outcomes` UI hint adds a handle per listed outcome;
 *  - with no flow ports the activity exposes a single `Done` handle unless it is terminal;
 *  - `Embedded` ports are nested slots rather than handles.
 */
export function computePorts(activity: Activity, descriptor: ActivityDescriptor | undefined): ActivityPorts {
  const embeddedPorts: EmbeddedPort[] = [];
  const outPorts: OutPort[] = [];

  for (const port of descriptor?.ports ?? []) {
    if (port.type === "Embedded") embeddedPorts.push({ name: port.name, displayName: port.displayName ?? port.name });
    else outPorts.push({ name: port.name, displayName: port.displayName ?? port.name });
  }

  if (activity.type === "Elsa.FlowSwitch") {
    const cases = (literalValue(activity.cases) as { label?: string }[] | undefined) ?? [];
    const names = cases.map((c) => c.label).filter((l): l is string => !!l);
    outPorts.unshift(...names.map((n) => ({ name: n, displayName: n })));
  }

  for (const input of descriptor?.inputs ?? []) {
    if (input.uiHint !== "dynamic-outcomes") continue;
    const outcomes = (literalValue(activity[portPropertyNameOf(input.name)]) as string[] | undefined) ?? [];
    outPorts.push(...outcomes.filter(Boolean).map((n) => ({ name: n, displayName: n })));
  }

  if (outPorts.length === 0 && !descriptor?.isTerminal) outPorts.push({ name: DEFAULT_DONE_PORT, displayName: "Done" });

  const unique = outPorts.filter((p, i, arr) => arr.findIndex((x) => x.name === p.name) === i);
  return { inPort: descriptor?.isStart ? undefined : DEFAULT_IN_PORT, outPorts: unique, embeddedPorts };
}

function portPropertyNameOf(inputName: string) {
  return inputName.charAt(0).toLowerCase() + inputName.slice(1);
}
