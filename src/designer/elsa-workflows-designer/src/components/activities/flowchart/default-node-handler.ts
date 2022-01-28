import 'reflect-metadata';
import {Node} from "@antv/x6";
import {Container, Service} from "typedi"
import {ActivityNodeHandler, CreateUINodeContext} from "./activity-node-handler";
import {PortProviderRegistry} from "./port-provider-registry";
import {PortProviderContext} from "./port-provider";

@Service()
export class DefaultNodeHandler implements ActivityNodeHandler {
  private readonly portProviderRegistry: PortProviderRegistry;

  constructor() {
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  createDesignerNode(context: CreateUINodeContext): Node.Metadata {
    const {activityDescriptor, activity, x, y} = context;
    const provider = this.portProviderRegistry.get(activityDescriptor.nodeType);
    const providerContext: PortProviderContext = { activityDescriptor, activity };
    let inPorts = provider.getInboundPorts(providerContext);
    let outPorts = provider.getOutboundPorts(providerContext);

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
