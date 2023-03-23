import {h} from '@stencil/core';
import {Container, Service} from "typedi";
import {Plugin, TabDefinition} from "../../../models";
import {EventBus} from "../../../services";
import {ActivityPropertyPanelEvents} from "../models/ui";
import {RenderActivityPropsContext, WorkflowDefinitionActivity} from "../components/models";
import {WorkflowDefinitionsApi} from "../services/api";

@Service()
export class CompositeActivityVersionPlugin implements Plugin {
  private readonly eventBus: EventBus
  private readonly workflowDefinitionsApi: WorkflowDefinitionsApi;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.workflowDefinitionsApi = Container.get(WorkflowDefinitionsApi);
    this.eventBus.on(ActivityPropertyPanelEvents.Displaying, this.onPropertyPanelRendering);
  }

  async initialize(): Promise<void> {

  }

  private onPropertyPanelRendering = (context: RenderActivityPropsContext) => {
    if (context.activityDescriptor.customProperties['RootType'] != 'WorkflowDefinitionActivity')
      return;

    const versionTab: TabDefinition = {
      order: 20,
      displayText: 'Version',
      content: () => <elsa-workflow-definition-activity-version-settings renderContext={context}/>
    };

    context.tabs.push(versionTab);
  }
}
