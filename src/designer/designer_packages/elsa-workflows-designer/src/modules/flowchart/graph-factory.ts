import {CellView, Graph, Node, Shape} from '@antv/x6';
import { v4 as uuid } from 'uuid';
import {autoOrientConnections, createEdge, deriveNewPortId, getPortNameByPortId} from '../../utils/graph';
import './ports';
import {Activity} from "../../models";
import {Connection} from "./models";
import descriptorsStore from "../../data/descriptors-store";
import {generateUniqueActivityName} from "../../utils/generate-activity-name";

export function createGraph(
  container: HTMLElement,
  interacting: CellView.Interacting,
  getAllActivities: () => Array<Activity>
): Graph {

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
      showNodeSelectionBox: true,
      rubberband: true,
    },
    scroller: {
      enabled: true,
      pannable: true,
      pageVisible: false,
      pageBreak: false,
      padding: 0,
      modifiers: ['ctrl', 'meta'],
    },
    connecting: {
      allowBlank: false,
      allowMulti: true,
      allowLoop: true,
      allowNode: true,
      allowEdge: false,
      allowPort: true,
      highlight: true,
      router: {
        name: 'manhattan',
        args: {
          startDirections: ['top', 'right', 'left', 'bottom'],
          endDirections: ['top', 'right', 'left', 'bottom'],
        },
      },
      // router: {
      //   name: 'metro',
      //   args: {
      //     startDirections: ['bottom'],
      //     endDirections: ['top'],
      //   },
      // },
      //connector: 'elsa-connector',
      connector: {
        name: 'rounded',
        args: {
          radius: 20
        },
      },
      snap: {
        radius: 20,
      },
      validateMagnet({view, magnet}) {
        const node = view.cell as Node;
        const sourcePort = node.getPort(magnet.getAttribute('port'));
        return sourcePort.type !== 'in'
      },
      validateConnection({sourceView, targetView, sourceMagnet, targetMagnet}) {
        if (!sourceMagnet || !targetMagnet) {
          return false;
        }

        const sourceNode = sourceView.cell as Node;
        const sourcePort = sourceNode.getPort(sourceMagnet.getAttribute('port'));

        const targetNode = targetView.cell as Node;
        const targetPort = targetNode.getPort(targetMagnet.getAttribute('port'));

        if (sourcePort.type === 'in') {
          return false
        }

        if (targetPort.type !== 'in') {
          return false
        }

        const portId = targetMagnet.getAttribute('port')!
        const node = targetView.cell as Node
        return !(targetPort && targetPort.connected);
      },
      createEdge() {
        return graph.createEdge({
          shape: 'elsa-edge',
          zIndex: -1,
        })
      }
    },
    onPortRendered(args) {
      const selectors = args.contentSelectors
      const container = selectors && selectors.foContent
      if (container) {
        const port = document.createElement('div');
        port.className = 'tw-rounded-full tw-border tw-border-2 tw-border-blue tw-h-8 tw-w-8';
        port.innerHTML = `<p>done</p>`;
        (container as HTMLElement).append(port);
      }
    },
    mousewheel: {
      enabled: true,
      modifiers: ['ctrl', 'meta'],
    },
    history: {
      enabled: true,
      beforeAddCommand: (e: string, args: any) => {

        if (args.key == 'tools')
          return false;

        const supportedEvents = ['cell:added', 'cell:removed', 'cell:change:*'];

        return supportedEvents.indexOf(e) >= 0;
      },
    },
    // Todo:
    // minimap: {
    //   enabled: true,
    //   container: this.container,
    // },
    //interacting: () => state.interactingMap,
  });

  //graph.on('node:change:parent', assignParent);

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

      const allActivities = [...getAllActivities()];
      const cells = graph.getCellsInClipboard();
      const activityCells = cells.filter(x => x.shape == 'activity');
      const connectionCells = cells.filter(x => x.shape == 'elsa-edge');
      const idMap = {};
      const newCells = [];

      for (const cell of activityCells) {
        const clonedCell = cell.clone({keepId: false}) as any;
        const activity = {...clonedCell.getData()} as Activity;
        const activityTypeName = activity.type;
        const activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activityTypeName);
        const currentId = activity.id;
        const idExists = !!allActivities.find(x => x.id == currentId);

        if (idExists) {
          const newId = await generateUniqueActivityName(allActivities, activityDescriptor);
          idMap[currentId] = newId;
          activity.id = newId;
        }

        clonedCell.replaceData(activity, {});
        clonedCell.activity = activity;
        clonedCell.isClone = true;
        clonedCell.id = activity.id;
        clonedCell.store.data.id = activity.id;

        const clonedNode = clonedCell as Node;
        const position = clonedNode.getPosition();
        position.x += 64;
        position.y += 64;
        clonedNode.setPosition(position);

        allActivities.push(activity);
        newCells.push(clonedCell);
      }

      for (const cell of connectionCells) {
        const connection = {...cell.getData()} as Connection;

        connection.source = {
          activity: idMap[connection.source.activity] ?? connection.source.activity,
          port: deriveNewPortId(connection.source.port)
        };

        connection.target = {
          activity: idMap[connection.target.activity] ?? connection.target.activity,
          port: deriveNewPortId(connection.target.port)
        };

        const newEdgeProps = createEdge(connection);
        const edge = graph.createEdge(newEdgeProps);
        newCells.push(edge);
      }

      graph.addCell(newCells, {});

      // Wait for the new cells to be rendered.
      requestAnimationFrame(() => {
        graph.cleanSelection();
        graph.select(newCells);
      });
    }
    return false
  });

  // undo
  graph.bindKey(['meta+z', 'ctrl+z'], () => {
    if (graph.history.canUndo()) {
      graph.history.undo()
    }
    return false
  });

  // redo
  graph.bindKey(['meta+y', 'ctrl+y'], () => {
    if (graph.history.canRedo()) {
      graph.history.redo()
    }
    return false
  });

  // select all
  graph.bindKey(['meta+a', 'ctrl+a'], () => {
    const nodes = graph.getNodes()
    if (nodes) {
      graph.select(nodes)
    }
  });

  // delete
  graph.bindKey('del', () => {
    const cells = graph.getSelectedCells()
    if (cells.length) {
      graph.removeCells(cells)
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

  graph.on("node:moving", ({node}) => {
    autoOrientConnections(graph, node);
  });

  return graph;
}
