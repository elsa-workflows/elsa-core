import { chromium, expect } from "@playwright/test";

const origin = `http://localhost:${process.env.ELSA_STUDIO_PORT ?? "5201"}`;
const api = `http://localhost:${process.env.ELSA_SERVER_PORT ?? "5001"}/elsa/api`;
const deadline = Date.now() + 90_000;
let ready = false;
while (!ready && Date.now() < deadline) {
  try {
    const response = await fetch(`${api}/identity/login`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: "admin", password: "password" }),
      signal: AbortSignal.timeout(2000),
    });
    ready = response.status === 200;
    await response.arrayBuffer();
  } catch { /* The reference backend may still be applying migrations. */ }
  if (!ready) await new Promise(resolve => setTimeout(resolve, 500));
}
if (!ready) throw new Error("Compose reference backend did not accept its Development account");

const config = await fetch(`${origin}/studio-preview/studio-config`).then(response => {
  if (!response.ok) throw new Error(`Studio runtime configuration failed: ${response.status}`);
  return response.json();
});
if (config.servers[0].url !== api || config.auth.provider !== "elsa-identity" || !config.title.includes("Preview"))
  throw new Error("Compose STUDIO_* overrides were not applied");

const browser = await chromium.launch(process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
  ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH } : {});
try {
  const page = await browser.newPage();
  await page.goto(`${origin}/studio-preview/login`);
  await page.getByLabel("Username", { exact: true }).fill("admin");
  await page.getByLabel("Password", { exact: true }).fill("password");
  const login = page.waitForResponse(response => response.url() === `${api}/identity/login` && response.request().method() === "POST");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  if ((await login).status() !== 200) throw new Error("Browser sign-in failed against the Compose backend");
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();
  const definitions = page.waitForResponse(response => response.url().startsWith(`${api}/workflow-definitions?`) && response.request().method() === "GET");
  await page.goto(`${origin}/studio-preview/workflows/definitions`);
  if ((await definitions).status() !== 200) throw new Error("Authenticated definition listing failed");
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  console.log("PASS: paired containers apply runtime config, sign in, list real workflows and reload a deep link");
} finally {
  await browser.close();
}
