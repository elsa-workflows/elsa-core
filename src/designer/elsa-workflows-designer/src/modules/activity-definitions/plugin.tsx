import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityDescriptor, Plugin} from "../../models";
import newButtonItemStore from "../../data/new-button-item-store";
import {MenuItem} from "../../components/shared/context-menu/models";
import {Flowchart} from "../flowchart/models";
import {generateUniqueActivityName} from '../../utils/generate-activity-name';
import descriptorsStore from "../../data/descriptors-store";
import studioComponentStore from "../../data/studio-component-store";
import toolbarButtonMenuItemStore from "../../data/toolbar-button-menu-item-store";
import {ToolbarMenuItem} from "../../components/toolbar/workflow-toolbar-menu/models";
import {ActivityDescriptorManager, EventBus} from "../../services";
import toolbarComponentStore from "../../data/toolbar-component-store";
import {NotificationEventTypes} from "../notifications/event-types";
import {PublishClickedArgs} from "./components/publish-button";
import {ActivityDefinitionManager} from "./services/manager";
import {ActivityDefinition, ActivityDefinitionSummary, ActivityDefinitionUpdatedArgs} from "./models";
import {ActivityDefinitionsApi} from "./services/api";

const FlowchartTypeName = 'Elsa.Flowchart';

@Service()
export class ActivityDefinitionsPlugin implements Plugin {
  private readonly eventBus: EventBus;
  private readonly activityDefinitionManager: ActivityDefinitionManager;
  private readonly activityDescriptorManager: ActivityDescriptorManager;
  private readonly api: ActivityDefinitionsApi;
  private activityDefinitionEditorElement: HTMLElsaActivityDefinitionEditorElement;
  private activityDefinitionBrowserElement: HTMLElsaActivityDefinitionBrowserElement;



  constructor() {
    this.eventBus = Container.get(EventBus);
    this.activityDefinitionManager = Container.get(ActivityDefinitionManager);
    this.api = Container.get(ActivityDefinitionsApi);
    this.activityDescriptorManager = Container.get(ActivityDescriptorManager);

    const newActivityDefinitionItem: MenuItem = {
      text: 'Activity Definition',
      clickHandler: this.onNewActivityDefinitionClick
    }

    const activityDefinitionBrowserItem: ToolbarMenuItem = {
      text: 'Activity Definitions',
      onClick: this.onBrowseActivityDefinitions,
      order: 5
    };

    newButtonItemStore.items = [...newButtonItemStore.items, newActivityDefinitionItem];
    toolbarButtonMenuItemStore.items = [...toolbarButtonMenuItemStore.items, activityDefinitionBrowserItem];

    studioComponentStore.modalComponents = [
      ...studioComponentStore.modalComponents,
      () => <elsa-activity-definition-browser ref={el => this.activityDefinitionBrowserElement = el} onActivityDefinitionSelected={this.onActivityDefinitionSelected} onNewActivityDefinitionSelected={this.onNewActivityDefinitionClick}/>];
  }

  async initialize(): Promise<void> {
  }

  newActivityDefinition = async () => {

    const flowchartDescriptor = this.getFlowchartDescriptor();
    const newName = await this.generateUniqueActivityName(flowchartDescriptor);

    const flowchart = {
      typeName: flowchartDescriptor.activityType,
      activities: [],
      connections: [],
      id: newName,
      metadata: {},
      applicationProperties: {},
      variables: []
    } as Flowchart;

    const activityDefinition: ActivityDefinition = {
      root: flowchart,
      id: '',
      definitionId: '',
      typeName: 'Activity1',
      category: 'Custom',
      displayName: 'Activity 1',
      version: 1,
      isLatest: true,
      isPublished: false
    };

    this.showActivityDefinitionEditor(activityDefinition);
  };

  private getFlowchartDescriptor = () => this.getActivityDescriptor(FlowchartTypeName);
  private getActivityDescriptor = (typeName: string): ActivityDescriptor => descriptorsStore.activityDescriptors.find(x => x.activityType == typeName)
  private generateUniqueActivityName = async (activityDescriptor: ActivityDescriptor): Promise<string> => await generateUniqueActivityName([], activityDescriptor);

  private saveActivityDefinition = async (definition: ActivityDefinition, publish: boolean): Promise<ActivityDefinition> => {
    const updatedDefinition = await this.activityDefinitionManager.save(definition, publish);
    await this.activityDefinitionEditorElement.updateActivityDefinition(updatedDefinition);
    return updatedDefinition;
  }

  private showActivityDefinitionEditor = (activityDefinition: ActivityDefinition) => {
    toolbarComponentStore.components = [() => <elsa-activity-publish-button onPublishClicked={this.onPublishClicked}/>];
    studioComponentStore.activeComponentFactory = () => <elsa-activity-definition-editor activityDefinition={activityDefinition} onActivityDefinitionUpdated={this.onActivityDefinitionUpdated}
                                                                                         ref={el => this.activityDefinitionEditorElement = el}/>;
  };

  private refreshActivityDescriptors = () => {

  }

  private onNewActivityDefinitionClick = async () => await this.newActivityDefinition();

  private onActivityDefinitionUpdated = async (e: CustomEvent<ActivityDefinitionUpdatedArgs>) => {
    const activityDefinition = e.detail.activityDefinition;
    await this.saveActivityDefinition(activityDefinition, false);
  }

  private onBrowseActivityDefinitions = async () => {
    await this.activityDefinitionBrowserElement.show();
  }

  private onActivityDefinitionSelected = async (e: CustomEvent<ActivityDefinitionSummary>) => {
    const definitionId = e.detail.definitionId;
    const workflowDefinition = await this.api.get({definitionId});
    this.showActivityDefinitionEditor(workflowDefinition);
  }

  private onPublishClicked = async (e: CustomEvent<PublishClickedArgs>) => {
    e.detail.begin();
    const activityDefinition = await this.activityDefinitionEditorElement.getActivityDefinition();
    await this.eventBus.emit(NotificationEventTypes.Add, this, {id: activityDefinition.definitionId, message: `Starting publishing ${activityDefinition.typeName}`});
    await this.saveActivityDefinition(activityDefinition, true);
    await this.eventBus.emit(NotificationEventTypes.Update, this, {id: activityDefinition.definitionId, message: `${activityDefinition.typeName} publish finished`});
    e.detail.complete();

    // Reload activity descriptors.
    await this.activityDescriptorManager.refresh();
  }
}
