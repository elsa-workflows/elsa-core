import { defineConfig } from "vite";
import { fileURLToPath } from "node:url";

export default defineConfig({
  base: "/studio-preview/vite/",
  resolve: {
    alias: { "@elsa-workflows/react-studio-sdk": fileURLToPath(new URL("./src/sdk.ts", import.meta.url)) },
    dedupe: ["react", "react-dom", "@tanstack/react-query"],
  },
  server: {
    port: 5173,
    strictPort: true,
    hmr: { clientPort: 5103 },
  },
});
