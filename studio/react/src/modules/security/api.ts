import type { ElsaClient } from "@/lib/api/client";
import type { PermissionResourceDescriptor } from "@/lib/api/endpoints/identity";

export type { IdentityRole, IdentityUser, PermissionResourceDescriptor, RoleDeletionImpact } from "@/lib/api/endpoints/identity";

/**
 * The security module reuses the core identity endpoint group (`client.identity`); this function only
 * exists so the module follows the same `api.ts` shape as the others.
 */
export function securityApi(client: ElsaClient) {
  return client.identity;
}

export type SecurityApi = ReturnType<typeof securityApi>;

/** Wildcard verb accepted by the server on any resource. */
export const ANY_VERB = "*";

/** Builds a `resource:verb` permission token. */
export function permissionToken(resource: string, verb: string): string {
  return `${resource}:${verb}`;
}

/** Splits a `resource:verb` token; returns `undefined` for malformed input. */
export function splitPermissionToken(token: string): { resource: string; verb: string } | undefined {
  const idx = token.lastIndexOf(":");
  if (idx <= 0) return token === ANY_VERB ? { resource: ANY_VERB, verb: ANY_VERB } : undefined;
  return { resource: token.slice(0, idx), verb: token.slice(idx + 1) };
}

export interface PermissionCategory {
  category: string;
  resources: PermissionResourceDescriptor[];
}

/** Groups the permission catalog by category, sorted alphabetically on both levels. */
export function groupByCategory(resources: PermissionResourceDescriptor[]): PermissionCategory[] {
  const map = new Map<string, PermissionResourceDescriptor[]>();
  for (const r of resources) {
    const key = r.category || "Other";
    const list = map.get(key) ?? [];
    list.push(r);
    map.set(key, list);
  }
  return [...map.entries()]
    .map(([category, list]) => ({ category, resources: [...list].sort((a, b) => a.displayName.localeCompare(b.displayName)) }))
    .sort((a, b) => a.category.localeCompare(b.category));
}
