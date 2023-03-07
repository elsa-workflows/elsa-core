import 'reflect-metadata';
import {Graph, Node} from '@antv/x6';
import {Activity, ActivityDescriptor, Port} from '../../models';
import {Container, Service} from "typedi";
import {NodeHandlerRegistry} from "./node-handler-registry";

@Service()
export class NodeFactory {
  private readonly handlerRegistry: NodeHandlerRegistry;

  constructor() {
    this.handlerRegistry = Container.get(NodeHandlerRegistry);
  }

  public createNode(activityDescriptor: ActivityDescriptor, activity: Activity, x: number, y: number): Node.Metadata {
    const handler = this.handlerRegistry.createHandler(activityDescriptor.typeName);
    return handler.createDesignerNode({activityDescriptor, activity, x, y});
  }

  public createPorts(activityDescriptor: ActivityDescriptor, activity: Activity): Array<any> {
    const handler = this.handlerRegistry.createHandler(activityDescriptor.typeName);
    return handler.createPorts({activityDescriptor, activity});
  }
}
