import 'reflect-metadata';
import {Node} from "@antv/x6";
import {Container, Service} from "typedi"
import {ActivityNodeHandler, CreateUINodeContext} from "./activity-node-handler";
import {PortProviderContext, PortProviderRegistry} from "../../services";
import {PortMode} from "../../models";
import {v4 as uuid} from 'uuid';

@Service()
export class DefaultNodeHandler implements ActivityNodeHandler {
  private readonly portProviderRegistry: PortProviderRegistry;

  constructor() {
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  createDesignerNode(context: CreateUINodeContext): Node.Metadata {
    const {activityDescriptor, activity, x, y} = context;
    const provider = this.portProviderRegistry.get(activityDescriptor.type);
    const providerContext: PortProviderContext = {activityDescriptor, activity};
    const inPorts = [{name: null, displayName: null, mode: PortMode.Port}];
    let outPorts = provider.getOutboundPorts(providerContext).filter(x => x.mode == PortMode.Port);

    // In a flowchart, always add a Done port to connect the next node.
    if (outPorts.length == 0)
      outPorts = [{name: 'Done', displayName: 'Done', mode: PortMode.Port}];

    if (outPorts.length == 1)
      outPorts[0].displayName = null;

    const leftPortModels = inPorts.map((x) => ({
      id: uuid() + '_' + x.name,
      group: 'left',
      attrs: !!x.displayName ? {
        text: {
          text: x.displayName
        },
      } : null,
      type:'in',
      position:'left'
    }));

    const rightPortModels = outPorts.map((x) => ({
      id: uuid() + '_' + x.name,
      group: 'right',
      attrs: {
        text: {
          text: x.displayName
        },
      },
      type: 'out',
      position: 'right'
    }));

    const portModels = [...leftPortModels, ...rightPortModels];

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
