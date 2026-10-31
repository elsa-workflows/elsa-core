import { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState, forwardRef } from 'react';
import {
    ReactFlow,
    ReactFlowProvider,
    Background,
    BackgroundVariant,
    Controls,
    MiniMap,
    Panel,
    MarkerType,
    ConnectionLineType,
    applyNodeChanges,
    applyEdgeChanges,
    useReactFlow,
    type Node,
    type Edge,
    type NodeChange,
    type EdgeChange,
    type Connection,
    type DefaultEdgeOptions,
    type IsValidConnection,
    type NodeMouseHandler,
    type OnNodeDrag,
    type OnConnectStart,
    type OnConnectEnd,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import type { ActivityDescriptorDto, DesignerMode, ElsaActivityNode, ElsaActivityStats, ElsaEdge, ElsaGraph, ElsaActivity, ElsaPort, SequenceLayoutOrientation } from '../types';
import { ActivityNode, type ActivityNodeData } from './ActivityNode';
import { DotNetReactDesigner } from '../dotnet-bridge';
import { dagreLayout } from '../internal/dagre-layout';
import { computeSnap, type SnapGuide } from '../internal/snap-lines';
import { arrangeHappyPath } from '../internal/happy-path-layout';
import { useCatalogLoader } from '../internal/use-catalog-loader';
import { SnapLines } from './SnapLines';
import { ElsaEdge as ElsaEdgeComponent, type ElsaEdgeData } from './ElsaEdge';
import { EdgeOpsContext, type EdgeOps } from './EdgeOpsContext';
import { ConnectMenu } from './ConnectMenu';

export interface DesignerHandle {
    setGraph: (graph: ElsaGraph) => void;
    setReadOnly: (readOnly: boolean) => void;
    selectNode: (id: string) => void;
    fitView: () => void;
    centerContent: () => void;
    readGraph: () => ElsaGraph;
    setSequenceOrientation: (orientation: SequenceLayoutOrientation) => void;
    moveSelectedSequenceNode: (direction: -1 | 1) => void;
    addNode: (node: ElsaActivityNode, dropPagePosition?: { x: number; y: number }) => void;
    updateNode: (node: ElsaActivityNode) => void;
    updateNodeStats: (activityId: string, stats: ElsaActivityStats) => void;
    autoLayout: () => void;
    pasteCells: (activityNodes: ElsaActivityNode[], edges: ElsaEdge[]) => void;
}

interface DesignerProps {
    initialReadOnly: boolean;
    interop: DotNetReactDesigner;
    mode?: DesignerMode;
}

const nodeTypes = { 'elsa-activity': ActivityNode };
const edgeTypes = { 'elsa-edge': ElsaEdgeComponent };

const defaultEdgeOptions: DefaultEdgeOptions = {
    type: 'elsa-edge',
    animated: true,
    markerEnd: {
        type: MarkerType.ArrowClosed,
        width: 18,
        height: 18,
        color: '#94a3b8',
    },
    style: {
        stroke: '#94a3b8',
        strokeWidth: 2,
    },
};

const connectionLineStyle = {
    stroke: '#0ea5e9',
    strokeWidth: 2,
    strokeDasharray: '6 4',
};

function miniMapNodeColor(node: Node): string {
    const activity = (node.data as ActivityNodeData | undefined)?.activity;
    if (activity?.canStartWorkflow === true) return '#0ea5e9';
    return '#cbd5e1';
}

function toReactFlowNode(
    node: ElsaActivityNode,
    onEmbeddedPortClick: (activity: ElsaActivity, portName: string) => void,
    onDeleteRequest: (nodeId: string) => void,
    readOnly: boolean,
): Node<ActivityNodeData> {
    return {
        id: node.id,
        type: 'elsa-activity',
        position: { x: node.position?.x ?? 0, y: node.position?.y ?? 0 },
        data: {
            activity: node.data,
            ports: node.ports?.items ?? [],
            activityStats: node.activityStats,
            selectedPort: null,
            readOnly,
            onEmbeddedPortClick,
            onDeleteRequest,
        },
        // Intentionally no style.width/height: the React Flow wrapper
        // auto-sizes to the rendered activity body so there's no padding
        // gap between the visible card and the click target.
    };
}

function toReactFlowEdge(edge: ElsaEdge, index: number): Edge {
    const id = `${edge.source.cell}:${edge.source.port ?? ''}->${edge.target.cell}:${edge.target.port ?? ''}#${index}`;
    return {
        id,
        source: edge.source.cell,
        target: edge.target.cell,
        sourceHandle: edge.source.port,
        targetHandle: edge.target.port,
        data: { vertices: edge.vertices ?? [], structural: edge.shape === 'elsa-sequence-edge' } satisfies ElsaEdgeData,
    };
}

function fromReactFlowEdge(edge: Edge): ElsaEdge {
    const vertices = (edge.data as ElsaEdgeData | undefined)?.vertices ?? [];
    return {
        source: { cell: edge.source, port: edge.sourceHandle ?? '' },
        target: { cell: edge.target, port: edge.targetHandle ?? '' },
        shape: 'elsa-edge',
        vertices,
    };
}

// Pick the "default" outgoing edge from a node — prefer one whose sourceHandle
// is exactly "Done" (Elsa's canonical default port), else the first 'out'
// edge we find. Used to bridge predecessors to successors when a node in the
// middle is removed.
function pickDefaultOutgoing(edges: Edge[], nodeId: string): Edge | undefined {
    const outs = edges.filter(e => e.source === nodeId);
    if (outs.length === 0) return undefined;
    return outs.find(e => (e.sourceHandle ?? '').toLowerCase() === 'done') ?? outs[0];
}

// Walk forward from `fromId` along default-output edges, skipping any nodes in
// `removedIds`, until we land on a surviving target. Returns null if we reach
// a dead end or detect a cycle.
function findFirstAliveSuccessor(
    fromId: string,
    removedIds: Set<string>,
    allEdges: Edge[],
    visited: Set<string> = new Set(),
): { target: string; targetHandle?: string | null } | null {
    if (visited.has(fromId)) return null;
    visited.add(fromId);
    const def = pickDefaultOutgoing(allEdges, fromId);
    if (!def) return null;
    if (removedIds.has(def.target)) {
        return findFirstAliveSuccessor(def.target, removedIds, allEdges, visited);
    }
    return { target: def.target, targetHandle: def.targetHandle ?? null };
}

// Builds bridge edges that re-link predecessors of removed nodes to the first
// surviving successor reachable via default-output edges. De-duplicates so we
// never produce two parallel edges between the same source/target/handles.
function computeReconnectingBridges(removedIds: Set<string>, allEdges: Edge[]): Edge[] {
    const bridges: Edge[] = [];
    const seen = new Set<string>();
    for (const removedId of removedIds) {
        const incoming = allEdges.filter(e => e.target === removedId && !removedIds.has(e.source));
        if (incoming.length === 0) continue;
        const successor = findFirstAliveSuccessor(removedId, removedIds, allEdges);
        if (!successor) continue;
        for (const inEdge of incoming) {
            const key = `${inEdge.source}:${inEdge.sourceHandle ?? ''}->${successor.target}:${successor.targetHandle ?? ''}`;
            if (seen.has(key)) continue;
            seen.add(key);
            bridges.push({
                id: `${key}#bridge-${Date.now()}-${bridges.length}`,
                source: inEdge.source,
                target: successor.target,
                sourceHandle: inEdge.sourceHandle ?? undefined,
                targetHandle: successor.targetHandle ?? undefined,
            });
        }
    }
    return bridges;
}

function fromReactFlowNode(node: Node<ActivityNodeData>): ElsaActivityNode {
    // Prefer the measured size (the rendered body's actual dimensions) over a
    // stale persisted size; fall back through style and finally a sensible
    // default for the very first save before measurement runs.
    const measuredW = node.measured?.width;
    const measuredH = node.measured?.height;
    const styleW = typeof node.style?.width === 'number' ? node.style.width : undefined;
    const styleH = typeof node.style?.height === 'number' ? node.style.height : undefined;
    const data = node.data as unknown as ActivityNodeData;
    return {
        id: node.id,
        shape: 'elsa-activity',
        position: { x: node.position.x, y: node.position.y },
        size: { width: measuredW ?? styleW ?? 200, height: measuredH ?? styleH ?? 80 },
        data: data.activity,
        ports: { items: data.ports ?? [] },
        activityStats: data.activityStats,
    };
}

function normalizeOrientation(value?: string | null): SequenceLayoutOrientation {
    return value === 'horizontal' ? 'horizontal' : 'vertical';
}

function isHorizontal(orientation: SequenceLayoutOrientation): boolean {
    return orientation === 'horizontal';
}

function sortSequenceNodes(nodes: Node<ActivityNodeData>[], orientation: SequenceLayoutOrientation): Node<ActivityNodeData>[] {
    return [...nodes].sort((a, b) => {
        const primary = isHorizontal(orientation)
            ? a.position.x - b.position.x
            : a.position.y - b.position.y;
        if (primary !== 0) return primary;
        return isHorizontal(orientation)
            ? a.position.y - b.position.y
            : a.position.x - b.position.x;
    });
}

function arrangeSequenceNodes(nodes: Node<ActivityNodeData>[], orientation: SequenceLayoutOrientation): Node<ActivityNodeData>[] {
    return arrangeSequenceNodesInOrder(sortSequenceNodes(nodes, orientation), orientation);
}

function arrangeSequenceNodesInOrder(nodes: Node<ActivityNodeData>[], orientation: SequenceLayoutOrientation): Node<ActivityNodeData>[] {
    const gap = 160;
    return nodes.map((node, index) => ({
        ...node,
        position: isHorizontal(orientation)
            ? { x: index * gap, y: 0 }
            : { x: 0, y: index * gap },
    }));
}

function buildSequenceEdges(nodes: Node<ActivityNodeData>[]): Edge[] {
    return nodes.slice(0, -1).map((node, index) => {
        const next = nodes[index + 1];
        return {
            id: `${node.id}:Done->${next.id}:In#sequence-${index}`,
            source: node.id,
            target: next.id,
            sourceHandle: 'Done',
            targetHandle: 'In',
            type: 'elsa-edge',
            data: { structural: true, vertices: [] } satisfies ElsaEdgeData,
        } satisfies Edge;
    });
}

function moveSequenceSelection(
    nodes: Node<ActivityNodeData>[],
    orientation: SequenceLayoutOrientation,
    direction: -1 | 1,
): Node<ActivityNodeData>[] {
    const ordered = sortSequenceNodes(nodes, orientation);
    const index = ordered.findIndex(n => n.selected);
    if (index < 0) return nodes;
    const target = index + direction;
    if (target < 0 || target >= ordered.length) return nodes;
    const next = [...ordered];
    [next[index], next[target]] = [next[target], next[index]];
    return arrangeSequenceNodesInOrder(next, orientation);
}

interface HistorySnapshot {
    nodes: Node<ActivityNodeData>[];
    edges: Edge[];
}

const HISTORY_LIMIT = 100;

const InnerDesigner = forwardRef<DesignerHandle, DesignerProps>(function InnerDesigner(props, ref) {
    const { initialReadOnly, interop, mode = 'flowchart' } = props;
    const isSequenceMode = mode === 'sequence';
    const [nodes, setNodes] = useState<Node<ActivityNodeData>[]>([]);
    const [edges, setEdges] = useState<Edge[]>([]);
    const [readOnly, setReadOnly] = useState<boolean>(initialReadOnly);
    const [sequenceOrientation, setSequenceOrientationState] = useState<SequenceLayoutOrientation>('vertical');
    const [snapGuides, setSnapGuides] = useState<SnapGuide[]>([]);
    const flow = useReactFlow();
    const containerRef = useRef<HTMLDivElement | null>(null);
    // Shared fit-to-content settings — exposed via the imperative handle and
    // also used internally after setGraph and auto-layout. One source of truth
    // so the camera behaves consistently across all entry points.
    const fitToContent = useCallback(() => flow.fitView({ padding: 0.2 }), [flow]);
    // Tracks the structural fingerprint of the last graph we loaded; used to
    // decide whether to fitView. We always re-apply nodes/edges to keep state
    // canonical with C#, but only re-fit the camera when content shape changes
    // — otherwise canvas clicks (which sometimes trigger a same-content reload)
    // would jiggle the view on every interaction.
    const lastFitSignatureRef = useRef<string | null>(null);

    // Inline activity picker state. Two open modes:
    //  - 'fromPort'  : user dragged from an output handle and released on empty
    //                  canvas — picking adds the new activity AND wires an edge
    //                  from the source port to its In handle.
    //  - 'spliceEdge': user clicked the + button on an edge — picking inserts
    //                  the new activity into that edge (the existing addNode
    //                  splice logic via pendingSplitEdgeIdRef).
    //  - 'fromEmpty' : the canvas is empty and the user clicked the "Add your
    //                  first activity" call-to-action — picking adds the
    //                  activity with no auto-connection (no source/target).
    type ConnectMenuState =
        | { kind: 'fromPort'; clientX: number; clientY: number; sourceNodeId: string; sourceHandleId: string | null }
        | { kind: 'spliceEdge'; clientX: number; clientY: number; edgeId: string }
        | { kind: 'fromEmpty'; clientX: number; clientY: number };
    const [connectMenu, setConnectMenu] = useState<ConnectMenuState | null>(null);
    // Edge-drop / "splice activity onto an edge" state. While the user is
    // dragging an activity from the toolbox over the canvas, we hit-test the
    // edge under the cursor so addNode can split it (source → new → target)
    // when the drop lands. Highlight is rendered via a CSS class.
    const pendingSplitEdgeIdRef = useRef<string | null>(null);
    const [highlightedSplitEdgeId, setHighlightedSplitEdgeId] = useState<string | null>(null);
    // Tracks whether a drag is in progress over the canvas. Toggles a CSS
    // class on the container so each edge's wide hit zone becomes visible —
    // the user gets an obvious ribbon to drop onto rather than the thin path.
    const isDraggingActivityRef = useRef(false);
    const [isDraggingActivity, setIsDraggingActivity] = useState(false);
    const { catalog: activityCatalog, loading: catalogLoading, ensureLoaded: ensureCatalogLoaded } =
        useCatalogLoader(interop as DotNetReactDesigner);
    // Tracks the source of an in-progress connect drag (set by onConnectStart,
    // cleared on connect-end or cancel) so we know what to wire when the user
    // drops on empty canvas.
    const connectDragRef = useRef<{ nodeId: string; handleId: string | null } | null>(null);

    // Live refs for keyboard handlers (which see stale state otherwise).
    const nodesRef = useRef(nodes);
    const edgesRef = useRef(edges);
    nodesRef.current = nodes;
    edgesRef.current = edges;
    const readOnlyRef = useRef(readOnly);
    readOnlyRef.current = readOnly;

    // ----- History (undo/redo) -----------------------------------------------
    // We snapshot on committed changes only — drag end, add, remove, edge add,
    // activity update — never on every dragging tick.
    const historyRef = useRef<{ stack: HistorySnapshot[]; cursor: number; suspend: number }>({
        stack: [],
        cursor: -1,
        suspend: 0,
    });

    const snapshot = useCallback((nextNodes: Node<ActivityNodeData>[], nextEdges: Edge[]) => {
        const h = historyRef.current;
        if (h.suspend > 0) return;
        const snap: HistorySnapshot = {
            nodes: nextNodes.map(n => ({ ...n, data: { ...n.data } })),
            edges: nextEdges.map(e => ({ ...e })),
        };
        h.stack = h.stack.slice(0, h.cursor + 1);
        h.stack.push(snap);
        if (h.stack.length > HISTORY_LIMIT) h.stack.shift();
        h.cursor = h.stack.length - 1;
    }, []);

    const restore = useCallback((snap: HistorySnapshot) => {
        const h = historyRef.current;
        h.suspend++;
        try {
            setNodes(snap.nodes.map(n => ({ ...n, data: { ...n.data } })));
            setEdges(snap.edges.map(e => ({ ...e })));
        } finally {
            // Defer the suspend release so the resulting setState doesn't snapshot.
            queueMicrotask(() => { h.suspend--; });
        }
    }, []);

    const undo = useCallback(() => {
        const h = historyRef.current;
        if (h.cursor <= 0) return;
        h.cursor--;
        restore(h.stack[h.cursor]);
        interop.raiseGraphUpdated();
    }, [restore, interop]);

    const redo = useCallback(() => {
        const h = historyRef.current;
        if (h.cursor >= h.stack.length - 1) return;
        h.cursor++;
        restore(h.stack[h.cursor]);
        interop.raiseGraphUpdated();
    }, [restore, interop]);

    // ----- Embedded port click ----------------------------------------------
    const onEmbeddedPortClickRef = useRef<(activity: ElsaActivity, portName: string) => void>(() => {});
    onEmbeddedPortClickRef.current = useCallback((activity: ElsaActivity, portName: string) => {
        const activityId = activity?.id;
        setNodes(prev => prev.map(n => {
            if (n.id !== activityId) return n;
            return { ...n, data: { ...n.data, selectedPort: portName }, selected: false } as Node<ActivityNodeData>;
        }));
        interop.raiseActivityEmbeddedPortSelected(activity, portName);
    }, [interop]);
    const onEmbeddedPortClick = useCallback((activity: ElsaActivity, portName: string) => {
        onEmbeddedPortClickRef.current(activity, portName);
    }, []);

    // ----- Remove nodes and bridge surviving neighbors ---------------------
    // Used by both the × delete button on activities and the Delete key path.
    // For each removed node, looks up its predecessors and the first alive
    // successor via the default outgoing port (Done or first 'out'), then
    // links predecessor → successor so the user doesn't have to manually
    // re-stitch the workflow.
    const removeNodesWithBridge = useCallback((nodeIds: string[]) => {
        if (readOnlyRef.current || nodeIds.length === 0) return;
        const removedIds = new Set(nodeIds);
        const allEdges = edgesRef.current;
        const bridges = computeReconnectingBridges(removedIds, allEdges);

        // De-dup against any pre-existing edge that already matches a bridge.
        const existing = new Set(allEdges
            .filter(e => !removedIds.has(e.source) && !removedIds.has(e.target))
            .map(e => `${e.source}:${e.sourceHandle ?? ''}->${e.target}:${e.targetHandle ?? ''}`));
        const filteredBridges = bridges.filter((b: Edge) =>
            !existing.has(`${b.source}:${b.sourceHandle ?? ''}->${b.target}:${b.targetHandle ?? ''}`),
        );

        const nextNodes = nodesRef.current.filter(n => !removedIds.has(n.id));
        const nextEdges = [
            ...allEdges.filter(e => !removedIds.has(e.source) && !removedIds.has(e.target)),
            ...filteredBridges,
        ];
        setNodes(nextNodes);
        setEdges(nextEdges);
        snapshot(nextNodes, nextEdges);
        interop.raiseGraphUpdated();
    }, [interop, snapshot]);

    // Stable callback passed to ActivityNode via node data. Ref-backed so
    // captured closures stay current across renders.
    const onDeleteNodeRef = useRef<(nodeId: string) => void>(() => {});
    onDeleteNodeRef.current = useCallback((nodeId: string) => {
        removeNodesWithBridge([nodeId]);
    }, [removeNodesWithBridge]);
    const onDeleteNode = useCallback((nodeId: string) => {
        onDeleteNodeRef.current(nodeId);
    }, []);

    // ----- Imperative handle exposed to .NET via createReactGraph -----------
    useImperativeHandle(ref, () => ({
        setGraph: (graph: ElsaGraph) => {
            const orientation = normalizeOrientation(graph.layoutOrientation);
            setSequenceOrientationState(orientation);
            const mappedNodes = (graph.nodes ?? []).map(n => toReactFlowNode(n, onEmbeddedPortClick, onDeleteNode, readOnly));
            const newNodes = isSequenceMode ? arrangeSequenceNodes(mappedNodes, orientation) : mappedNodes;
            const newEdges = isSequenceMode ? buildSequenceEdges(newNodes) : (graph.edges ?? []).map(toReactFlowEdge);
            setNodes(newNodes);
            setEdges(newEdges);
            // Reset history on a fresh load.
            historyRef.current = { stack: [{ nodes: newNodes, edges: newEdges }], cursor: 0, suspend: 0 };
            // Only re-fit the camera if the graph shape actually changed.
            const signature = `${(graph.nodes ?? []).map(n => n.id).sort().join(',')}|${(graph.edges ?? [])
                .map(e => `${e.source?.cell ?? ''}:${e.source?.port ?? ''}>${e.target?.cell ?? ''}:${e.target?.port ?? ''}`)
                .sort().join(',')}`;
            if (signature !== lastFitSignatureRef.current) {
                lastFitSignatureRef.current = signature;
                requestAnimationFrame(fitToContent);
            }
        },
        setReadOnly: (value: boolean) => setReadOnly(value),
        selectNode: (id: string) => {
            setNodes(prev => prev.map(n => ({ ...n, selected: n.id === id })));
        },
        fitView: fitToContent,
        centerContent: fitToContent,
        readGraph: () => ({
            nodes: nodes.map(fromReactFlowNode),
            edges: edges.map(fromReactFlowEdge),
            layoutOrientation: sequenceOrientation,
        }),
        setSequenceOrientation: (orientation) => {
            const normalized = normalizeOrientation(orientation);
            setSequenceOrientationState(normalized);
            const arranged = arrangeSequenceNodes(nodesRef.current, normalized);
            const sequenceEdges = buildSequenceEdges(arranged);
            setNodes(arranged);
            if (isSequenceMode) setEdges(sequenceEdges);
            snapshot(arranged, isSequenceMode ? sequenceEdges : edgesRef.current);
            requestAnimationFrame(fitToContent);
            interop.raiseGraphUpdated();
        },
        moveSelectedSequenceNode: (direction) => {
            if (readOnlyRef.current) return;
            const moved = moveSequenceSelection(nodesRef.current, sequenceOrientation, direction);
            if (moved === nodesRef.current) return;
            const nextEdges = buildSequenceEdges(moved);
            setNodes(moved);
            setEdges(nextEdges);
            snapshot(moved, nextEdges);
            interop.raiseGraphUpdated();
        },
        addNode: (node, dropPagePosition) => {
            const reactNode = toReactFlowNode(node, onEmbeddedPortClick, onDeleteNode, readOnly);
            if (dropPagePosition) {
                const clientX = dropPagePosition.x - window.scrollX;
                const clientY = dropPagePosition.y - window.scrollY;
                reactNode.position = flow.screenToFlowPosition({ x: clientX, y: clientY });
            }

            // Read & clear the pending split target captured during drag-over.
            const splitEdgeId = pendingSplitEdgeIdRef.current;
            pendingSplitEdgeIdRef.current = null;
            setHighlightedSplitEdgeId(null);

            const baseNodes = [
                ...nodesRef.current.map(n => ({ ...n, selected: false })),
                { ...reactNode, selected: true },
            ];

            if (isSequenceMode) {
                const arranged = arrangeSequenceNodes(baseNodes, sequenceOrientation);
                const sequenceEdges = buildSequenceEdges(arranged);
                setNodes(arranged);
                setEdges(sequenceEdges);
                snapshot(arranged, sequenceEdges);
                interop.raiseGraphUpdated();
                requestAnimationFrame(fitToContent);
                return;
            }

            let nextEdges = edgesRef.current;
            let didSplice = false;
            if (splitEdgeId) {
                const originalIdx = edgesRef.current.findIndex(e => e.id === splitEdgeId);
                const original = originalIdx >= 0 ? edgesRef.current[originalIdx] : undefined;
                if (original) {
                    const outPort = pickDefaultOutPort(reactNode.data.ports ?? []);
                    const stamp = Date.now();
                    const inEdge: Edge = {
                        id: `${original.source}:${original.sourceHandle ?? ''}->${reactNode.id}:In#split-${stamp}-1`,
                        source: original.source,
                        target: reactNode.id,
                        sourceHandle: original.sourceHandle,
                        targetHandle: 'In',
                    };
                    const outEdge: Edge = {
                        id: `${reactNode.id}:${outPort}->${original.target}:${original.targetHandle ?? ''}#split-${stamp}-2`,
                        source: reactNode.id,
                        target: original.target,
                        sourceHandle: outPort,
                        targetHandle: original.targetHandle,
                    };
                    // Drop the original at its slot and insert the new "in" edge
                    // in its place — this keeps the source's first-outcome order
                    // intact so arrangeHappyPath still treats this branch as
                    // the happy path. The new "out" edge from the inserted node
                    // goes at the end (it's the only edge from a fresh node).
                    nextEdges = [
                        ...edgesRef.current.slice(0, originalIdx),
                        inEdge,
                        ...edgesRef.current.slice(originalIdx + 1),
                        outEdge,
                    ];
                    didSplice = true;
                }
            }

            // Apply the new node + spliced edges synchronously so the user
            // sees the connection update immediately.
            setNodes(baseNodes);
            if (nextEdges !== edgesRef.current) setEdges(nextEdges);
            interop.raiseGraphUpdated();

            if (didSplice) {
                // Defer the auto-arrange by two animation frames so React Flow's
                // resize observer has populated `node.measured` for the new
                // activity. Arranging synchronously here would force the layout
                // to fall back to default width/height for the brand-new node
                // and produce a looser layout than necessary — clicking Arrange
                // a second time would tighten it. Double-rAF avoids that.
                requestAnimationFrame(() => requestAnimationFrame(() => {
                    if (readOnlyRef.current) return;
                    const arranged = arrangeHappyPath(nodesRef.current, edgesRef.current);
                    setNodes(arranged);
                    snapshot(arranged, edgesRef.current);
                    requestAnimationFrame(fitToContent);
                }));
            } else {
                snapshot(baseNodes, nextEdges);
            }
        },
        updateNode: (node) => {
            setNodes(prev => {
                const next = prev.map(n => {
                    if (n.id !== node.id) return n;
                    return {
                        ...n,
                        data: {
                            ...n.data,
                            activity: node.data,
                            ports: node.ports?.items ?? n.data.ports,
                            activityStats: node.activityStats ?? n.data.activityStats,
                        },
                        style: {
                            ...(n.style ?? {}),
                            width: node.size?.width ?? n.style?.width,
                            height: node.size?.height ?? n.style?.height,
                        },
                    };
                });
                snapshot(next, edgesRef.current);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        updateNodeStats: (activityId, stats) => {
            // Stats updates are not user actions, don't snapshot.
            setNodes(prev => prev.map(n => {
                if (n.id !== activityId) return n;
                return { ...n, data: { ...n.data, activityStats: stats } };
            }));
        },
        autoLayout: () => {
            setNodes(prev => {
                const next = isSequenceMode ? arrangeSequenceNodes(prev, sequenceOrientation) : dagreLayout(prev, edgesRef.current);
                const nextEdges = isSequenceMode ? buildSequenceEdges(next) : edgesRef.current;
                if (isSequenceMode) setEdges(nextEdges);
                snapshot(next, nextEdges);
                requestAnimationFrame(fitToContent);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        pasteCells: (activityNodes, edgeCells) => {
            const newNodes = activityNodes.map(n => toReactFlowNode(n, onEmbeddedPortClick, onDeleteNode, readOnly));
            const stamp = Date.now();
            const newEdges: Edge[] = isSequenceMode ? [] : edgeCells.map((e, i) => ({
                id: `${e.source?.cell}:${e.source?.port ?? ''}->${e.target?.cell}:${e.target?.port ?? ''}#paste-${stamp}-${i}`,
                source: e.source.cell,
                target: e.target.cell,
                sourceHandle: e.source.port,
                targetHandle: e.target.port,
            }));
            setNodes(prev => {
                const next = [...prev.map(n => ({ ...n, selected: false })), ...newNodes.map(n => ({ ...n, selected: true }))];
                const arranged = isSequenceMode ? arrangeSequenceNodes(next, sequenceOrientation) : next;
                const nextEdges = isSequenceMode ? buildSequenceEdges(arranged) : [...edgesRef.current, ...newEdges];
                snapshot(arranged, nextEdges);
                if (isSequenceMode) setEdges(nextEdges);
                return arranged;
            });
            if (!isSequenceMode) setEdges(prev => [...prev, ...newEdges]);
            interop.raiseGraphUpdated();
        },
    }), [flow, nodes, edges, interop, onEmbeddedPortClick, onDeleteNode, readOnly, snapshot, isSequenceMode, sequenceOrientation, fitToContent]);

    // ----- Change handlers --------------------------------------------------
    const onNodesChange = useCallback((changes: NodeChange[]) => {
        if (readOnly) return;
        if (isSequenceMode) {
            const positionDone = changes.some(c => c.type === 'position' && c.dragging === false);
            const removeIds = changes
                .filter((c): c is Extract<NodeChange, { type: 'remove' }> => c.type === 'remove')
                .map(c => c.id);

            setNodes(prev => {
                const nonRemoveChanges = changes.filter(c => c.type !== 'remove');
                const changed = applyNodeChanges(nonRemoveChanges, prev) as Node<ActivityNodeData>[];
                const remaining = removeIds.length > 0 ? changed.filter(n => !removeIds.includes(n.id)) : changed;
                const next = positionDone || removeIds.length > 0
                    ? arrangeSequenceNodes(remaining, sequenceOrientation)
                    : remaining;
                const nextEdges = buildSequenceEdges(next);
                setEdges(nextEdges);
                if (positionDone || removeIds.length > 0) snapshot(next, nextEdges);
                return next;
            });

            if (positionDone || removeIds.length > 0) interop.raiseGraphUpdated();
            return;
        }

        // c.type discriminates the union; after narrowing, dragging/id are typed.
        const positionDone = changes.some(c => c.type === 'position' && c.dragging === false);
        const removeIds = changes
            .filter((c): c is Extract<NodeChange, { type: 'remove' }> => c.type === 'remove')
            .map(c => c.id);

        if (removeIds.length > 0) {
            // Route removals (Delete/Backspace key) through the bridge helper
            // so neighbors are auto-stitched. Apply any non-remove changes in
            // the same batch (e.g. selection updates) afterwards.
            removeNodesWithBridge(removeIds);
            const otherChanges = changes.filter(c => c.type !== 'remove');
            if (otherChanges.length > 0) {
                setNodes(prev => applyNodeChanges(otherChanges, prev) as Node<ActivityNodeData>[]);
            }
            return;
        }

        setNodes(prev => {
            const next = applyNodeChanges(changes, prev) as Node<ActivityNodeData>[];
            if (positionDone) snapshot(next, edgesRef.current);
            return next;
        });
        if (positionDone) interop.raiseGraphUpdated();
    }, [readOnly, interop, snapshot, removeNodesWithBridge, isSequenceMode, sequenceOrientation]);

    const onEdgesChange = useCallback((changes: EdgeChange[]) => {
        if (readOnly || isSequenceMode) return;
        const removed = changes.some(c => c.type === 'remove');
        setEdges(prev => {
            const next = applyEdgeChanges(changes, prev);
            if (removed) snapshot(nodesRef.current, next);
            return next;
        });
        if (removed) interop.raiseGraphUpdated();
    }, [readOnly, interop, snapshot, isSequenceMode]);

    const onConnect = useCallback((connection: Connection) => {
        if (readOnly || isSequenceMode) return;
        if (!connection.source || !connection.target) return;
        const edge: Edge = {
            id: `${connection.source}:${connection.sourceHandle ?? ''}->${connection.target}:${connection.targetHandle ?? ''}#${Date.now()}`,
            source: connection.source,
            target: connection.target,
            sourceHandle: connection.sourceHandle ?? undefined,
            targetHandle: connection.targetHandle ?? undefined,
        };
        setEdges(prev => {
            const next = [...prev, edge];
            snapshot(nodesRef.current, next);
            return next;
        });
        interop.raiseGraphUpdated();
    }, [readOnly, interop, snapshot, isSequenceMode]);

    // Inline activity picker: when the user releases a connect-drag on empty
    // canvas (not on a target handle), open the picker at the release point.
    const onConnectStart: OnConnectStart = useCallback((_event, params) => {
        if (readOnly || isSequenceMode) {
            connectDragRef.current = null;
            return;
        }
        if (params.nodeId && params.handleType === 'source') {
            connectDragRef.current = { nodeId: params.nodeId, handleId: params.handleId };
        } else {
            connectDragRef.current = null;
        }
    }, [readOnly, isSequenceMode]);

    const onConnectEnd: OnConnectEnd = useCallback((event) => {
        const drag = connectDragRef.current;
        connectDragRef.current = null;
        if (readOnly || isSequenceMode || !drag) return;

        // If the release happened on a handle/node, React Flow's onConnect
        // already handled it — bail out.
        const target = event.target as HTMLElement | null;
        if (target?.closest('.react-flow__handle, .react-flow__node')) return;

        const clientX = (event as MouseEvent).clientX
            ?? (event as TouchEvent).changedTouches?.[0]?.clientX
            ?? 0;
        const clientY = (event as MouseEvent).clientY
            ?? (event as TouchEvent).changedTouches?.[0]?.clientY
            ?? 0;

        ensureCatalogLoaded();
        setConnectMenu({
            kind: 'fromPort',
            sourceNodeId: drag.nodeId,
            sourceHandleId: drag.handleId,
            clientX,
            clientY,
        });
    }, [readOnly, ensureCatalogLoaded, isSequenceMode]);

    // ----- "Arrange" button -------------------------------------------------
    // Re-flows the workflow so the happy path runs left-to-right on the top
    // row and side branches stack underneath. Equal gaps between cells
    // regardless of node sizes; goes through history so Cmd/Ctrl+Z reverts it.
    const arrangeNodes = useCallback(() => {
        if (readOnlyRef.current) return;
        if (nodesRef.current.length === 0) return;
        if (isSequenceMode) {
            const next = arrangeSequenceNodes(nodesRef.current, sequenceOrientation);
            const nextEdges = buildSequenceEdges(next);
            setNodes(next);
            setEdges(nextEdges);
            snapshot(next, nextEdges);
            requestAnimationFrame(fitToContent);
            interop.raiseGraphUpdated();
            return;
        }
        const next = arrangeHappyPath(nodesRef.current, edgesRef.current);
        setNodes(next);
        snapshot(next, edgesRef.current);
        requestAnimationFrame(fitToContent);
        interop.raiseGraphUpdated();
    }, [snapshot, interop, fitToContent, isSequenceMode, sequenceOrientation]);

    const closeConnectMenu = useCallback(() => {
        // Drop any pending splice intent so a subsequent unrelated drag-drop
        // doesn't accidentally splice a previously-clicked edge.
        pendingSplitEdgeIdRef.current = null;
        setHighlightedSplitEdgeId(null);
        setConnectMenu(null);
    }, []);

    // Track the edge the cursor is over while a drag is in progress so the
    // drop can splice the new activity into it. We use elementFromPoint over
    // ElsaEdge's wide invisible hit-path (.elsa-react-flow-edge-hit) so the
    // detection threshold matches what the user can already click on.
    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        const updateHover = (clientX: number, clientY: number) => {
            if (readOnlyRef.current || isSequenceMode) return;
            const el = document.elementFromPoint(clientX, clientY) as HTMLElement | null;
            const edgeEl = el?.closest('.react-flow__edge') as HTMLElement | null;
            const edgeId = edgeEl?.getAttribute('data-id') ?? null;
            if (pendingSplitEdgeIdRef.current !== edgeId) {
                pendingSplitEdgeIdRef.current = edgeId;
                setHighlightedSplitEdgeId(edgeId);
            }
        };

        const setDragging = (value: boolean) => {
            if (isDraggingActivityRef.current === value) return;
            isDraggingActivityRef.current = value;
            setIsDraggingActivity(value);
        };

        const onDragOver = (e: DragEvent) => {
            setDragging(true);
            updateHover(e.clientX, e.clientY);
        };
        const onDragLeave = (e: DragEvent) => {
            // Only clear when actually leaving the container, not when crossing
            // a child boundary inside it. globalThis.Node disambiguates from
            // React Flow's `Node` type which is in scope from imports above.
            const related = e.relatedTarget as globalThis.Node | null;
            if (related && container.contains(related)) return;
            setDragging(false);
            pendingSplitEdgeIdRef.current = null;
            setHighlightedSplitEdgeId(null);
        };
        // After a drop the wrapper invokes addNode → it reads the split-edge
        // ref, then clears it. We do clear the dragging flag on drop so the
        // ribbon overlay disappears immediately.
        const onDrop = () => setDragging(false);

        container.addEventListener('dragover', onDragOver);
        container.addEventListener('dragleave', onDragLeave);
        container.addEventListener('drop', onDrop);
        return () => {
            container.removeEventListener('dragover', onDragOver);
            container.removeEventListener('dragleave', onDragLeave);
            container.removeEventListener('drop', onDrop);
        };
    }, [isSequenceMode]);

    // Choose which output port a freshly-spliced node should connect FROM:
    // prefer a port literally named "Done", otherwise the first 'out' port,
    // otherwise the synthetic 'Done' that ActivityNode renders by default.
    const pickDefaultOutPort = useCallback((ports: ElsaPort[]): string => {
        const outs = ports.filter(p => p.group === 'out');
        if (outs.length === 0) return 'Done';
        const done = outs.find(p => (p.id ?? '').toLowerCase() === 'done');
        return done?.id ?? outs[0].id;
    }, []);

    const onConnectMenuPick = useCallback(async (descriptor: ActivityDescriptorDto) => {
        const menu = connectMenu;
        if (!menu) return;
        // Match the drag-drop pipeline: pass page-relative coords; binding.addNode
        // converts them to flow coords via screenToFlowPosition.
        const pageX = menu.clientX + window.scrollX;
        const pageY = menu.clientY + window.scrollY;
        setConnectMenu(null);

        if (menu.kind === 'spliceEdge' && !isSequenceMode) {
            // Hand the splice off to addNode via pendingSplitEdgeIdRef. addNode
            // reads & clears the ref, removes the original edge, and inserts
            // two replacement edges (source → new → target).
            pendingSplitEdgeIdRef.current = menu.edgeId;
            await (interop as DotNetReactDesigner)
                .addActivityAtPosition(descriptor.typeName, descriptor.version, pageX, pageY);
            setHighlightedSplitEdgeId(null);
            return;
        }

        if (menu.kind === 'fromEmpty' || isSequenceMode) {
            // First-activity flow: just add it. addNode will select it; no
            // edges to wire because there's nothing to connect to.
            await (interop as DotNetReactDesigner)
                .addActivityAtPosition(descriptor.typeName, descriptor.version, pageX, pageY);
            return;
        }

        if (menu.kind !== 'fromPort') return;

        // 'fromPort' mode: add the activity and wire an edge from the original
        // source port to the new node's "In" handle.
        const created = await (interop as DotNetReactDesigner)
            .addActivityAtPosition(descriptor.typeName, descriptor.version, pageX, pageY);
        const newId = created?.id;
        if (!newId) return;
        const edge: Edge = {
            id: `${menu.sourceNodeId}:${menu.sourceHandleId ?? ''}->${newId}:In#${Date.now()}`,
            source: menu.sourceNodeId,
            target: newId,
            sourceHandle: menu.sourceHandleId ?? undefined,
            targetHandle: 'In',
        };
        setEdges(prev => {
            const next = [...prev, edge];
            snapshot(nodesRef.current, next);
            return next;
        });
        interop.raiseGraphUpdated();
    }, [connectMenu, interop, snapshot, isSequenceMode]);

    // Triggered by ElsaEdge's + button. Lazily loads the catalog (same hook
    // the port-drag path uses) and opens the picker at the click point.
    const requestInsertActivityOnEdge = useCallback((edgeId: string, clientX: number, clientY: number) => {
        if (readOnlyRef.current) return;
        ensureCatalogLoaded();
        setHighlightedSplitEdgeId(edgeId);
        setConnectMenu({ kind: 'spliceEdge', clientX, clientY, edgeId });
    }, [ensureCatalogLoaded]);

    // Empty-state CTA: opens the same activity picker so the user can add
    // their first activity. No source/target wiring — addNode just inserts.
    const onAddFirstActivity = useCallback((event: React.MouseEvent<HTMLButtonElement>) => {
        if (readOnlyRef.current) return;
        ensureCatalogLoaded();
        setConnectMenu({ kind: 'fromEmpty', clientX: event.clientX, clientY: event.clientY });
    }, [ensureCatalogLoaded]);

    const isValidConnection: IsValidConnection = useCallback((connection) => {
        if (isSequenceMode) return false;
        const { source, target, sourceHandle, targetHandle } = connection as Connection;
        if (!source || !target || source === target) return false;
        const sourceNode = nodesRef.current.find(n => n.id === source);
        const targetNode = nodesRef.current.find(n => n.id === target);
        const sourcePorts: ElsaPort[] = (sourceNode?.data as ActivityNodeData | undefined)?.ports ?? [];
        const targetPorts: ElsaPort[] = (targetNode?.data as ActivityNodeData | undefined)?.ports ?? [];
        const isSourceOut = sourcePorts.some(p => p.id === sourceHandle && p.group === 'out')
            || sourcePorts.length === 0;
        const isTargetIn = targetPorts.some(p => p.id === targetHandle && p.group === 'in')
            || targetPorts.every(p => p.group !== 'in');
        return isSourceOut && isTargetIn;
    }, [isSequenceMode]);

    const onNodeClick: NodeMouseHandler = useCallback((_e, node) => {
        const activity = (node.data as unknown as ActivityNodeData).activity;
        interop.raiseActivitySelected(activity as ElsaActivity);
    }, [interop]);

    const onNodeDoubleClick: NodeMouseHandler = useCallback((_e, node) => {
        const activity = (node.data as unknown as ActivityNodeData).activity;
        interop.raiseActivityDoubleClick(activity as ElsaActivity);
    }, [interop]);

    const onPaneClick = useCallback(() => {
        interop.raiseCanvasSelected();
    }, [interop]);

    // Edge-level operations exposed via context to the custom ElsaEdge.
    const edgeOps = useMemo<EdgeOps>(() => ({
        readOnly,
        requestInsertActivity: requestInsertActivityOnEdge,
        deleteEdge: (edgeId) => {
            if (readOnly) return;
            setEdges(prev => {
                const next = prev.filter(e => e.id !== edgeId);
                snapshot(nodesRef.current, next);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        addVertex: (edgeId, position, segmentIndex) => {
            setEdges(prev => {
                const next = prev.map(e => {
                    if (e.id !== edgeId) return e;
                    const verts = ((e.data as ElsaEdgeData | undefined)?.vertices ?? []).slice();
                    const idx = segmentIndex ?? verts.length;
                    verts.splice(idx, 0, position);
                    return { ...e, data: { ...(e.data ?? {}), vertices: verts } };
                });
                snapshot(nodesRef.current, next);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        updateVertex: (edgeId, vertexIndex, position) => {
            // No snapshot here — drag updates fire continuously. snapshotEdges()
            // is called explicitly on drag end via EdgeOps.snapshotEdges below.
            setEdges(prev => prev.map(e => {
                if (e.id !== edgeId) return e;
                const verts = ((e.data as ElsaEdgeData | undefined)?.vertices ?? []).slice();
                if (vertexIndex < 0 || vertexIndex >= verts.length) return e;
                verts[vertexIndex] = position;
                return { ...e, data: { ...(e.data ?? {}), vertices: verts } };
            }));
        },
        removeVertex: (edgeId, vertexIndex) => {
            setEdges(prev => {
                const next = prev.map(e => {
                    if (e.id !== edgeId) return e;
                    const verts = ((e.data as ElsaEdgeData | undefined)?.vertices ?? []).slice();
                    if (vertexIndex < 0 || vertexIndex >= verts.length) return e;
                    verts.splice(vertexIndex, 1);
                    return { ...e, data: { ...(e.data ?? {}), vertices: verts } };
                });
                snapshot(nodesRef.current, next);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        snapshotEdges: (next) => {
            snapshot(nodesRef.current, next);
            interop.raiseGraphUpdated();
        },
    }), [snapshot, interop, readOnly, requestInsertActivityOnEdge]);

    // Snap-to-other-node alignment guides while dragging. We compute against
    // the live state (after React Flow applied its own delta) and snap by
    // patching the node position. Guides are visible only mid-drag.
    const onNodeDrag: OnNodeDrag<Node<ActivityNodeData>> = useCallback((_event, draggedNode) => {
        if (readOnly || isSequenceMode) return;
        const { position, guides } = computeSnap(draggedNode, nodesRef.current);
        if (position.x !== draggedNode.position.x || position.y !== draggedNode.position.y) {
            setNodes(prev => prev.map(n => (n.id === draggedNode.id ? { ...n, position } : n)));
        }
        setSnapGuides(guides);
    }, [readOnly, isSequenceMode]);

    const onNodeDragStart = useCallback(() => {
        if (readOnly) return;
        setSnapGuides([]);
    }, [readOnly]);

    const onNodeDragStop = useCallback(() => {
        setSnapGuides([]);
        if (isSequenceMode && !readOnlyRef.current) {
            const arranged = arrangeSequenceNodes(nodesRef.current, sequenceOrientation);
            const nextEdges = buildSequenceEdges(arranged);
            setNodes(arranged);
            setEdges(nextEdges);
            snapshot(arranged, nextEdges);
            interop.raiseGraphUpdated();
        }
    }, [isSequenceMode, sequenceOrientation, snapshot, interop]);

    // ----- Keyboard: copy / paste / undo / redo -----------------------------
    // React Flow already handles Delete/Backspace via deleteKeyCode; we layer
    // on Ctrl/Cmd shortcuts. Listener is scoped to the container so global
    // hotkeys elsewhere on the page aren't hijacked.
    const clipboardRef = useRef<{ nodes: Node<ActivityNodeData>[]; edges: Edge[] } | null>(null);

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        // Listen on window so the shortcuts work even when focus is in the
        // toolbox/side-panel, not just on the canvas. Bail out if the user is
        // typing in a real text field — let the browser handle its own undo.
        const isEditableTarget = (target: EventTarget | null): boolean => {
            if (!(target instanceof HTMLElement)) return false;
            const tag = target.tagName;
            if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true;
            return target.isContentEditable;
        };

        const onKeyDown = (e: KeyboardEvent) => {
            if (isEditableTarget(e.target)) return;
            const mod = e.metaKey || e.ctrlKey;
            if (!mod) return;

            const key = e.key.toLowerCase();

            // Undo: Ctrl/Cmd+Z (without shift).
            if (key === 'z' && !e.shiftKey) {
                e.preventDefault();
                undo();
                return;
            }
            // Redo: Ctrl/Cmd+Y, or Ctrl/Cmd+Shift+Z.
            if (key === 'y' || (key === 'z' && e.shiftKey)) {
                e.preventDefault();
                redo();
                return;
            }
            if (readOnlyRef.current) return;

            if (key === 'c') {
                const selectedNodes = nodesRef.current.filter(n => n.selected);
                if (selectedNodes.length === 0) return;
                const selectedIds = new Set(selectedNodes.map(n => n.id));
                const includedEdges = edgesRef.current.filter(eg => selectedIds.has(eg.source) && selectedIds.has(eg.target));
                clipboardRef.current = {
                    nodes: selectedNodes.map(n => ({ ...n, data: { ...n.data } })),
                    edges: includedEdges.map(eg => ({ ...eg })),
                };
                e.preventDefault();
                return;
            }
            if (key === 'x') {
                const selectedNodes = nodesRef.current.filter(n => n.selected);
                if (selectedNodes.length === 0) return;
                const selectedIds = new Set(selectedNodes.map(n => n.id));
                const includedEdges = edgesRef.current.filter(eg => selectedIds.has(eg.source) && selectedIds.has(eg.target));
                clipboardRef.current = {
                    nodes: selectedNodes.map(n => ({ ...n, data: { ...n.data } })),
                    edges: includedEdges.map(eg => ({ ...eg })),
                };
                setNodes(prev => {
                    const next = prev.filter(n => !selectedIds.has(n.id));
                    const nextEdges = isSequenceMode
                        ? buildSequenceEdges(next)
                        : edgesRef.current.filter(eg => !selectedIds.has(eg.source) && !selectedIds.has(eg.target));
                    snapshot(next, nextEdges);
                    if (isSequenceMode) setEdges(nextEdges);
                    return next;
                });
                if (!isSequenceMode) setEdges(prev => prev.filter(eg => !selectedIds.has(eg.source) && !selectedIds.has(eg.target)));
                interop.raiseGraphUpdated();
                e.preventDefault();
                return;
            }
            if (key === 'v') {
                const clip = clipboardRef.current;
                if (!clip || clip.nodes.length === 0) return;
                // Send to .NET so it generates fresh IDs and processes embedded ports;
                // .NET will then call back into pasteReactCells.
                const activityCells = clip.nodes.map(n => fromReactFlowNode(n));
                const edgeCells = isSequenceMode ? [] : clip.edges.map(eg => ({
                    shape: 'elsa-edge',
                    source: { cell: eg.source, port: eg.sourceHandle ?? '' },
                    target: { cell: eg.target, port: eg.targetHandle ?? '' },
                }));
                interop.raisePasteCellsRequested(activityCells, edgeCells);
                e.preventDefault();
                return;
            }
            if (key === 'a') {
                e.preventDefault();
                setNodes(prev => prev.map(n => ({ ...n, selected: true })));
                return;
            }
        };

        window.addEventListener('keydown', onKeyDown);
        return () => window.removeEventListener('keydown', onKeyDown);
    }, [undo, redo, snapshot, interop, isSequenceMode]);

    // hideAttribution=true requires a React Flow Pro licence; we don't have one, so leave it
    // false to keep the default attribution visible per @xyflow/react's open-source terms.
    const proOptions = useMemo(() => ({ hideAttribution: false }), []);

    // Decorate the edge currently highlighted as a drop target with a CSS
    // class so it visually pops while the user is dragging an activity over it.
    const renderedEdges = useMemo<Edge[]>(() => {
        if (!highlightedSplitEdgeId) return edges;
        return edges.map(e => e.id === highlightedSplitEdgeId
            ? { ...e, className: `${e.className ?? ''} elsa-react-flow-edge-split-target`.trim() }
            : e);
    }, [edges, highlightedSplitEdgeId]);

    return (
        // tabIndex makes the container focusable so its keydown listener fires.
        <div
            ref={containerRef}
            tabIndex={-1}
            className={isDraggingActivity ? 'elsa-is-dragging-activity' : undefined}
            style={{ width: '100%', height: '100%', outline: 'none' }}
        >
            {!readOnly && nodes.length === 0 && (
                // Empty-state CTA. Wrapper has pointer-events:none so it
                // doesn't intercept canvas pan; only the button is clickable.
                <div className="elsa-react-flow-empty-state">
                    <button
                        type="button"
                        className="elsa-react-flow-empty-btn"
                        onClick={onAddFirstActivity}
                        title="Add your first activity"
                    >
                        + Add your first activity
                    </button>
                </div>
            )}
            <EdgeOpsContext.Provider value={edgeOps}>
            <ReactFlow
                nodes={nodes}
                edges={renderedEdges}
                nodeTypes={nodeTypes}
                edgeTypes={edgeTypes}
                defaultEdgeOptions={defaultEdgeOptions}
                connectionLineType={ConnectionLineType.SmoothStep}
                connectionLineStyle={connectionLineStyle}
                isValidConnection={isValidConnection}
                onNodesChange={onNodesChange}
                onEdgesChange={onEdgesChange}
                onConnect={onConnect}
                onConnectStart={onConnectStart}
                onConnectEnd={onConnectEnd}
                onNodeClick={onNodeClick}
                onNodeDoubleClick={onNodeDoubleClick}
                onNodeDragStart={onNodeDragStart}
                onNodeDrag={onNodeDrag}
                onNodeDragStop={onNodeDragStop}
                onPaneClick={onPaneClick}
                nodesDraggable={!readOnly}
                nodesConnectable={!readOnly && !isSequenceMode}
                elementsSelectable={true}
                deleteKeyCode={readOnly ? null : ['Delete', 'Backspace']}
                multiSelectionKeyCode={['Shift', 'Meta', 'Control']}
                proOptions={proOptions}
                fitView
            >
                <Background variant={BackgroundVariant.Dots} gap={20} size={1.4} color="#cbd5e1" />
                <Controls showInteractive={!readOnly} />
                <MiniMap pannable zoomable nodeColor={miniMapNodeColor} maskColor="rgba(15, 23, 42, 0.06)" />
                <SnapLines guides={snapGuides} />
                {!readOnly && !isSequenceMode && (
                    <Panel position="top-right" className="elsa-react-flow-toolbar">
                        <button
                            type="button"
                            className="elsa-react-flow-toolbar-btn"
                            onClick={arrangeNodes}
                            title="Arrange the workflow with the happy path on top"
                            aria-label="Arrange workflow"
                        >
                            Arrange
                        </button>
                    </Panel>
                )}
            </ReactFlow>
            </EdgeOpsContext.Provider>
            {connectMenu && (
                <ConnectMenu
                    clientX={connectMenu.clientX}
                    clientY={connectMenu.clientY}
                    activities={activityCatalog}
                    loading={catalogLoading}
                    onPick={onConnectMenuPick}
                    onClose={closeConnectMenu}
                />
            )}
        </div>
    );
});

export const Designer = forwardRef<DesignerHandle, DesignerProps>(function Designer(props, ref) {
    return (
        <ReactFlowProvider>
            <InnerDesigner ref={ref} {...props} />
        </ReactFlowProvider>
    );
});
