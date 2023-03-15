import 'reflect-metadata';
import {FunctionalComponent, h} from '@stencil/core';
import {Container, Service} from "typedi";
import {ActivityIconRegistry} from "../../services";
import {Plugin} from "../../models";
import {ActivityIconSettings, getActivityIconCssClass} from "../../components/icons/activities";

@Service()
export class SequencePlugin implements Plugin {
  public static readonly ActivityTypeName: string = 'Elsa.Sequence';

  constructor() {
    const iconRegistry = Container.get(ActivityIconRegistry);

    iconRegistry.add(SequencePlugin.ActivityTypeName, settings => <SequenceIcon size={settings?.size}/>);
  }

  async initialize(): Promise<void> {

  }
}

const SequenceIcon: FunctionalComponent<ActivityIconSettings> = (settings) => (
  <svg class={getActivityIconCssClass(settings)} width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <path d="M9 4.55a8 8 0 0 1 6 14.9m0 -4.45v5h5"/>
    <path d="M11 19.95a8 8 0 0 1 -5.3 -12.8" stroke-dasharray=".001 4.13"/>
  </svg>
);
