import {CellView, Graph, Node} from '@antv/x6';
import { ActivityModel } from '../../../models';
import './ports';
import {ActivityNodeShape} from './shapes';

export function createGraph(
  container: HTMLElement,
  interacting: CellView.Interacting,
  disableEvents: () => void,
  enableEvents: (emitWorkflowChanged: boolean) => void,
  disableEdit: boolean = false): Graph {

  const graph = new Graph({
    container: container,
    interacting: interacting,
    embedding: {
      enabled: false,
    },
    grid: {
      type: 'mesh',
      size: 20,
      visible: true,
      args: {
        color: '#e0e0e0'
      }
    },
    height: 5000,
    width: 5000,

    // Keep disabled for now until we find that performance degrades significantly when adding too many nodes.
    // When we do enable async rendering, we need to take care of the selection rectangle after pasting nodes, which would be calculated too early (before rendering completed).
    async: false,

    autoResize: true,
    keyboard: {
      enabled: true,
      global: false,
    },
    clipboard: {
      enabled: true,
      useLocalStorage: true,
    },
    selecting: {
      enabled: true,
      showNodeSelectionBox: false,
      rubberband: !disableEdit
    },
    scroller: {
      enabled: true,
      pannable: true,
      pageVisible: true,
      pageBreak: false,
      padding: 0,
      modifiers: ['ctrl', 'meta'],
    },
    connecting: {
      allowBlank: false,
      allowMulti: !disableEdit,
      allowLoop: !disableEdit,
      allowNode: !disableEdit,
      allowEdge: false,
      allowPort: !disableEdit,
      highlight: !disableEdit,
      router: {
        name: 'manhattan',
        args: {
          padding: 1,
          startDirections: ['right', 'bottom'],
          endDirections: ['left'],
        },
      },
      //connector: 'elsa-connector',
      connector: {
        name: 'rounded',
        args: {
          radius: 10
        },
      },
      snap: {
        radius: 10,
      },
      validateMagnet({magnet}) {
        return magnet.getAttribute('port-group') !== 'in';
      },
      validateConnection({sourceView, targetView, sourceMagnet, targetMagnet}) {
        // if (!sourceMagnet || sourceMagnet.getAttribute('port-group') === 'in') {
        //   return false
        // }

        if (!targetMagnet || targetMagnet.getAttribute('port-group') !== 'in') {
          return false
        }

        const portId = sourceMagnet.getAttribute('port')!
        const node = sourceView.cell as Node
        const port = node.getPort(portId)
        return !(port && port.connected);

        // if (sourceView) {
        //   const node = sourceView.cell;
        //   if (node instanceof ActivityNodeShape) {
        //     const portId = targetMagnet.getAttribute('port');
        //     const usedOutPorts = node.getUsedOutPorts(graph);
        //     if (usedOutPorts.find((port) => port && port.id === portId)) {
        //       return false
        //     }
        //   }
        // }
        return true

      },
      createEdge() {
        return graph.createEdge({
          shape: 'elsa-edge',
          zIndex: -1,
        })
      }
    },
    onPortRendered(args) {
    },
    highlighting: {
      magnetAdsorbed: {
        name: 'stroke',
        args: {
          attrs: {
            fill: '#5F95FF',
            stroke: '#5F95FF',
          },
        },
      },
    },
    mousewheel: {
      enabled: true,
      modifiers: ['ctrl', 'meta'],
      zoomAtMousePosition: true,
      minScale: 0.5,
      maxScale: 3,
    },
    history: {
      enabled: !disableEdit,
      beforeAddCommand: (e: string, args: any) => {

        if (args.key == 'tools')
          return false;

        const supportedEvents = ['cell:added', 'cell:removed', 'cell:change:*', 'edge:change:*'];
        return supportedEvents.indexOf(e) >= 0;
      },
    },
  });

  addGraphEvents(graph, disableEvents, enableEvents, disableEdit);

  return graph;
};

export function addGraphEvents(graph,
  disableEvents: () => void,
  enableEvents: (emitWorkflowChanged: boolean) => void,
  disableEdit: boolean) {
  if (!disableEdit) {
    graph.on('node:mousedown', ({node}) => {
      node.toFront();
    });

    graph.on('edge:mouseenter', ({edge}) => {
      edge.addTools([
        'source-arrowhead',
        'target-arrowhead',
        {
          name: 'button-remove',
          args: {
            distance: -30,
          },
        },
      ])
    });

    graph.on('edge:mouseleave', ({edge}) => {
      edge.removeTools()
    });

    graph.on('edge:removed', ({ edge }) => {

    })

    graph.bindKey(['meta+c', 'ctrl+c'], () => {
      const cells = graph.getSelectedCells()
      if (cells.length) {
        graph.copy(cells)
      }
      return false
    });

    graph.bindKey(['meta+x', 'ctrl+x'], () => {
      const cells = graph.getSelectedCells()
      if (cells.length) {
        graph.cut(cells)
      }
      return false
    });

    graph.bindKey(['meta+v', 'ctrl+v'], async () => {
      if (!graph.isClipboardEmpty()) {
        disableEvents();
        const cells = graph.paste({offset: 32});

        var activityIdsMap = cells.filter(x => !!x.activity).reduce(function(map, x) {
          map[x.activity.activityId] = x.id;
          return map;
        }, {});
        for (const cell of cells) {
          if (cell.activity) {
            cell.activity.activityId = cell.id;

            const cellPosition = cell.position({relative: false});
            cell.activity.x = Math.round(cellPosition.x);
            cell.activity.y = Math.round(cellPosition.y);
          } else {
            if (cell.data) {
              cell.data.sourceId = activityIdsMap[cell.data.sourceId] || cell.data.sourceId;
              cell.data.targetId = activityIdsMap[cell.data.targetId] || cell.data.targetId;
            }
          }
        }

        await enableEvents(true);
        graph.cleanSelection();
        graph.select(cells);
      }
      return false
    });

    //undo redo
    graph.bindKey(['meta+z', 'ctrl+z'], () => {
      if (graph.history.canUndo()) {
        graph.history.undo()
      }
      return false
    });

    graph.bindKey(['meta+y', 'ctrl+y'], () => {
      if (graph.history.canRedo()) {
        graph.history.redo()
      }
      return false
    });

    //delete
    graph.bindKey('del', () => {
      const cells = graph.getSelectedCells()
      if (cells.length) {
        graph.removeCells(cells)
      }
    });
  }


  // select all;
  graph.bindKey(['meta+a', 'ctrl+a'], () => {
    const nodes = graph.getNodes()
    if (nodes) {
      graph.select(nodes)
    }
  });

  // zoom
  graph.bindKey(['ctrl+1', 'meta+1'], () => {
    const zoom = graph.zoom()
    if (zoom < 1.5) {
      graph.zoom(0.1)
    }
  });

  graph.bindKey(['ctrl+2', 'meta+2'], () => {
    const zoom = graph.zoom()
    if (zoom > 0.5) {
      graph.zoom(-0.1)
    }
  });
}

export function removeGraphEvents(
  graph: Graph
) {
  graph.off('node:mousedown');
  graph.off('edge:mouseenter');
  graph.off('edge:mouseleave');
  graph.off('edge:removed');
  graph.unbindKey(['meta+c', 'ctrl+c']);
  graph.unbindKey(['meta+x', 'ctrl+x']);
  graph.unbindKey(['meta+v', 'ctrl+v']);
  graph.unbindKey(['meta+z', 'ctrl+z']);
  graph.unbindKey(['meta+y', 'ctrl+y']);
  graph.unbindKey('del');
  graph.unbindKey(['meta+a', 'ctrl+a']);
  graph.unbindKey(['ctrl+1', 'meta+1']);
  graph.unbindKey(['ctrl+2', 'meta+2']);
}

Graph.registerNode('activity', ActivityNodeShape, true);
