import 'reflect-metadata';
import {Container, Service} from "typedi";
import {PortProvider} from "./port-provider";
import {DefaultPortProvider} from "./default-port-provider";

export type PortProviderFactory = () => PortProvider;

@Service()
export class PortProviderRegistry {
  private map: Map<string, PortProviderFactory> = new Map<string, PortProviderFactory>();
  private defaultProviderFactory: PortProviderFactory = () => Container.get(DefaultPortProvider);

  public add(activityType: string, defaultProviderFactory: PortProviderFactory) {
    this.map.set(activityType, defaultProviderFactory);
  }

  public get(activityType: string): PortProvider {
    const factory = this.map.get(activityType) ?? this.defaultProviderFactory;
    return factory();
  }
}
