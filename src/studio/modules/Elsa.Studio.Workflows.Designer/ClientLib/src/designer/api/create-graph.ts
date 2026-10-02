import {Graph, Shape, Node, Edge} from '@antv/x6';
import {Selection} from "@antv/x6-plugin-selection";
import {Snapline} from "@antv/x6-plugin-snapline";
import {Transform} from "@antv/x6-plugin-transform";
import {Keyboard} from "@antv/x6-plugin-keyboard";
import {Clipboard} from '@antv/x6-plugin-clipboard';
import {Export} from '@antv/x6-plugin-export';
import {History} from '@antv/x6-plugin-history';
import {DotNetComponentRef, graphBindings} from "./graph-bindings";
import {DotNetFlowchartDesigner} from "./dotnet-flowchart-designer";
import {Activity} from "../models";
import {enforceMinimumNodeSize} from "./update-activity-size";
import {getActivityMeasurementScopeClass} from "./calculate-activity-size";
import {arrangeSequenceGraph, moveSelectedSequenceNode, normalizeSequenceOrientation, withSuppressedGraphUpdated} from "./sequence-mode";
import {applyDesignerThemeVariables, X6DesignerTheme} from "./apply-graph-theme";
import {createDesignerGridOptions, resolveDesignerGridOptions} from "./grid-options";
import {ActivityShape, FlowchartEdgeShape, getDesignerModePolicy, resolveDesignerMode} from './designer-mode';
import {applyStateMachineEdgeAccessibility, applyStateMachineNodeAccessibility} from '../internal/state-machine-accessibility';

export async function createGraph(containerId: string, componentRef: DotNetComponentRef, readOnly: boolean, settings?: any): Promise<string> {
    const containerElement = document.getElementById(containerId);
    const interop = new DotNetFlowchartDesigner(componentRef);
    let lastSelectedNode: any = null;
    const graphId = containerId;
    const mode = resolveDesignerMode(settings?.mode);
    const modePolicy = getDesignerModePolicy(mode);
    const isSequenceMode = modePolicy.arrangesSequence;
    const theme: X6DesignerTheme | undefined = settings?.theme;
    const measurementScopeClass = getActivityMeasurementScopeClass(containerElement);
    const gridOptions = createDesignerGridOptions(settings?.grid);

    if (!theme)
        throw new Error("An X6 designer theme is required.");

    applyDesignerThemeVariables(containerElement, theme);

    const graph = new Graph({
        container: containerElement,
        autoResize: true,
        grid: resolveDesignerGridOptions(gridOptions, theme.grid),
        magnetThreshold: settings?.magnetThreshold || 0,
        panning: settings?.panning || {
            enabled: true,
        },
        mousewheel: settings?.mousewheel || {
            enabled: true,
            factor: 1.05,
            minScale: 0.4,
            maxScale: 3,
        },
        interacting: {
            nodeMovable: () => !readOnly,
            arrowheadMovable: () => !readOnly && modePolicy.allowsInteractiveEdges,
            edgeMovable: () => !readOnly && modePolicy.allowsInteractiveEdges,
            vertexMovable: () => !readOnly && modePolicy.allowsInteractiveEdges,
            vertexAddable: () => !readOnly && modePolicy.allowsInteractiveEdges,
            vertexDeletable: () => !readOnly && modePolicy.allowsInteractiveEdges,
            edgeLabelMovable: () => !readOnly && modePolicy.allowsInteractiveEdges,
            magnetConnectable: () => !readOnly && modePolicy.allowsConnections,
            toolsAddable: () => !readOnly && modePolicy.allowsInteractiveEdges,
            useEdgeTools: () => !readOnly && modePolicy.allowsInteractiveEdges,
        },
        connecting: {
            router: mode === 'stateMachine' ? 'orth' : 'manhattan',
            allowMulti: true,
            connector: {
                name: 'rounded',
                args: {
                    radius: 8,
                },
            },
            anchor: 'center',
            connectionPoint: 'anchor',
            allowBlank: false,
            snap: {
                radius: 20,
                anchor: "bbox"
            },
            createEdge() {
                return graph.createEdge({
                    shape: modePolicy.defaultEdgeShape,
                    attrs: {
                        line: {
                            strokeDasharray: '5 5',
                        },
                    },
                    zIndex: -1,
                })
            },
            validateConnection({sourceMagnet, targetMagnet}) {
                if (!modePolicy.allowsConnections) {
                    return false;
                }

                if (!sourceMagnet || sourceMagnet.getAttribute('port-group') === 'in') {
                    return false
                }

                if (!targetMagnet || targetMagnet.getAttribute('port-group') !== 'in') {
                    return false
                }

                return true
            },
        },
        highlighting: {
            magnetAdsorbed: {
                name: 'stroke',
                args: {
                    attrs: {
                        fill: 'var(--elsa-designer-port-surface)',
                        stroke: 'var(--elsa-designer-connection-highlight)',
                        strokeWidth: 4,
                    },
                },
            },
            embedding: {
                name: 'stroke',
                args: {
                    padding: -1,
                    attrs: {
                        stroke: 'var(--elsa-designer-embedding-highlight)',
                    },
                },
            },
        }
    });

    // Registered regardless of the read-only flag: exporting the graph as an image is available in both modes.
    graph.use(new Export());

    graph.use(
        new History({
            enabled: true,
            beforeAddCommand: (e: string, args: any) => {
                if (args.key == 'tools')
                    return false;

                if (args.options?.sequenceLayout)
                    return false;

                const supportedEvents = ['cell:added', 'cell:removed', 'cell:change:*'];
                return supportedEvents.indexOf(e) >= 0;
            },
        }),
    )

    graph.use(new Snapline({
        enabled: true,
        className: 'elsa-snapline',
    }));

    graph.use(
        new Selection({
            enabled: true,
            multiple: !readOnly,
            modifiers: ['ctrl', 'shift'],
            rubberEdge: false,
            rubberNode: true,
            rubberband: true,
            movable: !readOnly,
            showNodeSelectionBox: true,
            className: 'elsa-selection'
        }),
    );

    if (!readOnly) {
        graph.use(
            new Keyboard({
                enabled: true
            })
        );

        graph.use(
            new Clipboard({
                enabled: true,
            }),
        )

        graph.use(
            new Transform({
                resizing: {
                    enabled: modePolicy.usesActivityInteractions && (settings?.resizingEnabled ?? true),
                }
            })
        );

        // Copy the cells in the graph to the internal clipboard with Ctrl+C.
        graph.bindKey(['ctrl+c', 'meta+c'], () => {
            if (!modePolicy.usesActivityInteractions)
                return false;

            const cells = graph.getSelectedCells()
            if (cells.length) {
                graph.copy(cells)
            }

            return false;
        });

        graph.bindKey(['meta+x', 'ctrl+x'], () => {
            if (!modePolicy.usesActivityInteractions)
                return false;

            const cells = graph.getSelectedCells()
            if (cells.length) {
                if (isSequenceMode) {
                    const binding = graphBindings[graphId];
                    const nodes = cells.filter(cell => cell.isNode());
                    graph.copy(nodes);
                    withSuppressedGraphUpdated(binding, () => graph.removeCells(nodes));
                    arrangeSequenceGraph(binding);
                    interop.raiseGraphUpdated();
                    return false;
                }

                graph.cut(cells)
            }

            return false;
        });

        // Paste
        graph.bindKey(['ctrl+v', 'meta+v'], () => {
            if (!graph.isClipboardEmpty()) {
                const cells = graph.getCellsInClipboard();

                if (cells.length == 0)
                    return;

                if (!modePolicy.usesActivityInteractions)
                    return false;

                const activityCells = cells.filter(x => x.shape == ActivityShape);
                const edgeCells: Edge[] = isSequenceMode ? [] : cells.filter(x => x.shape == FlowchartEdgeShape);

                interop.raisePasteCellsRequested(activityCells, edgeCells);
            }

            return false;
        });

        // Undo
        graph.bindKey(['meta+z', 'ctrl+z'], () => {
            if (graph.canUndo()) {
                graph.undo()
            }
            return false
        });

        // Redo
        graph.bindKey(['meta+y', 'ctrl+y'], () => {
            if (graph.canRedo()) {
                graph.redo()
            }
            return false
        });

        // Delete
        graph.bindKey('del', () => {
            const cells = graph.getSelectedCells()
            if (cells.length) {
                if (mode === 'stateMachine') {
                    const cell = cells[0];
                    const kind = cell.isNode() ? 'state' : 'transition';
                    void interop.raiseStateMachineDeleteRequested(kind, cell.id);
                    return false;
                }

                if (isSequenceMode) {
                    const binding = graphBindings[graphId];
                    const nodes = cells.filter(cell => cell.isNode());
                    withSuppressedGraphUpdated(binding, () => graph.removeCells(nodes));
                    arrangeSequenceGraph(binding);
                    interop.raiseGraphUpdated();
                    return false;
                }

                graph.removeCells(cells)
            }

            return false;
        });
    }

    // Select all
    graph.bindKey(['meta+a', 'ctrl+a'], () => {
        const nodes = graph.getNodes()
        if (nodes) {
            graph.select(nodes)
        }

        return false;
    });

    // zoom
    graph.bindKey(['ctrl+1', 'meta+1'], () => {
        const zoom = graph.zoom()
        if (zoom < 1.5) {
            graph.zoom(0.1)
        }
        return false;
    });

    graph.bindKey(['ctrl+2', 'meta+2'], () => {
        const zoom = graph.zoom()
        if (zoom > 0.5) {
            graph.zoom(-0.1)
        }
        return false;
    });

    graph.on('blank:click', async () => {
        if (!!lastSelectedNode) {
            lastSelectedNode.setProp('selected-port', null);
        }
        await interop.raiseCanvasSelected();
        return false;
    });

    // Move the clicked node to the front. This helps when the user clicks on a node that is behind another node.
    graph.on('node:mousedown', ({node}) => {
        node.toFront();
        return false;
    });

    // Change the edge's color and style when it is connected to a magnet.
    graph.on('edge:connected', ({edge}) => {
        edge.attr({
            line: {
                strokeDasharray: '',
            },
        });
        return false;
    });

    graph.on("edge:mouseenter", ({cell}) => {
        if (!modePolicy.allowsInteractiveEdges) {
            return false;
        }

        cell.addTools([
            {name: "vertices"},
            {
                name: "button-remove",
                args: {distance: 20},
            },
        ]);
        return false;
    });

    graph.on("edge:mouseleave", ({cell}) => {
        if (!modePolicy.allowsInteractiveEdges) {
            return false;
        }

        if (cell.hasTool("button-remove")) {
            cell.removeTool("button-remove");
        }
        return false;
    });

    graph.on('node:click', async args => {
        const {e, node} = args;

        if (!modePolicy.usesActivityInteractions) {
            if (!graph.isSelected(node))
                graph.select(node);

            return;
        }

        const activity: Activity = node.data;
        const activityId = activity.id;
        const activityElementId = `activity-${activityId}`;
        const activityElement = document.getElementById(activityElementId);
        const menuButtonElement = activityElement.querySelector('.mud-button-root');
        const embeddedPortElements = activityElement.querySelectorAll('.embedded-port');
        const mousePosition = graph.clientToLocal(e.clientX, e.clientY);
        
        // Check if the menu button was clicked.
        const menuButtonElementRect = menuButtonElement.getBoundingClientRect();
        const menuButtonElementBBox = graph.pageToLocal(menuButtonElementRect);
        
        if (menuButtonElementBBox.containsPoint(mousePosition)) {
            await interop.raiseActivityMenuButtonClicked(activity);
            return;
        }

        // Check which of the embedded ports intersect with the selected node.
        for (let i = 0; i < embeddedPortElements.length; i++) {
            const embeddedPortElement = embeddedPortElements[i];
            const embeddedPortElementRect = embeddedPortElement.getBoundingClientRect();
            const embeddedPortElementBBox = graph.pageToLocal(embeddedPortElementRect);

            if (!embeddedPortElementBBox.containsPoint(mousePosition))
                continue;

            // Mark the node as unselected.
            if (graph.isSelected(node)) {
                graph.unselect(node);
            }

            const embeddedPortName = embeddedPortElement.getAttribute('data-port-name');
            node.setProp('selected-port', embeddedPortName);
            lastSelectedNode = node;

            await interop.raiseActivityEmbeddedPortSelected(activity, embeddedPortName);
            return;
        }

        if (!graph.isSelected(node)) {
            graph.select(node);
        }

        node.setProp('selected-port', null);
    });

    graph.on('node:dblclick', async args => {
        if (!modePolicy.usesActivityInteractions)
            return;

        const {node} = args;
        const activity: Activity = node.data;
        await interop.raiseActivityDoubleClick(activity);
    });

    graph.on('node:selected', async args => {
        const {node} = args;
        if (mode === 'stateMachine') {
            if (graphBindings[graphId]?.suppressSelectionCallbacks)
                return;

            const data = node.getData();
            if (data?.kind === 'missing-state' && data.transitionVisualId) {
                await interop.raiseStateMachineTransitionSelected(data.transitionVisualId);
                return;
            }

            await interop.raiseStateMachineStateSelected(node.id);
            return;
        }

        if (!modePolicy.usesActivityInteractions)
            return;

        const activity: Activity = node.data;
        await interop.raiseActivitySelected(activity);
    });

    graph.on('edge:selected', async ({edge}) => {
        if (mode === 'stateMachine' && !graphBindings[graphId]?.suppressSelectionCallbacks)
            await interop.raiseStateMachineTransitionSelected(edge.id);
    });

    const onGraphUpdated = async (e: any) => {
        const binding = graphBindings[graphId];
        if (binding?.suppressGraphUpdated && binding.suppressGraphUpdated > 0)
            return false;

        await interop.raiseGraphUpdated();
        return false;
    };

    const onNodeRemoved = async (e: any) => {
        await onGraphUpdated(e);
        return false;
    };

    const onNodeAdded = async (e: any) => {
        await onGraphUpdated(e);
        return false;
    };

    const onNodeResizeCompleted = async (e: any) => {
        // Honor graph-level suppression during internal graph mutations.
        const binding = graphBindings[graphId];
        if (binding?.suppressGraphUpdated > 0) {
            return false;
        }
        
        const node = e.node || e.cell;
        if (node && modePolicy.enforcesActivityMinimumSize)
            await enforceMinimumNodeSize(node, measurementScopeClass);

        if (isSequenceMode)
            arrangeSequenceGraph(binding);
        
        await interop.raiseGraphUpdated();
        return false;
    };

    graph.on('node:moved', async () => {
        if (!isSequenceMode)
            return await onGraphUpdated({});

        if (readOnly)
            return false;

        const binding = graphBindings[graphId];
        arrangeSequenceGraph(binding);
        await interop.raiseGraphUpdated();
        return false;
    });
    graph.on('node:added', async e => {
        if (mode === 'stateMachine') {
            const node = e.node || e.cell;
            // X6 emits node:added before its SVG view is guaranteed to be mounted.
            // Defer one frame so the accessibility attributes land on the actual node view.
            requestAnimationFrame(() => applyStateMachineNodeAccessibility(graph, node));
        }

        if (!isSequenceMode)
            return await onNodeAdded(e);

        const binding = graphBindings[graphId];
        if (binding?.suppressGraphUpdated && binding.suppressGraphUpdated > 0)
            return false;

        arrangeSequenceGraph(binding);
        await interop.raiseGraphUpdated();
        return false;
    });
    graph.on('edge:added', async e => {
        if (mode === 'stateMachine') {
            const edge = e.edge || e.cell;
            // X6 emits edge:added before its SVG view is guaranteed to be mounted.
            // Defer one frame so the accessibility attributes land on the edge view.
            requestAnimationFrame(() => applyStateMachineEdgeAccessibility(graph, edge));
        }

        return false;
    });
    graph.on('node:removed', async e => {
        if (!isSequenceMode)
            return await onNodeRemoved(e);

        const binding = graphBindings[graphId];
        if (binding?.suppressGraphUpdated && binding.suppressGraphUpdated > 0)
            return false;

        arrangeSequenceGraph(binding);
        await interop.raiseGraphUpdated();
        return false;
    });
    // The Transform plugin emits `node:change:size` for every pointer movement. Waiting for
    // `node:resized` avoids racing X6's active gesture with asynchronous minimum-size
    // enforcement and Blazor graph updates.
    graph.on('node:resized', onNodeResizeCompleted);
    graph.on('edge:removed', onGraphUpdated);
    graph.on('edge:connected', onGraphUpdated);
    graph.on('edge:vertexs:added', onGraphUpdated);
    graph.on('edge:vertexs:removed', onGraphUpdated);

    graphBindings[graphId] = {
        graphId: graphId,
        graph: graph,
        interop: interop,
        mode,
        layoutOrientation: normalizeSequenceOrientation(settings?.layoutOrientation),
        gridOptions,
    };

    return graphId;
}
