import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { expect, it } from "vitest";

const script = fileURLToPath(new URL("../../scripts/test-settings-roundtrip.mjs", import.meta.url));

for (const cleanupFails of [false, true]) {
  it(`retains the original settings-check failure when cleanup ${cleanupFails ? "also fails" : "succeeds"}`, () => {
    // Run the real CLI against deterministic HTTP responses, with no server or credentials.
    const bootstrap = `globalThis.fetch = async (url, options) => {
      if (String(url).endsWith('/identity/login')) return Response.json({isAuthenticated:true,accessToken:'fixture'});
      if (options.method === 'DELETE') return ${cleanupFails ? "Response.json({title:'Cleanup fixture failure'},{status:500})" : "new Response(null,{status:204})"};
      return Response.json({workflowDefinition:{definitionId:'temporary',version:1}});
    };`;
    const result = spawnSync(process.execPath, ["--import", `data:text/javascript,${encodeURIComponent(bootstrap)}`, script], {
      env: { ...process.env, ELSA_TEST_API_URL: "https://fixture.invalid/api", ELSA_TEST_USERNAME: "fixture", ELSA_TEST_PASSWORD: "fixture" },
      encoding: "utf8", timeout: 10_000,
    });
    expect(result.status).toBe(1);
    expect(result.stderr).toContain("AssertionError");
    expect(result.stderr).toContain("1 !== 2");
    if (cleanupFails) {
      expect(result.stderr).toContain("Settings check and temporary definition cleanup failed");
      expect(result.stderr).toContain("Cleanup fixture failure");
    } else expect(result.stderr).not.toContain("AggregateError");
  });
}
