import { build } from "vite";

for (const entry of ["shared", "shell", "labels", "weather"]) {
  await build({
    configFile: false,
    define: { "process.env.NODE_ENV": JSON.stringify("production") },
    build: {
      outDir: `dist/${entry}`,
      lib: { entry: `src/${entry}.ts`, formats: ["es"], fileName: () => "entry.js" },
      rollupOptions: {
        external: entry === "shared" ? [] : ["react", "react-dom/client", "@tanstack/react-query", "@elsa-workflows/react-studio-sdk"],
        output: { assetFileNames: "style.css" },
      },
    },
  });
}
