import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "tests",
  workers: 1,
  use: {
    baseURL: "http://127.0.0.1:5101",
    launchOptions: { executablePath: process.env.CHROMIUM_PATH ?? "/usr/bin/chromium" },
  },
  webServer: [
    { command: "dotnet run --project ../Host --no-build --no-restore --urls http://127.0.0.1:5101", url: "http://127.0.0.1:5101/studio-preview/manifest", reuseExistingServer: false },
    { command: "dotnet run --project ../Host --no-build --no-restore --urls http://127.0.0.1:5102 --Spike:ThirdPartyEnabled=false", url: "http://127.0.0.1:5102/studio-preview/manifest", reuseExistingServer: false },
    { command: "pnpm dev", url: "http://127.0.0.1:5173/studio-preview/vite/@vite/client", reuseExistingServer: false },
    { command: "dotnet run --project ../Host --no-build --no-restore --urls http://127.0.0.1:5103 --Spike:ViteUrl=http://127.0.0.1:5173", url: "http://127.0.0.1:5103/studio-preview/manifest", reuseExistingServer: false },
  ],
});
