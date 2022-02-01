import 'reflect-metadata';
import {Container, Service} from "typedi";
import {TransposeHandler} from "./transpose-handler";
import {DefaultTransposeHandler} from "./default-transpose-handler";

export type TransposeHandlerFactory = () => TransposeHandler;

@Service()
export class TransposeHandlerRegistry {
  private map: Map<string, TransposeHandlerFactory> = new Map<string, TransposeHandlerFactory>();
  private defaultHandlerFactory: TransposeHandlerFactory = () => Container.get(DefaultTransposeHandler);

  public add(activityType: string, handlerFactory: TransposeHandlerFactory) {
    this.map.set(activityType, handlerFactory);
  }

  public get(activityType: string): TransposeHandler {
    const factory = this.map.get(activityType) ?? this.defaultHandlerFactory;
    return factory();
  }
}
