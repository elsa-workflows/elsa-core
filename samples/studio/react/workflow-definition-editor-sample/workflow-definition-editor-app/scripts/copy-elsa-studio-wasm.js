// Copies the Elsa Studio Blazor WebAssembly assets into public/, where Vite serves them from the site root.
import {cpSync, mkdirSync} from "node:fs";

const source = "node_modules/@elsa-workflows/elsa-studio-wasm";

mkdirSync("public", {recursive: true});

for (const asset of ["_content", "_framework", "appsettings.json", "Elsa.Studio.Host.CustomElements.styles.css"])
    cpSync(`${source}/${asset}`, `public/${asset}`, {recursive: true});
