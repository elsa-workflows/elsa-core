"use client";
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";


import { ReactFlowProvider } from "@xyflow/react";
import { Ban, FileCode2, Loader2, RefreshCw, Trash2 } from "lucide-react";
import Link from "@elsa-workflows/react-studio-sdk/navigation/link";
import { useRouter } from "@elsa-workflows/react-studio-sdk/navigation/index";
import { useCallback, useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useConfirm } from "@elsa-workflows/react-studio-ui/common/confirm-dialog";
import { describeError, ErrorAlert } from "@elsa-workflows/react-studio-ui/common/error-alert";
import { PageHeader } from "@elsa-workflows/react-studio-ui/common/page-header";
import { StatusBadge, ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import { Button } from "@elsa-workflows/react-studio-ui/button";
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from "@elsa-workflows/react-studio-ui/resizable";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@elsa-workflows/react-studio-ui/tabs";
import type { Activity } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { ActivityExecutionStats, WorkflowInstance } from "@elsa-workflows/react-studio-api-client/types/workflow-instances";
import { href } from "@elsa-workflows/react-studio-sdk/routing";
import { useExtensions } from "@elsa-workflows/react-studio-ui/runtime/module-registry";
import type { ListActionHelpers, RouteParams } from "@elsa-workflows/react-studio-sdk";
import { usePermissions, Resources, Verbs } from "@elsa-workflows/react-studio-auth/permissions";
import {
  createDesignerStore,
  DesignerSurface,
  isDesignableContainer,
  DesignerProvider,
  indexActivities,
  revealActivity,
  useActivityRegistry,
  useDesignerState,
  type DesignerStore,
} from "@elsa-workflows/react-studio-sdk/designer/index";
import { WorkflowDefinitionRoutes } from "@elsa-workflows/react-studio-sdk/routes";
import { ActivityDetailsPane } from "../components/activity-details-pane";
import { ActivityExecutionsList } from "../components/activity-executions-list";
import { IncidentsPanel } from "../components/incidents-panel";
import { InputOutputPanel } from "../components/input-output-panel";
import { InstanceDetails } from "../components/instance-details";
import { JournalTree } from "../components/journal-tree";
import { VariablesPanel } from "../components/variables-panel";
import {
  useActivityExecutions,
  useCancelWorkflowInstance,
  useDeleteWorkflowInstance,
  useExecutionStats,
  useInstanceJournal,
  useInstanceKeys,
  useInstanceVariables,
  useWorkflowDefinitionVersion,
  useWorkflowInstance,
} from "../hooks";
import { activityNodeIds } from "../model";
import { WorkflowInstanceRoutes } from "../routes";

export function WorkflowInstanceViewerPage({ params }: { params: RouteParams }) {
  const _t = useTranslation();
  const id = params.id;
  const router = useRouter();
  const confirm = useConfirm();
  const queryClient = useQueryClient();
  const keys = useInstanceKeys();
  const permissions = usePermissions();

  const instanceQuery = useWorkflowInstance(id, { poll: true });
  const instance = instanceQuery.data;
  const definitionQuery = useWorkflowDefinitionVersion(instance?.definitionVersionId);
  const definition = definitionQuery.data;
  const { registry } = useActivityRegistry();

  // Any container root (flowchart, sequence, state machine) gets its own read-only designer.
  const root = useMemo<Activity | undefined>(() => (definition && isDesignableContainer(definition.root) ? definition.root : undefined), [definition]);
  const store = useMemo<DesignerStore | undefined>(() => (root ? createDesignerStore(root, true) : undefined), [root]);
  const nodeIds = useMemo(() => (root ? activityNodeIds(root) : []), [root]);

  const executions = useActivityExecutions(id);
  const report = useExecutionStats(id, nodeIds, executions.records);
  const journal = useInstanceJournal(id);
  const variables = useInstanceVariables(id);

  const stats = useMemo(() => mergeBookmarks(report.stats, instance), [report.stats, instance]);

  const cancel = useCancelWorkflowInstance();
  const remove = useDeleteWorkflowInstance();
  const viewerActions = useExtensions("instanceViewerActions");

  const refresh = useCallback(() => queryClient.invalidateQueries({ queryKey: keys.instance(id) }), [queryClient, keys, id]);
  const helpers = useMemo<ListActionHelpers>(() => ({ refresh: () => void refresh(), navigate: (to) => router.push(to) }), [refresh, router]);

  const handleCancel = async () => {
    if (!instance) return;
    if (!(await confirm({ title: _t("Cancel workflow instance?"), description: _t("Instance {{value0}} will be cancelled.", { value0: instance.id }), confirmLabel: _t("Cancel instance") }))) return;
    try {
      await cancel.mutateAsync(instance.id);
      toast.success(_t("Workflow instance cancelled."));
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const handleDelete = async () => {
    if (!instance) return;
    if (!(await confirm({ title: _t("Delete workflow instance?"), description: _t("This cannot be undone."), confirmLabel: _t("Delete"), destructive: true }))) return;
    try {
      await remove.mutateAsync(instance.id);
      toast.success(_t("Workflow instance deleted."));
      router.push(WorkflowInstanceRoutes.list);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  if (instanceQuery.isLoading)
    return (
      <div className="flex items-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="size-4 animate-spin" /> {_t("Loading workflow instance…")}</div>
    );
  if (instanceQuery.error) return <ErrorAlert error={instanceQuery.error} title={_t("Could not load workflow instance")} />;
  if (!instance) return null;

  const running = instance.status === "Running";
  const canCancel = permissions.has(Resources.workflowInstances, Verbs.cancel);
  const canDelete = permissions.has(Resources.workflowInstances, Verbs.delete);
  const extensions = viewerActions.filter((a) => (a.isVisible ? a.isVisible(instance) : true) && permissions.hasAll(a.permissions ?? []));

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <PageHeader
        className="pb-3"
        title={
          <span className="inline-flex items-center gap-2">
            <span className="font-mono">{instance.id}</span>
            <StatusBadge status={instance.subStatus} />
            {instanceQuery.isPolling && (
              <span className="text-xs font-normal text-muted-foreground" title={_t("Polling for changes")}>
                {_t("live")}</span>
            )}
          </span>
        }
        description={definition ? _t("{{value0}} · v{{value1}}", { value0: definition.name ?? instance.definitionId, value1: instance.version }) : _t("{{value0}} · v{{value1}}", { value0: instance.definitionId, value1: instance.version })}
        actions={
          <>
            <Button variant="outline" size="sm" onClick={() => void refresh()} disabled={instanceQuery.isFetching}>
              <RefreshCw className={instanceQuery.isFetching ? "animate-spin" : undefined} />
              {_t("Refresh")}</Button>
            {extensions.map(({ id: actionId, label, icon: Icon, onSelect }) => (
              <Button key={actionId} variant="outline" size="sm" onClick={() => void onSelect(instance, helpers)}>
                {Icon && <Icon />}
                {label}
              </Button>
            ))}
            {canCancel && running && (
              <Button variant="outline" size="sm" onClick={() => void handleCancel()} disabled={cancel.isPending}>
                <Ban />
                {_t("Cancel")}</Button>
            )}
            <Button variant="outline" size="sm" nativeButton={false} render={<Link href={href(WorkflowDefinitionRoutes.edit, { definitionId: instance.definitionId })} />}>
              <FileCode2 />
              {_t("Edit workflow definition")}</Button>
            {canDelete && (
              <Button variant="destructive" size="sm" onClick={() => void handleDelete()} disabled={remove.isPending}>
                <Trash2 />
                {_t("Delete")}</Button>
            )}
          </>
        }
      />

      {definitionQuery.error && <ErrorAlert error={definitionQuery.error} title={_t("Could not load the workflow definition")} />}

      {store && root ? (
        <ReactFlowProvider>
          <DesignerProvider store={store} registry={registry} stats={stats}>
            <ViewerBody
              instance={instance}
              root={root}
              store={store}
              definitionName={definition?.name}
              customProperties={definition?.customProperties}
              journal={journal}
              executions={executions}
              variables={variables}
            />
          </DesignerProvider>
        </ReactFlowProvider>
      ) : (
        !definitionQuery.error && (
          <div className="flex items-center gap-2 p-6 text-sm text-muted-foreground">
            <Loader2 className="size-4 animate-spin" /> {_t("Loading workflow definition…")}</div>
        )
      )}
    </div>
  );
}

/** Marks every node that currently holds a bookmark as blocked (the report may lag behind the instance). */
function mergeBookmarks(stats: Map<string, ActivityExecutionStats>, instance: WorkflowInstance | undefined) {
  const bookmarks = instance?.workflowState?.bookmarks ?? [];
  if (bookmarks.length === 0) return stats;
  const next = new Map(stats);
  for (const b of bookmarks) {
    const existing = next.get(b.activityNodeId);
    next.set(b.activityNodeId, existing ? { ...existing, isBlocked: true } : { activityNodeId: b.activityNodeId, startedCount: 1, completedCount: 0, uncompletedCount: 1, isBlocked: true, isFaulted: false });
  }
  return next;
}

function ViewerBody({
  instance,
  root,
  store,
  definitionName,
  customProperties,
  journal,
  executions,
  variables,
}: {
  instance: WorkflowInstance;
  root: Activity;
  store: DesignerStore;
  definitionName?: string;
  customProperties?: Record<string, unknown>;
  journal: ReturnType<typeof useInstanceJournal>;
  executions: ReturnType<typeof useActivityExecutions>;
  variables: ReturnType<typeof useInstanceVariables>;
}) {
  const _t = useTranslation();
  const selectedIds = useDesignerState((s) => s.selectedIds);
  const [leftTab, setLeftTab] = useState("journal");
  const [rightTab, setRightTab] = useState("details");

  const index = useMemo(() => indexActivities(root), [root]);
  const selectedActivity = selectedIds.length === 1 ? index.get(selectedIds[0])?.activity : undefined;
  const selectedNodeId = selectedActivity ? (selectedActivity.nodeId ?? selectedActivity.id) : undefined;
  const selectedExecutions = useMemo(
    () => (selectedNodeId ? executions.records.filter((r) => r.activityNodeId === selectedNodeId) : []),
    [executions.records, selectedNodeId],
  );
  const traversed = useMemo(() => new Set(executions.records.filter((r) => r.status === "Completed").map((r) => r.activityId)), [executions.records]);

  const selectActivity = useCallback((activityId: string) => revealActivity(store, activityId), [store]);
  const clearSelection = useCallback(() => store.getState().select([]), [store]);

  const incidents = instance.workflowState?.incidents ?? [];

  return (
    <ResizablePanelGroup orientation="horizontal" className="h-[calc(100svh-10rem)] min-h-[24rem] overflow-hidden rounded-lg border">
      <ResizablePanel defaultSize="22" minSize="14" className="min-w-0">
        <Tabs value={leftTab} onValueChange={(v) => setLeftTab(String(v))} className="h-full min-h-0 gap-0">
          <TabsList variant="line" className="w-full justify-start border-b px-2">
            <TabsTrigger value="journal" className="flex-none">
              {_t("Journal")}</TabsTrigger>
            <TabsTrigger value="executions" className="flex-none">
              {_t("Call stack")}</TabsTrigger>
          </TabsList>
          <TabsContent value="journal" className="min-h-0 flex-1">
            <JournalTree
              records={journal.records}
              totalCount={journal.totalCount}
              selectedActivityId={selectedActivity?.id}
              onSelect={(r) => selectActivity(r.activityId)}
              hasNextPage={journal.hasNextPage}
              isFetchingNextPage={journal.isFetchingNextPage}
              isLoading={journal.isLoading}
              fetchNextPage={() => void journal.fetchNextPage()}
            />
          </TabsContent>
          <TabsContent value="executions" className="min-h-0 flex-1 overflow-y-auto">
            <ActivityExecutionsList records={executions.records} isLoading={executions.isLoading} selectedActivityId={selectedActivity?.id} onSelect={(r) => selectActivity(r.activityId)} />
          </TabsContent>
        </Tabs>
      </ResizablePanel>
      <ResizableHandle withHandle />
      <ResizablePanel defaultSize="50" minSize="30" className="min-w-0">
        <ResizablePanelGroup orientation="vertical">
          <ResizablePanel defaultSize={selectedActivity ? "60" : "100"} minSize="25">
            <DesignerSurface minimap={false} traversedActivityIds={traversed} onNodeDoubleClick={selectActivity} />
          </ResizablePanel>
          {selectedActivity && (
            <>
              <ResizableHandle withHandle />
              <ResizablePanel defaultSize="40" minSize="20">
                <ActivityDetailsPane activity={selectedActivity} journal={journal.records} executions={selectedExecutions} onClose={clearSelection} />
              </ResizablePanel>
            </>
          )}
        </ResizablePanelGroup>
      </ResizablePanel>
      <ResizableHandle withHandle />
      <ResizablePanel defaultSize="28" minSize="18" className="min-w-0">
        <Tabs value={rightTab} onValueChange={(v) => setRightTab(String(v))} className="h-full min-h-0 gap-0">
          <TabsList variant="line" className="w-full justify-start border-b px-2">
            <TabsTrigger value="details" className="flex-none">
              {_t("Details")}</TabsTrigger>
            <TabsTrigger value="incidents" className="flex-none">
              {_t("Incidents")}{incidents.length > 0 && <ToneBadge tone="danger" className="h-4 px-1.5 text-[10px]">{incidents.length}</ToneBadge>}
            </TabsTrigger>
            <TabsTrigger value="variables" className="flex-none">
              {_t("Variables")}</TabsTrigger>
            <TabsTrigger value="io" className="flex-none">
              {_t("Input/Output")}</TabsTrigger>
          </TabsList>
          <TabsContent value="details" className="min-h-0 flex-1 overflow-y-auto">
            <InstanceDetails instance={instance} definitionName={definitionName} customProperties={customProperties} />
          </TabsContent>
          <TabsContent value="incidents" className="min-h-0 flex-1 overflow-y-auto">
            <IncidentsPanel incidents={incidents} onSelectActivity={selectActivity} />
          </TabsContent>
          <TabsContent value="variables" className="min-h-0 flex-1 overflow-y-auto">
            <VariablesPanel variables={variables.data?.items ?? []} isLoading={variables.isLoading} error={variables.error} />
          </TabsContent>
          <TabsContent value="io" className="min-h-0 flex-1 overflow-y-auto">
            <InputOutputPanel input={instance.workflowState?.input} output={instance.workflowState?.output} />
          </TabsContent>
        </Tabs>
      </ResizablePanel>
    </ResizablePanelGroup>
  );
}
