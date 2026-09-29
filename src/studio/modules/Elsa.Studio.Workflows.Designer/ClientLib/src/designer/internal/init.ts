import {Graph, Shape} from '@antv/x6';
import {createActivityElement} from "./create-activity-element";
import {Activity} from "../models";
import {ActivityShape, FlowchartEdgeShape, SequenceEdgeShape} from '../api/designer-mode';
import {createDesignerPortGroups, designerPortMarkup} from './designer-ports';
import {registerStateMachineShapes} from './state-machine-shapes';
import {registerBpmnShapes} from '../bpmn/shapes';

export function initialize() {
    Shape.HTML.register({
        shape: ActivityShape,
        effect: ["data", "activityStats"],
        html(cell) {
            const activity: Activity = cell.getData();
            const selectedPort = cell.prop('selected-port');
            const activityStats = cell.prop('activityStats');
            return createActivityElement(activity, false, selectedPort, activityStats);
        },
        portMarkup: designerPortMarkup,
        ports: {
            groups: createDesignerPortGroups(),
        }
    });

    Graph.registerEdge(
        FlowchartEdgeShape,
        {
            inherit: 'edge',
            attrs: {
                line: {
                    stroke: 'var(--elsa-designer-edge)',
                    strokeWidth: 1,
                    targetMarker: 'classic',
                    size: 6,
                },
            },
        },
        true,
    );

    Graph.registerEdge(
        SequenceEdgeShape,
        {
            inherit: 'edge',
            attrs: {
                line: {
                    stroke: 'var(--elsa-designer-edge)',
                    strokeWidth: 2,
                    targetMarker: 'classic',
                    size: 6,
                },
            },
            connector: {
                name: 'rounded',
                args: {
                    radius: 8,
                },
            },
            router: {
                name: 'orth',
            },
        },
        true,
    );

    registerStateMachineShapes();
    registerBpmnShapes();

}
