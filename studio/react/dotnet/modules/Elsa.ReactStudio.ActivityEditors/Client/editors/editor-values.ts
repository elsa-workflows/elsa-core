import type { InputDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import type { InputDefinition } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import type { ActivityInput } from "@elsa-workflows/react-studio-api-client/types/activities";

export function parseObjectValue(value: unknown): unknown {
  if (typeof value !== "string") return value;
  try { return JSON.parse(value); } catch { return undefined; }
}

/** The server's status-code cases can carry embedded activities and extension fields. */
export interface StatusCodeCase { statusCode: number; [key: string]: unknown }
export function statusCodeCases(value: unknown): StatusCodeCase[] {
  const parsed = parseObjectValue(value);
  return Array.isArray(parsed) ? parsed.filter((item): item is StatusCodeCase =>
    item !== null && typeof item === "object" && Number.isInteger(item.statusCode)) : [];
}
export function changeStatusCodes(previous: unknown, codes: readonly number[]): StatusCodeCase[] {
  const existing = statusCodeCases(previous);
  return [...new Set(codes)].map(statusCode => existing.find(item => item.statusCode === statusCode) ?? { statusCode });
}
export function statusCodes(value: unknown): number[] {
  const parsed = parseObjectValue(value);
  return Array.isArray(parsed) ? parsed.map(item => typeof item === "object" && item !== null ? item.statusCode : item)
    .map(item => typeof item === "number" ? item : Number(item)).filter(code => Number.isInteger(code) && code >= 100 && code <= 599) : [];
}
export function statusCodeValue(previous: unknown, codes: number[], descriptor: InputDescriptor): unknown {
  const unique = [...new Set(codes)];
  // SendHttpRequest stores cases with embedded activities; FlowSendHttpRequest
  // stores an Input<int[]> whose Object expression contains JSON numbers.
  const next = descriptor.isWrapped === false || descriptor.typeName.includes("HttpStatusCodeCase")
    ? changeStatusCodes(previous, unique) : unique;
  return descriptor.isWrapped === false ? next : JSON.stringify(next);
}
export function parseStatusCode(text: string): number | undefined {
  return /^[1-5]\d\d$/.test(text.trim()) ? Number(text.trim()) : undefined;
}

export function fixedOutcomes(descriptor: InputDescriptor): string[] {
  const options = descriptor.uiSpecifications?.DynamicOutcomesOptions as { fixedOutcomes?: unknown; FixedOutcomes?: unknown } | undefined;
  const values = options?.fixedOutcomes ?? options?.FixedOutcomes;
  return Array.isArray(values) ? values.filter((value): value is string => typeof value === "string") : [];
}
export function serializeOutcomes(values: readonly string[], descriptor: InputDescriptor): string {
  const fixed = new Set(fixedOutcomes(descriptor));
  return JSON.stringify(values.filter(value => !fixed.has(value)));
}

export function selectedInput(value: unknown, descriptor: InputDescriptor): InputDefinition | undefined {
  const raw = descriptor.isWrapped === false ? value : (value as ActivityInput | undefined)?.expression?.value;
  const parsed = parseObjectValue(raw);
  return parsed !== null && typeof parsed === "object" && "name" in parsed && typeof parsed.name === "string"
    ? parsed as InputDefinition : undefined;
}
export function inputSelection(input: InputDefinition | undefined, descriptor: InputDescriptor): ActivityInput | undefined {
  if (!input) return undefined;
  return descriptor.isWrapped === false ? { ...input } as unknown as ActivityInput : {
    typeName: descriptor.typeName, expression: { type: "Input", value: JSON.stringify(input) },
  };
}

export function dateTimeParts(value: unknown): { date: string; time: string } {
  if (typeof value !== "string" || !value.trim()) return { date: "", time: "" };
  const parsed = new Date(value);
  if (!Number.isFinite(parsed.getTime())) return { date: "", time: "" };
  const pad = (part: number) => String(part).padStart(2, "0");
  return { date: `${parsed.getFullYear()}-${pad(parsed.getMonth() + 1)}-${pad(parsed.getDate())}`,
    time: `${pad(parsed.getHours())}:${pad(parsed.getMinutes())}:${pad(parsed.getSeconds())}` };
}
export function dateTimeValue(date: string, time: string): string {
  if (!date) return "";
  const value = new Date(`${date}T${time || "00:00:00"}`);
  return Number.isFinite(value.getTime()) ? value.toISOString() : "";
}
