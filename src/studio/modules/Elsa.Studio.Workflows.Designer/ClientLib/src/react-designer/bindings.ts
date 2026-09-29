import type { Root } from 'react-dom/client';
import type { ElsaActivityNode, ElsaActivityStats, ElsaEdge, ElsaGraph, DotNetComponentRef, SequenceLayoutOrientation } from './types';

export interface ReactDesignerBinding {
    graphId: string;
    container: HTMLElement;
    root: Root;
    componentRef: DotNetComponentRef;
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

export const reactBindings: { [graphId: string]: ReactDesignerBinding } = {};
