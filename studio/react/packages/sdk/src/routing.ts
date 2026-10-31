import type { ModuleRoute, RouteParams, StudioModule } from "./modules.js";

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

/** Resolves enabled routes by explicit order and stable module/slot IDs. */
export function resolveRoute(modules: StudioModule[], path: string): RouteMatch | undefined {
  const routes = modules.flatMap(module => (module.routes ?? []).map(route => ({ module, route })));
  routes.sort((a, b) => (a.route.order ?? 0) - (b.route.order ?? 0) ||
    compare(a.module.id, b.module.id) || compare(a.route.id ?? a.route.path, b.route.id ?? b.route.path));
  for (const { module, route } of routes) {
    const params = matchPath(route.path, path);
    if (params) return { module, route, params };
  }
  return undefined;
}

function compare(a: string, b: string) { return a < b ? -1 : a > b ? 1 : 0; }

/** Builds a concrete href from a pattern, e.g. `href("/workflows/definitions/:id/edit", { id })`. */
export function href(pattern: string, params: RouteParams = {}) {
  return (
    "/" +
    split(pattern)
      .map((seg) => (seg.startsWith(":") ? encodeURIComponent(params[seg.slice(1)] ?? "") : seg))
      .join("/")
  );
}
