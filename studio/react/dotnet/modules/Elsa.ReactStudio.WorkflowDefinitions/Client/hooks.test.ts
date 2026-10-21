import { afterEach, expect, it, vi } from "vitest";
import { QueryClient, QueryObserver } from "@tanstack/react-query";
import { useWorkflowDefinitionMutations } from "./hooks";

let queryClient: QueryClient;
vi.mock("@tanstack/react-query", async original => ({
  ...await original<typeof import("@tanstack/react-query")>(),
  useQueryClient: () => queryClient,
  useMutation: (options: unknown) => options,
}));
vi.mock("@elsa-workflows/react-studio-api-client/react", () => ({
  useQueryScope: () => "account-scope",
  useElsaClient: () => ({ workflowDefinitions: {} }),
}));
afterEach(() => queryClient?.clear());

it("saving refreshes an initially pending expression catalog without keeping the save pending or accepting its old result", async () => {
  queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const key = ["account-scope", "expression-types", "definition", "Activity", "Value"];
  let completeOld!: (value: string[]) => void;
  let completeNew!: (value: string[]) => void;
  let requests = 0;
  const observer = new QueryObserver(queryClient, { queryKey: key,
    queryFn: ({ signal }) => {
      void signal;
      requests++;
      return new Promise<string[]>(resolve => { if (requests === 1) completeOld = resolve; else completeNew = resolve; });
    } });
  const results: unknown[] = [];
  const unsubscribe = observer.subscribe(result => { if (result.data) results.push(result.data); });
  try {
    expect(requests).toBe(1);
    let saved = false;
    const save = useWorkflowDefinitionMutations().save as unknown as { onSuccess: () => Promise<unknown> };
    void save.onSuccess().then(() => { saved = true; });
    await vi.waitFor(() => { expect(saved).toBe(true); expect(requests).toBe(2); });
    completeOld(["OldInput"]);
    completeNew(["NewInput"]);
    await vi.waitFor(() => expect(queryClient.getQueryData(key)).toEqual(["NewInput"]));
    expect(results).not.toContainEqual(["OldInput"]);
  } finally { unsubscribe(); }
});
