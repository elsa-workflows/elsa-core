import type { ModuleRoute, RouteParams, StudioModule } from "./types";

export interface RouteMatch {
  module: StudioModule;
  route: ModuleRoute;
  params: RouteParams;
}

function split(path: string) {
  return path.split("/").filter(Boolean);
}

/** Matches a URL path against a route pattern; returns params or `undefined`. */
export function matchPath(pattern: string, path: string): RouteParams | undefined {
  const p = split(pattern);
  const s = split(path);
  const params: RouteParams = {};
  for (let i = 0; i < p.length; i++) {
    const seg = p[i];
    if (seg === "*") {
      params["*"] = s.slice(i).join("/");
      return params;
    }
    if (i >= s.length) return undefined;
    if (seg.startsWith(":")) params[seg.slice(1)] = decodeURIComponent(s[i]);
    else if (seg !== s[i]) return undefined;
  }
  return p.length === s.length ? params : undefined;
}

/** Resolves the first enabled module route that matches. Static segments win over parameters by declaration order. */
export function resolveRoute(modules: StudioModule[], path: string): RouteMatch | undefined {
  for (const mod of modules) {
    for (const route of mod.routes ?? []) {
      const params = matchPath(route.path, path);
      if (params) return { module: mod, route, params };
    }
  }
  return undefined;
}

/** Builds a concrete href from a pattern, e.g. `href("/workflows/definitions/:id/edit", { id })`. */
export function href(pattern: string, params: RouteParams = {}) {
  return (
    "/" +
    split(pattern)
      .map((seg) => (seg.startsWith(":") ? encodeURIComponent(params[seg.slice(1)] ?? "") : seg))
      .join("/")
  );
}
