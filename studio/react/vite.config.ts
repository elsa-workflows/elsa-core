import { fileURLToPath } from "node:url";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { aliases, externalBare } from "./scripts/assets-config.mjs";

export default defineConfig({
  base: "./",
  plugins: [react(), tailwindcss()],
  resolve: { alias: {
    "@": fileURLToPath(new URL("./src", import.meta.url)),
    ...aliases,
  } },
  build: { outDir: "dist", emptyOutDir: false, rollupOptions: { external: externalBare } },
  server: {
    proxy: {
      "/studio-config": {
        target: "http://127.0.0.1:5201",
        rewrite: path => "/studio-preview" + path,
      },
      "/studio-manifest": {
        target: "http://127.0.0.1:5201",
        rewrite: path => "/studio-preview" + path,
      },
    },
  },
});
