import {Graph, Line, Path} from "@antv/x6";
import {toResult} from "@antv/x6/lib/registry/port-layout/util";

Graph.registerPortLayout('dynamicOut', (portsPositionArgs, elemBBox) => {
  return portsPositionArgs.map((_, index) => {

    const portCount = portsPositionArgs.length;
    const ratio = (index + 0.5) / portCount;
    const p1 = portCount <= 3 ? elemBBox.getTopRight() : elemBBox.getBottomLeft();
    const p2 = portCount <= 3 ? elemBBox.getBottomRight() : elemBBox.getBottomRight();
    const line = new Line(p1, p2)
    const p = line.pointAt(ratio);

    return toResult(p.round(), 0, {});

  });
});

Graph.registerPortLayout('dynamicIn', (portsPositionArgs, elemBBox) => {
  return portsPositionArgs.map((_, index) => {

    const portCount = portsPositionArgs.length;
    const ratio = (index + 0.5) / portCount;
    const p1 = portCount <= 3 ? elemBBox.getTopLeft() : elemBBox.getTopLeft();
    const p2 = portCount <= 3 ? elemBBox.getBottomLeft() : elemBBox.getTopRight();
    const line = new Line(p1, p2)
    const p = line.pointAt(ratio);

    return toResult(p.round(), 0, {});

  });
});

Graph.registerConnector(
  'elsa-connector',
  (s, e) => {
    const offset = 0;
    const deltaY = Math.abs(e.y - s.y)
    const control = Math.floor((deltaY / 3) * 2)

    const v1 = {x: s.x, y: s.y + offset + control}
    const v2 = {x: e.x, y: e.y - offset - control}

    return Path.normalize(
      `M ${s.x} ${s.y}
       L ${s.x} ${s.y + offset}
       C ${v1.x} ${v1.y} ${v2.x} ${v2.y} ${e.x} ${e.y - offset}
       L ${e.x} ${e.y}
      `,
    )
  },
  true,
);

Graph.registerEdge(
  'elsa-edge',
  {
    inherit: 'edge',
    attrs: {
      line: {
        stroke: '#C2C8D5',
        strokeWidth: 1,
        targetMarker: null
      },
    },
  },
  true,
)
