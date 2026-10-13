import type { HttpClient } from "@/lib/api/http";

/*
 * Types mirror `Elsa.Secrets.Models.SecretApiModels` (camelCase on the wire). Note that the C#
 * models, not `specs/007-secrets-module/contracts/rest-api.md`, are the source of truth: the
 * server uses `name`/`typeName`/`totalCount`, routes updates through `POST /secrets/{name}` and
 * serves the picker at `POST /secrets/picker`.
 */

export type SecretStatus = "Active" | "Retired" | "Expired" | "Revoked" | "Deleted";

export const SECRET_STATUSES: SecretStatus[] = ["Active", "Retired", "Expired", "Revoked", "Deleted"];

/** Metadata-only view of a secret; values are never returned by the API. */
export interface SecretModel {
  id: string;
  /** Technical, immutable, unique name used by `Secret` expressions. */
  name: string;
  displayName: string;
  description?: string;
  typeName: string;
  storeName: string;
  scope?: string;
  tags: string[];
  status: SecretStatus;
  currentVersion?: number | null;
  createdAt: string;
  updatedAt?: string | null;
  expiresAt?: string | null;
}

export interface CreateSecretRequest {
  name: string;
  displayName?: string;
  description?: string;
  typeName: string;
  storeName: string;
  scope?: string;
  tags?: string[];
  /** Write-only: submitted on create/rotate, never read back. */
  value?: string;
  /** For the `configuration` store: the configuration key that holds the value. */
  configurationKey?: string;
  expiresAt?: string | null;
  metadata?: Record<string, string>;
}

export interface UpdateSecretRequest {
  displayName?: string;
  description?: string;
}

export interface RotateSecretRequest {
  value?: string;
  configurationKey?: string;
  expiresAt?: string | null;
  metadata?: Record<string, string>;
}

export interface ListSecretsQuery {
  search?: string;
  typeName?: string;
  storeName?: string;
  scope?: string;
  status?: SecretStatus;
  /** 0-based. */
  page?: number;
  pageSize?: number;
}

export interface ListSecretsResponse {
  items: SecretModel[];
  totalCount: number;
}

/** `SecretStoreCapabilities` is a [Flags] enum; it arrives as a comma-separated string or a number. */
export type SecretStoreCapabilities = string | number;

export interface SecretStoreDescriptor {
  name: string;
  displayName: string;
  description: string;
  capabilities: SecretStoreCapabilities;
  isReadOnly: boolean;
}

export interface SecretTypeDescriptor {
  name: string;
  displayName: string;
  description: string;
  /** Hint for the value editor, e.g. `text`, `multiline`, `pem`. */
  editorHint: string;
  supportedStoreNames: string[];
}

export interface SecretDescriptorsResponse {
  types: SecretTypeDescriptor[];
  stores: SecretStoreDescriptor[];
}

export interface SecretPickerRequest {
  search?: string;
  typeNames?: string[];
  storeNames?: string[];
  scope?: string;
  /** Defaults to true on the server. */
  activeOnly?: boolean;
}

export interface SecretPickerResponse {
  items: SecretModel[];
  canCreateInline: boolean;
}

export interface SecretTestResponse {
  succeeded: boolean;
  error?: string | null;
}

/** Value of a `Secret` expression: `Elsa.Secrets.Models.SecretReference`. */
export interface SecretReference {
  name: string;
  typeName?: string | null;
  scope?: string | null;
}

export const SECRET_EXPRESSION_TYPE = "Secret";

const CAPABILITY_BITS: Record<string, number> = { None: 0, Read: 1, Write: 2, Delete: 4, Test: 8, ExportEncrypted: 16, Versioned: 32 };

export function hasCapability(capabilities: SecretStoreCapabilities | undefined, capability: keyof typeof CAPABILITY_BITS): boolean {
  if (capabilities === undefined) return false;
  if (typeof capabilities === "number") return (capabilities & CAPABILITY_BITS[capability]) !== 0;
  return capabilities
    .split(",")
    .map((s) => s.trim().toLowerCase())
    .includes(capability.toLowerCase());
}

export function secretsApi(http: HttpClient) {
  const item = (name: string) => `secrets/${encodeURIComponent(name)}`;
  return {
    list: (query: ListSecretsQuery, signal?: AbortSignal) => http.get<ListSecretsResponse>("secrets", { query: { ...query }, signal }),
    get: (name: string, signal?: AbortSignal) => http.get<SecretModel>(item(name), { signal }),
    descriptors: (signal?: AbortSignal) => http.get<SecretDescriptorsResponse>("secrets/descriptors", { signal }),
    create: (body: CreateSecretRequest) => http.post<SecretModel>("secrets", body),
    update: (name: string, body: UpdateSecretRequest) => http.post<SecretModel>(item(name), body),
    rotate: (name: string, body: RotateSecretRequest) => http.post<SecretModel>(`${item(name)}/rotate`, body),
    revoke: (name: string) => http.post<SecretModel>(`${item(name)}/revoke`),
    test: (name: string) => http.post<SecretTestResponse>(`${item(name)}/test`),
    delete: (name: string) => http.delete<void>(item(name), { parse: "none" }),
    picker: (body: SecretPickerRequest, signal?: AbortSignal) => http.post<SecretPickerResponse>("secrets/picker", body, { signal }),
  };
}

export type SecretsApi = ReturnType<typeof secretsApi>;
