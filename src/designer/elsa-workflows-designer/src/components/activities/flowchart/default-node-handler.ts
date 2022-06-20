import 'reflect-metadata';
import {Node} from "@antv/x6";
import {Container, Service} from "typedi"
import {ActivityNodeHandler, CreateUINodeContext} from "./activity-node-handler";
import {PortProviderContext, PortProviderRegistry} from "../../../services";

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
    let inPorts = provider.getInboundPorts(providerContext);
    let outPorts = provider.getOutboundPorts(providerContext);

    //if (inPorts.length == 0)
    inPorts = [{name: 'In', displayName: 'In'}];

    if (inPorts.length == 1)
      inPorts[0].displayName = null;

    // In a flowchart, always add a Done port to connect the next node.
    //outPorts = [...outPorts, {name: 'Done', displayName: 'Done'}];
    outPorts = [{name: 'Done', displayName: 'Done'}];

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
