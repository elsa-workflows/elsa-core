import 'reflect-metadata';
import {Container, Service} from "typedi";
import {EventBus} from "../../services";
import {ConnectionCreatedEventArgs, FlowchartEvents} from "../../components/activities/flowchart/events";
import {Plugin, Port} from "../../models";
import {NodeHandlerRegistry} from "../../components/activities/flowchart/node-handler-registry";
import {SwitchNodeHandler} from "./switch-node-handler";
import {SwitchActivity, SwitchCase} from "./models";
import {PortManager} from "@antv/x6/lib/model/port";
import PortMetadata = PortManager.PortMetadata;

@Service()
export class SwitchPlugin implements Plugin {
  private static readonly ActivityTypeName = 'ControlFlow.Switch';

  constructor() {
    const eventBus = Container.get(EventBus);
    const nodeHandlerRegistry = Container.get(NodeHandlerRegistry);

    eventBus.on(FlowchartEvents.ConnectionCreated, this.onConnectionCreated);
    nodeHandlerRegistry.add('ControlFlow.Switch', () => Container.get(SwitchNodeHandler));
  }

  private onConnectionCreated = (e: ConnectionCreatedEventArgs) => {

    if (e.sourceActivity.nodeType !== SwitchPlugin.ActivityTypeName)
      return;

    const graph = e.graph;

    // Remove created edge.
    graph.removeEdge(e.edge);

    const switchActivity = e.sourceActivity as SwitchActivity;
    const currentCases = switchActivity.cases || [];
    const newLabel = `Case ${currentCases.length + 1}`;

    // Create Switch Case.
    const switchCase: SwitchCase = {
      label: newLabel,
      condition: {type: 'Boolean', expression: {type: 'JavaScript', value: ''}}
    }

    currentCases.push(switchCase);
    switchActivity.cases = currentCases;

    // Update source node with new port.
    const newPort: PortMetadata = {
      id: switchCase.label,
      group: 'out',
      attrs: {
        text: {
          text: switchCase.label
        }
      }
    }

    const sourceNode = e.sourceNode;
    sourceNode.addPort(newPort);

    // Create new connection between new port and target node.
    const targetNode = e.targetNode;
    const targetPort = e.connection.targetPort;

    const edge = graph.createEdge({
      source: sourceNode,
      sourcePort: switchCase.label,
      target: targetNode,
      targetPort: targetPort
    });

    graph.addEdge(edge);
  }
}
