// @vitest-environment jsdom
import { act, StrictMode, useEffect } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, useQuery, useQueryClient } from "@tanstack/react-query";
import { afterEach, expect, it, vi } from "vitest";
import { createElsaClient } from "./client.js";
import { ElsaApiProvider } from "./react.js";

afterEach(() => { document.body.replaceChildren(); vi.unstubAllGlobals(); });

it("retains a populated public cache through actual StrictMode effect replay and clears it on final unmount", async () => {
  vi.stubGlobal("IS_REACT_ACT_ENVIRONMENT", true);
  const element = document.createElement("div");
  document.body.append(element);
  const root = createRoot(element);
  const client = createElsaClient({ baseUrl: "https://example.invalid/api", getAuthHeaders: () => ({}) });
  let cache: QueryClient | undefined;
  let setups = 0;
  let cleanups = 0;
  function Value() {
    cache = useQueryClient();
    const query = useQuery({ queryKey: ["seed"], queryFn: async () => "Fetched", initialData: "Retained", staleTime: Infinity });
    useEffect(() => { setups++; return () => { cleanups++; }; }, []);
    return <output>{query.data}</output>;
  }
  try {
    await act(async () => root.render(<StrictMode><ElsaApiProvider client={client} scope="account:a"><Value /></ElsaApiProvider></StrictMode>));
    expect(setups).toBe(2);
    expect(cleanups).toBe(1);
    expect(element.textContent).toBe("Retained");
    expect(cache?.getQueryData(["seed"])).toBe("Retained");
  } finally {
    await act(async () => root.unmount());
  }
  expect(cleanups).toBe(2);
  expect(cache?.getQueryCache().getAll()).toEqual([]);
});
