"use client";
import { translate } from "@elsa-workflows/react-studio-sdk/localization";

import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ArrowLeft, CheckCircle2, Eye, Hand, RefreshCw, UserMinus, UserPlus, XCircle } from "lucide-react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@elsa-workflows/react-studio-ui/card";
import { ErrorAlert, describeError } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { JsonViewer } from "@elsa-workflows/react-studio-ui/common/json-viewer";
import { KeyValueTable } from "@elsa-workflows/react-studio-ui/common/key-value-table";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { Timestamp } from "@elsa-workflows/react-studio-ui/common/timestamp";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import type { RouteParams } from "@elsa-workflows/react-studio-sdk";
import { isTerminalStatus, participantLabel, type UserTaskDetail, type UserTaskFormField } from "../api";
import { AssignTaskDialog } from "../components/assign-task-dialog";
import { CancelTaskDialog } from "../components/cancel-task-dialog";
import { CompleteTaskDialog } from "../components/complete-task-dialog";
import { PriorityBadge, UserTaskStatusBadge } from "../components/user-task-status-badge";
import { UserTasksGate } from "../components/user-tasks-gate";
import { useUserTask, useUserTaskCapabilities, useUserTaskEvents, useUserTaskOperation, useUserTasksApi } from "../hooks";
import { UserTaskRoutes } from "../routes";

export function UserTaskDetailPage({ params }: { params: RouteParams }) {
  useTranslation();
  return (
    <UserTasksGate>
      <UserTaskDetailView taskId={params.taskId} />
    </UserTasksGate>
  );
}

function UserTaskDetailView({ taskId }: { taskId: string }) {
  const _t = useTranslation();
  const query = useUserTask(taskId);
  const task = query.data;
  const { capabilities } = useUserTaskCapabilities();
  const events = useUserTaskEvents(taskId, !!task?.disclosure.canViewHistory);
  const confirm = useConfirm();
  const [dialog, setDialog] = useState<"complete" | "assign" | "cancel" | null>(null);
  const op = useUserTaskOperation(() => setDialog(null));

  const run = async (variables: Parameters<typeof op.mutateAsync>[0], success: string) => {
    try {
      const res = await op.mutateAsync(variables);
      toast.success(res.status === "Accepted" ? translate("{{value0}} (processing in the background).", { value0: success }) : success);
      setDialog(null);
    } catch {
      // Already reported by the mutation's onError.
    }
  };

  const can = (verb: "claim" | "release" | "assign" | "complete" | "cancel" | "retry") => {
    if (!task || isTerminalStatus(task.status)) return false;
    const allowed = (key: string) => task.allowedActions.length === 0 || task.allowedActions.some((a) => a.toLowerCase() === key);
    switch (verb) {
      case "claim":
        return !!capabilities?.canClaim && allowed("claim") && (task.status === "Available" || task.status === "Unassigned");
      case "release":
        return !!capabilities?.canRelease && allowed("release") && task.status === "Assigned";
      case "assign":
        return !!capabilities?.canAssign && allowed("assign");
      case "complete":
        return !!capabilities?.canComplete && task.status === "Assigned";
      case "cancel":
        return !!capabilities?.canCancel && allowed("cancel");
      case "retry":
        return !!task.healthCode && !!capabilities?.canUpdate;
    }
  };

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        title={
          <span className="flex items-center gap-2 flex-wrap">
            {task?.title ?? _t("User task")}
            <UserTaskStatusBadge status={task?.status} />
            {task && <PriorityBadge priority={task.priority} />}
            {task?.isOverdue && <ToneBadge tone="danger">{_t("Overdue")}</ToneBadge>}
          </span>
        }
        description={task?.summary ?? undefined}
        actions={
          <>
            {can("claim") && (
              <Button size="sm" onClick={() => task && run({ op: "claim", taskId, expectedRevision: task.revision }, "Task claimed.")} disabled={op.isPending}>
                <Hand data-icon="inline-start" />
                {_t("Claim")}</Button>
            )}
            {can("release") && (
              <Button size="sm" variant="outline" onClick={() => task && run({ op: "release", taskId, expectedRevision: task.revision }, "Task released.")} disabled={op.isPending}>
                <UserMinus data-icon="inline-start" />
                {_t("Release")}</Button>
            )}
            {can("assign") && (
              <Button size="sm" variant="outline" onClick={() => setDialog("assign")} disabled={op.isPending}>
                <UserPlus data-icon="inline-start" />
                {_t("Assign")}</Button>
            )}
            {can("complete") && (
              <Button size="sm" onClick={() => setDialog("complete")} disabled={op.isPending}>
                <CheckCircle2 data-icon="inline-start" />
                {_t("Complete")}</Button>
            )}
            {can("cancel") && (
              <Button size="sm" variant="destructive" onClick={() => setDialog("cancel")} disabled={op.isPending}>
                <XCircle data-icon="inline-start" />
                {_t("Cancel task")}</Button>
            )}
            {can("retry") && (
              <Button
                size="sm"
                variant="outline"
                disabled={op.isPending}
                onClick={async () => {
                  if (task && (await confirm({ title: _t("Retry form resolution?"), description: task.healthCode ?? undefined })))
                    run({ op: "retryResolution", taskId, expectedRevision: task.revision }, "Resolution retried.");
                }}
              >
                <RefreshCw data-icon="inline-start" />
                {_t("Retry resolution")}</Button>
            )}
            <Button variant="ghost" size="sm" nativeButton={false} render={<Link href={UserTaskRoutes.list} />}>
              <ArrowLeft data-icon="inline-start" />
              {_t("All tasks")}</Button>
          </>
        }
      />

      <ErrorAlert error={query.error} title={_t("Could not load the task")} />

      {task && (
        <div className="grid gap-4 lg:grid-cols-3">
          <div className="lg:col-span-2 flex flex-col gap-4">
            {task.instructions && (
              <Card>
                <CardHeader>
                  <CardTitle>{_t("Instructions")}</CardTitle>
                </CardHeader>
                <CardContent className="text-sm whitespace-pre-wrap">{task.instructions}</CardContent>
              </Card>
            )}
            <Card>
              <CardHeader>
                <CardTitle>{_t("Fields")}</CardTitle>
              </CardHeader>
              <CardContent>
                <TaskFields task={task} />
              </CardContent>
            </Card>
            {task.data !== undefined && task.data !== null && (
              <Card>
                <CardHeader>
                  <CardTitle>{_t("Task data")}</CardTitle>
                </CardHeader>
                <CardContent>
                  <JsonViewer value={redactMasked(task.data, task)} maxHeight="16rem" />
                </CardContent>
              </Card>
            )}
            {(task.outcome || task.response !== undefined) && (
              <Card>
                <CardHeader>
                  <CardTitle>{_t("Outcome")}</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-2">
                  <KeyValueTable
                    rows={[
                      { label: _t("Action"), value: task.outcome },
                      { label: _t("Completed by"), value: participantLabel(task.completedBy) || undefined },
                      { label: _t("Completed at"), value: <Timestamp value={task.completedAt} /> },
                    ]}
                  />
                  {task.response !== undefined && task.response !== null && <JsonViewer value={redactMasked(task.response, task)} maxHeight="12rem" />}
                </CardContent>
              </Card>
            )}
          </div>
          <div className="flex flex-col gap-4">
            <Card>
              <CardHeader>
                <CardTitle>{_t("Assignment")}</CardTitle>
              </CardHeader>
              <CardContent>
                <KeyValueTable
                  rows={[
                    { label: _t("Assignee"), value: participantLabel(task.assignee) || undefined },
                    { label: _t("Candidates"), value: task.candidateSummary ?? undefined },
                    { label: _t("Assigned at"), value: <Timestamp value={task.assignedAt} /> },
                    { label: _t("Due"), value: <Timestamp value={task.dueAt} /> },
                    { label: _t("Allowed actions"), value: task.allowedActions.length ? task.allowedActions.join(", ") : undefined },
                    { label: _t("Revision"), value: task.revision },
                  ]}
                />
              </CardContent>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>{_t("Details")}</CardTitle>
              </CardHeader>
              <CardContent>
                <KeyValueTable
                  rows={[
                    { label: _t("Id"), value: <span className="font-mono text-xs">{task.id}</span> },
                    { label: _t("Reference"), value: task.reference ?? undefined },
                    { label: _t("Type"), value: task.taskType ?? undefined },
                    { label: _t("Tags"), value: task.tags.length ? task.tags.join(", ") : undefined },
                    { label: _t("Health"), value: task.healthCode ? `${task.healthSeverity ?? ""} ${task.healthCode}`.trim() : undefined },
                    { label: _t("Created"), value: <Timestamp value={task.createdAt} /> },
                    { label: _t("Updated"), value: <Timestamp value={task.updatedAt} /> },
                  ]}
                />
              </CardContent>
            </Card>
            {task.disclosure.canViewWorkflow && (task.workflow ?? task.workflowInstanceId) && (
              <Card>
                <CardHeader>
                  <CardTitle>{_t("Workflow")}</CardTitle>
                </CardHeader>
                <CardContent>
                  <KeyValueTable
                    rows={[
                      { label: _t("Definition"), value: task.workflow?.definitionName ?? task.workflowDefinitionName ?? task.workflowDefinitionId ?? undefined },
                      { label: _t("Version"), value: task.workflow?.definitionVersion ?? task.workflowDefinitionVersion ?? undefined },
                      {
                        label: _t("Instance"),
                        value: (task.workflow?.instanceId ?? task.workflowInstanceId) ? (
                          <Link className="underline underline-offset-4 font-mono text-xs" href={`/workflows/instances/${encodeURIComponent(task.workflow?.instanceId ?? task.workflowInstanceId ?? "")}/view`}>
                            {task.workflow?.instanceReference ?? task.workflowInstanceReference ?? task.workflow?.instanceId ?? task.workflowInstanceId}
                          </Link>
                        ) : undefined,
                      },
                    ]}
                  />
                </CardContent>
              </Card>
            )}
            {task.disclosure.canViewHistory && (
              <Card>
                <CardHeader>
                  <CardTitle>{_t("History")}</CardTitle>
                </CardHeader>
                <CardContent>
                  {events.error ? (
                    <p className="text-xs text-muted-foreground">{describeError(events.error)}</p>
                  ) : events.data?.items.length ? (
                    <ol className="flex flex-col gap-2 text-xs">
                      {events.data.items.map((e) => (
                        <li key={e.id} className="flex flex-col">
                          <span className="font-medium">
                            {e.kind}
                            {e.actorDisplayName && <span className="text-muted-foreground font-normal"> · {e.actorDisplayName}</span>}
                          </span>
                          {e.summary && <span className="text-muted-foreground">{e.summary}</span>}
                          <Timestamp value={e.occurredAt} relative />
                        </li>
                      ))}
                    </ol>
                  ) : (
                    <p className="text-xs text-muted-foreground">{_t("No events.")}</p>
                  )}
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      )}

      {task && dialog === "complete" && (
        <CompleteTaskDialog
          task={task}
          open
          onOpenChange={(o) => !o && setDialog(null)}
          isPending={op.isPending}
          onSubmit={(actionKey, data) => run({ op: "complete", taskId, expectedRevision: task.revision, actionKey, data }, "Task completed.")}
        />
      )}
      {task && dialog === "assign" && (
        <AssignTaskDialog
          open
          onOpenChange={(o) => !o && setDialog(null)}
          isPending={op.isPending}
          participantPicker={!!capabilities?.participantPicker}
          onSubmit={(assignee, reason) => run({ op: "assign", taskId, expectedRevision: task.revision, assignee, reason }, `Task assigned to ${participantLabel(assignee)}.`)}
        />
      )}
      {task && dialog === "cancel" && (
        <CancelTaskDialog open onOpenChange={(o) => !o && setDialog(null)} isPending={op.isPending} onSubmit={(reason) => run({ op: "cancel", taskId, expectedRevision: task.revision, reason }, "Task cancelled.")} />
      )}
    </div>
  );
}

/**
 * The server masks a field only in the form projection; the raw payload still arrives for anyone who
 * may read protected data. Hide the masked keys in the raw views too, so the field's Reveal action is
 * the only way to see them.
 */
function redactMasked(value: unknown, task: UserTaskDetail): unknown {
  const masked = new Set((task.form?.fields ?? []).filter((f) => f.masked).map((f) => f.key));
  if (masked.size === 0 || !value || typeof value !== "object" || Array.isArray(value)) return value;
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(value as Record<string, unknown>)) out[k] = masked.has(k) ? "••••••" : v;
  return out;
}

function TaskFields({ task }: { task: UserTaskDetail }) {
  const _t = useTranslation();
  const fields = task.form?.fields ?? [];
  if (fields.length === 0) return <p className="text-sm text-muted-foreground">{_t("This task has no form fields.")}</p>;
  return (
    <KeyValueTable
      rows={fields.map((f) => ({
        label: (
          <span>
            {f.label}
            {f.required && <span className="text-destructive ml-0.5">*</span>}
          </span>
        ),
        value: <FieldValue taskId={task.id} field={f} />,
      }))}
    />
  );
}

function FieldValue({ taskId, field }: { taskId: string; field: UserTaskFormField }) {
  const _t = useTranslation();
  const api = useUserTasksApi();
  const [revealed, setRevealed] = useState<unknown>();
  const [busy, setBusy] = useState(false);
  const value = revealed !== undefined ? revealed : field.value;

  if (field.masked && value === undefined) {
    return (
      <span className="flex items-center gap-2">
        <span className="text-muted-foreground">••••••</span>
        {field.canReveal && (
          <Button
            variant="ghost"
            size="xs"
            disabled={busy}
            onClick={async () => {
              setBusy(true);
              try {
                const res = await api.reveal(taskId, field.key);
                setRevealed(res.value ?? null);
              } catch (e) {
                toast.error(describeError(e));
              } finally {
                setBusy(false);
              }
            }}
          >
            <Eye data-icon="inline-start" />
            {_t("Reveal")}</Button>
        )}
      </span>
    );
  }
  if (value === undefined || value === null || value === "") return <span className="text-muted-foreground">–</span>;
  if (typeof value === "object") return <JsonViewer value={value} maxHeight="8rem" />;
  return <span className="break-words">{String(value)}</span>;
}
