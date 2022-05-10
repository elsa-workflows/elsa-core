import 'reflect-metadata';
import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {ActivityIconRegistry} from "../../services";
import {Plugin} from "../../models";
import {TransposeHandlerRegistry} from "../../components/activities/flowchart/transpose-handler-registry";
import {SequenceTransposeHandler} from "./sequence-transpose-handler";

@Service()
export class SequencePlugin implements Plugin {
  public static readonly ActivityTypeName: string = 'Elsa.Sequence';

  constructor() {
    const activityTypeName = SequencePlugin.ActivityTypeName;
    const transposeHandlerRegistry = Container.get(TransposeHandlerRegistry);
    const iconRegistry = Container.get(ActivityIconRegistry);

    transposeHandlerRegistry.add(activityTypeName, () => Container.get(SequenceTransposeHandler));
    iconRegistry.add(SequencePlugin.ActivityTypeName, Icon);
  }
}

const Icon: () => string = () =>
  `<svg class="h-6 w-6 text-white" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
    <path stroke="none" d="M0 0h24v24H0z"/>
    <line x1="4" y1="6" x2="20" y2="6"/>
    <line x1="4" y1="18" x2="9" y2="18"/>
    <path d="M4 12h13a3 3 0 0 1 0 6h-4l2 -2m0 4l-2 -2"/>
  </svg>`;
