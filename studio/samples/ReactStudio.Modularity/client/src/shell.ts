import { Component, createElement as h, useEffect, useState, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeContext, ordered, type StudioApi } from "@elsa-workflows/react-studio-sdk";
import "./shell.css";

const base = "/studio-preview";
interface Manifest {
  modules: { id: string; version: string; entry: string; css: string | null }[];
  diagnostics: { id: string; message: string }[];
}

const manifest: Manifest = await fetch(`${base}/manifest`).then(response => {
  if (!response.ok) throw new Error(`Manifest request failed: ${response.status}`);
  return response.json();
});
const api: StudioApi = { routes: [], navigation: [], editors: [] };
for (const module of manifest.modules) {
  try {
    if (module.css) {
      const link = document.createElement("link");
      link.rel = "stylesheet";
      link.href = module.css;
      document.head.append(link);
    }
    const entry = await import(/* @vite-ignore */ module.entry);
    entry.register(api);
  } catch (error) {
    manifest.diagnostics.push({ id: module.id, message: `Module ${module.id} could not load: ${String(error)}` });
  }
}

const client = new QueryClient();
class ModuleBoundary extends Component<{ id: string; children?: ReactNode }, { message: string | null }> {
  state = { message: null as string | null };

  static getDerivedStateFromError(error: unknown) {
    return { message: error instanceof Error ? error.message : String(error) };
  }

  render() {
    return this.state.message
      ? h("p", { role: "alert" }, `Module ${this.props.id} could not render: ${this.state.message}`)
      : this.props.children;
  }
}

function Shell() {
  const [theme, setTheme] = useState("light");
  const [path, setPath] = useState(location.pathname.slice(base.length) || "/labels");
  const [, invalidate] = useState(0);
  useEffect(() => {
    const popstate = () => setPath(location.pathname.slice(base.length) || "/labels");
    const update = (event: Event) => {
      (event as CustomEvent<(api: StudioApi) => void>).detail(api);
      invalidate(value => value + 1);
    };
    window.addEventListener("popstate", popstate);
    document.addEventListener("studio:module-update", update);
    return () => {
      window.removeEventListener("popstate", popstate);
      document.removeEventListener("studio:module-update", update);
    };
  }, []);
  const route = ordered(api.routes).find(x => x.value.path === path);
  return h(QueryClientProvider, { client },
    h(ThemeContext.Provider, { value: { theme, setTheme } },
      h("h1", null, "React Studio Preview — modularity spike"),
      h("button", { onClick: () => setTheme(value => value === "light" ? "dark" : "light") }, "Toggle theme"),
      h("nav", null, ...ordered(api.navigation).map(item => h("a", {
        key: item.id,
        href: base + item.value.path,
        onClick: (event: { preventDefault(): void }) => {
          event.preventDefault();
          history.pushState(null, "", base + item.value.path);
          setPath(item.value.path);
        },
      }, item.value.label))),
      ...manifest.diagnostics.map(item => h("p", { key: item.id, role: "alert" }, item.message)),
      route ? h(ModuleBoundary, { id: route.id, key: route.id }, h(route.value.component)) : h("p", null, "Page not found"),
      h("section", { "aria-label": "UI hint editors" }, ...ordered(api.editors).map(item =>
        h(ModuleBoundary, { id: item.id, key: item.id }, h(item.value.component))))));
}

createRoot(document.getElementById("root")!).render(h(Shell));
