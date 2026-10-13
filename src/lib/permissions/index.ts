"use client";

import { useMemo } from "react";
import { useAuth } from "@/lib/auth/provider";

/**
 * Permission model mirroring the Elsa server: `resource:verb`, with `*` wildcards on either axis and
 * `path/*` matching descendants. When the principal carries no `permissions` claim (e.g. a third-party
 * OIDC token) the set is *unknown* and every check passes: the server stays the source of truth.
 */
export interface Permission {
  resource: string;
  verb: string;
}

export function parsePermission(value: string): Permission | undefined {
  const idx = value.lastIndexOf(":");
  if (idx <= 0) return value === "*" ? { resource: "*", verb: "*" } : undefined;
  return { resource: value.slice(0, idx), verb: value.slice(idx + 1) };
}

function resourceMatches(granted: string, requested: string) {
  if (granted === "*" || granted === requested) return true;
  if (granted.endsWith("/*")) {
    const prefix = granted.slice(0, -1); // keep trailing slash
    return requested.startsWith(prefix) || requested === granted.slice(0, -2);
  }
  return false;
}

export class PermissionSet {
  static readonly Unknown = new PermissionSet(undefined);

  private constructor(private readonly granted: Permission[] | undefined) {}

  static fromClaims(values: string[] | undefined): PermissionSet {
    if (!values) return PermissionSet.Unknown;
    const granted = values.map(parsePermission).filter((p): p is Permission => !!p);
    return new PermissionSet(granted);
  }

  get isUnknown() {
    return this.granted === undefined;
  }

  has(resource: string, verb: string): boolean {
    if (!this.granted) return true;
    return this.granted.some((g) => (g.verb === "*" || g.verb === verb) && resourceMatches(g.resource, resource));
  }

  hasAny(list: Permission[]) {
    return list.length === 0 || list.some((p) => this.has(p.resource, p.verb));
  }

  hasAll(list: Permission[]) {
    return list.every((p) => this.has(p.resource, p.verb));
  }
}

export function usePermissions(): PermissionSet {
  const { user } = useAuth();
  return useMemo(() => PermissionSet.fromClaims(user?.permissions), [user?.permissions]);
}

export function usePermission(resource: string, verb: string): boolean {
  return usePermissions().has(resource, verb);
}

/** Resource names used by the UI. Kept in one place so modules don't scatter string literals. */
export const Resources = {
  workflowDefinitions: "workflows/definitions",
  workflowDefinitionVersions: "workflows/definitions/versions",
  workflowDefinitionLabels: "workflows/definitions/labels",
  workflowInstances: "workflows/instances",
  workflowRuntime: "workflows/runtime",
  workflowTests: "workflows/tests",
  alterations: "alterations",
  labels: "labels",
  secrets: "secrets",
  users: "identity/users",
  roles: "identity/roles",
  userTasks: "user-tasks",
  dashboard: "dashboard",
  structuredLogs: "diagnostics/structured-logs",
  consoleLogs: "diagnostics/console-logs",
  openTelemetry: "diagnostics/opentelemetry",
} as const;

export const Verbs = {
  view: "view",
  create: "create",
  update: "update",
  write: "write",
  delete: "delete",
  execute: "execute",
  publish: "publish",
  retract: "retract",
  revert: "revert",
  cancel: "cancel",
  test: "test",
} as const;
