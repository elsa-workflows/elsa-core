import type { HttpClient } from "../http";

export interface IdentityUser {
  id: string;
  name: string;
  roles: string[];
  tenantId?: string;
  generatedPassword?: string;
}

export interface IdentityRole {
  id: string;
  name: string;
  permissions: string[];
  tenantId?: string;
}

export interface ResourceGrant {
  resource: string;
  verbs: string[];
}

export interface PermissionResourceDescriptor {
  resource: string;
  supportedVerbs: string[];
  nonCoreVerbs: string[];
  displayName: string;
  description: string;
  category: string;
  verified: boolean;
}

export interface RoleDeletionImpact {
  roleId: string;
  dependencyVersion?: string;
  executionMode?: "atomic" | "bestEffort";
  canDelete: boolean;
  canRemediate: boolean;
  configurationReferences: unknown[];
  editableReferences: unknown[];
  warnings: string[];
}

export function identityApi(http: HttpClient) {
  return {
    myPermissions: (signal?: AbortSignal) => http.get<{ grants: ResourceGrant[] }>("identity/me/permissions", { signal }),
    permissions: (signal?: AbortSignal) =>
      http.get<{ coreVerbs: string[]; resources: PermissionResourceDescriptor[] }>("identity/permissions", { signal }),

    users: (signal?: AbortSignal) => http.get<{ users: IdentityUser[] }>("identity/users", { signal }),
    createUser: (body: { name: string; password?: string; roles?: string[] }) => http.post<IdentityUser>("identity/users", body),
    updateUser: (id: string, body: { password?: string; roles?: string[] }) =>
      http.put<IdentityUser>(`identity/users/${encodeURIComponent(id)}`, body),
    deleteUser: (id: string) => http.delete<void>(`identity/users/${encodeURIComponent(id)}`, { parse: "none" }),

    roles: (signal?: AbortSignal) => http.get<{ roles: IdentityRole[] }>("identity/roles", { signal }),
    createRole: (body: { id?: string; name: string; permissions?: string[] }) => http.post<IdentityRole>("identity/roles", body),
    updateRole: (id: string, body: { name?: string; permissions?: string[] }) =>
      http.put<IdentityRole>(`identity/roles/${encodeURIComponent(id)}`, body),
    deleteRole: (id: string) => http.delete<void>(`identity/roles/${encodeURIComponent(id)}`, { parse: "none" }),
    roleDeletionImpact: (id: string) => http.get<RoleDeletionImpact>(`identity/roles/${encodeURIComponent(id)}/deletion-impact`),

    createApplication: (body: { name: string; roles?: string[] }) =>
      http.post<{ id: string; name: string; roles: string[]; clientId: string; clientSecret: string; apiKey: string }>(
        "identity/applications",
        body,
      ),
  };
}

export type IdentityApi = ReturnType<typeof identityApi>;
