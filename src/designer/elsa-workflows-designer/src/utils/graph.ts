import { Edge, Graph, Node } from "@antv/x6";
import { PortManager } from "@antv/x6/lib/model/port";
import { Connection } from "../modules/flowchart/models";
import { v4 as uuid } from 'uuid';
import { Activity } from "../models";
import {ActivityNode} from "../services";

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
    }
    else {
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

function createEdge(connection: Connection): Edge.Metadata {
  return {
    shape: 'elsa-edge',
    zIndex: -1,
    data: connection,
    source: connection.source,
    target: connection.target,
    sourcePort: connection.sourcePort,
    targetPort: connection.targetPort
  };
}

function hasPortAnEdge(graph: Graph, port: PortManager.PortMetadata) {
  return graph.getEdges().some(({ data }) => data.sourcePort == port.id || data.targetPort == port.id);
}

function findMatchingPortForEdge(node: Node<Node.Properties>, position: string, portType: string, portName: string) {
  return node.getPorts().find(p => p.position == position && p.type == portType && getPortNameByPortId(p.id) == portName);
}

function getPortNameByPortId(portId: string) {
  return portId.includes('_') ? (portId.split('_')[1] == 'null' ? null : portId.split('_')[1]) : portId;
}

function findOutgoingEdgesByPortId(graph: Graph, portId: string): Edge<Edge.Properties>[] {
  return graph.model.getEdges().filter(({ data }) => data.sourcePort == portId);
}

function updatePortsAndEdges(
  graph: Graph,
  selectedNode: Node,
  newSelectedNodePosition: "left" | "right" | "top" | "bottom",
  neighbourNode: Node,
  newNeighbourNodePosition: "left" | "right" | "top" | "bottom"
) {
  updatePortsAndEdgeOfNodeCouple(graph, selectedNode, neighbourNode, newSelectedNodePosition, newNeighbourNodePosition);
  updatePortsAndEdgeOfNodeCouple(graph, neighbourNode, selectedNode, newNeighbourNodePosition, newSelectedNodePosition);

  adjustPortMarkupByNode(selectedNode);
  adjustPortMarkupByNode(neighbourNode);
}

function updatePortsAndEdgeOfNodeCouple(graph: Graph, sourceNode: Node<Node.Properties>, targetNode: Node<Node.Properties>, newSourceNodePosition: string, newTargetNodePosition: string) {
  const edge = graph.model.getEdges().find(({ data }) => data.source == sourceNode.id && data.target == targetNode.id);

  if (edge != null) {
    const sourcePortOfConnection = edge.data.sourcePort;

    if(isNewCalculationNeededForInflexiblePort(graph, sourcePortOfConnection)){
      // const outgoingEdges = findOutgoingEdgesByPortId(graph, sourcePortOfConnection);
      // const targetNodes = graph.getNodes().filter(node => outgoingEdges.map(edge => edge.data.target).includes(node.id));
      // const nodeCouplesWithPositions = calculatePositionsForInflexibleNode(sourceNode, targetNodes);
      // console.log(nodeCouplesWithPositions);

      // nodeCouplesWithPositions.forEach(couple => {
      //   update(graph, couple.sourceNode, couple.targetNode, couple.sourceNodePosition, couple.targetNodePosition);
      // });

      return;
    };

    update(graph, sourceNode, targetNode, newSourceNodePosition, newTargetNodePosition);
  }
}

function update(graph: Graph, sourceNode: Node<Node.Properties>, targetNode: Node<Node.Properties>, newSourceNodePosition: string, newTargetNodePosition: string) {
  const edge = graph.model.getEdges().find(({ data }) => data.source == sourceNode.id && data.target == targetNode.id);

  const sourceNodePort = sourceNode.getPort(edge.data.sourcePort) ?? sourceNode.getPorts().find(p => p.type == "out" && getPortNameByPortId(p.id) == getPortNameByPortId(edge.data.sourcePort));
  const targetNodePort = targetNode.getPort(edge.data.targetPort) ?? targetNode.getPorts().find(p => p.type == "in" && getPortNameByPortId(p.id) == getPortNameByPortId(edge.data.targetPort));

  if (sourceNodePort?.position != newSourceNodePosition || targetNodePort?.position != newTargetNodePosition) {
    graph.removeEdge(edge);

    const newSourceNodePortId = updatePort(graph, sourceNode, sourceNodePort, newSourceNodePosition);
    const newTargetNodePortId = updatePort(graph, targetNode, targetNodePort, newTargetNodePosition);

    graph.addEdge(createEdge({
      source: sourceNode.id,
      target: targetNode.id,
      sourcePort: newSourceNodePortId ?? sourceNodePort.id,
      targetPort: newTargetNodePortId ?? targetNodePort.id
    }));
  }
}

function calculatePositionsForInflexibleNode(sourceNode: Node<Node.Properties>, targetNodes: Node<Node.Properties>[]): 
{ sourceNode: Node<Node.Properties>, sourceNodePosition: "left" | "right" | "top" | "bottom"; targetNode: Node<Node.Properties>, targetNodePosition: "left" | "right" | "top" | "bottom"; }[] {
  const sourceNodeCenter = sourceNode.getBBox().center;
  const dxAverageForTargetNodes = targetNodes.map(node => node.getBBox().center.x).reduce((a, b) => a + b, 0);
  const dyAverageForTargetNodes = targetNodes.map(node => node.getBBox().center.y).reduce((a, b) => a + b, 0);

  const sourceNodeWithNewPosition = { node: sourceNode, position: calculatePositionsOfNodeCouple(sourceNodeCenter.x, sourceNodeCenter.y, dxAverageForTargetNodes, dyAverageForTargetNodes).selectedNodePosition };
  return targetNodes.map((targetNode) => {
    return {
      sourceNode: sourceNodeWithNewPosition.node,
      sourceNodePosition: sourceNodeWithNewPosition.position,
      targetNode: targetNode,
      targetNodePosition: calculatePositionsOfNodeCouple(sourceNode.getBBox().center.x, sourceNode.getBBox().center.y, targetNode.getBBox().center.x, targetNode.getBBox().center.y).neighbourNodePosition
    }
  });
}

function isNewCalculationNeededForInflexiblePort(graph: Graph, inflexiblePort: any) {
  const portName = getPortNameByPortId(inflexiblePort);
  if (portName != null && portName != "Done") {
    const outgoingEdges = findOutgoingEdgesByPortId(graph, inflexiblePort);
    if (outgoingEdges.length > 1) {
      return true;
    }
  }
  return false;
}

function updatePort(graph: Graph, node: Node<Node.Properties>, nodePort: PortManager.PortMetadata, newNodePosition: string) {
  let newNodePortId = null;
  if (nodePort?.position != newNodePosition) {

    if (!hasPortAnEdge(graph, nodePort)) {
      node.removePort(nodePort);
    }

    const matchingPort = findMatchingPortForEdge(node, newNodePosition, nodePort.type, getPortNameByPortId(nodePort.id));

    if (matchingPort == null) {
      newNodePortId = createNewPort(nodePort, node, newNodePosition);
    }
    else {
      newNodePortId = matchingPort.id;
    }
  }
  return newNodePortId;
}

function createNewPort(nodePort: PortManager.PortMetadata, node: Node<Node.Properties>, newNodePosition: string) {
  const newNodePortId = uuid() + '_' + getPortNameByPortId(nodePort.id);

  node.addPort({
    ...nodePort,
    group: newNodePosition,
    id: newNodePortId,
    position: newNodePosition,
    type: nodePort.type
  });
  return newNodePortId;
}

export function autoOrientPortsAndEdges(graph: Graph, selectedNode: Node) {
  const neighbors = graph.getNeighbors(selectedNode);
  const selectedCenter = selectedNode.getBBox().center;
  const nodeGroupWithPositions = neighbors.map((neighbourNode) => {
    return {
      ...calculatePositionsOfNodeCouple(selectedCenter.x, selectedCenter.y, neighbourNode.getBBox().center.x, neighbourNode.getBBox().center.y),
      selectedNode: selectedNode,
      neighbourNode: neighbourNode
    }
  });
  nodeGroupWithPositions.forEach(group => updatePortsAndEdges(graph,  group.selectedNode, group.selectedNodePosition, group.neighbourNode as Node, group.neighbourNodePosition));
}

export function rebuildGraph(graph: Graph) {
  graph.getNodes().forEach((node: Node<Node.Properties>) => {
    autoOrientPortsAndEdges(graph, node);
    adjustPortMarkupByNode(node);
  });
}

export function adjustConnectionsInRequestModel(root: Activity) {
  if (root.connections?.length > 0) {
    root.connections.forEach((connection: { sourcePort: string; targetPort: string; }) => {
      connection.sourcePort = getPortNameByPortId(connection.sourcePort);
      connection.targetPort = getPortNameByPortId(connection.targetPort);
    });
  }
  let activitiesWithConnections = root.activities?.filter(act => act.body?.connections?.length > 0);
  activitiesWithConnections.forEach(activity => {
    adjustConnectionsInRequestModel(activity.body);
  });
}

export function adjustConnectionsInResponseModel(root: Activity) {
  if (root.connections.length > 0) {
    root.connections.forEach((connection: { sourcePort: string; targetPort: string; }) => {
      connection.sourcePort = uuid() + '_' + connection.sourcePort;
      connection.targetPort = uuid() + '_' + connection.targetPort;
    });
  }
  let activitiesWithConnections = root.activities?.filter(act => act.body?.connections?.length > 0);
  activitiesWithConnections.forEach(activity => {
    adjustConnectionsInResponseModel(activity.body);
  });
}

function calculatePositionsOfNodeCouple(selectedNodeX: number, selectedNodeY: number, neighbourNodeX: number, neighbourNodeY: number): { selectedNodePosition: "left" | "right" | "top" | "bottom"; neighbourNodePosition: "left" | "right" | "top" | "bottom"; } {
  const dx = selectedNodeX - neighbourNodeX;
  const dy = selectedNodeY - neighbourNodeY;
  if (dx >= 0 && dy >= 0) {
    if (dx > dy) {
      return {selectedNodePosition: "left", neighbourNodePosition: "right"};
    } else {
      return {selectedNodePosition: "top", neighbourNodePosition: "bottom"};
    }
  } else if (dx >= 0 && dy <= 0) {
    if (dx > -dy) {
      return {selectedNodePosition: "left", neighbourNodePosition: "right"};
    } else {
      return {selectedNodePosition: "bottom", neighbourNodePosition: "top"};
    }
  } else if (dx <= 0 && dy >= 0) {
    if (-dx > dy) {
      return {selectedNodePosition: "right", neighbourNodePosition: "left"};
    } else {
      return {selectedNodePosition: "top", neighbourNodePosition: "bottom"};
    }
  } else if (dx <= 0 && dy <= 0) {
    if (dx > dy) {
      return {selectedNodePosition: "right",  neighbourNodePosition: "left"};
    } else {
      return {selectedNodePosition: "bottom", neighbourNodePosition: "left"};
    }
  }
}

