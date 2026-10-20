import { createRoot } from "react-dom/client";
import { BrowserRouter, useLocation } from "react-router-dom";
import { studioConfigSchema } from "@/config/schema";
import { Providers } from "./providers";
import { StudioRouter } from "@/components/shell/studio-router";
import { LoginPage } from "@/components/shell/login-page";
import { AuthCallback } from "@/components/shell/auth-callback";
import { studioBasePath } from "@/lib/navigation/base-path";
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
  document.title = `${config.title} · Preview`;
  document.documentElement.dataset.elsaTheme = config.ui.theme;
  createRoot(document.getElementById("root")!).render(
    <BrowserRouter basename={studioBasePath() || "/"}>
      <Providers config={config}><Application /></Providers>
    </BrowserRouter>,
  );
}

void start().catch((error: Error) => {
  const alert = document.createElement("p");
  alert.setAttribute("role", "alert");
  alert.textContent = `Elsa Studio Preview: ${error.message}`;
  document.getElementById("root")!.replaceChildren(alert);
});
