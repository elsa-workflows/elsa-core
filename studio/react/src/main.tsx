import { createRoot } from "react-dom/client";
import { BrowserRouter, useLocation } from "react-router-dom";
import { studioConfigSchema } from "@/config/schema";
import { Providers } from "./providers";
import { StudioRouter } from "@/components/shell/studio-router";
import { LoginPage } from "@/components/shell/login-page";
import { AuthCallback } from "@/components/shell/auth-callback";
import { studioBasePath } from "@/lib/navigation/base-path";
import { registerStudioModule } from "@elsa-workflows/react-studio-sdk";
import { StudioRegistry } from "@elsa-workflows/react-studio-sdk/host";
import { studioModules } from "@/modules";
import metadata from "../package.json";
import "./globals.css";

function Application() {
  const { pathname } = useLocation();
  if (pathname === "/login") return <LoginPage />;
  if (pathname === "/auth/callback") return <AuthCallback />;
  return <StudioRouter path={pathname} />;
}

async function start() {
  const response = await fetch(new URL("studio-config", document.baseURI), { cache: "no-store" });
  if (!response.ok) throw new Error(`Configuration could not be loaded (${response.status})`);
  const config = studioConfigSchema.parse(await response.json());
  const registry = new StudioRegistry();
  // The port's bundled modules use the same registration API as independent ESM
  // entries. #8845 replaces their bundled imports with enabled RCL manifests.
  await Promise.all(studioModules.map(module => registry.load({
    id: module.id, name: module.name, description: module.description,
    core: module.core, requiresServerFeatures: module.requiresServerFeatures,
    version: metadata.version, hostApiVersion: 1, sdkApiVersion: 1,
    entry: `${import.meta.url}#${module.id}`,
  }, async () => ({ register: api => registerStudioModule(api, module) }))));
  document.title = `${config.title} · Preview`;
  document.documentElement.dataset.elsaTheme = config.ui.theme;
  createRoot(document.getElementById("root")!).render(
    <BrowserRouter basename={studioBasePath() || "/"}>
      <Providers config={config} modules={registry.studioModules()}>
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
