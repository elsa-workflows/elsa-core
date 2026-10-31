import { expect, test } from "@playwright/test";

test.beforeEach(async ({ page }) => {
  // Only the backend is stubbed: HTML, built ESM/CSS and runtime config come from ASP.NET.
  await page.route("**/elsa/api/**", async route => {
    await route.fulfill({ json: { items: [], totalCount: 0 } });
  });
});

test("deep links, search state, SPA links and browser history stay under the mount", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.goto("/studio-preview/workflows/definitions?search=original");
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  await expect(page.locator("input[placeholder*='Search']").first()).toHaveValue("original");
  await page.reload();
  await expect(page.locator("input[placeholder*='Search']").first()).toHaveValue("original");
  const marker = await page.evaluate(() => { window.name = "spa-navigation"; return performance.timeOrigin; });
  await page.getByRole("link", { name: "Dashboard", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  expect(await page.evaluate(() => performance.timeOrigin)).toBe(marker);
  await page.goBack();
  await expect(page).toHaveURL(/\/studio-preview\/workflows\/definitions\?search=original$/);
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  expect(errors).toEqual([]);
});

test("dashboard legend preserves the mount and instance filter during SPA navigation", async ({ page }) => {
  // Exercise the retained core-API fallback, whose count data produces the legend.
  await page.route("**/elsa/api/dashboard/**", route => route.fulfill({ status: 404 }));
  await page.goto("/studio-preview/");
  const link = page.getByRole("link", { name: /^Executing/ });
  await expect(link).toHaveAttribute("href", "/studio-preview/workflows/instances?subStatus=Executing");
  const marker = await page.evaluate(() => performance.timeOrigin);
  await link.click();
  await expect(page).toHaveURL(/\/studio-preview\/workflows\/instances\?subStatus=Executing$/);
  await expect(page.getByRole("heading", { name: "Workflow instances", exact: true })).toBeVisible();
  expect(await page.evaluate(() => performance.timeOrigin)).toBe(marker);
  await page.reload();
  await expect(page).toHaveURL(/\/studio-preview\/workflows\/instances\?subStatus=Executing$/);
  await expect(page.getByRole("heading", { name: "Workflow instances", exact: true })).toBeVisible();
});

test("host title, Preview label and CSS load without a Next runtime", async ({ page }) => {
  const failed: string[] = [];
  page.on("response", response => {
    if (/\.(js|css)$/.test(response.url()) && response.status() !== 200) failed.push(response.url());
  });
  await page.goto("/studio-preview/");
  await expect(page).toHaveTitle("Elsa test Studio · Preview");
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await expect(page.getByText("Preview", { exact: true })).toBeVisible();
  expect(await page.locator("body").evaluate(body => getComputedStyle(body).backgroundColor)).not.toBe("rgba(0, 0, 0, 0)");
  expect(failed).toEqual([]);
});

test("missing assets return 404 rather than a SPA document", async ({ request }) => {
  const response = await request.get("/studio-preview/assets/missing.js");
  expect(response.status()).toBe(404);
  expect(response.headers()["content-type"] ?? "").not.toContain("text/html");
});

test("an incompatible module is rejected before import and a broken contribution leaves the shell usable", async ({ page }) => {
  let incompatibleFetched = false;
  await page.route("**/incompatible-module.js", route => {
    incompatibleFetched = true;
    return route.fulfill({ contentType: "text/javascript", body: "throw new Error('must not import');" });
  });
  await page.route("**/broken-module.js", route => route.fulfill({ contentType: "text/javascript", body: `
    export function register(api) {
      api.contribute("headerItems", { id: "broken-header", component: () => { throw new Error("broken header fixture"); } });
    }
  ` }));
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch();
    const manifest = await response.json();
    manifest.modules.push(
      { id: "incompatible", version: "1", core: true, hostApiVersion: 2, sdkApiVersion: 1, entry: "/incompatible-module.js" },
      { id: "broken", version: "1", core: true, hostApiVersion: 1, sdkApiVersion: 1, entry: "/broken-module.js" },
    );
    await route.fulfill({ json: manifest });
  });
  await page.goto("/studio-preview/");
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await expect(page.getByRole("alert").filter({ hasText: "requires host API 2" })).toBeVisible();
  await expect(page.getByRole("alert").filter({ hasText: "Module broken could not render broken-header" })).toBeVisible();
  await page.getByRole("link", { name: "Definitions", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  expect(incompatibleFetched).toBe(false);
});

test("OIDC authorization uses a callback under the host mount", async ({ page }) => {
  await page.route("**/studio-config", route => route.fulfill({ json: {
    servers: [{ url: "http://127.0.0.1:5001/elsa/api" }],
    auth: { provider: "oidc", oidc: { authority: "https://identity.example.invalid", clientId: "preview" } },
  } }));
  await page.route("https://identity.example.invalid/**", route => {
    if (new URL(route.request().url()).pathname.endsWith("/.well-known/openid-configuration")) {
      return route.fulfill({ json: {
        issuer: "https://identity.example.invalid",
        authorization_endpoint: "https://identity.example.invalid/authorize",
        token_endpoint: "https://identity.example.invalid/token",
        jwks_uri: "https://identity.example.invalid/jwks",
        response_types_supported: ["code"],
        subject_types_supported: ["public"],
        id_token_signing_alg_values_supported: ["RS256"],
      } });
    }
    return route.fulfill({ contentType: "text/html", body: "Identity provider fixture" });
  });
  await page.goto("/studio-preview/login");
  const authorization = page.waitForRequest(request => request.url().startsWith("https://identity.example.invalid/authorize?"));
  await page.getByRole("button", { name: "Sign in with identity.example.invalid" }).click();
  const url = new URL((await authorization).url());
  expect(url.searchParams.get("redirect_uri")).toBe("http://127.0.0.1:5201/studio-preview/auth/callback");
  expect(url.searchParams.get("code_challenge_method")).toBe("S256");
});

test("the preview command serves the built module graph and its import map", async ({ page }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  await page.goto("http://127.0.0.1:4173/studio-preview/workflows/definitions");
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  await expect(page.locator('script[type="importmap"]')).toHaveCount(1);
  await page.getByRole("link", { name: "Dashboard", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  expect(errors).toEqual([]);
});

test("a failing optional provider preserves shell children and navigation", async ({ page }) => {
  await page.route("**/broken-provider.js", route => route.fulfill({ contentType: "text/javascript", body: `
    import { createElement as h, Fragment, useState } from "react";
    function Crash() { throw new Error("optional provider UI failed"); }
    function Provider({ children }) {
      const [broken, setBroken] = useState(false);
      return h(Fragment, null, children, broken ? h(Crash) : h("button", { onClick: () => setBroken(true) }, "Break optional provider"));
    }
    export function register(api) { api.contribute("providers", { id: "optional-ui", component: Provider }); }
  ` }));
  await page.route("**/studio-manifest", async route => {
    const response = await route.fetch();
    const manifest = await response.json();
    manifest.modules.push({ id: "broken-provider", version: "1", core: true, hostApiVersion: 1, sdkApiVersion: 1, entry: "/broken-provider.js" });
    await route.fulfill({ json: manifest });
  });
  await page.goto("/studio-preview/");
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Break optional provider", exact: true }).click();
  await expect(page.getByRole("alert").filter({ hasText: "Module broken-provider could not render optional-ui" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Definitions", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
});
