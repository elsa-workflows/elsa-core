/**
 * Hrefs into the workflow modules. Kept local (not imported from those modules) so the dashboard
 * never couples to another module's internals; the paths are the public routes documented in
 * `docs/ARCHITECTURE.md`.
 */
const INSTANCES = "/workflows/instances";
const DEFINITIONS = "/workflows/definitions";

export const DashboardLinks = {
  definitions: DEFINITIONS,
  instances: INSTANCES,
  instance: (id: string) => `${INSTANCES}/${encodeURIComponent(id)}/view`,
  definition: (definitionId: string) => `${DEFINITIONS}/${encodeURIComponent(definitionId)}/edit`,
  /** Instances list pre-filtered through its query string (`status`, `subStatus`, `hasIncidents`, `definitionId`). */
  instancesWhere: (filter: Record<string, string | boolean | undefined>) => {
    const qs = new URLSearchParams();
    for (const [k, v] of Object.entries(filter)) if (v !== undefined) qs.set(k, String(v));
    const s = qs.toString();
    return s ? `${INSTANCES}?${s}` : INSTANCES;
  },
} as const;
