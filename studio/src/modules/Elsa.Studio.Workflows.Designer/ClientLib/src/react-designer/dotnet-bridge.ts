import type { DotNetComponentRef, ElsaActivity, ActivityDescriptorDto } from './types';

// Mirrors DotNetFlowchartDesigner so the C# side keeps the same JSInvokable
// surface (HandleActivitySelected, HandleActivityDoubleClick, ...).
export class DotNetReactDesigner {
    constructor(private readonly componentRef: DotNetComponentRef) {}

    raiseActivitySelected(activity: ElsaActivity): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleActivitySelected', activity);
    }

    raiseActivityDoubleClick(activity: ElsaActivity): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleActivityDoubleClick', activity);
    }

    raiseActivityEmbeddedPortSelected(activity: ElsaActivity, portName: string): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleActivityEmbeddedPortSelected', activity, portName);
    }

    raiseCanvasSelected(): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleCanvasSelected');
    }

    raiseGraphUpdated(): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleGraphUpdated');
    }

    raisePasteCellsRequested(activityCells: any[], edgeCells: any[]): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandlePasteCellsRequested', activityCells, edgeCells);
    }

    /** Loads the catalog of activities the inline picker can offer. */
    getAvailableActivities(): Promise<ActivityDescriptorDto[]> {
        return this.componentRef.invokeMethodAsync<ActivityDescriptorDto[]>('GetAvailableActivities');
    }

    /**
     * Creates a new activity at the given page-coordinate point and returns it.
     * Position is page-relative to match the existing drag-drop coordinate
     * pipeline (binding.addNode uses screenToFlowPosition internally).
     */
    addActivityAtPosition(typeName: string, version: number, pageX: number, pageY: number): Promise<ElsaActivity | null> {
        return this.componentRef.invokeMethodAsync<ElsaActivity | null>('AddActivityAtPosition', typeName, version, pageX, pageY);
    }
}
