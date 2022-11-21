import {CellView, Graph, Node} from '@antv/x6';
import { ConnectionModel } from '../../../models';
import './ports';
import {ActivityNodeShape} from './shapes';

export function createGraph(
  container: HTMLElement,
  interacting: CellView.Interacting,
  onUndoRedo: () => void,
  pasteActivities: (activities?: Array<any>, connections?: Array<ConnectionModel>) => void,
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
      showNodeSelectionBox: !disableEdit,
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

        const supportedEvents = ['cell:added', 'cell:removed', 'edge:added', 'edge:change:*', 'edge:removed'];
        return supportedEvents.indexOf(e) >= 0 || (e === 'cell:change:*' && args.key === 'position');
      },
    },
  });

  addGraphEvents(graph, onUndoRedo, pasteActivities, disableEdit);

  return graph;
};

export function addGraphEvents(graph,
  onUndoRedo: () => void,
  pasteActivities: (activities?: Array<any>, connections?: Array<ConnectionModel>) => void,
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

        const cells = graph.getCellsInClipboard()
        graph.paste({offset: 32})
        let copiedNodes: Array<any> = []
        let copiedEges: Array<any> = []

        for (const cell of cells) {
          if(cell instanceof ActivityNodeShape) {
            let activity = cell.activity

            const activityItem = {
              activityId: cell.id,
              type: activity.type,
              outcomes: activity.outcomes,
              x: activity.x,
              y: activity.y,
              properties: activity.properties,
            }
            copiedNodes.push(activityItem)
          } else {
            let source = cell.source
            let target = cell.target

            const connectionItem = {
              targetId: target.cell,
              sourceId: source.cell,
              outcome: source.port
            }
            copiedEges.push(connectionItem)
          }
        }

        if(copiedNodes.length > 0 || copiedEges.length > 0) {
          pasteActivities(copiedNodes, copiedEges)
        }

        graph.cleanSelection()
      }
      return false
    });

    //undo redo
    graph.bindKey(['meta+z', 'ctrl+z'], () => {
      if (graph.history.canUndo()) {
        graph.history.undo()
        onUndoRedo()
      }
      return false
    });

    graph.bindKey(['meta+y', 'ctrl+y'], () => {
      if (graph.history.canRedo()) {
        graph.history.redo()
        onUndoRedo()
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
