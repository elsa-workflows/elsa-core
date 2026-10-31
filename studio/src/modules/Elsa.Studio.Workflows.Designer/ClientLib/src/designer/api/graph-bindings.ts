import {Graph} from '@antv/x6';
import {DotNetFlowchartDesigner} from "./dotnet-flowchart-designer";
import type {DesignerGridOptions} from "./grid-options";
import type {DesignerMode} from './designer-mode';

// This is a global dictionary that is used to store graph instances.
export const graphBindings: { [key: string]: GraphBinding } = {};

// A dotnet component reference.
export interface DotNetComponentRef {
    invokeMethodAsync<T>(methodName: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

export interface GraphBinding {
    graphId: string;
    graph: Graph;
    interop: DotNetFlowchartDesigner;
    mode: DesignerMode;
    layoutOrientation?: 'vertical' | 'horizontal';
    suppressGraphUpdated?: number;
    suppressSelectionCallbacks?: number;
    gridOptions: DesignerGridOptions;
}
