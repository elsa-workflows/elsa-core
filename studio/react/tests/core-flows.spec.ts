import { expect, test, type Page, type Route } from "@playwright/test";

/**
 * Core React studio preview journeys from #8986: sign-in, the workflow list, designer
 * create/edit/save/publish/run, the instance viewer and journal, and settings.
 *
 * HTML, ESM and CSS come from the ASP.NET host (the same `pnpm test:browser` setup CI uses).
 * Elsa API responses are an in-memory backend so the clicks, dialogs and saved payloads stay
 * obvious. Dedicated specs cover expressions, tenancy, BPMN, localization and activity settings.
 */

const api = "http://127.0.0.1:5001/elsa/api";

function jwt(name: string) {
  return "stub." + Buffer.from(JSON.stringify({
    sub: name, name, exp: Math.floor(Date.now() / 1000) + 3600,
  })).toString("base64url") + ".stub";
}

function writeLineDescriptor() {
  return {
    typeName: "Elsa.WriteLine", name: "WriteLine", displayName: "Write line", version: 1,
    category: "Console", kind: "Action", isBrowsable: true,
    inputs: [{ name: "Text", typeName: "String", uiHint: "singleline", isWrapped: true, defaultSyntax: "Literal" }],
    outputs: [], ports: [],
  };
}

function emptyPaged() {
  return { items: [], totalCount: 0 };
}

type SavedDefinition = Record<string, unknown> & {
  name?: string;
  root?: { type?: string; activities?: { type?: string }[] };
};

function hasWriteLine(root: SavedDefinition["root"]) {
  if (!root) return false;
  if (String(root.type ?? "").includes("WriteLine")) return true;
  return (root.activities ?? []).some(activity => String(activity.type ?? "").includes("WriteLine"));
}

async function waitForFinishedInstance(page: Page, instanceId: string) {
  await expect.poll(async () => {
    const instance = await page.evaluate(async ({ api, instanceId }) => {
      const response = await fetch(`${api}/workflow-instances/${instanceId}`);
      if (!response.ok) return { subStatus: `http-${response.status}` };
      return await response.json() as { subStatus?: string };
    }, { api, instanceId });
    if (instance.subStatus === "Faulted") {
      throw new Error(`Workflow instance ${instanceId} faulted`);
    }
    return instance.subStatus;
  }, { timeout: 30_000 }).toBe("Finished");
}

async function assertJournalSucceeded(page: Page) {
  const journal = page.getByRole("tree", { name: "Workflow journal" });
  await expect(journal).toBeVisible();
  const step = page.getByRole("treeitem").filter({ hasText: /WriteLine/ });
  await expect(step).toBeVisible();
  await step.click();
  await expect(journal.getByText("Completed", { exact: true }).first()).toBeVisible();
  await expect(journal.getByText("Faulted", { exact: true })).toHaveCount(0);
}

async function installPreviewConfig(page: Page) {
  await page.route("**/studio-config", route => route.fulfill({ json: {
    title: "Elsa test Studio",
    servers: [{ name: "Test backend", url: api }],
    auth: { provider: "elsa-identity" },
    features: { settings: { enabled: true }, appearance: { enabled: true } },
  } }));
}

async function installStudioBackend(page: Page) {
  await installPreviewConfig(page);

  const definitions = new Map<string, Record<string, unknown>>();
  const instances = new Map<string, Record<string, unknown>>();
  const journals = new Map<string, object[]>();
  const saves: SavedDefinition[] = [];
  let nextDefinition = 1;
  let nextInstance = 1;

  const fulfill = (route: Route, json: unknown, status = 200) => route.fulfill({ status, json });

  await page.route("**/elsa/api/**", async route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname.replace(/^\/elsa\/api\/?/, "");
    const method = request.method();

    if (path === "identity/login" && method === "POST") {
      const body = request.postDataJSON() as { username?: string; password?: string };
      if (body.username === "admin" && body.password === "password") {
        return fulfill(route, { isAuthenticated: true, accessToken: jwt("admin"), refreshToken: jwt("refresh") });
      }
      return fulfill(route, { isAuthenticated: false }, 401);
    }

    if (path === "identity/logout") return fulfill(route, {});

    if (path === "descriptors/activities") return fulfill(route, { items: [writeLineDescriptor()], totalCount: 1 });
    if (path === "features/installed") return fulfill(route, { items: [] });
    if (path.includes("scripting/javascript/type-definitions")) {
      return route.fulfill({ contentType: "application/x-typescript", body: "" });
    }

    if (path === "workflow-definitions" && method === "GET") {
      const search = (url.searchParams.get("searchTerm") ?? "").toLowerCase();
      const items = [...definitions.values()].filter(item => {
        if (!search) return true;
        return String(item.name ?? "").toLowerCase().includes(search) || String(item.definitionId).includes(search);
      });
      return fulfill(route, { items, totalCount: items.length });
    }

    if (path === "workflow-definitions" && method === "POST") {
      const { model, publish } = request.postDataJSON() as { model: SavedDefinition; publish?: boolean };
      saves.push(model);
      const definitionId = String(model.definitionId ?? `wf-${nextDefinition++}`);
      const previous = definitions.get(definitionId);
      const version = publish ? Number(previous?.version ?? 0) + (previous?.isPublished ? 1 : 0) || 1 : Number(previous?.version ?? 1);
      const saved = {
        ...model,
        id: String(model.id ?? `version-${definitionId}-${version}`),
        definitionId,
        name: model.name,
        description: model.description,
        version: Number(model.version ?? version),
        isLatest: true,
        isPublished: !!publish,
        createdAt: String(previous?.createdAt ?? "2026-10-22T00:00:00Z"),
        variables: model.variables ?? [],
        inputs: model.inputs ?? [],
        outputs: model.outputs ?? [],
        outcomes: model.outcomes ?? [],
        options: model.options ?? {},
        customProperties: model.customProperties ?? {},
        root: model.root,
      };
      definitions.set(definitionId, saved);
      return fulfill(route, { workflowDefinition: saved });
    }

    const byDefinitionId = path.match(/^workflow-definitions\/by-definition-id\/([^/]+)$/);
    if (byDefinitionId) {
      const item = definitions.get(decodeURIComponent(byDefinitionId[1]));
      return item ? fulfill(route, item) : fulfill(route, { title: "Not found" }, 404);
    }

    const byVersionId = path.match(/^workflow-definitions\/by-id\/([^/]+)$/);
    if (byVersionId) {
      const item = [...definitions.values()].find(definition => definition.id === decodeURIComponent(byVersionId[1]));
      return item ? fulfill(route, item) : fulfill(route, { title: "Not found" }, 404);
    }

    const versions = path.match(/^workflow-definitions\/([^/]+)\/versions$/);
    if (versions) {
      const item = definitions.get(decodeURIComponent(versions[1]));
      return fulfill(route, item ? [item] : []);
    }

    const dispatch = path.match(/^workflow-definitions\/([^/]+)\/dispatch$/);
    if (dispatch && method === "POST") {
      const definition = definitions.get(decodeURIComponent(dispatch[1]));
      if (!definition) return fulfill(route, { title: "Not found" }, 404);
      const id = `instance-${nextInstance++}`;
      const now = "2026-10-22T12:00:00Z";
      const root = definition.root as { id?: string; type?: string; activities?: { id: string; type: string; name?: string }[] };
      const child = root.activities?.[0];
      instances.set(id, {
        id, definitionId: definition.definitionId, definitionVersionId: definition.id, version: definition.version,
        name: definition.name, status: "Finished", subStatus: "Finished", incidentCount: 0, createdAt: now, finishedAt: now,
        workflowState: { id, definitionId: definition.definitionId, definitionVersionId: definition.id, definitionVersion: definition.version,
          status: "Finished", subStatus: "Finished", bookmarks: [], incidents: [], activityExecutionContexts: [],
          input: {}, output: {}, properties: {}, createdAt: now, finishedAt: now },
      });
      journals.set(id, [
        { id: "j1", activityInstanceId: "root", activityId: root.id ?? "root", activityType: root.type ?? "Elsa.Flowchart", activityTypeVersion: 1, nodeId: root.id ?? "root", timestamp: now, sequence: 0, eventName: "Started" },
        ...(child ? [
          { id: "j2", activityInstanceId: child.id, parentActivityInstanceId: "root", activityId: child.id, activityType: child.type, activityTypeVersion: 1, activityName: child.name, nodeId: `${root.id ?? "root"}:${child.id}`, timestamp: now, sequence: 1, eventName: "Started" },
          { id: "j3", activityInstanceId: child.id, parentActivityInstanceId: "root", activityId: child.id, activityType: child.type, activityTypeVersion: 1, activityName: child.name, nodeId: `${root.id ?? "root"}:${child.id}`, timestamp: now, sequence: 2, eventName: "Completed" },
        ] : []),
        { id: "j4", activityInstanceId: "root", activityId: root.id ?? "root", activityType: root.type ?? "Elsa.Flowchart", activityTypeVersion: 1, nodeId: root.id ?? "root", timestamp: now, sequence: 3, eventName: "Completed" },
      ]);
      return fulfill(route, { workflowInstanceId: id });
    }

    if (path === "workflow-instances" && method === "POST") {
      const items = [...instances.values()];
      return fulfill(route, { items, totalCount: items.length });
    }

    const instancePath = path.match(/^workflow-instances\/([^/]+)$/);
    if (instancePath && method === "GET") {
      const item = instances.get(instancePath[1]);
      return item ? fulfill(route, item) : fulfill(route, { title: "Not found" }, 404);
    }

    const journalPath = path.match(/^workflow-instances\/([^/]+)\/journal$/);
    if (journalPath && method === "POST") {
      const items = journals.get(journalPath[1]) ?? [];
      return fulfill(route, { items, totalCount: items.length });
    }

    if (path.endsWith("/variables") && method === "GET") return fulfill(route, { items: [] });
    if (path === "activity-executions/list") {
      const workflowInstanceId = url.searchParams.get("workflowInstanceId") ?? "";
      const instance = instances.get(workflowInstanceId);
      const definition = instance ? definitions.get(String(instance.definitionId)) : undefined;
      const root = definition?.root as { activities?: { id: string; type: string; name?: string }[] } | undefined;
      const child = root?.activities?.[0];
      return fulfill(route, {
        items: child ? [{
          id: `exec-${child.id}`, workflowInstanceId, activityId: child.id, activityNodeId: child.id,
          activityType: child.type, activityTypeVersion: 1, activityName: child.name, status: "Completed",
          startedAt: "2026-10-22T12:00:00Z", completedAt: "2026-10-22T12:00:01Z",
        }] : [],
      });
    }
    if (path === "activity-executions/report") return fulfill(route, { stats: [] });

    return fulfill(route, emptyPaged());
  });

  return { saves };
}

async function signIn(page: Page, username = "admin", password = "password") {
  await expect(page.getByLabel("Username", { exact: true })).toBeVisible();
  await page.getByLabel("Username", { exact: true }).fill(username);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
}

test("rejected credentials stay on the sign-in page", async ({ page }) => {
  await installStudioBackend(page);
  await page.goto("/studio-preview/");
  await signIn(page, "admin", "wrong-password");
  await expect(page.getByText("Invalid username or password.", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Username", { exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toHaveCount(0);
});

test("sign-in, list, create, edit, save, publish, run, journal and settings", async ({ page }) => {
  test.setTimeout(60_000);
  const { saves } = await installStudioBackend(page);
  await page.goto("/studio-preview/");
  await signIn(page);
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true })).toBeVisible();

  await page.getByRole("link", { name: "Definitions", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Workflow definitions", exact: true })).toBeVisible();
  await expect(page.getByText("No workflow definitions yet. Create one to get started.")).toBeVisible();

  await page.getByRole("button", { name: "Create workflow", exact: true }).click();
  const create = page.getByRole("dialog", { name: "Create workflow", exact: true });
  await create.getByLabel("Name").fill("Order notice");
  await create.getByLabel("Description").fill("Core-flow fixture");
  await create.getByRole("button", { name: "Create", exact: true }).click();
  await expect(page).toHaveURL(/\/studio-preview\/workflows\/definitions\/[^/]+\/edit$/);
  await expect(page.locator(".react-flow").first()).toBeVisible();

  const properties = page.getByRole("tabpanel", { name: "Properties", exact: true });
  const name = properties.locator("label").filter({ hasText: /^Name[*]?$/ }).locator("..").getByRole("textbox");
  await expect(name).toHaveValue("Order notice");
  await name.fill("Order notice v1");
  await expect.poll(() => saves.at(-1)?.name, { timeout: 15_000 }).toBe("Order notice v1");
  await expect(page.getByText("Saved", { exact: true })).toBeVisible({ timeout: 15_000 });

  await page.getByRole("button", { name: "Add activity", exact: true }).click();
  const palette = page.getByRole("dialog", { name: "Add activity", exact: true });
  await palette.locator("input").fill("Write line");
  await page.getByRole("option").filter({ hasText: "Write line" }).first().click();
  await expect(page.locator(".react-flow__node").first()).toBeVisible();
  await expect.poll(() => hasWriteLine(saves.at(-1)?.root), { timeout: 15_000 }).toBe(true);
  expect(saves.at(-1)?.name).toBe("Order notice v1");
  await expect(page.getByText("Saved", { exact: true })).toBeVisible({ timeout: 15_000 });

  await page.getByRole("button", { name: "Publish", exact: true }).click();
  await expect(page.getByText("Published version 1")).toBeVisible();
  await expect(page.getByText("Published", { exact: true }).first()).toBeVisible();

  await page.getByRole("button", { name: "Run", exact: true }).click();
  const run = page.getByRole("dialog").filter({ hasText: "Dispatches the published version" });
  await run.getByPlaceholder("optional").fill("order-42");
  await run.getByRole("button", { name: "Run", exact: true }).click();
  await expect(page.getByText("Workflow started", { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Open", exact: true }).click();

  await expect(page).toHaveURL(/\/studio-preview\/workflows\/instances\/instance-1\/view$/);
  await waitForFinishedInstance(page, "instance-1");
  await expect(page.getByRole("tab", { name: "Journal", exact: true })).toBeVisible();
  await assertJournalSucceeded(page);

  await page.goto("/studio-preview/settings");
  await expect(page.getByRole("heading", { name: "Settings", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Open Appearance", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Appearance", exact: true })).toBeVisible();
  await expect(page.getByRole("radiogroup", { name: "Appearance mode" })).toBeVisible();
});
