import 'reflect-metadata';
import {Node} from "@antv/x6";
import {Container, Service} from "typedi"
import {ActivityNodeHandler, UINodeContext, UIPortContext} from "./activity-node-handler";
import {PortProviderContext, PortProviderRegistry} from "../../services";
import {PortType} from "../../models";
import {v4 as uuid} from 'uuid';

@Service()
export class DefaultNodeHandler implements ActivityNodeHandler {
  private readonly portProviderRegistry: PortProviderRegistry;

  constructor() {
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  createDesignerNode(context: UINodeContext): Node.Metadata {
    const {activityDescriptor, activity, x, y} = context;
    const portModels = this.createPorts(context);

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

  createPorts(context: UIPortContext): Array<any> {
    const {activityDescriptor, activity} = context;
    const provider = this.portProviderRegistry.get(activityDescriptor.typeName);
    const providerContext: PortProviderContext = {activityDescriptor, activity};
    const inPorts = [{name: 'In', displayName: null, mode: PortType.Flow}];
    let outPorts = provider.getOutboundPorts(providerContext).filter(x => x.type == PortType.Flow);

    // In a flowchart, always add a Done port to connect the next node.
    if (outPorts.length == 0)
      outPorts = [{name: 'Done', displayName: 'Done', type: PortType.Flow}];

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

    return portModels;
  }
}
