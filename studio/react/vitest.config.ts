import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: { alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) } },
  test: { include: ["src/**/*.test.{ts,tsx}", "packages/*/src/**/*.test.{ts,tsx}", "dotnet/modules/*/Client/**/*.test.{ts,tsx}"], environment: "node" },
});
