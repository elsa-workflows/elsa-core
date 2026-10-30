import {Activity} from "../models";
import {graphBindings} from "./graph-bindings";

export async function raiseActivitySelected(elementId: string, activityModel: Activity | string) {
    // Get wrapper element.
    const wrapper = document.getElementById(elementId);

    // Get container element.
    const container = wrapper.closest('.graph-container');

    // Get graph ID.
    const graphId = container.id;

    // Get graph reference.
    const {interop} = graphBindings[graphId];

    // Parse activity model.
    const activity = typeof activityModel === 'string' ? JSON.parse(activityModel) : activityModel;

    await interop.raiseActivitySelected(activity);
}