/** Shapes shared by many Elsa API endpoints. */

export interface Link {
  href: string;
  rel: string;
  method: string;
}

export interface PagedList<T> {
  items: T[];
  totalCount: number;
  links?: Link[];
}

export interface ListResponse<T> {
  items: T[];
  count?: number;
}

export type OrderDirection = "Ascending" | "Descending";

/**
 * Version selector accepted by many endpoints as `versionOptions` query parameter.
 * Serialized as `AllVersions`, `Draft`, `Latest`, `Published`, `LatestOrPublished`,
 * `LatestAndPublished` or a bare integer for a specific version.
 */
export type VersionOptions =
  | { kind: "AllVersions" }
  | { kind: "Draft" }
  | { kind: "Latest" }
  | { kind: "Published" }
  | { kind: "LatestOrPublished" }
  | { kind: "LatestAndPublished" }
  | { kind: "Version"; version: number };

export function serializeVersionOptions(v: VersionOptions): string {
  return v.kind === "Version" ? String(v.version) : v.kind;
}

export interface PagingArgs {
  page?: number;
  pageSize?: number;
}

export interface ApiProblem {
  status: number;
  title: string;
  detail?: string;
  errors?: Record<string, string[]>;
  raw?: unknown;
}
