import { Edge, Graph, Node } from "@antv/x6";
import { PortManager } from "@antv/x6/lib/model/port";
import { Connection } from "../modules/flowchart/models";
import {v4 as uuid} from 'uuid';
import { Activity } from "../models";

export function adjustPortMarkupByNode(node: Node) {
    node.getPorts().forEach(port => {
        if(port.type == 'out'){
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
    return graph.getEdges().some(({data}) => data.sourcePort == port.id || data.targetPort == port.id);
}

function findMatchingPortForEdge(node: Node<Node.Properties>, position: string, portType: string, portName: string) {
    return node.getPorts().find(p => p.position == position && p.type == portType && (portName == "In" || p.attrs?.text?.text == portName));
}

function getPortNameByPortId(portId: string) {
    return portId.split('_')[1] == 'null' ? null : portId.split('_')[1];
}

function updatePortsAndEdges(
    graph: Graph,
    selectedNode: Node,
    newSelectedNodePosition: "left" | "right" | "top" | "bottom",
    neighbourNode: Node,
    newNeighbourNodePosition: "left" | "right" | "top" | "bottom"
) 
{
    updatePortsAndEdgesOfNodeCouple(graph, selectedNode, neighbourNode, newSelectedNodePosition, newNeighbourNodePosition);
    updatePortsAndEdgesOfNodeCouple(graph, neighbourNode, selectedNode, newNeighbourNodePosition, newSelectedNodePosition);

    adjustPortMarkupByNode(selectedNode);
    adjustPortMarkupByNode(neighbourNode);
}

function updatePortsAndEdgesOfNodeCouple(graph: Graph, sourceNode: Node<Node.Properties>, targetNode: Node<Node.Properties>, newSourceNodePosition: string, newTargetNodePosition: string) 
{
    const edge = graph.model.getEdges().find(({ data }) => data.source == sourceNode.id && data.target == targetNode.id);

    if (edge != null) {
        const sourceNodePort = sourceNode.getPort(edge.data.sourcePort) ?? sourceNode.getPorts().find(p => p.type == "out" && getPortNameByPortId(p.id) == getPortNameByPortId(edge.data.sourcePort));
        const targetNodePort = targetNode.getPort(edge.data.targetPort) ?? targetNode.getPorts().find(p => p.type == "in");
        
        if(sourceNodePort.position != newSourceNodePosition || targetNodePort.position != newTargetNodePosition) {
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
}

function updatePort(graph: Graph, node: Node<Node.Properties>, nodePort: PortManager.PortMetadata, newNodePosition: string) 
{
    let newNodePortId = null;
    if (nodePort.position != newNodePosition) {

        if (!hasPortAnEdge(graph, nodePort)) {
            node.removePort(nodePort);
        }

        const matchingPort = findMatchingPortForEdge(node, newNodePosition, nodePort.type, getPortNameByPortId(nodePort.id));

        if(matchingPort == null){
            newNodePortId = createNewPort(nodePort, node, newNodePosition);
        }
        else{
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

export function autoOrientPortsAndEdges(graph: Graph, selectedNode: Node) 
{
  const selectedCenter = selectedNode.getBBox().center;
  const neighbors = graph.getNeighbors(selectedNode);
  neighbors.forEach((neighbourNode) => {
    const neighborCenter = neighbourNode.getBBox().center;
    const dx = selectedCenter.x - neighborCenter.x;
    const dy = selectedCenter.y - neighborCenter.y;
    if (dx >= 0 && dy >= 0) {
      if (dx > dy) {
        updatePortsAndEdges(graph, selectedNode, "left", neighbourNode as Node, "right");
      } else {
        updatePortsAndEdges(graph, selectedNode, "top", neighbourNode as Node, "bottom");
      }
    } else if (dx >= 0 && dy <= 0) {
      if (dx > -dy) {
        updatePortsAndEdges(graph, selectedNode, "left", neighbourNode as Node, "right");
      } else {
        updatePortsAndEdges(graph, selectedNode, "bottom", neighbourNode as Node, "top");
      }
    } else if (dx <= 0 && dy >= 0) {
      if (-dx > dy) {
        updatePortsAndEdges(graph, selectedNode, "right", neighbourNode as Node, "left");
      } else {
        updatePortsAndEdges(graph, selectedNode, "top", neighbourNode as Node, "bottom");
      }
    } else if (dx <= 0 && dy <= 0) {
      if (dx > dy) {
        updatePortsAndEdges(graph, selectedNode, "right", neighbourNode as Node, "left");
      } else {
        updatePortsAndEdges(graph, selectedNode, "bottom", neighbourNode as Node, "left");
      }
    }
  });
}

export function rebuildGraph(graph: Graph) 
{
  graph.getNodes().forEach((node: Node<Node.Properties>) => {
    autoOrientPortsAndEdges(graph, node);
  });
}

export function adjustConnectionsInRequestModel(root: Activity) {
  if(root.connections.length > 0){
    root.connections.forEach((connection: { sourcePort: string; targetPort: string; }) => {
      connection.sourcePort = getPortNameByPortId(connection.sourcePort);
      connection.targetPort = getPortNameByPortId(connection.targetPort);
    });
  }
}

export function adjustConnectionsInResponseModel(root: Activity) {
  if(root.connections.length > 0){
    root.connections.forEach((connection: { sourcePort: string; targetPort: string; }) => {
      connection.sourcePort = uuid() + '_' + connection.sourcePort;
      connection.targetPort = uuid() + '_' + connection.targetPort;
    });
  }
}
