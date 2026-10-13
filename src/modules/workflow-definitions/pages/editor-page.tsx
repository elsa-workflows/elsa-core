"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { ReactFlowProvider } from "@xyflow/react";
import { toast } from "sonner";
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from "@/components/ui/resizable";
import { useConfirmChoice } from "@/components/common/confirm-dialog";
import { describeError, ErrorAlert } from "@/components/common/error-alert";
import { useShellHeader } from "@/components/shell/shell-chrome";
import { FullPageSpinner } from "@/components/shell/full-page-spinner";
import type { RouteParams } from "@/lib/modules/types";
import type { Activity } from "@/lib/api/types/activities";
import type { WorkflowDefinition } from "@/lib/api/types/workflow-definitions";
import { useElsaClient } from "@/lib/api/provider";
import { Resources, usePermissions, Verbs } from "@/lib/permissions";
import { downloadBlob, pickFiles } from "@/lib/utils/download";
import { useUrlState } from "@/lib/utils/url-state";
import { Network } from "lucide-react";
import { ActivityToolbox, createDesignerStore, DesignerProvider, DesignerSurface, emptyFlowchart, IconButton, indexDataFlow, isDesignableContainer, useActivityRegistry } from "@/modules/designer";
import { CodeView } from "../components/code-view";
import { EditorHeader, type EditorViewState } from "../components/editor-header";
import { EditorSidePane } from "../components/editor-side-pane";
import { RunWorkflowDialog } from "../components/run-workflow-dialog";
import { CallContextStatus } from "../components/call-context/call-context-status";
import { useCallContext } from "../components/call-context/use-call-context";
import { readCallTarget, useCallGraphService } from "../call-graph";
import { WorkflowEditorProvider, type WorkflowEditorContextValue } from "../editor-context";
import { toModel, useWorkflowDefinition, useWorkflowDefinitionMutations } from "../hooks";
import { WorkflowDefinitionRoutes } from "../routes";
import { href } from "@/lib/modules/router";

/** Query parameter that carries call-context mode across drill-in navigations. */
const CONTEXT_PARAM = "context";
/** Query parameter naming the workflow the user came from, so the header can offer a way back. */
const FROM_PARAM = "from";

const AUTOSAVE_DELAY_MS = 2000;

export function WorkflowDefinitionEditorPage({ params }: { params: RouteParams }) {
  const url = useUrlState();
  const requestedVersion = url.get("version");
  const versionOptions = requestedVersion ? String(Number(requestedVersion)) : "Latest";
  const definition = useWorkflowDefinition(params.definitionId, versionOptions);
  const contextMode = url.get(CONTEXT_PARAM) === "1";
  const fromId = url.get(FROM_PARAM);
  const { registry, isLoading: registryLoading, error: registryError } = useActivityRegistry();

  if (definition.isLoading || registryLoading) return <FullPageSpinner label="Loading workflow…" />;
  if (definition.error || registryError) return <ErrorAlert error={definition.error ?? registryError} title="Could not load the workflow" />;
  if (!definition.data) return null;

  // Keyed by definition + requested version, not by version id: saving a published workflow creates
  // a new draft version, and background refetches must never remount the workspace (that would
  // drop the selection and the field being edited).
  return (
    <EditorWorkspace
      key={`${params.definitionId}:${versionOptions}`}
      initial={definition.data}
      registry={registry}
      viewingOldVersion={!!requestedVersion && !definition.data.isLatest}
      onViewVersion={(v) => url.set({ version: v })}
      initialContextMode={contextMode}
      onContextModeChange={(on) => url.set({ [CONTEXT_PARAM]: on ? "1" : undefined })}
      fromDefinitionId={fromId && fromId !== params.definitionId ? fromId : undefined}
    />
  );
}

function EditorWorkspace({
  initial,
  registry,
  viewingOldVersion,
  onViewVersion,
  initialContextMode,
  onContextModeChange,
  fromDefinitionId,
}: {
  initial: WorkflowDefinition;
  registry: ReturnType<typeof useActivityRegistry>["registry"];
  viewingOldVersion: boolean;
  onViewVersion: (version: number | undefined) => void;
  /** Call-context mode requested through the URL (drill-in from another workflow). */
  initialContextMode: boolean;
  onContextModeChange: (on: boolean) => void;
  /** The workflow the user navigated from (step link or call-context drill-in), for the back button. */
  fromDefinitionId?: string;
}) {
  const permissions = usePermissions();
  const client = useElsaClient();
  const router = useRouter();
  const pathname = usePathname();
  const choose = useConfirmChoice();
  const callGraph = useCallGraphService();
  const fromDefinition = useWorkflowDefinition(fromDefinitionId);
  const mutations = useWorkflowDefinitionMutations();
  const canWrite = permissions.has(Resources.workflowDefinitions, Verbs.write) || permissions.has(Resources.workflowDefinitions, Verbs.update);
  const canPublish = permissions.has(Resources.workflowDefinitions, Verbs.publish);
  const canRetract = permissions.has(Resources.workflowDefinitions, Verbs.retract);
  const canExecute = permissions.has(Resources.workflowDefinitions, Verbs.execute);
  const readOnly = viewingOldVersion || initial.isReadonly === true || !canWrite;

  const [definition, setDefinition] = useState<WorkflowDefinition>(initial);
  const [savedSnapshot, setSavedSnapshot] = useState(() => JSON.stringify(toModel(initial)));
  const [saving, setSaving] = useState(false);
  const [runOpen, setRunOpen] = useState(false);
  const [viewState, setViewState] = useState<EditorViewState>({ view: "designer", showToolbox: true, showProperties: true, showMinimap: true, autoSave: true, showContext: initialContextMode });
  const { view, showToolbox, showProperties, showMinimap, autoSave, showContext } = viewState;
  const patchView = useCallback(
    (patch: Partial<EditorViewState>) => {
      setViewState((v) => ({ ...v, ...patch }));
      if (patch.showContext !== undefined) onContextModeChange(patch.showContext);
    },
    [onContextModeChange],
  );

  // Created once per mount; later changes to `initial` (refetches after autosave) are ignored on purpose.
  const [store] = useState(() => createDesignerStore(normalizeRoot(initial.root), readOnly));
  const [storeVersion, setStoreVersion] = useState(0);
  useEffect(() => store.subscribe((s) => setStoreVersion(s.version)), [store]);

  // The model that would be saved right now.
  const currentModel = useCallback(() => toModel({ ...definition, root: store.getState().root }), [definition, store]);
  const serializedNow = useMemo(() => JSON.stringify(currentModel()), [currentModel, storeVersion]); // eslint-disable-line react-hooks/exhaustive-deps
  const isDirty = serializedNow !== savedSnapshot;

  const save = useCallback(
    async (options?: { publish?: boolean }): Promise<WorkflowDefinition | undefined> => {
      if (readOnly) return undefined;
      setSaving(true);
      try {
        const model = currentModel();
        const result = await mutations.save.mutateAsync({ model, publish: options?.publish ?? false });
        const saved = result.workflowDefinition;
        if (result.validationErrors?.length) toast.warning("Saved with validation errors", { description: result.validationErrors.join("\n") });
        // Keep the user's graph; only adopt server-assigned identity/version fields.
        setDefinition((d) => ({ ...d, id: saved.id, version: saved.version, isLatest: saved.isLatest, isPublished: saved.isPublished, createdAt: saved.createdAt }));
        setSavedSnapshot(JSON.stringify(model));
        // The cached call graph learns this definition's new outgoing edges; nothing else is rebuilt.
        callGraph.updateDefinition({ definitionId: definition.definitionId, name: model.name, id: saved.id, version: saved.version, isPublished: saved.isPublished, root: model.root });
        return saved;
      } catch (e) {
        toast.error(describeError(e));
        return undefined;
      } finally {
        setSaving(false);
      }
    },
    [currentModel, mutations.save, readOnly, callGraph, definition.definitionId],
  );

  // Autosave after a pause in editing.
  const saveRef = useRef(save);
  useEffect(() => {
    saveRef.current = save;
  }, [save]);
  useEffect(() => {
    if (!autoSave || !isDirty || readOnly || saving) return;
    const t = setTimeout(() => void saveRef.current(), AUTOSAVE_DELAY_MS);
    return () => clearTimeout(t);
  }, [autoSave, isDirty, readOnly, saving, serializedNow]);

  // "Settings" on a step's context toolbar makes sure the properties pane is visible.
  useEffect(
    () =>
      store.subscribe((state, prev) => {
        if (state.settingsRequest !== prev.settingsRequest) setViewState((v) => (v.showProperties ? v : { ...v, showProperties: true }));
      }),
    [store],
  );

  // Warn before leaving the page with unsaved changes.
  useEffect(() => {
    if (!isDirty) return;
    const handler = (e: BeforeUnloadEvent) => {
      e.preventDefault();
    };
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [isDirty]);

  // Ctrl/Cmd+S saves.
  useEffect(() => {
    const handler = (e: globalThis.KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "s") {
        e.preventDefault();
        void saveRef.current();
      }
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, []);

  const publish = async () => {
    const saved = await save({ publish: true });
    if (saved) toast.success(`Published version ${saved.version}`);
  };

  const unpublish = async () => {
    try {
      const result = await mutations.retract.mutateAsync(definition.definitionId);
      setDefinition((d) => ({ ...d, isPublished: result.workflowDefinition.isPublished }));
      toast.success("Workflow unpublished");
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const exportJson = async () => {
    try {
      if (isDirty) await save();
      const blob = await client.workflowDefinitions.export(definition.definitionId, "Latest");
      downloadBlob(blob, `${(definition.name ?? definition.definitionId).replace(/[^\w.-]+/g, "_")}_v${definition.version}.json`);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const importJson = async () => {
    const [file] = await pickFiles(".json", false);
    if (!file) return;
    try {
      const parsed = JSON.parse(await file.text()) as WorkflowDefinition;
      applyDefinition({ ...parsed, definitionId: definition.definitionId, id: definition.id, version: definition.version });
      toast.success("Definition imported into the editor; save to persist.");
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const saveAs = async () => {
    const name = window.prompt("Name for the copy", `${definition.name ?? "Workflow"} (copy)`);
    if (!name) return;
    try {
      const model = currentModel();
      const result = await mutations.save.mutateAsync({ model: { ...model, definitionId: undefined, id: undefined, version: undefined, name }, publish: false });
      router.push(`/workflows/definitions/${result.workflowDefinition.definitionId}/edit`);
    } catch (e) {
      toast.error(describeError(e));
    }
  };

  const applyDefinition = (next: WorkflowDefinition) => {
    setDefinition((d) => ({ ...d, ...next, root: d.root }));
    store.getState().replaceRoot(normalizeRoot(next.root));
  };

  const editorContext: WorkflowEditorContextValue = {
    definition,
    readOnly,
    isDirty,
    save,
    updateDefinition: (patch) => setDefinition((d) => ({ ...d, ...(typeof patch === "function" ? patch(d) : patch) })),
  };

  const liveDefinition = useMemo(() => ({ ...definition, root: store.getState().root }), [definition, store, storeVersion]); // eslint-disable-line react-hooks/exhaustive-deps
  const dataFlow = useMemo(
    () => indexDataFlow(store.getState().root, definition.variables ?? [], definition.inputs ?? [], definition.outputs ?? [], registry),
    [store, definition.variables, definition.inputs, definition.outputs, registry, storeVersion], // eslint-disable-line react-hooks/exhaustive-deps
  );

  // Opening another workflow (a step's target, a call-context node, or the back button): honour unsaved
  // changes first, then navigate with the call-context mode and the origin carried in the URL.
  // Nothing here toggles the mode or triggers a second navigation.
  const openDefinition = useCallback(
    async (targetId: string, options: { rememberOrigin?: boolean } = {}) => {
      const query = new URLSearchParams();
      if (showContext) query.set(CONTEXT_PARAM, "1");
      if (options.rememberOrigin !== false) query.set(FROM_PARAM, definition.definitionId);
      const qs = query.toString();
      const target = `${href(WorkflowDefinitionRoutes.edit, { definitionId: targetId })}${qs ? `?${qs}` : ""}`;
      if (target.startsWith(pathname)) return;
      if (isDirty && !readOnly) {
        const choice = await choose({
          title: "Unsaved changes",
          description: "Save this workflow before opening the other one? Discarding reverts the editor to the last saved version.",
          confirmLabel: "Save",
          alternateLabel: "Discard",
          alternateDestructive: true,
        });
        if (choice === "cancel") return;
        if (choice === "confirm") {
          const saved = await save();
          if (!saved) return; // the save failed: the toast already said why, and the workflow stays open
        } else {
          // Discard: restore the stored version in the editor and in the cached graph.
          setSavedSnapshot(JSON.stringify(toModel(initial)));
          store.getState().replaceRoot(normalizeRoot(initial.root));
          callGraph.revertLive(definition.definitionId);
        }
      }
      router.push(target);
    },
    [pathname, showContext, isDirty, readOnly, choose, save, initial, store, callGraph, definition.definitionId, router],
  );

  const onOpenFromContext = useCallback((id: string) => void openDefinition(id), [openDefinition]);

  // "Open target workflow" on Dispatch / Execute Workflow steps.
  const activityLink = useCallback(
    (activity: Activity) => {
      const call = readCallTarget(activity);
      if (!call?.targetId || call.targetId === definition.definitionId) return undefined;
      const known = callGraph.graph.nodes.get(call.targetId)?.name;
      const targetId = call.targetId;
      return { label: `Open ${known ?? "target workflow"}`, onOpen: () => void openDefinition(targetId) };
    },
    [definition.definitionId, callGraph, openDefinition],
  );

  const backTo = useMemo(
    () => (fromDefinitionId ? { label: `Back to ${fromDefinition.data?.name ?? "previous workflow"}`, onClick: () => void openDefinition(fromDefinitionId, { rememberOrigin: false }) } : undefined),
    [fromDefinitionId, fromDefinition.data?.name, openDefinition],
  );
  const callContext = useCallContext({
    enabled: showContext && view === "designer",
    definitionId: definition.definitionId,
    name: definition.name,
    store,
    storeVersion,
    onOpen: onOpenFromContext,
  });

  return (
    <WorkflowEditorProvider value={editorContext}>
      <ReactFlowProvider>
        <DesignerProvider store={store} registry={registry} dataFlow={dataFlow} activityLink={activityLink}>
          <EditorChrome
            editorContext={editorContext}
            store={store}
            registry={registry}
            dataFlow={dataFlow}
            backTo={backTo}
            state={viewState}
            onChange={patchView}
            saving={saving}
            viewingOldVersion={viewingOldVersion}
            canPublish={canPublish}
            canRetract={canRetract}
            canExecute={canExecute}
            onSave={() => void save()}
            onPublish={() => void publish()}
            onUnpublish={() => void unpublish()}
            onRun={() => setRunOpen(true)}
            onSaveAs={() => void saveAs()}
            onExport={() => void exportJson()}
            onImport={() => void importJson()}
            onBackToLatest={() => onViewVersion(undefined)}
          />
          <div className="flex h-[calc(100vh-3rem)] min-h-0 flex-col">
            <ResizablePanelGroup orientation="horizontal" className="min-h-0 flex-1">
              {!readOnly && view === "designer" && showToolbox && (
                <>
                  <ResizablePanel defaultSize="15%" minSize="11%" maxSize="30%" className="min-w-0 border-r bg-background">
                    <ActivityToolbox onCollapse={() => patchView({ showToolbox: false })} />
                  </ResizablePanel>
                  <ResizableHandle />
                </>
              )}
              <ResizablePanel defaultSize={readOnly || !showToolbox ? "72%" : "57%"} minSize="30%" className="min-w-0">
                {view === "designer" ? (
                  <div className="relative h-full w-full">
                    <DesignerSurface
                      minimap={showMinimap}
                      onToggleMinimap={() => patchView({ showMinimap: !showMinimap })}
                      onOpenToolbox={!readOnly && !showToolbox ? () => patchView({ showToolbox: true }) : undefined}
                      decorations={showContext ? callContext.decorations : undefined}
                      toolbar={
                        <IconButton
                          label={showContext ? "Hide call context" : "Show call context (callers & callees)"}
                          onClick={() => patchView({ showContext: !showContext })}
                          className={showContext ? "bg-accent text-primary" : undefined}
                        >
                          <Network />
                        </IconButton>
                      }
                    />
                    {showContext && <CallContextStatus state={callContext} />}
                  </div>
                ) : (
                  <CodeView definition={liveDefinition} onApply={applyDefinition} readOnly={readOnly} />
                )}
              </ResizablePanel>
              {(showProperties || view === "code") && (
                <>
                  <ResizableHandle />
                  <ResizablePanel defaultSize="28%" minSize="20%" maxSize="45%" className="min-w-0 border-l bg-background">
                    <EditorSidePane onViewVersion={onViewVersion} />
                  </ResizablePanel>
                </>
              )}
            </ResizablePanelGroup>
          </div>
          <RunWorkflowDialog definition={runOpen ? definition : null} onOpenChange={(o) => !o && setRunOpen(false)} />
        </DesignerProvider>
      </ReactFlowProvider>
    </WorkflowEditorProvider>
  );
}

/**
 * Mounts the editor header into the app shell header (one toolbar row instead of two). The shell
 * renders it outside this page's tree, so the element carries its own context providers.
 */
function EditorChrome({
  editorContext,
  store,
  registry,
  dataFlow,
  ...props
}: React.ComponentProps<typeof EditorHeader> & {
  editorContext: WorkflowEditorContextValue;
  store: ReturnType<typeof createDesignerStore>;
  registry: ReturnType<typeof useActivityRegistry>["registry"];
  dataFlow: ReturnType<typeof indexDataFlow>;
}) {
  // Rebuilt on every render on purpose: the shell only re-renders its own header, and the
  // page's element identity stays stable, so this cannot loop.
  const header = (
    <WorkflowEditorProvider value={editorContext}>
      <DesignerProvider store={store} registry={registry} dataFlow={dataFlow}>
        <EditorHeader {...props} />
      </DesignerProvider>
    </WorkflowEditorProvider>
  );
  useShellHeader(header, { fullBleed: true });
  return null;
}

/** The designer needs a container root (Flowchart, Sequence or StateMachine); anything else is wrapped in a flowchart. */
function normalizeRoot(root: WorkflowDefinition["root"]): Activity {
  if (isDesignableContainer(root)) return root;
  if (!root || !root.type) return { ...(root as Activity), ...emptyFlowchart(root?.id) };
  // A single non-container activity as root: show it inside a flowchart so it stays editable.
  const flowchart = emptyFlowchart();
  return { ...flowchart, activities: [{ ...root, metadata: { ...root.metadata, designer: { position: { x: 80, y: 80 } } } }] };
}
