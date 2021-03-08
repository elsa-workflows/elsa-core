import {jsPlumb} from 'jsplumb';

let jsPlumbInstance = null;

function onConnectionCreated(e, callback) {
  const source = e.sourceEndpoint;
  const target = e.targetEndpoint;

  const sourceId = source.getParameter('sourceActivityId');
  const targetId = target.getParameter('targetActivityId');
  const outcome = source.getParameter('outcome');

  debugger;
  callback(sourceId, targetId, outcome);
}

function onConnectionClick(connection) {

}

export function cleanup() {
  destroy();
}

export function destroy() {
  if (jsPlumbInstance != null) {

    jsPlumbInstance.batch(() => {
      jsPlumbInstance.unbind("connection", onConnectionCreated);
      jsPlumbInstance.unmakeEverySource(); // Ensures all mouse event handlers are removed.
      jsPlumbInstance.unmakeEveryTarget();
      jsPlumbInstance.reset();
    });

    jsPlumbInstance = null;
  }
}

export function updateConnections(container, connections, sourceEndpoints, targets, connectionCreatedCallback) {

  destroy();

  jsPlumbInstance = (jsPlumb as any).getInstance({
    Container: container,
    Connector: ['Flowchart', {cornerRadius: 5}],
    Anchors: ['Bottom', 'Top', 'Left', 'Right'], // The typescript definition does not have an `Anchors` field, but the jsPlumb library does.
    Endpoint: ['Dot', {radius: 5}]
  });

  jsPlumbInstance.ready(() => {

    jsPlumbInstance.batch(function () {

      for (const connection of connections) {
        const jsPlumbConnection = jsPlumbInstance.connect({
          source: connection.sourceId,
          target: connection.targetId,
          endpoint: 'Blank',
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
          anchor: ['Left', 'Right'],
          endpoint: 'Blank',
          parameters: {
            targetActivityId: target.targetActivityId
          }
        });
      }
    });

    jsPlumbInstance.bind("connection", e => onConnectionCreated(e, connectionCreatedCallback));
  });
}
