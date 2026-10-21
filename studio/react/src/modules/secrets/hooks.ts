"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import {
  secretsApi,
  type CreateSecretRequest,
  type ListSecretsQuery,
  type RotateSecretRequest,
  type SecretPickerRequest,
  type UpdateSecretRequest,
} from "./api";

export const secretKeys = {
  all: (scope: string) => [scope, "secrets"] as const,
  list: (scope: string, query: ListSecretsQuery) => [scope, "secrets", "list", query] as const,
  detail: (scope: string, name: string) => [scope, "secrets", "detail", name] as const,
  descriptors: (scope: string) => [scope, "secrets", "descriptors"] as const,
  picker: (scope: string, request: SecretPickerRequest) => [scope, "secrets", "picker", request] as const,
};

export function useSecretsApi() {
  const client = useElsaClient();
  return useMemo(() => secretsApi(client.http), [client]);
}

export function useSecrets(query: ListSecretsQuery) {
  const api = useSecretsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: secretKeys.list(scope, query),
    queryFn: ({ signal }) => api.list(query, signal),
    placeholderData: (prev) => prev,
  });
}

/** One secret by its technical name (metadata only; values are never returned). */
export function useSecret(name: string | undefined) {
  const api = useSecretsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: secretKeys.detail(scope, name ?? ""),
    queryFn: ({ signal }) => api.get(name ?? "", signal),
    enabled: !!name,
  });
}

export function useSecretDescriptors() {
  const api = useSecretsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: secretKeys.descriptors(scope),
    queryFn: ({ signal }) => api.descriptors(signal),
    staleTime: 5 * 60_000,
  });
}

export function usePickerSecrets(request: SecretPickerRequest, enabled = true) {
  const api = useSecretsApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: secretKeys.picker(scope, request),
    queryFn: ({ signal }) => api.picker(request, signal),
    enabled,
  });
}

function useInvalidateSecrets() {
  const queryClient = useQueryClient();
  const scope = useQueryScope();
  return () => queryClient.invalidateQueries({ queryKey: secretKeys.all(scope) });
}

export function useCreateSecret() {
  const api = useSecretsApi();
  const invalidate = useInvalidateSecrets();
  return useMutation({ mutationFn: (body: CreateSecretRequest) => api.create(body), onSuccess: () => invalidate() });
}

export function useUpdateSecret() {
  const api = useSecretsApi();
  const invalidate = useInvalidateSecrets();
  return useMutation({ mutationFn: ({ name, ...body }: UpdateSecretRequest & { name: string }) => api.update(name, body), onSuccess: () => invalidate() });
}

export function useRotateSecret() {
  const api = useSecretsApi();
  const invalidate = useInvalidateSecrets();
  return useMutation({ mutationFn: ({ name, ...body }: RotateSecretRequest & { name: string }) => api.rotate(name, body), onSuccess: () => invalidate() });
}

export function useRevokeSecret() {
  const api = useSecretsApi();
  const invalidate = useInvalidateSecrets();
  return useMutation({ mutationFn: (name: string) => api.revoke(name), onSuccess: () => invalidate() });
}

export function useDeleteSecret() {
  const api = useSecretsApi();
  const invalidate = useInvalidateSecrets();
  return useMutation({ mutationFn: (name: string) => api.delete(name), onSuccess: () => invalidate() });
}

export function useTestSecret() {
  const api = useSecretsApi();
  return useMutation({ mutationFn: (name: string) => api.test(name) });
}
