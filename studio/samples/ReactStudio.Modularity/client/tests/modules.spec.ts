import { test, expect } from "@playwright/test";
import { readFile, writeFile } from "node:fs/promises";

test("packed modules share React, query state and theme under a nonce CSP", async ({ page, request }) => {
  const errors: string[] = [];
  page.on("pageerror", error => errors.push(error.message));
  const response = await page.goto("/studio-preview/labels");
  expect(response?.headers()["content-security-policy"]).toContain("'nonce-");
  await expect(page.getByRole("heading", { name: "Labels from NuGet" })).toBeVisible();
  await expect(page.locator(".labels-module")).toHaveCSS("border-left-color", "rgb(84, 112, 233)");
  await expect(page.getByRole("alert")).toContainText(["requires host 1 and SDK 0", "requires host 0 and SDK 1"]);
  await expect(page.getByRole("textbox", { name: "Label editor" })).toBeVisible();
  await page.getByRole("button", { name: "Add label" }).click();
  await page.getByRole("button", { name: "Toggle theme" }).click();
  await page.getByRole("link", { name: "Weather", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Third-party weather from NuGet" })).toBeVisible();
  await expect(page.locator(".weather-module")).toHaveCSS("border-left-color", "rgb(38, 140, 102)");
  await expect(page.getByTestId("weather-counter")).toHaveText("Shared count: 1");
  await expect(page.getByText("Theme: dark")).toBeVisible();
  await page.screenshot({ path: "test-results/packed-modules.png" });
  await page.goBack();
  await expect(page.getByTestId("labels-counter")).toHaveText("Shared count: 1");
  expect(errors).toEqual([]);

  const manifest = await (await request.get("/studio-preview/manifest")).json();
  expect(manifest.modules.map((x: { id: string }) => x.id)).toEqual(["labels", "weather"]);
  expect(manifest.diagnostics.map((x: { id: string }) => x.id)).toEqual(["incompatible-host", "incompatible-sdk"]);
  expect((await request.get("/studio-preview/modules/incompatible-host/0.0.1-spike/entry.js")).status()).toBe(404);
  expect((await request.get("/studio-preview/modules/incompatible-sdk/0.0.1-spike/entry.js")).status()).toBe(404);
  for (const module of manifest.modules) {
    expect(module.entry).toContain(`/${module.version}/`);
    expect((await request.get(module.entry)).headers()["content-type"]).toContain("javascript");
    expect((await request.get(module.css)).headers()["content-type"]).toContain("css");
  }
});

test("disabled CShells features contribute neither manifests nor assets", async ({ page, request }) => {
  const base = "http://127.0.0.1:5102";
  const manifest = await (await request.get(base + "/studio-preview/manifest")).json();
  expect(manifest.modules.map((x: { id: string }) => x.id)).toEqual(["labels"]);
  expect(manifest.diagnostics).toEqual([]);
  expect((await request.get(base + "/studio-preview/modules/weather/0.0.1-spike/entry.js")).status()).toBe(404);
  await page.goto(base + "/studio-preview/labels");
  await expect(page.getByRole("heading", { name: "Labels from NuGet" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Weather", exact: true })).toHaveCount(0);
});

test("a module render failure leaves navigation and other modules usable", async ({ page }) => {
  await page.goto("/studio-preview/weather/error");
  await expect(page.getByRole("alert").filter({ hasText: "Module weather-error could not render: Weather view failed" })).toBeVisible();
  await expect(page.getByRole("textbox", { name: "Label editor" })).toBeVisible();
  await page.getByRole("link", { name: "Labels", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Labels from NuGet" })).toBeVisible();
  await page.getByRole("button", { name: "Add label" }).click();
  await expect(page.getByTestId("labels-counter")).toHaveText("Shared count: 1");
});

test("Vite hot updates travel through the ASP.NET host with its nonce CSP", async ({ page }) => {
  const base = "http://127.0.0.1:5103";
  const source = new URL("../src/labels.ts", import.meta.url);
  const original = await readFile(source, "utf8");
  const websocket = page.waitForEvent("websocket");
  await page.goto(base + "/studio-preview/labels");
  const socket = await websocket;
  expect(socket.url()).toContain("127.0.0.1:5103");
  await expect(page.getByRole("heading", { name: "Labels from NuGet" })).toBeVisible();
  await page.getByRole("button", { name: "Add label" }).click();
  try {
    await writeFile(source, original.replace("Labels from NuGet", "Labels hot update"));
    await expect(page.getByRole("heading", { name: "Labels hot update" })).toBeVisible({ timeout: 20_000 });
    await page.getByRole("link", { name: "Weather", exact: true }).click();
    await expect(page.getByTestId("weather-counter")).toHaveText("Shared count: 1");
  } finally {
    await writeFile(source, original);
  }
});
