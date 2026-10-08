import type {Cell} from '@antv/x6';

export type DesignerMode = 'flowchart' | 'sequence' | 'stateMachine';

export const ActivityShape = 'elsa-activity';
export const FlowchartEdgeShape = 'elsa-edge';
export const SequenceEdgeShape = 'elsa-sequence-edge';
export const StateMachineStateShape = 'elsa-state-machine-state';
export const StateMachineTransitionShape = 'elsa-state-machine-transition';

export interface DesignerModePolicy {
    mode: DesignerMode;
    nodeShapes: readonly string[];
    persistentEdgeShapes: readonly string[];
    defaultEdgeShape: string;
    allowsConnections: boolean;
    allowsInteractiveEdges: boolean;
    usesActivityInteractions: boolean;
    enforcesActivityMinimumSize: boolean;
    arrangesSequence: boolean;
}

const policies: Record<DesignerMode, DesignerModePolicy> = {
    flowchart: {
        mode: 'flowchart',
        nodeShapes: [ActivityShape],
        persistentEdgeShapes: [FlowchartEdgeShape],
        defaultEdgeShape: FlowchartEdgeShape,
        allowsConnections: true,
        allowsInteractiveEdges: true,
        usesActivityInteractions: true,
        enforcesActivityMinimumSize: true,
        arrangesSequence: false,
    },
    sequence: {
        mode: 'sequence',
        nodeShapes: [ActivityShape],
        // Sequence edges are derived from activity order and are intentionally not persisted.
        persistentEdgeShapes: [],
        defaultEdgeShape: SequenceEdgeShape,
        allowsConnections: false,
        allowsInteractiveEdges: false,
        usesActivityInteractions: true,
        enforcesActivityMinimumSize: true,
        arrangesSequence: true,
    },
    stateMachine: {
        mode: 'stateMachine',
        nodeShapes: [StateMachineStateShape],
        persistentEdgeShapes: [StateMachineTransitionShape],
        defaultEdgeShape: StateMachineTransitionShape,
        allowsConnections: true,
        allowsInteractiveEdges: true,
        usesActivityInteractions: false,
        enforcesActivityMinimumSize: false,
        arrangesSequence: false,
    },
};

export function resolveDesignerMode(value?: unknown): DesignerMode {
    if (value === 'sequence' || value === 'stateMachine')
        return value;

    return 'flowchart';
}

export function getDesignerModePolicy(mode: DesignerMode): DesignerModePolicy {
    return policies[mode];
}

export function isPersistentDesignerCell(cell: Cell.Properties, mode: DesignerMode): boolean {
    const policy = getDesignerModePolicy(mode);

    if (policy.nodeShapes.includes(cell.shape))
        return true;

    return policy.persistentEdgeShapes.includes(cell.shape) && hasConnectedEndpoints(cell);
}

export function hasConnectedEndpoints(cell: Cell.Properties): boolean {
    const edge = cell as any;
    return !!edge.source?.cell && !!edge.target?.cell;
}
