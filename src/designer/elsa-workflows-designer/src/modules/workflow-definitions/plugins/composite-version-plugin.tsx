import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {Plugin, TabDefinition} from "../../../models";
import {EventBus} from "../../../services";
import {RenderActivityPropsContext} from "../components/models";
import {ActivityPropertyPanelEvents} from "../models/ui";

@Service()
export class CompositeActivityVersionPlugin implements Plugin {
  private readonly eventBus: EventBus

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.eventBus.on(ActivityPropertyPanelEvents.Rendering, this.onPropertyPanelRendering);
  }

  async initialize(): Promise<void> {

  }

  private onPropertyPanelRendering = (context: RenderActivityPropsContext) => {
    if (context.activityDescriptor.customProperties['RootType'] != 'WorkflowDefinitionActivity')
      return;

    const versionTab: TabDefinition = {
      order: 20,
      displayText: 'Version',
      content: () => <elsa-workflow-definition-activity-version-settings/>
    };

    context.tabs.push(versionTab);
  };
}
