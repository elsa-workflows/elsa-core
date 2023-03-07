import 'reflect-metadata';
import {FunctionalComponent, h} from '@stencil/core';
import {Container, Service} from "typedi";
import {ActivityIconRegistry, InputControlRegistry, PortProviderRegistry} from "../../../services";
import {Plugin} from "../../../models";
import {FlowSwitchPortProvider} from "./flow-switch-port-provider";
import {ActivityIconSettings, getActivityIconCssClass} from "../../../components/icons/activities";

@Service()
export class FlowSwitchPlugin implements Plugin {
  static readonly ActivityTypeName: string = 'Elsa.FlowSwitch';

  constructor() {
    const activityTypeName = FlowSwitchPlugin.ActivityTypeName;
    const inputControlRegistry = Container.get(InputControlRegistry);
    const portProviderRegistry = Container.get(PortProviderRegistry);
    const iconRegistry = Container.get(ActivityIconRegistry);

    inputControlRegistry.add('flow-switch-editor', c => <elsa-flow-switch-editor inputContext={c}/>);
    portProviderRegistry.add(activityTypeName, () => Container.get(FlowSwitchPortProvider));
    iconRegistry.add(FlowSwitchPlugin.ActivityTypeName, settings => <FlowSwitchIcon size={settings?.size}/>);
  }

  async initialize(): Promise<void> {

  }
}

const FlowSwitchIcon: FunctionalComponent<ActivityIconSettings> = (settings) => (
  <svg class={getActivityIconCssClass(settings)} width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <polyline points="15 4 19 4 19 8"/>
    <line x1="14.75" y1="9.25" x2="19" y2="4"/>
    <line x1="5" y1="19" x2="9" y2="15"/>
    <polyline points="15 19 19 19 19 15"/>
    <line x1="5" y1="5" x2="19" y2="19"/>
  </svg>
);
