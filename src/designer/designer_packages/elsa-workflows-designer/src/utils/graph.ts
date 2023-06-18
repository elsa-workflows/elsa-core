import {Edge, Graph, Node} from "@antv/x6";
import {PortManager} from "@antv/x6/lib/model/port";
import {Connection} from "../modules/flowchart/models";
import {v4 as uuid} from 'uuid';
import {Activity} from "../models";
import optionsStore from '../data/designer-options-store';

export function rebuildGraph(graph: Graph) {
  graph.getNodes().forEach((node: Node<Node.Properties>) => {
    autoOrientConnections(graph, node);
    adjustPortMarkupByNode(node);
  });
}

export function autoOrientConnections(graph: Graph, selectedNode: Node) {
  const neighbors = graph.getNeighbors(selectedNode);
  const selectedCenter = selectedNode.getBBox().center;
  const nodeCouplesWithPositions = neighbors.map((neighbourNode) => {
    return {
      ...calculatePortPositionsOfNodeCouple(selectedCenter.x, selectedCenter.y, neighbourNode.getBBox().center.x, neighbourNode.getBBox().center.y),
      selectedNode: selectedNode,
      neighbourNode: neighbourNode
    }
  });
  nodeCouplesWithPositions.forEach(couple => updatePortsWithNewPositions(graph, couple.selectedNode, couple.portPositionOfSelectedNode, couple.neighbourNode as Node, couple.portPositionOfNeighbourNode));
}

function updatePortsWithNewPositions(
  graph: Graph,
  selectedNode: Node,
  portPositionOfSelectedNode: "left" | "right" | "top" | "bottom",
  neighbourNode: Node,
  portPositionOfNeighbourNode: "left" | "right" | "top" | "bottom"
) {
  updatePortsAndEdgeOfNodeCouple(graph, selectedNode, neighbourNode, portPositionOfSelectedNode, portPositionOfNeighbourNode);
  updatePortsAndEdgeOfNodeCouple(graph, neighbourNode, selectedNode, portPositionOfNeighbourNode, portPositionOfSelectedNode);

  adjustPortMarkupByNode(selectedNode);
  adjustPortMarkupByNode(neighbourNode);
}

function calculatePositionsForInflexibleNode(sourceNode: Node<Node.Properties>, targetNodes: Node<Node.Properties>[]):
  { sourceNode: Node<Node.Properties>, sourceNodePosition: "left" | "right" | "top" | "bottom"; targetNode: Node<Node.Properties>, targetNodePosition: "left" | "right" | "top" | "bottom"; }[] {
  const sourceNodeCenter = sourceNode.getBBox().center;
  const dxAverageForTargetNodes = targetNodes.map(node => node.getBBox().center.x).reduce((a, b) => a + b, 0) / targetNodes.length;
  const dyAverageForTargetNodes = targetNodes.map(node => node.getBBox().center.y).reduce((a, b) => a + b, 0) / targetNodes.length;

  const sourcePortWithNewPosition = {node: sourceNode, position: calculatePortPositionsOfNodeCouple(sourceNodeCenter.x, sourceNodeCenter.y, dxAverageForTargetNodes, dyAverageForTargetNodes).portPositionOfSelectedNode};
  return targetNodes.map((targetNode) => {
    return {
      sourceNode: sourcePortWithNewPosition.node,
      sourceNodePosition: sourcePortWithNewPosition.position,
      targetNode: targetNode,
      targetNodePosition: calculatePortPositionsOfNodeCouple(sourceNode.getBBox().center.x, sourceNode.getBBox().center.y, targetNode.getBBox().center.x, targetNode.getBBox().center.y).portPositionOfNeighbourNode
    }
  });
}

function calculatePortPositionsOfNodeCouple(selectedNodeX: number, selectedNodeY: number, neighbourNodeX: number, neighbourNodeY: number): { portPositionOfSelectedNode: "left" | "right" | "top" | "bottom"; portPositionOfNeighbourNode: "left" | "right" | "top" | "bottom"; } {
  const dx = selectedNodeX - neighbourNodeX;
  const dy = selectedNodeY - neighbourNodeY;
  if (dx >= 0 && dy >= 0) {
    if (dx > dy) {
      return {portPositionOfSelectedNode: "left", portPositionOfNeighbourNode: "right"};
    } else {
      return {portPositionOfSelectedNode: "top", portPositionOfNeighbourNode: "bottom"};
    }
  } else if (dx >= 0 && dy <= 0) {
    if (dx > -dy) {
      return {portPositionOfSelectedNode: "left", portPositionOfNeighbourNode: "right"};
    } else {
      return {portPositionOfSelectedNode: "bottom", portPositionOfNeighbourNode: "top"};
    }
  } else if (dx <= 0 && dy >= 0) {
    if (-dx > dy) {
      return {portPositionOfSelectedNode: "right", portPositionOfNeighbourNode: "left"};
    } else {
      return {portPositionOfSelectedNode: "top", portPositionOfNeighbourNode: "bottom"};
    }
  } else if (dx <= 0 && dy <= 0) {
    if (dx > dy) {
      return {portPositionOfSelectedNode: "right", portPositionOfNeighbourNode: "left"};
    } else {
      return {portPositionOfSelectedNode: "bottom", portPositionOfNeighbourNode: "left"};
    }
  }
}

function updatePortsAndEdgeOfNodeCouple(graph: Graph, sourceNode: Node<Node.Properties>, targetNode: Node<Node.Properties>, portPositionOfSourceNode: string, portPositionOfTargetNode: string) {

  const edge = graph.model.getEdges().find(({data}) => {
    const connection: Connection = data;
    const source = connection.source;
    const target = connection.target;
    const sourceActivityId = source.activity;
    const targetActivityId = target.activity;
    return sourceActivityId == sourceNode.id && targetActivityId == targetNode.id;
  });

  if (edge != null) {
    const connection: Connection = edge.data;
    const sourcePortOfConnection = connection.source.port;
    if (!optionsStore.enableFlexiblePorts && isNewCalculationNeededForInflexiblePort(graph, sourceNode, sourcePortOfConnection)) {
      const outgoingEdges = findOutgoingEdges(graph, sourceNode, sourcePortOfConnection);
      const targetNodes = graph.getNodes().filter(node => outgoingEdges.map(edge => connection.target.activity).includes(node.id));
      const nodeCouplesWithPositions = calculatePositionsForInflexibleNode(sourceNode, targetNodes);

      nodeCouplesWithPositions.forEach(couple => {
        updatePortsAndEdge(graph, couple.sourceNode, couple.targetNode, couple.sourceNodePosition, couple.targetNodePosition);
      });

      return;
    }

    updatePortsAndEdge(graph, sourceNode, targetNode, portPositionOfSourceNode, portPositionOfTargetNode);
  }
}

function isNewCalculationNeededForInflexiblePort(graph: Graph, sourceNode: Node<Node.Properties>, inflexiblePort: any) {
  const portName = getPortNameByPortId(inflexiblePort);
  if (portName != null && portName != "Done") {
    const outgoingEdges = findOutgoingEdges(graph, sourceNode, inflexiblePort);
    if (outgoingEdges.length > 1) {
      return true;
    }
  }
  return false;
}

function updatePortsAndEdge(graph: Graph, sourceNode: Node<Node.Properties>, targetNode: Node<Node.Properties>, newSourceNodePosition: string, newTargetNodePosition: string) {
  const edge = graph.model.getEdges().find(({data}) => {
    const conn: Connection = data;
    return conn.source.activity == sourceNode.id && conn.target.activity == targetNode.id;
  });
  const connection: Connection = edge.data;
  const source = connection.source;
  const sourcePort = source.port;
  const target = connection.target;
  const targetPort = target.port;
  const sourceNodePort = sourceNode.getPort(sourcePort) ?? sourceNode.getPorts().find(p => p.type == "out" && getPortNameByPortId(p.id) == getPortNameByPortId(sourcePort));
  const targetNodePort = targetNode.getPort(targetPort) ?? targetNode.getPorts().find(p => p.type == "in" && getPortNameByPortId(p.id) == getPortNameByPortId(targetPort));

  if (sourceNode.getPort(sourcePort) == null || targetNode.getPort(targetPort) == null || sourceNodePort?.position != newSourceNodePosition || targetNodePort?.position != newTargetNodePosition) {
    graph.removeEdge(edge);

    const newSourceNodePortId = updatePort(graph, sourceNode, sourceNodePort, newSourceNodePosition);
    const newTargetNodePortId = updatePort(graph, targetNode, targetNodePort, newTargetNodePosition);

    graph.addEdge(createEdge({
      source: {
        activity: sourceNode.id,
        port: newSourceNodePortId ?? sourceNodePort.id
      },
      target: {
        activity: targetNode.id,
        port: newTargetNodePortId ?? targetNodePort.id
      }
    }));
  }
}

function hasPortAnEdge(graph: Graph, port: PortManager.PortMetadata) {
  return graph.getEdges().some(({data}) => {
    const connection: Connection = data;
    const source = connection.source;
    const sourcePort = source.port;
    const target = connection.target;
    const targetPort = target.port;
    return sourcePort == port.id || targetPort == port.id;
  });
}

function findMatchingPortForEdge(node: Node<Node.Properties>, position: string, portType: string, portName: string) {
  return node.getPorts().find(p => p.position == position && p.type == portType && getPortNameByPortId(p.id) == portName);
}

export function getPortNameByPortId(portId: string) {
  return portId.includes('_') ? (portId.split('_')[1] == 'null' ? null : portId.split('_')[1]) : portId;
}

function findOutgoingEdges(graph: Graph, node: Node<Node.Properties>, portId: string): Edge<Edge.Properties>[] {
  return graph.model.getEdges().filter(({data}) => {
    const connection: Connection = data;
    const source = connection.source;
    const sourceActivityId = source.activity;
    const sourcePort = source.port;
    return sourceActivityId == node.id && getPortNameByPortId(sourcePort) == getPortNameByPortId(portId);
  });
}

function updatePort(graph: Graph, node: Node<Node.Properties>, nodePort: PortManager.PortMetadata, newPortPosition: string) {
  let newNodePortId = null;
  if (nodePort?.position != newPortPosition) {

    if (!hasPortAnEdge(graph, nodePort)) {
      node.removePort(nodePort);
    }

    const matchingPort = findMatchingPortForEdge(node, newPortPosition, nodePort.type, getPortNameByPortId(nodePort.id));

    if (matchingPort == null) {
      newNodePortId = createNewPort(nodePort, node, newPortPosition);
    } else {
      newNodePortId = matchingPort.id;
    }
  }
  return newNodePortId;
}

export function deriveNewPortId(portId: string) {
  return uuid() + '_' + getPortNameByPortId(portId);
}

function createNewPort(nodePort: PortManager.PortMetadata, node: Node<Node.Properties>, newPortPosition: string) {
  const newNodePortId = deriveNewPortId(nodePort.id);

  node.addPort({
    ...nodePort,
    group: newPortPosition,
    id: newNodePortId,
    position: newPortPosition,
    type: nodePort.type
  });
  return newNodePortId;
}

export function adjustPortMarkupByNode(node: Node) {
  node.getPorts().forEach(port => {
    if (port.type == 'out') {
      node.setPortProp(port.id, "attrs", {
        circle: {
          r: 5,
          magnet: true,
          stroke: '#fff',
          strokeWidth: 2,
          fill: '#3c82f6',
        },
        text: {
          fontSize: 12,
          fill: '#888',
        },
      });
    } else {
      node.setPortProp(port.id, "attrs", {
        circle: {
          r: 5,
          magnet: true,
          stroke: '#3c82f6',
          strokeWidth: 2,
          fill: '#fff',
        },
        text: {
          fontSize: 12,
          fill: '#888',
        },
      });
    }
  });
}

export function createEdge(connection: Connection): Edge.Metadata {
  return {
    shape: 'elsa-edge',
    zIndex: -1,
    data: connection,
    source: connection.source.activity,
    target: connection.target.activity,
    sourcePort: connection.source.port,
    targetPort: connection.target.port
  };
}

export function removeGuidsFromPortNames(root: Activity) {
  if (root.connections?.length > 0) {
    root.connections.forEach((connection: Connection) => {
      connection.source.port = getPortNameByPortId(connection.source.port);
      connection.target.port = getPortNameByPortId(connection.target.port);
    });
  }
  let activitiesWithConnections = root.activities?.filter(act => act.body?.connections?.length > 0);
  activitiesWithConnections.forEach(activity => {
    removeGuidsFromPortNames(activity.body);
  });
}

export function addGuidsToPortNames(root: Activity) {
  if (root.connections.length > 0) {
    root.connections.forEach((connection: Connection) => {
      connection.source.port = uuid() + '_' + connection.source.port;
      connection.target.port = uuid() + '_' + connection.target.port;
    });
  }
  let activitiesWithConnections = root.activities?.filter(act => act.body?.connections?.length > 0);
  activitiesWithConnections.forEach(activity => {
    addGuidsToPortNames(activity.body);
  });
}
