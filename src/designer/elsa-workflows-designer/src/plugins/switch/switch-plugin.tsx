import 'reflect-metadata';
import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {InputControlRegistry} from "../../services";
import {Plugin} from "../../models";
import {NodeHandlerRegistry} from "../../components/activities/flowchart/node-handler-registry";
import {SwitchNodeHandler} from "./switch-node-handler";


@Service()
export class SwitchPlugin implements Plugin {

  constructor() {
    const inputControlRegistry = Container.get(InputControlRegistry);
    const nodeHandlerRegistry = Container.get(NodeHandlerRegistry);

    inputControlRegistry.add('switch-editor', c => <elsa-switch-editor inputContext={c} />)
    nodeHandlerRegistry.add('ControlFlow.Switch', () => Container.get(SwitchNodeHandler));
  }
}
