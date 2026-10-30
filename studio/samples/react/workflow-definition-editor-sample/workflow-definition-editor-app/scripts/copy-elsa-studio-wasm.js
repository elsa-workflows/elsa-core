// Copies the Elsa Studio Blazor WebAssembly assets into public/, where Vite serves them from the site root.
import {cpSync, mkdirSync, rmSync} from "node:fs";

const source = "node_modules/@elsa-workflows/elsa-studio-wasm";

mkdirSync("public", {recursive: true});

for (const asset of ["_content", "_framework", "appsettings.json", "Elsa.Studio.Host.CustomElements.styles.css"]) {
    // Replace rather than merge, so files a newer package no longer ships do not linger.
    rmSync(`public/${asset}`, {recursive: true, force: true});
    cpSync(`${source}/${asset}`, `public/${asset}`, {recursive: true});
}
