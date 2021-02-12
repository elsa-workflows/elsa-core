import {jsPlumb} from 'jsplumb';

let count = 0;
let jsPlumbInstance = null;

function onConnectionCreated(e) {
  const source = e.sourceEndpoint;
  const target = e.targetEndpoint;

  const model = {
    SourceId: source.getParameter('sourceActivityId'),
    TargetId: target.getParameter('targetActivityId'),
    Outcome: source.getParameter('outcome')
  }

  console.log(`Connection created between ${model.SourceId}:${model.Outcome} and ${model.TargetId}.`);
  //DotNet.invokeMethodAsync('ElsaDashboard.Application', 'InvokeConnectionCreated', model);
}

function onConnectionClick(connection) {

}

export function destroy() {
  if (jsPlumbInstance != null) {
    console.debug("Destroying JsPlumb instance");

    jsPlumbInstance.unbind("connection", onConnectionCreated);
    jsPlumbInstance.unmakeEverySource(); // Ensures all mouse event handlers are removed.
    jsPlumbInstance.unmakeEveryTarget();
    jsPlumbInstance.reset();
    jsPlumbInstance = null;
  }
}

function unmakeSourceWrapper(){
  this.jsPlumbInstance.unmakeSource = function (el, connectionType, doNotClearArrays) {
    debugger;
    var info = this._info(el);
    this._currentInstance.destroyDroppable(info.el, "internal");
    var eldefs = this.sourceEndpointDefinitions[info.id];
    if (eldefs) {
      for (var def in eldefs) {
        if (connectionType == null || connectionType === def) {
          var mouseDownListener = eldefs[def].trigger;
          if (mouseDownListener) {
            this._currentInstance.off(info.el, "mousedown", mouseDownListener);
          }
          if (!doNotClearArrays) {
            delete this.sourceEndpointDefinitions[info.id][def];
          }
        }
      }
    }

    return this;
  };
}

export function updateConnections(container, connections, sourceEndpoints, targets) {

  destroy();

  console.debug("Creating JsPlumb instance");
  jsPlumbInstance = (jsPlumb as any).getInstance({

    Container: container,
    Connector: ['Flowchart', {cornerRadius: 5}],
    Anchors: ['Bottom', 'Top', 'Left', 'Right'], // The typescript definition does not have an `Anchors` field, but the jsPlumb library does.
    Endpoint: ['Dot', {radius: 5}]
  });

  (jsPlumbInstance as any).MyCount = ++count;

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
          anchor: ['Bottom', 'Top', 'Left', 'Right'],
          endpoint: 'Blank',
          parameters: {
            targetActivityId: target.targetActivityId
          }
        });
      }
    });

    jsPlumbInstance.bind("connection", onConnectionCreated);
  });
}
