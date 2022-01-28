import 'reflect-metadata';
import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {ActivityDriverRegistry, InputControlRegistry} from "../../services";
import {Plugin} from "../../models";
import {SwitchActivityDriver} from "./switch-activity-driver";


@Service()
export class SwitchPlugin implements Plugin {
  private static readonly ActivityTypeName: string = 'ControlFlow.Switch';

  constructor() {
    const activityTypeName = SwitchPlugin.ActivityTypeName;
    const driverRegistry = Container.get(ActivityDriverRegistry);
    const inputControlRegistry = Container.get(InputControlRegistry);

    driverRegistry.add(activityTypeName, () => Container.get(SwitchActivityDriver));
    inputControlRegistry.add('switch-editor', c => <elsa-switch-editor inputContext={c}/>);
  }
}
