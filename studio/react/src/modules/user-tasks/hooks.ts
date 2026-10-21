"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { nanoid } from "nanoid";
import { useMemo } from "react";
import { toast } from "sonner";
import { useElsaClient, useQueryScope } from "@/lib/api/provider";
import { ApiError } from "@/lib/api/http";
import { useAuth } from "@/lib/auth/provider";
import { describeError } from "@/components/common/error-alert";
import { useStudioConfig } from "@/config/provider";
import {
  userTasksApi,
  type AssignUserTaskRequest,
  type CancelUserTaskRequest,
  type CompleteUserTaskRequest,
  type ListUserTasksArgs,
  type ScheduleUserTaskRequest,
  type UserTaskMutationRequest,
  type UserTaskOperationResponse,
} from "./api";

export function useUserTasksApi() {
  const client = useElsaClient();
  return useMemo(() => userTasksApi(client.http), [client]);
}

export function userTasksKey(scope: string) {
  return [scope, "user-tasks"] as const;
}

/**
 * Module-level capabilities (`GET /user-tasks/capabilities`). `enabled` is true only when the server
 * answered and the caller may list and read tasks; a 404/403 yields `enabled: false`.
 */
export function useUserTaskCapabilities() {
  const api = useUserTasksApi();
  const scope = useQueryScope();
  const { status } = useAuth();
  const query = useQuery({
    queryKey: [...userTasksKey(scope), "capabilities"],
    queryFn: ({ signal }) => api.capabilities(signal),
    enabled: status === "authenticated",
    staleTime: 5 * 60_000,
    retry: false,
  });
  const caps = query.data;
  const enabled = !!caps && caps.enabled && caps.canList && caps.canRead;
  return { capabilities: caps, enabled, isLoading: query.isLoading, isResolved: query.isSuccess || query.isError, error: query.error };
}

/** Small convenience for nav gating: true once capabilities confirm the feature is usable. */
export function useUserTasksEnabled() {
  return useUserTaskCapabilities().enabled;
}

export function useUserTasks(args: ListUserTasksArgs, options: { poll?: boolean } = {}) {
  const api = useUserTasksApi();
  const scope = useQueryScope();
  const { ui } = useStudioConfig();
  return useQuery({
    queryKey: [...userTasksKey(scope), "list", args],
    queryFn: ({ signal }) => api.list(args, signal),
    placeholderData: keepPreviousData,
    refetchInterval: options.poll ? ui.pollIntervalMs : false,
  });
}

export function useUserTask(id: string | undefined) {
  const api = useUserTasksApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: [...userTasksKey(scope), "task", id],
    queryFn: ({ signal }) => api.get(id!, signal),
    enabled: !!id,
    retry: false,
  });
}

export function useUserTaskEvents(id: string | undefined, enabled = true) {
  const api = useUserTasksApi();
  const scope = useQueryScope();
  return useQuery({
    queryKey: [...userTasksKey(scope), "task", id, "events"],
    queryFn: ({ signal }) => api.events(id!, undefined, 50, signal),
    enabled: !!id && enabled,
    retry: false,
  });
}

export function newOperationId() {
  return typeof crypto !== "undefined" && "randomUUID" in crypto ? crypto.randomUUID() : nanoid();
}

export function isConflict(error: unknown) {
  return error instanceof ApiError && error.status === 409;
}

type Op = "claim" | "release" | "assign" | "schedule" | "complete" | "cancel" | "retryResolution";

interface OpVariables {
  op: Op;
  taskId: string;
  expectedRevision: number;
  reason?: string;
  assignee?: AssignUserTaskRequest["assignee"];
  priority?: number;
  dueAt?: string | null;
  actionKey?: string;
  data?: unknown;
}

/**
 * One mutation for every task operation. Each call carries the task's `expectedRevision` and a
 * freshly generated `operationId` (idempotency key). A 409 means the task changed underneath us:
 * the task is reloaded and the user is told to try again.
 */
export function useUserTaskOperation(onConflict?: () => void) {
  const api = useUserTasksApi();
  const scope = useQueryScope();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (v: OpVariables): Promise<UserTaskOperationResponse> => {
      const operationId = newOperationId();
      const base: UserTaskMutationRequest = { expectedRevision: v.expectedRevision, operationId, reason: v.reason };
      switch (v.op) {
        case "claim":
          return api.claim(v.taskId, base);
        case "release":
          return api.release(v.taskId, base);
        case "retryResolution":
          return api.retryResolution(v.taskId, base);
        case "assign":
          if (!v.assignee) throw new Error("An assignee is required.");
          return api.assign(v.taskId, { ...base, assignee: v.assignee } satisfies AssignUserTaskRequest);
        case "schedule":
          return api.schedule(v.taskId, { ...base, priority: v.priority, dueAt: v.dueAt } satisfies ScheduleUserTaskRequest);
        case "complete":
          return api.complete(v.taskId, { expectedRevision: v.expectedRevision, operationId, actionKey: v.actionKey ?? "Complete", data: v.data } satisfies CompleteUserTaskRequest);
        case "cancel":
          return api.cancel(v.taskId, { expectedRevision: v.expectedRevision, operationId, reason: v.reason ?? "" } satisfies CancelUserTaskRequest);
      }
    },
    onSuccess: (_res, v) => {
      queryClient.invalidateQueries({ queryKey: [...userTasksKey(scope), "task", v.taskId] });
      queryClient.invalidateQueries({ queryKey: [...userTasksKey(scope), "list"] });
    },
    onError: (error, v) => {
      if (isConflict(error)) {
        toast.warning(`${describeError(error)} The task has been reloaded.`);
        queryClient.invalidateQueries({ queryKey: [...userTasksKey(scope), "task", v.taskId] });
        queryClient.invalidateQueries({ queryKey: [...userTasksKey(scope), "list"] });
        onConflict?.();
      } else {
        toast.error(describeError(error));
      }
    },
  });
}
