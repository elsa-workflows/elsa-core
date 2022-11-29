import {Graph, Line, Path} from "@antv/x6";

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
        targetMarker: 'classic', size: 6,
      },
    },
  },
  true,
)
