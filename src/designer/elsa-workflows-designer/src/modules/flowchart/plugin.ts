import {Plugin} from "../../models";
import {Container, Service} from "typedi";
import {PortProviderRegistry} from "../../services";
import {FlowchartPortProvider} from "./flowchart-port-provider";

@Service()
export class FlowchartPlugin implements Plugin {
  async initialize(): Promise<void> {
    const registry = Container.get(PortProviderRegistry);
    registry.add('Elsa.Flowchart', () => Container.get(FlowchartPortProvider));
  }

}
