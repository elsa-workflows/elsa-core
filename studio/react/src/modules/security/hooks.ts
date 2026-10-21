"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { groupByCategory, securityApi } from "./api";

export const securityKeys = {
  users: (scope: string) => [scope, "identity", "users"] as const,
  roles: (scope: string) => [scope, "identity", "roles"] as const,
  permissions: (scope: string) => [scope, "identity", "permissions"] as const,
};

export function useSecurityApi() {
  const client = useElsaClient();
  return useMemo(() => securityApi(client), [client]);
}

export function useUsers() {
  const api = useSecurityApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: securityKeys.users(scope),
    queryFn: async ({ signal }) => (await api.users(signal)).users,
  });
}

export function useRoles() {
  const api = useSecurityApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: securityKeys.roles(scope),
    queryFn: async ({ signal }) => (await api.roles(signal)).roles,
  });
}

/** The server's permission catalog, both flat and grouped by category. */
export function usePermissionCatalog() {
  const api = useSecurityApi();
  const scope = useQueryScope();
  const query = useQuery({
    queryKey: securityKeys.permissions(scope),
    queryFn: ({ signal }) => api.permissions(signal),
    staleTime: 5 * 60_000,
  });
  const categories = useMemo(() => groupByCategory(query.data?.resources ?? []), [query.data]);
  return { ...query, categories, coreVerbs: query.data?.coreVerbs ?? [] };
}

function useInvalidate(key: (scope: string) => readonly unknown[]) {
  const queryClient = useQueryClient();
  const scope = useQueryScope();
  return () => queryClient.invalidateQueries({ queryKey: key(scope) });
}

export function useCreateUser() {
  const api = useSecurityApi();
  const invalidate = useInvalidate(securityKeys.users);
  return useMutation({
    mutationFn: (body: { name: string; password?: string; roles?: string[] }) => api.createUser(body),
    onSuccess: () => invalidate(),
  });
}

export function useUpdateUser() {
  const api = useSecurityApi();
  const invalidate = useInvalidate(securityKeys.users);
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string; password?: string; roles?: string[] }) => api.updateUser(id, body),
    onSuccess: () => invalidate(),
  });
}

export function useDeleteUser() {
  const api = useSecurityApi();
  const invalidate = useInvalidate(securityKeys.users);
  return useMutation({
    mutationFn: (id: string) => api.deleteUser(id),
    onSuccess: () => invalidate(),
  });
}

export function useCreateRole() {
  const api = useSecurityApi();
  const invalidate = useInvalidate(securityKeys.roles);
  return useMutation({
    mutationFn: (body: { id?: string; name: string; permissions?: string[] }) => api.createRole(body),
    onSuccess: () => invalidate(),
  });
}

export function useUpdateRole() {
  const api = useSecurityApi();
  const invalidate = useInvalidate(securityKeys.roles);
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string; name?: string; permissions?: string[] }) => api.updateRole(id, body),
    onSuccess: () => invalidate(),
  });
}

export function useDeleteRole() {
  const api = useSecurityApi();
  const invalidateRoles = useInvalidate(securityKeys.roles);
  const invalidateUsers = useInvalidate(securityKeys.users);
  return useMutation({
    mutationFn: (id: string) => api.deleteRole(id),
    onSuccess: () => Promise.all([invalidateRoles(), invalidateUsers()]),
  });
}

/** On-demand fetch of the deletion impact report, used right before a delete. */
export function useRoleDeletionImpact() {
  const api = useSecurityApi();
  return useMutation({ mutationFn: (id: string) => api.roleDeletionImpact(id) });
}
