import 'reflect-metadata';
import {Node} from "@antv/x6";
import {Container, Service} from "typedi"
import {ActivityNodeHandler, CreateUINodeContext} from "./activity-node-handler";
import {PortProviderContext, PortProviderRegistry} from "../../../services";
import {PortMode} from "../../../models";

@Service()
export class DefaultNodeHandler implements ActivityNodeHandler {
  private readonly portProviderRegistry: PortProviderRegistry;

  constructor() {
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  createDesignerNode(context: CreateUINodeContext): Node.Metadata {
    const {activityDescriptor, activity, x, y} = context;
    const provider = this.portProviderRegistry.get(activityDescriptor.activityType);
    const providerContext: PortProviderContext = {activityDescriptor, activity};
    const inPorts = [{name: 'In', displayName: null, mode: PortMode.Port}];
    let outPorts = provider.getOutboundPorts(providerContext).filter(x => x.mode == PortMode.Port);

    // In a flowchart, always add a Done port to connect the next node.
    if (outPorts.length == 0)
      outPorts = [{name: 'Done', displayName: 'Done', mode: PortMode.Port}];

    if (outPorts.length == 1)
      outPorts[0].displayName = null;

    const inPortModels = inPorts.map(x => ({
      id: x.name,
      group: 'in',
      attrs: !!x.displayName ? {
        text: {
          text: x.displayName
        }
      } : null
    }));

    const outPortModels = outPorts.map(x => ({
      id: x.name,
      group: 'out',
      attrs: {
        text: {
          text: x.displayName
        }
      }
    }));

    const portModels = [...inPortModels, ...outPortModels];

    return {
      id: activity.id,
      shape: 'activity',
      activity: activity,
      activityDescriptor: activityDescriptor,
      x: x,
      y: y,
      data: activity,
      ports: portModels
    } as Node.Metadata;
  }
}
