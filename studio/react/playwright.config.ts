import { defineConfig } from "@playwright/test";
import { fileURLToPath } from "node:url";

const fixture = fileURLToPath(new URL("./tests/studio.config.json", import.meta.url));
export default defineConfig({
  testDir: "tests",
  workers: 1,
  use: {
    baseURL: "http://127.0.0.1:5201",
    browserName: "chromium",
    launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
      ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH } : {},
  },
  webServer: {
    command: "dotnet run --project dotnet/Elsa.ReactStudio.Server --no-build --no-restore --no-launch-profile --urls http://127.0.0.1:5201",
    env: { STUDIO_CONFIG_PATH: fixture },
    url: "http://127.0.0.1:5201/studio-preview/studio-config",
    reuseExistingServer: false,
    timeout: 60_000,
  },
});
