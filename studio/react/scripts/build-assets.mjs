import { cpSync, mkdirSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { join, relative } from "node:path";
import { build } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import ts from "typescript";
import { aliases, externalBare, root } from "./assets-config.mjs";

const catalog = JSON.parse(readFileSync(join(root, "module-catalog.json"), "utf8"));
function files(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    if (["node_modules", "dist"].includes(entry.name)) return [];
    const path = join(directory, entry.name);
    return entry.isDirectory() ? files(path) : /\.[cm]?tsx?$/.test(entry.name) && !/\.test\./.test(entry.name) ? [path] : [];
  });
}

mkdirSync(join(root, "node_modules/.cache"), { recursive: true });
const temporary = mkdtempSync(join(root, "node_modules/.cache/react-studio-build-"));
try {
  const input = {};
  const imports = {};
  const dependencies = new Map([["react", false], ["react/jsx-runtime", false]]);
  const packageFiles = [];
  for (const name of ["api-client", "sdk", "auth", "ui"]) {
    for (const path of files(join(root, "packages", name, "src"))) {
      const subpath = relative(join(root, "packages", name, "src"), path).replaceAll("\\", "/").replace(/\.[cm]?tsx?$/, "");
      const key = `${name}/${subpath}`;
      input[key] = path;
      imports[`@elsa-workflows/react-studio-${name}/${subpath}`] = `shared/${key}.js`;
      if (subpath === "index") imports[`@elsa-workflows/react-studio-${name}`] = `shared/${key}.js`;
      packageFiles.push(path);
    }
  }
  for (const path of [...packageFiles, ...files(join(root, "src")), ...catalog.flatMap(module => files(join(root, module.client)))]) {
    const source = ts.createSourceFile(path, readFileSync(path, "utf8"), ts.ScriptTarget.Latest, true);
    for (const statement of source.statements) {
      if (!ts.isImportDeclaration(statement) || !ts.isStringLiteral(statement.moduleSpecifier) || statement.importClause?.isTypeOnly) continue;
      const specifier = statement.moduleSpecifier.text;
      if (!externalBare(specifier) || specifier.startsWith("@elsa-workflows/")) continue;
      const bindings = statement.importClause?.namedBindings;
      if (!statement.importClause?.name && bindings && ts.isNamedImports(bindings) && bindings.elements.every(element => element.isTypeOnly)) continue;
      dependencies.set(specifier, dependencies.get(specifier) || !!statement.importClause?.name);
    }
  }
  for (const [specifier, hasDefault] of [...dependencies].sort(([a], [b]) => a.localeCompare(b))) {
    const key = `dependencies/${specifier.replaceAll("@", "").replaceAll("/", "-")}`;
    const path = join(temporary, `${Object.keys(input).length}.js`);
    // CommonJS entrypoints such as react/jsx-runtime cannot forward their
    // synthetic names through export *. Materialize the public exports once.
    const runtime = await import(specifier);
    const names = [...new Set([...Object.keys(runtime), ...Object.keys(runtime.default ?? {})])]
      .filter(name => name !== "default" && /^[A-Za-z_$][\w$]*$/.test(name)).sort();
    writeFileSync(path, `import * as runtime from ${JSON.stringify(specifier)};\nfunction defaultOf(value) { return Reflect.get(value, "default"); }\nconst values = Object.assign({}, defaultOf(runtime), runtime);\n` +
      names.map((name, index) => `const value${index} = values[${JSON.stringify(name)}]; export { value${index} as ${name} };`).join("\n") +
      (hasDefault ? "\nexport default runtime.default;\n" : "\n"));
    input[key] = path;
    imports[specifier] = `shared/${key}.js`;
  }
  await build({
    configFile: false, root, plugins: [react()], resolve: { alias: aliases },
    build: { outDir: "dist/shared", emptyOutDir: true, minify: true,
      rollupOptions: { input, preserveEntrySignatures: "strict", output: { format: "es", entryFileNames: "[name].js", chunkFileNames: "chunks/[name]-[hash].js" } },
    },
  });
  mkdirSync(join(root, "dist"), { recursive: true });
  writeFileSync(join(root, "dist/shared-imports.json"), JSON.stringify({ imports }));

  for (const module of catalog) {
    const output = join(root, module.client, "../wwwroot");
    await build({ configFile: false, root, plugins: [react(), tailwindcss()], resolve: { alias: aliases },
      build: { outDir: output, emptyOutDir: true, cssCodeSplit: false,
        rollupOptions: { input: join(root, module.client, "entry.tsx"), external: externalBare, preserveEntrySignatures: "strict",
          output: { format: "es", entryFileNames: "entry.js", inlineDynamicImports: true, assetFileNames: asset => asset.names.some(name => name.endsWith(".css")) ? "module.css" : "[name]-[hash][extname]" } },
      },
    });
    // Vite omits comment-only stylesheets. The manifest still publishes a CSS
    // resource so modules can add their own styles without changing its shape.
    if (!readdirSync(output).includes("module.css")) writeFileSync(join(output, "module.css"), "/* No module-specific styles. */\n");
    // Docker copies this generated mirror without copying workspace node_modules links.
    const mirror = join(root, "module-assets", module.packageId, "wwwroot");
    rmSync(mirror, { recursive: true, force: true });
    cpSync(output, mirror, { recursive: true });
  }
  await build({ root, build: { emptyOutDir: false } });
} finally {
  rmSync(temporary, { recursive: true, force: true });
}
