import {jsPlumb, jsPlumbInstance} from "jsplumb";

export class JsPlumbUtils {
  static createInstance = (container: any): jsPlumbInstance => {
   return jsPlumb.getInstance({
      ConnectionsDetachable: true,
      DragOptions: { cursor: 'pointer', zIndex: 2000 },
      ConnectionOverlays: [
        ['Arrow', {
          location: 1,
          visible: true,
          width: 11,
          length: 11
        }],
        ['Label', {
          location: 0.5,
          id: 'label',
          cssClass: 'connection-label'
        }]
      ],
      Container: container
    });}

  static createEndpointUuid = (activityId: string, outcome: string) =>  `activity-${activityId}-${outcome}`;

  static getSourceEndpointOptions = (activityId: string, outcome?: string): any => {
    const fill = '#fff';
    const stroke = '#7da7f2';
    const connectorFill = '#999';

    return {
      type: "Dot",
      endpoint: ["Dot", { radius: 5 }],
      anchor: "Continuous",
      paintStyle: {
        strokeStyle:"#F09E30",
        stroke: stroke,
        fill: fill,
        strokeWidth: 2,
        lineWidth: 1
      },
      isSource: true,
      isTarget: true,
      connector: [
        "Flowchart",
        {
          alwaysRespectStubs: true,
          cornerRadius: 5,
          gap: 0,
          stub: [10, 30]
        }
      ],
      connectorStyle: {
        strokeWidth: 1,
        stroke: connectorFill
      },
      connectorHoverStyle: {
        strokeWidth: 1,
        stroke: connectorFill
      },
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
}
