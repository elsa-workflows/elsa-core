import 'reflect-metadata';
import {Container, Service} from "typedi";
import {ActivityNodeHandler} from "./activity-node-handler";
import {DefaultNodeHandler} from "./default-node-handler";

export type ActivityNodeHandlerFactory = () => ActivityNodeHandler;

@Service()
export class NodeHandlerRegistry {
  private handlerMap: Map<string, ActivityNodeHandlerFactory> = new Map<string, ActivityNodeHandlerFactory>();
  private defaultHandlerFactory: ActivityNodeHandlerFactory = () => Container.get(DefaultNodeHandler);

  public add(activityType: string, handlerFactory: ActivityNodeHandlerFactory) {
    this.handlerMap.set(activityType, handlerFactory);
  }

  public get(activityType: string): ActivityNodeHandlerFactory {
    return this.handlerMap.get(activityType);
  }

  public createHandler(activityType: string): ActivityNodeHandler {
    const factory = this.get(activityType) ?? this.defaultHandlerFactory;
    return factory();
  }
}
