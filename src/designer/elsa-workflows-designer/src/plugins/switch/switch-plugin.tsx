import 'reflect-metadata';
import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {ActivityDriverRegistry, InputControlRegistry} from "../../services";
import {Plugin} from "../../models";
import {SwitchActivityDriver} from "./switch-activity-driver";
import {PortProviderRegistry} from "../../components/activities/flowchart/port-provider-registry";
import {SwitchPortProvider} from "./switch-port-provider";
import {SwitchPortUpdater} from "./switch-port-updater";
import {TransposeHandlerRegistry} from "../../components/activities/flowchart/transpose-handler-registry";
import {SwitchTransposeHandler} from "./switch-transpose-handler";


@Service()
export class SwitchPlugin implements Plugin {
  public static readonly ActivityTypeName: string = 'ControlFlow.Switch';

  constructor() {
    const activityTypeName = SwitchPlugin.ActivityTypeName;
    const driverRegistry = Container.get(ActivityDriverRegistry);
    const inputControlRegistry = Container.get(InputControlRegistry);
    const portProviderRegistry = Container.get(PortProviderRegistry);
    const switchPortUpdater = Container.get(SwitchPortUpdater);
    const transposeHandlerRegistry = Container.get(TransposeHandlerRegistry);

    driverRegistry.add(activityTypeName, () => Container.get(SwitchActivityDriver));
    inputControlRegistry.add('switch-editor', c => <elsa-switch-editor inputContext={c}/>);
    portProviderRegistry.add(activityTypeName, () => Container.get(SwitchPortProvider));
    transposeHandlerRegistry.add(activityTypeName, () => Container.get(SwitchTransposeHandler));
  }
}
