// @vitest-environment jsdom
import { act } from "react";
import { createRoot } from "react-dom/client";
import { afterEach, expect, it, vi } from "vitest";
import { studioConfigSchema } from "@elsa-workflows/react-studio-sdk/config";
import { StudioConfigProvider } from "@elsa-workflows/react-studio-sdk/config-provider";
import { studioI18n, useCulture } from "@elsa-workflows/react-studio-sdk/localization";
import { useTenant } from "@elsa-workflows/react-studio-sdk/tenancy";
import { LocalizationProvider } from "./localization-provider";
import { TenancyProvider } from "./tenancy-provider";
vi.mock("@/lib/auth/provider", () => ({ useAuth: () => ({ status: "authenticated", user: { name: "Alice", claims: { sub: "alice" } } }) }));
Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
const container = document.body.appendChild(document.createElement("div"));
const root = createRoot(container);
afterEach(async () => { await act(() => root.render(null)); vi.restoreAllMocks(); await studioI18n.changeLanguage("en"); });
it("retranslates storage failures without changing the selected tenant or culture", async () => {
  const config = studioConfigSchema.parse({ servers: [{ name: "Test", url: "https://example.test/api", tenancy: { mode: "selection", tenants: [{ id: "a", name: "Tenant A" }, { id: "b", name: "Tenant B" }] } }] });
  function Preferences() {
    const culture = useCulture(); const tenant = useTenant();
    return <><button onClick={() => culture.selectCulture("nl")}>Language</button><button onClick={() => tenant.selectTenant("b")}>Tenant</button><p data-culture={culture.culture}>{culture.error}</p><p data-tenant={tenant.tenantId}>{tenant.error}</p></>;
  }
  vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => { throw new Error("Storage disabled"); });
  await act(() => root.render(<StudioConfigProvider config={config}><LocalizationProvider modules={[]}><TenancyProvider><Preferences /></TenancyProvider></LocalizationProvider></StudioConfigProvider>));
  await act(() => container.querySelectorAll("button").forEach(button => button.click()));
  expect(container.textContent).toContain("Language preference could not be saved.");
  expect(container.textContent).toContain("Tenant selection could not be saved.");
  expect(container.querySelector("[data-culture]")?.getAttribute("data-culture")).toBe("en");
  expect(container.querySelector("[data-tenant]")?.getAttribute("data-tenant")).toBe("a");
  await act(async () => { await studioI18n.changeLanguage("nl"); });
  expect(container.textContent).toContain("De taalvoorkeur kon niet worden opgeslagen.");
  expect(container.textContent).toContain("De tenantkeuze kon niet worden opgeslagen.");
  expect(container.querySelector("[data-tenant]")?.getAttribute("data-tenant")).toBe("a");
});
