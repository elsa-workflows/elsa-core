import {Connection, jsPlumb} from 'jsplumb';
import {ConnectionModel} from "../models";

let jsPlumbInstance = null;

function onConnectionCreated(e, callback) {
  debugger;
  const connection: ConnectionModel = {
    sourceId: e.connection.getParameter('sourceActivityId'),
    targetId: e.connection.getParameter('targetActivityId'),
    outcome: e.connection.getParameter('outcome')
  };

  callback(connection);
}

export function cleanup() {
  destroy();
}

export function destroy() {
  console.debug(`destroy called`);
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

export function updateConnections(container, connections, sourceEndpoints, targets, connectionCreatedCallback, connectionDetachedCallback): Array<ConnectionModel> {

  destroy();
  const invalidConnections: Array<ConnectionModel> = [];

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
          detachable: connection.sourceActivityId && connection.targetActivityId,
          parameters: {
            sourceActivityId: connection.sourceActivityId,
            targetActivityId: connection.targetActivityId,
            outcome: connection.outcome
          }
        });

        if (!jsPlumbConnection) {
          console.warn(`Unable to connect ${connection.sourceId} to ${connection.targetId} via ${connection.outcome}`);
          //invalidConnections.push({sourceId: connection.sourceActivityId, targetId: connection.targetActivityId, outcome: connection.outcome});
        }
        else
          jsPlumbConnection.setData(connection);
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
    jsPlumbInstance.bind("click", (connection, e) => {
      const data = connection.getData();
      const sourceActivityId: string = data.sourceActivityId;
      const targetActivityId: string = data.targetActivityId;
      const outcome: string = data.outcome;

      if (sourceActivityId && targetActivityId && outcome) {
        const model: ConnectionModel = {
          sourceId: sourceActivityId,
          targetId: targetActivityId,
          outcome: outcome
        }
        connectionDetachedCallback(model);
      }
    });
  });

  return invalidConnections;
}
