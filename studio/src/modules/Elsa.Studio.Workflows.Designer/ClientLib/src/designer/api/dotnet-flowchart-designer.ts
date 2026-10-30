import {DotNetComponentRef} from "./graph-bindings";
import {Activity} from "../models";

export class DotNetFlowchartDesigner {
    constructor(private componentRef: DotNetComponentRef) {
    }

    /// <summary>
    /// Raises the <see cref="ActivitySelected"/> event.
    /// </summary>
    async raiseActivitySelected(activity: Activity): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleActivitySelected', activity);
    }

    /// <summary>
    /// Raises the <see cref="ActivitySelected"/> event.
    /// </summary>
    async raiseActivityMenuButtonClicked(activity: Activity): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleActivityMenuButtonClicked', activity);
    }

    /// <summary>
    /// Raises the <see cref="ActivitySelected"/> event.
    /// </summary>
    async raiseActivityEmbeddedPortSelected(activity: Activity, portName: string): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleActivityEmbeddedPortSelected', activity, portName);
    }

    /// <summary>
    /// Raises the <see cref="ActivityDoubleClick"/> event.
    /// </summary>
    async raiseActivityDoubleClick(activity: Activity): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleActivityDoubleClick', activity);
    }

    /// <summary>
    /// Raises the <see cref="CanvasSelected"/> event.
    /// </summary>
    async raiseCanvasSelected(): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleCanvasSelected');
    }

    /// <summary>
    /// Raises the <see cref="GraphUpdated"/> event.
    /// </summary>
    async raiseGraphUpdated(): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleGraphUpdated');
    }

    async raiseStateMachineStateSelected(visualId: string): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleStateMachineStateSelected', visualId);
    }

    async raiseStateMachineTransitionSelected(visualId: string): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleStateMachineTransitionSelected', visualId);
    }

    async raiseStateMachineDeleteRequested(kind: 'state' | 'transition', visualId: string): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleStateMachineDeleteRequested', kind, visualId);
    }

    /// <summary>
    /// Raises the <see cref="PasteCellsRequested"/> event.
    /// </summary>
    async raisePasteCellsRequested(activityCells: any[], edgeCells: any[]): Promise<void> {
        await this.componentRef.invokeMethodAsync('HandlePasteCellsRequested', activityCells, edgeCells);
    }
}
