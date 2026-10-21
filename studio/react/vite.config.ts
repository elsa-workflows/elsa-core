import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  base: "./",
  plugins: [react(), tailwindcss()],
  resolve: { alias: {
    "@": fileURLToPath(new URL("./src", import.meta.url)),
    "@elsa-workflows/react-studio-api-client": fileURLToPath(new URL("./packages/api-client/src", import.meta.url)),
    "@elsa-workflows/react-studio-auth": fileURLToPath(new URL("./packages/auth/src", import.meta.url)),
    "@elsa-workflows/react-studio-ui": fileURLToPath(new URL("./packages/ui/src", import.meta.url)),
    "@elsa-workflows/react-studio-sdk": fileURLToPath(new URL("./packages/sdk/src", import.meta.url)),
  } },
  build: { outDir: "dist", emptyOutDir: true },
  server: {
    proxy: {
      "/studio-config": {
        target: "http://127.0.0.1:5201",
        rewrite: path => "/studio-preview" + path,
      },
    },
  },
});
