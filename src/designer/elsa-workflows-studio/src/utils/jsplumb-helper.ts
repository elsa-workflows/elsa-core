import {jsPlumb} from 'jsplumb';

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
    jsPlumbInstance.reset();
    jsPlumbInstance.unbind("connection", onConnectionCreated);
  }
}

export function updateConnections(canvas, connections, sourceEndpoints, targets) {

  destroy();


  jsPlumbInstance = jsPlumb.getInstance({

    Container: canvas,
    PaintStyle:{
      strokeWidth:6,
      stroke:"#567567",
    },
    Connector: ['Flowchart', {cornerRadius: 5}],
    Anchor: ['Bottom', 'Top', 'Left', 'Right'],
    Endpoint: ['Dot', {radius: 5}],
    EndpointStyle : { fill: "#567567"  }
  });

  jsPlumbInstance.ready(() => {

    jsPlumbInstance.batch(function () {

      for (const connection of connections) {
        const jsPlumbConnection = jsPlumbInstance.connect({
          source: connection.sourceId,
          target: connection.targetId,
          // endpoint: 'Blank',
          // paintStyle: {lineWidth: 15, strokeStyle: 'rgb(243,230,18)'},
          // detachable: true,
          // parameters: {
          //   sourceActivityId: connection.sourceActivityId,
          //   targetActivityId: connection.targetActivityId,
          //   outcome: connection.outcome
          // }
        });
      }

      // for (const endpoint of sourceEndpoints) {
      //   jsPlumbInstance.makeSource(endpoint.sourceId, {
      //     anchor: ['Bottom', 'Top', 'Left', 'Right'],
      //     endpoint: 'Blank',
      //     maxConnections: 1,
      //     parameters: {
      //       sourceActivityId: endpoint.sourceActivityId,
      //       outcome: endpoint.outcome
      //     }
      //   });
      // }
      //
      // for (const target of targets) {
      //   jsPlumbInstance.makeTarget(target.targetId, {
      //     anchor: ['Bottom', 'Top', 'Left', 'Right'],
      //     endpoint: 'Blank',
      //     parameters: {
      //       targetActivityId: target.targetActivityId
      //     }
      //   });
      // }
    });

    //jsPlumbInstance.bind("connection", onConnectionCreated);
  });
}
