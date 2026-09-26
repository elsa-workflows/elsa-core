import { createRef } from 'react';
import { createRoot } from 'react-dom/client';
import { Designer, type DesignerHandle } from '../components/Designer';
import { DotNetReactDesigner } from '../dotnet-bridge';
import { reactBindings } from '../bindings';
import type { DotNetComponentRef, ElsaGraph, ReactDesignerSettings } from '../types';

export async function createReactGraph(
    containerId: string,
    componentRef: DotNetComponentRef,
    readOnly: boolean,
    settings?: ReactDesignerSettings
): Promise<string> {
    const container = document.getElementById(containerId);
    if (!container) throw new Error(`Container ${containerId} not found`);

    const interop = new DotNetReactDesigner(componentRef);
    const handleRef = createRef<DesignerHandle>();
    const root = createRoot(container);

    // Render the React Flow app. The handle is captured via ref so the
    // imperative methods (setGraph, fitView, ...) are available to the
    // .NET-callable functions below.
    root.render(<Designer ref={handleRef} initialReadOnly={readOnly} interop={interop} mode={settings?.mode ?? 'flowchart'} />);

    reactBindings[containerId] = {
        graphId: containerId,
        container,
        root,
        componentRef,
        setGraph: (graph: ElsaGraph) => handleRef.current?.setGraph(graph),
        setReadOnly: (value: boolean) => handleRef.current?.setReadOnly(value),
        selectNode: (id: string) => handleRef.current?.selectNode(id),
        fitView: () => handleRef.current?.fitView(),
        centerContent: () => handleRef.current?.centerContent(),
        readGraph: () => handleRef.current?.readGraph() ?? { nodes: [], edges: [] },
        setSequenceOrientation: (orientation) => handleRef.current?.setSequenceOrientation(orientation),
        moveSelectedSequenceNode: (direction) => handleRef.current?.moveSelectedSequenceNode(direction),
        addNode: (node, dropPagePosition) => handleRef.current?.addNode(node, dropPagePosition),
        updateNode: (node) => handleRef.current?.updateNode(node),
        updateNodeStats: (activityId, stats) => handleRef.current?.updateNodeStats(activityId, stats),
        autoLayout: () => handleRef.current?.autoLayout(),
        pasteCells: (activityNodes, edges) => handleRef.current?.pasteCells(activityNodes, edges),
    };

    return containerId;
}
