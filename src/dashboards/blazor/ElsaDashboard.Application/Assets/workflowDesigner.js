import 'https://cdnjs.cloudflare.com/ajax/libs/jsPlumb/2.15.0/js/jsplumb.min.js';

let jsPlumbInstance = null;
let currentConnections = [];
let currentSourceEndpoints = [];
let currentTargets = [];

function onConnectionCreated(e) {
    const source = e.sourceEndpoint;
    const target = e.targetEndpoint;

    const model = {
        SourceId: source.getParameter('sourceActivityId'),
        TargetId: target.getParameter('targetActivityId'),
        Outcome: source.getParameter('outcome')
    }

    console.log(`Connection created between ${model.SourceId}:${model.Outcome} and ${model.TargetId}.`);
    DotNet.invokeMethodAsync('ElsaDashboard.Application', 'InvokeConnectionCreated', model);
}

function onConnectionClick(connection) {

}

export function destroy() {
    if (jsPlumbInstance != null) {
        console.debug("Destroying JsPlumb instance");
        jsPlumbInstance.reset();
        jsPlumbInstance.unbind("connection", onConnectionCreated);
    }
}

export function updateConnections(connections, sourceEndpoints, targets) {

    destroy();

    jsPlumbInstance = jsPlumb.getInstance({
        Container: '#workflow-canvas .canvas',
        Connector: ['Flowchart', {cornerRadius: 5}],
        Anchors: ['Bottom', 'Top', 'Left', 'Right'],
        Endpoint: ['Dot', {radius: 5}],
    });

    jsPlumbInstance.batch(function () {

        for (const connection of connections) {
            const jsPlumbConnection = jsPlumbInstance.connect({
                source: connection.sourceId,
                target: connection.targetId,
                endpoint: 'Blank',
                paintStyle: {lineWidth: 15, strokeStyle: 'rgb(243,230,18)'},
                detachable: true,
                parameters: {
                    sourceActivityId: connection.sourceActivityId,
                    targetActivityId: connection.targetActivityId,
                    outcome: connection.outcome
                }
            });
        }

        for (const endpoint of sourceEndpoints) {
            jsPlumbInstance.makeSource(endpoint.sourceId, {
                anchor: ['Bottom', 'Top', 'Left', 'Right'],
                endpoint: 'Blank',
                maxConnections: 1,
                parameters: {
                    sourceActivityId: endpoint.sourceActivityId,
                    outcome: endpoint.outcome
                }
            });
        }

        for (const target of targets) {
            jsPlumbInstance.makeTarget(target.targetId, {
                anchor: ['Bottom', 'Top', 'Left', 'Right'],
                endpoint: 'Blank',
                parameters: {
                    targetActivityId: target.targetActivityId
                }
            });
        }
    });

    jsPlumbInstance.bind("connection", onConnectionCreated);
}