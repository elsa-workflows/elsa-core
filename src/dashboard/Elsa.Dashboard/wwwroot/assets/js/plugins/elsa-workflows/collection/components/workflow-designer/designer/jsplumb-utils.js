import { jsPlumb } from "jsplumb";
export class JsPlumbUtils {
}
JsPlumbUtils.createInstance = (container, readonly) => jsPlumb.getInstance({
    ConnectionsDetachable: !readonly,
    DragOptions: { cursor: 'pointer', zIndex: 2000 },
    ConnectionOverlays: [
        ['Arrow', {
                location: 1,
                visible: true,
                width: 20,
                length: 10,
                foldback: 0.8,
                paintStyle: {
                    stroke: '#7da7f2',
                    fill: '#7da7f2'
                }
            }],
        ['Label', {
                location: 0.5,
                id: 'label',
                cssClass: 'connection-label'
            }]
    ],
    Container: container
});
JsPlumbUtils.createEndpointUuid = (activityId, outcome) => `activity-${activityId}-${outcome}`;
JsPlumbUtils.getSourceEndpointOptions = (activityId, outcome, executed) => {
    const fill = '#7da7f2';
    const stroke = fill;
    const connectorFill = executed ? '#6faa44' : '#999999';
    return {
        type: "Dot",
        anchor: 'Continuous',
        paintStyle: {
            stroke: stroke,
            fill: fill,
            strokeWidth: 2
        },
        isSource: true,
        connector: ['StateMachine', {}],
        connectorStyle: {
            strokeWidth: 2,
            stroke: connectorFill
        },
        connectorHoverStyle: {
            strokeWidth: 2,
            stroke: connectorFill
        },
        connectorOverlays: [['Label', { location: [3, -1.5], cssClass: 'endpointSourceLabel' }]],
        hoverPaintStyle: {
            fill: stroke,
            stroke: fill
        },
        dragOptions: {},
        uuid: JsPlumbUtils.createEndpointUuid(activityId, outcome),
        parameters: {
            outcome
        },
        scope: null,
        reattachConnections: true,
        maxConnections: 1
    };
};
