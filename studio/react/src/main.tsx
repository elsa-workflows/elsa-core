
import { translate } from "@elsa-workflows/react-studio-sdk/localization";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { studioConfigSchema } from "@/config/schema";
import { Providers } from "./providers";
import { studioBasePath } from "@elsa-workflows/react-studio-sdk/navigation/base-path";
import type { RegisterModule, StudioModuleManifest } from "@elsa-workflows/react-studio-sdk";
import { Application } from "@elsa-workflows/react-studio-sdk/components";
import { StudioRegistry } from "@elsa-workflows/react-studio-sdk/host";
import catalog from "../module-catalog.json";
import "./globals.css";

async function start() {
  const response = await fetch(new URL("studio-config", document.baseURI), { cache: "no-store" });
  if (!response.ok) throw new Error(translate("Configuration could not be loaded ({{value0}})", { value0: response.status }));
  const config = studioConfigSchema.parse(await response.json());
  const registry = new StudioRegistry();
  const manifestResponse = await fetch(new URL("studio-manifest", document.baseURI), { cache: "no-store" });
  if (!manifestResponse.ok) throw new Error(translate("Studio modules could not be loaded ({{value0}})", { value0: manifestResponse.status }));
  const manifest = await manifestResponse.json() as { modules: StudioModuleManifest[] };
  await Promise.all(manifest.modules.filter(module => module.core || config.features[module.id]?.enabled).map(async module => {
    if (import.meta.env.DEV) {
      const source = catalog.find(item => item.id === module.id);
      if (source) module = { ...module, entry: `/${source.client}/entry.tsx`, styles: [] };
    }
    const loaded = await registry.load(module, entry => import(/* @vite-ignore */ entry) as Promise<{ register: RegisterModule }>);
    if (loaded) for (const href of module.styles ?? []) {
      const link = document.createElement("link");
      link.rel = "stylesheet";
      link.href = href;
      document.head.append(link);
    }
  }));
  if (!registry.components().Application) throw new Error("The Studio Shell module could not be activated. " +
    registry.diagnostics.map(diagnostic => diagnostic.message).join(" "));
  document.title = `${config.title} · Preview`;
  document.documentElement.dataset.elsaTheme = config.ui.theme;
  createRoot(document.getElementById("root")!).render(
    <BrowserRouter basename={studioBasePath() || "/"}>
      <Providers config={config} modules={registry.studioModules()} components={registry.components()}>
        {registry.diagnostics.length > 0 && <aside role="alert">
          {registry.diagnostics.map((diagnostic, index) => <p key={`${diagnostic.moduleId}:${index}`}>
            {diagnostic.moduleId}: {diagnostic.message}
          </p>)}
        </aside>}
        <Application />
      </Providers>
    </BrowserRouter>,
  );
}

void start().catch((error: Error) => {
  const alert = document.createElement("p");
  alert.setAttribute("role", "alert");
  alert.textContent = `Elsa Studio Preview: ${error.message}`;
  document.getElementById("root")!.replaceChildren(alert);
});
